using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;              // dùng OrderTextCleaner
using TraSuaApp.Shared.Services;
using TraSuaApp.WpfClient.AiOrdering;

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

        /// <summary>
        /// Chỉ gửi LINES (đã normalize & đánh số) như cơ chế cũ. Bỏ qua chatContext.
        /// </summary>
        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(
            string rawInput,
            string? HisList = null,
            string? LearnList = null,
            Guid? khachHangId = null,
            int shortlistTopK = 12,
            string model = DefaultModel,
            string? chatContext = null,
            string? customerNameHint = null) // ✅ thêm
        {
            var result = new List<QuickOrderDto>();
            var baoCao = new List<string>();

            // ==== 1) Chuẩn bị menu & shortlist
            var menu = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();

            // SHORTLIST học máy (theo khách + global) + lịch sử server
            string learnedShort = QuickGptLearningStore.Instance.BuildShortlistForPrompt(
                customerId: khachHangId,
                currentMenu: menu,
                serverTopForCustomer: null,
                topK: 12
            );

            var shortFinal = JoinShortlists(HisList, LearnList);
            string menuText = BuildMenuForGpt(menu);

            // ==== 2) Luôn dùng LINES từ rawInput (không dùng CHAT)
            var normLines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput, customerNameHint).ToList();
            string linesText = BuildNumberedLines(normLines);

            // ==== 3) Prompt chỉ có SHORTLIST + MENU + LINES
            var systemPrompt = @"
Bạn là hệ thống POS. Chỉ trả về DUY NHẤT một mảng JSON hợp lệ (không kèm giải thích).
Đầu vào gồm:
- SHORTLIST (Id<TAB>Tên)
- MENU (Id<TAB>ten_khong_dau)
- LINES (các dòng KH nhập, đã đánh số: 1), 2), 3)...)

YÊU CẦU:
- Một Line có thể không có món hoặc có nhiều hơn 1 món.
- Chỉ tạo item KH thực sự đặt món (bỏ qua xã giao, trò chuyện...).
- Không tạo sản phẩm mới; chỉ chọn Id có trong SHORTLIST/MENU.
- Map từng item về:
  {
    ""Id"": ""GUID sản phẩm"",
    ""SoLuong"": số nguyên >=1,
    ""NoteText"": ""ghi chú..."",
    ""Line"": số dòng nguồn trong LINES
  }
