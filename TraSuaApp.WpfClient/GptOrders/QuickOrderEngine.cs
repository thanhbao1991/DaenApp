using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.WpfClient.Services
{
    public class QuickOrderEngine
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private const string DefaultModel = "gpt-4.1-mini";

        public QuickOrderEngine(string apiKey)
        {
            _apiKey = apiKey;
            _http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/") };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }


        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(
            string rawInput,
            string model = DefaultModel,
            string? chatContext = null,
            string? customerNameHint = null)
        {
            var result = new List<QuickOrderDto>();
            var baoCao = new List<string>();

            // ==== 1) Chuẩn bị menu & shortlist
            var menu = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
            string menuText = BuildMenuForGpt(menu);

            // ==== 2) Lấy và chuẩn hoá LINES
            var normLines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput, customerNameHint).ToList();
            string linesText = BuildNumberedLines(normLines);

            var systemPrompt = @"
Bạn là hệ thống POS đọc hội thoại order nước.
Đầu vào gồm:
- MENU (Id<TAB>ten_khong_dau)
- LINES (các dòng KH nhập, đã đánh số: 1), 2), 3)...

YÊU CẦU:
1. Một Line có thể chứa nhiều món hoặc không có món.
2. Chỉ tạo item KH thật sự đặt (bỏ qua lời xã giao, chào hỏi, giờ giấc...).
3. Không tạo sản phẩm mới; chỉ chọn Id có trong MENU.
4. Nếu thấy giá (vd: '1 ly 25k', '35k nước dừa') → ghi vào Gia (đồng, không có 'k'); nếu không thấy thì Gia = null.
5. Không đưa các từ/cụm đã nằm trong tên sản phẩm vào NoteText.
6. Chỉ giữ các ghi chú thật sự: 'ít ngọt', 'ít đá', 'nóng', 'mang đi', 'bớt đường', 'uống tại quán', v.v.
7. Nếu có dòng riêng chỉ gồm ghi chú (không có món) như 'xin thêm ly', 'ít đường thôi', 'mang đi nha',
   hoặc dòng bắt đầu bằng các từ: 'xin', 'cho', 'bớt', 'ít', 'thêm', 'đừng', 'nhiều', 'mang', 'uống', 'trà',
   thì coi đó là **NoteText** cho nhóm món gần nhất ở trên.
   → Nếu dòng đó nằm ngay sau dòng có món, gán NoteText cho món cuối cùng trong nhóm đó.
   → Nếu nhiều dòng ghi chú liên tiếp, hãy gộp chúng lại thành một NoteText duy nhất.
8. Nếu không có ghi chú, để NoteText = rỗng.
9. Nếu khách nhắc đến món không có trong MENU như 'trà đá', 'ống hút', 'ly đá' → 
   thêm vào NoteText của món gần nhất.

Trả về duy nhất một MẢNG JSON hợp lệ:
[
  {
    ""Id"": ""GUID sản phẩm"",
    ""SoLuong"": int >= 1,
    ""NoteText"": ""ghi chú..."",
    ""Line"": số thứ tự dòng có món,
    ""Gia"": số tiền hoặc null
  }
]
".Trim();



            var userPromptSb = new StringBuilder();
            userPromptSb.AppendLine(menuText);
            userPromptSb.AppendLine();
            userPromptSb.AppendLine("LINES");
            userPromptSb.AppendLine(linesText);
            string userPrompt = userPromptSb.ToString();

            // ==== 4) Audit input

            // ==== 5) Gọi GPT ====
            var sw = Stopwatch.StartNew();
            string jsonOut = await CallChatCompletionsAsync(model, systemPrompt, userPrompt);
            sw.Stop();
            var elapsedMs = sw.ElapsedMilliseconds;

            baoCao.Add(".");
            baoCao.Add(".");
            baoCao.Add(".");
            baoCao.Add($"Model: {model} | GPT time: {elapsedMs} ms");
            baoCao.Add("-----customerNameHint-----"); baoCao.Add(customerNameHint);
            baoCao.Add("-----rawInput-----"); baoCao.Add(rawInput);
            baoCao.Add("-----linesText------"); baoCao.Add(linesText);


            try
            {
                using var doc = JsonDocument.Parse(jsonOut);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var dto = new QuickOrderDto();

                        if (el.TryGetProperty("Id", out var idP))
                        {
                            if (Guid.TryParse(idP.GetString(), out Guid gid))
                                dto.Id = gid;
                        }

                        if (el.TryGetProperty("SoLuong", out var slP) && slP.TryGetInt32(out var sl))
                            dto.SoLuong = Math.Max(1, sl);

                        if (el.TryGetProperty("NoteText", out var nP))
                            dto.NoteText = nP.GetString() ?? "";

                        if (el.TryGetProperty("Line", out var lP) && lP.TryGetInt32(out var ln))
                            dto.Line = ln;

                        // 🟟 NEW: đọc thêm giá nếu có
                        if (el.TryGetProperty("Gia", out var gP))
                        {
                            if (gP.ValueKind == JsonValueKind.Number && gP.TryGetDecimal(out var g))
                                dto.Gia = g;
                            else if (decimal.TryParse(gP.GetString(), out var gStr))
                                dto.Gia = gStr;
                        }

                        if (dto.Id != Guid.Empty)
                            result.Add(dto);
                    }
                }
            }
            catch
            {
                // nếu GPT trả sai format thì để trống
            }

            // ==== 6) Ghi log GPT trả về & kết quả parse
            //baoCao.Add("-----gptOutput-----");
            //baoCao.Add(JsonSerializer.Serialize(JsonDocument.Parse(jsonOut), new JsonSerializerOptions { WriteIndented = true }));

            if (result.Any())
            {
                baoCao.Add("-----parsedItems-----");

                // 🟟 Map Id → Tên món từ menu hiện có

                foreach (var r in result)
                {
                    var spMap = AppProviders.SanPhams.Items.SingleOrDefault(x => x.Id == r.Id);

                    if (spMap != null)
                        baoCao.Add($"{r.Line}): {r.SoLuong} {spMap.Ten} - {r.Gia?.ToString()} - {r.NoteText}");
                }
            }

            await DiscordService.SendAsync(Shared.Enums.DiscordEventType.Admin, string.Join("\n", baoCao), customerNameHint);
            return result;
        }

        public async Task<ObservableCollection<ChiTietHoaDonDto>> MapToChiTietAsync(
            string rawInput,
            IEnumerable<QuickOrderDto> preds,
            string? customerNameHint = null)
        {
            var chiTiets = new ObservableCollection<ChiTietHoaDonDto>();
            if (preds == null) return chiTiets;

            var spMap = AppProviders.SanPhams.Items.ToDictionary(x => x.Id, x => x);
            var normLines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput, customerNameHint).ToList();
            int i = 1;

            foreach (var p in preds)
            {
                if (p.Id == Guid.Empty || !spMap.TryGetValue(p.Id, out var sp)) continue;

                string? lineText = null;
                if (p.Line.HasValue && p.Line.Value >= 1 && p.Line.Value <= normLines.Count)
                    lineText = normLines[p.Line.Value - 1];

                SanPhamBienTheDto? bt = null;

                // 🟟 Ưu tiên chọn theo note như cũ
                bt = PickVariantByNote(sp, $"{p.NoteText} {lineText}");

                // 🟟 Nếu GPT có giá và chưa chọn được biến thể
                if (bt == null && p.Gia.HasValue)
                {
                    var giaGpt = p.Gia.Value;
                    bt = sp.BienThe
                        ?.OrderBy(v => Math.Abs(v.GiaBan - giaGpt))  // chọn giá gần nhất GPT dự đoán
                        .FirstOrDefault();
                }

                // 🟟 Nếu vẫn chưa có → fallback mặc định
                bt ??= sp.BienThe.FirstOrDefault(x => x.MacDinh)
                    ?? sp.BienThe.OrderBy(v => v.GiaBan).FirstOrDefault();
                if (bt == null) continue;
                chiTiets.Add(new ChiTietHoaDonDto
                {
                    Stt = i++,
                    Id = Guid.NewGuid(),
                    SanPhamId = sp.Id,
                    SanPhamIdBienThe = bt.Id,
                    TenSanPham = sp.Ten,
                    DonGia = bt.GiaBan,
                    TenBienThe = bt.TenBienThe ?? "Size Chuẩn",
                    SoLuong = Math.Max(1, p.SoLuong),
                    NoteText = p.NoteText ?? ""
                });

                var lines = new List<string>();
                lines.Add("===== GPT ORDER FINALIZED =====");

                foreach (var ct in chiTiets)
                {
                    lines.Add($"{ct.Stt}. {ct.TenSanPham} - {ct.TenBienThe} x{ct.SoLuong} - {ct.DonGia:N0}đ {ct.NoteText}");
                }

                await DiscordService.SendAsync(
                    Shared.Enums.DiscordEventType.Admin,
                    string.Join("\n", lines)

                );


            }
            return chiTiets;
        }

        private async Task<string> CallChatCompletionsAsync(string model, string systemPrompt, string userPrompt)
        {
            var body = new
            {
                model,
                temperature = 0,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

            if (msg.TryGetProperty("content", out var contentEl))
            {
                if (contentEl.ValueKind == JsonValueKind.String) return contentEl.GetString() ?? "[]";
                if (contentEl.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var part in contentEl.EnumerateArray())
                        if (part.TryGetProperty("type", out var t) && t.GetString() == "text" &&
                            part.TryGetProperty("text", out var txt)) sb.Append(txt.GetString());
                    var s = sb.ToString().Trim();
                    if (s.StartsWith("[") || s.StartsWith("{")) return s;
                }
            }
            return ExtractJsonArray(json) ?? "[]";
        }

        private static string? ExtractJsonArray(string s)
        {
            int start = s.IndexOf('[');
            if (start < 0) return null;
            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                if (s[i] == '[') depth++;
                else if (s[i] == ']')
                {
                    depth--;
                    if (depth == 0) return s.Substring(start, i - start + 1);
                }
            }
            return null;
        }

        private static string BuildMenuForGpt(IEnumerable<SanPhamDto> menu)
            => "MENU (Id<TAB>ten_khong_dau)\n" +
               string.Join("\n", menu.Where(m => !m.NgungBan)
                                     .Select(m => $"{m.Id}\t{OrderTextCleaner.NormalizeNoDiacritics(m.Ten)}"));

        private static string BuildNumberedLines(List<string> normLines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < normLines.Count; i++) sb.AppendLine($"{i + 1}) {normLines[i]}");
            return sb.ToString().TrimEnd();
        }

        private static SanPhamBienTheDto? PickVariantByNote(SanPhamDto sp, string? noteOrLine)
        {
            if (sp.BienThe == null || sp.BienThe.Count == 0) return null;

            var text = OrderTextCleaner.NormalizeNoDiacritics(noteOrLine ?? "");
            bool wantL = Regex.IsMatch(text, @"\b(size\s*l|sz\s*l|\bl\b|lon|to|bu|large|big)\b");
            bool forceStandard = Regex.IsMatch(text,
                @"\b(size\s*m|sz\s*m|\bm\b|chuan|thuong|vua|medium|regular|normal|size\s*chuan|"
              + @"size\s*s|sz\s*s|\bs\b|nho|small)\b");

            static string Norm(string s) => OrderTextCleaner.NormalizeNoDiacritics(s ?? "");
            static bool EqualsStd(string s) => Norm(s) == "size chuan" || Norm(s) == "chuan";
            static bool ContainsStd(string s) => Norm(s).Contains("size chuan") || Norm(s).Contains("chuan");
            static bool ContainsL(string s) => Norm(s).Contains("size l") || Norm(s).Contains("l");

            if (wantL && !forceStandard)
            {
                var byNameL = sp.BienThe.FirstOrDefault(v => !EqualsStd(v.TenBienThe ?? "") && ContainsL(v.TenBienThe ?? ""));
                if (byNameL != null) return byNameL;
                return sp.BienThe.OrderByDescending(v => v.GiaBan).FirstOrDefault()
                       ?? sp.BienThe.FirstOrDefault(x => x.MacDinh)
                       ?? sp.BienThe.FirstOrDefault();
            }

            var exactStd = sp.BienThe.FirstOrDefault(v => EqualsStd(v.TenBienThe ?? ""));
            if (exactStd != null) return exactStd;

            var containStd = sp.BienThe.FirstOrDefault(v => ContainsStd(v.TenBienThe ?? ""));
            if (containStd != null) return containStd;

            return sp.BienThe.FirstOrDefault(x => x.MacDinh)
                ?? sp.BienThe.OrderBy(v => v.GiaBan).FirstOrDefault()
                ?? sp.BienThe.FirstOrDefault();
        }
    }
}