Trả về **một mảng JSON duy nhất**.
".Trim();

            var userPromptSb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(shortFinal)) { userPromptSb.AppendLine(shortFinal); userPromptSb.AppendLine(); }
            userPromptSb.AppendLine(menuText);
            userPromptSb.AppendLine();
            userPromptSb.AppendLine("LINES");
            userPromptSb.AppendLine(linesText);
            string userPrompt = userPromptSb.ToString();

            // ==== 4) Audit Discord (chỉ những gì còn dùng)
            baoCao.Add("-----customerNameHint-----"); baoCao.Add(customerNameHint);
            baoCao.Add("-----rawInput-----"); baoCao.Add(rawInput);
            baoCao.Add("-----linesText------"); baoCao.Add(linesText);
            baoCao.Add("-----HisList-----"); baoCao.Add(HisList);
            baoCao.Add("-----LearnList-----"); baoCao.Add(LearnList);

            //baoCao.Add("menuText"); baoCao.Add(menuText);
            // baoCao.Add("linesText"); baoCao.Add(linesText);
            //baoCao.Add("systemPrompt"); baoCao.Add(systemPrompt);
            // baoCao.Add("userPrompt"); baoCao.Add(userPrompt);
            await DiscordService.SendAsync(Shared.Enums.DiscordEventType.Admin, string.Join("\n", baoCao), customerNameHint);
            //return null;

            // ==== 5) Call OpenAI
            string jsonOut = await CallChatCompletionsAsync(model, systemPrompt, userPrompt);
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

                        if (dto.Id != Guid.Empty)
                            result.Add(dto);
                    }
                }
            }
            catch { /* nếu GPT trả sai format thì để trống */ }

            return result;
        }
        public async Task<ObservableCollection<ChiTietHoaDonDto>> MapToChiTietAsync(
            string rawInput,
            IEnumerable<QuickOrderDto> preds,          // ✅ truyền sẵn
            string? customerNameHint = null)
        {
            var chiTiets = new ObservableCollection<ChiTietHoaDonDto>();
            if (preds == null) return chiTiets;

            var spMap = AppProviders.SanPhams.Items.ToDictionary(x => x.Id, x => x);
            var normLines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput, customerNameHint).ToList();

            foreach (var p in preds)
            {
                if (p.Id == Guid.Empty || !spMap.TryGetValue(p.Id, out var sp)) continue;

                string? lineText = null;
                if (p.Line.HasValue && p.Line.Value >= 1 && p.Line.Value <= normLines.Count)
                    lineText = normLines[p.Line.Value - 1];

                var bt = PickVariantByNote(sp, $"{p.NoteText} {lineText}");
                if (bt == null) continue;

                chiTiets.Add(new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),
                    SanPhamId = sp.Id,
                    SanPhamIdBienThe = bt.Id,
                    TenSanPham = sp.Ten,
                    DonGia = bt.GiaBan,
                    TenBienThe = bt.TenBienThe ?? "Size Chuẩn",
                    SoLuong = Math.Max(1, p.SoLuong),
                    NoteText = p.NoteText ?? ""
                });
            }
            return chiTiets;
        }

        //public async Task<ObservableCollection<ChiTietHoaDonDto>> MapToChiTietAsync(
        //   string rawInput, string? combinedShortlistText = null, Guid? khachHangId = null,
        //   int shortlistTopK = 12, string model = DefaultModel, string? chatContext = null,
        //   string? customerNameHint = null) // ✅ thêm
        //{


        //    var preds = await ParseQuickOrderAsync(rawInput, combinedShortlistText, khachHangId, shortlistTopK, model, chatContext, customerNameHint);

        //    var chiTiets = new ObservableCollection<ChiTietHoaDonDto>();
        //    if (preds == null || preds.Count == 0) return chiTiets;

        //    var spMap = AppProviders.SanPhams.Items.ToDictionary(x => x.Id, x => x);

        //    // LINES chuẩn hoá để map Line -> ghi chú/biến thể
        //    var normLines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput, customerNameHint).ToList(); // ✅

        //    foreach (var p in preds)
        //    {
        //        if (p.Id == Guid.Empty || !spMap.TryGetValue(p.Id, out var sp)) continue;

        //        string? lineText = null;
        //        if (p.Line.HasValue && p.Line.Value >= 1 && p.Line.Value <= normLines.Count)
        //            lineText = normLines[p.Line.Value - 1];

        //        var bt = PickVariantByNote(sp, $"{p.NoteText} {lineText}");
        //        if (bt == null) continue;

        //        chiTiets.Add(new ChiTietHoaDonDto
        //        {
        //            Id = Guid.NewGuid(),
        //            SanPhamId = sp.Id,
        //            SanPhamIdBienThe = bt.Id,
        //            TenSanPham = sp.Ten,
        //            DonGia = bt.GiaBan,
        //            TenBienThe = bt.TenBienThe ?? "Size Chuẩn",
        //            SoLuong = Math.Max(1, p.SoLuong),
        //            NoteText = p.NoteText ?? ""
        //        });
        //    }
        //    return chiTiets;
        //}

        //// ======== helpers ========

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

        private static string JoinShortlists(string? a, string? b)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(a)) parts.Add(a.Trim());
            if (!string.IsNullOrWhiteSpace(b)) parts.Add(b.Trim());
            if (parts.Count == 0) return "";
            var merged = string.Join("\n", parts);
            var lines = merged.Split('\n').Select(x => x.TrimEnd()).ToList();
            var cleaned = new List<string>();
            bool headerWritten = false;
            foreach (var ln in lines)
            {
                if (ln.StartsWith("SHORTLIST", StringComparison.OrdinalIgnoreCase))
                {
                    if (!headerWritten) { cleaned.Add("SHORTLIST (Id<TAB>Tên)"); headerWritten = true; }
                    continue;
                }
                cleaned.Add(ln);
            }
            return string.Join("\n", cleaned);
        }

        private static string BuildNumberedLines(List<string> normLines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < normLines.Count; i++) sb.AppendLine($"{i + 1}) {normLines[i]}");
            return sb.ToString().TrimEnd();
        }

        // ========= Size/variant picker (2-size: Chuẩn & L) =========
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