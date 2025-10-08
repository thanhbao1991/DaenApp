using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TraSuaApp.Shared.Dtos;
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

        public async Task<List<QuickOrderDto>> ParseQuickOrderAsync(
            string rawInput,
            string? combinedShortlistText = null,
            Guid? khachHangId = null,
            int shortlistTopK = 12,
            string model = DefaultModel)
        {
            var result = new List<QuickOrderDto>();
            List<string> baoCao = new List<string>();
            // ✨ Dùng cleaner chung
            var lines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput).ToList();
            if (lines.Count == 0) return result;

            var menu = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();

            string learnedShort = QuickGptLearningStore.Instance.BuildShortlistForPrompt(
                customerId: khachHangId,
                currentMenu: menu,
                serverTopForCustomer: null,
                topK: shortlistTopK);

            var shortFinal = JoinShortlists(combinedShortlistText, learnedShort);
            string menuText = BuildMenuForGpt(menu);
            string linesText = BuildNumberedLines(lines);

            string systemPrompt = @"
Bạn là hệ thống POS. Chỉ trả về DUY NHẤT một mảng JSON hợp lệ, không có text nào khác.
Bạn sẽ nhận: SHORTLIST (Id<TAB>Tên) → MENU (Id<TAB>ten_khong_dau) → LINES (đánh số).
Mỗi dòng tạo đúng 1 item:
{
  ""Id"": ""GUID sản phẩm"",
  ""SoLuong"": số nguyên >=1,
  ""NoteText"": ""ghi chú..."",
  ""Line"": số dòng tương ứng trong LINES
}
Chỉ chọn từ SHORTLIST/MENU; không tạo sản phẩm mới; chỉ xuất JSON array.
".Trim();

            var userPromptSb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(shortFinal)) { userPromptSb.AppendLine(shortFinal); userPromptSb.AppendLine(); }
            userPromptSb.AppendLine(menuText);
            userPromptSb.AppendLine();
            userPromptSb.AppendLine("LINES");
            userPromptSb.AppendLine(linesText);
            string userPrompt = userPromptSb.ToString();


            baoCao.Add("learnedShort"); baoCao.Add(learnedShort);
            baoCao.Add("shortFinal"); baoCao.Add(shortFinal);
            baoCao.Add("menuText"); baoCao.Add(menuText);
            baoCao.Add("linesText"); baoCao.Add(linesText);
            baoCao.Add("systemPrompt"); baoCao.Add(systemPrompt);
            baoCao.Add("userPrompt"); baoCao.Add(userPrompt);
            await DiscordService.SendAsync(
     Shared.Enums.DiscordEventType.Admin,
     string.Join("\n", baoCao)
 );

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
            catch { /* GPT có thể trả sai - an toàn để rỗng */ }

            return result;
        }

        public async Task<ObservableCollection<ChiTietHoaDonDto>> MapToChiTietAsync(
            string rawInput, string? combinedShortlistText = null, Guid? khachHangId = null,
            int shortlistTopK = 12, string model = DefaultModel)
        {
            var preds = await ParseQuickOrderAsync(rawInput, combinedShortlistText, khachHangId, shortlistTopK, model);
            var chiTiets = new ObservableCollection<ChiTietHoaDonDto>();
            if (preds == null || preds.Count == 0) return chiTiets;

            var spMap = AppProviders.SanPhams.Items.ToDictionary(x => x.Id, x => x);

            // Lấy lại danh sách line đã normalize để hỗ trợ pick size theo dòng
            var normLines = OrderTextCleaner.PreCleanThenNormalizeLines(rawInput).ToList();

            foreach (var p in preds)
            {
                if (p.Id == Guid.Empty || !spMap.TryGetValue(p.Id, out var sp)) continue;

                // Lấy line text (nếu GPT trả Line) để tăng tín hiệu chọn biến thể
                string? lineText = null;
                if (p.Line.HasValue && p.Line.Value >= 1 && p.Line.Value <= normLines.Count)
                    lineText = normLines[p.Line.Value - 1];

                // ✨ Chọn biến thể theo keyword; fallback MacĐịnh/giá
                var bt = PickVariantByNote(sp, $"{p.NoteText} {lineText}");
                if (bt == null) continue;

                chiTiets.Add(new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),
                    SanPhamId = sp.Id,
                    SanPhamIdBienThe = bt.Id,
                    TenSanPham = sp.Ten,
                    DonGia = bt.GiaBan,
                    TenBienThe = bt.TenBienThe ?? "Size Chuẩn", // đảm bảo nhãn chuẩn
                    SoLuong = Math.Max(1, p.SoLuong),
                    NoteText = p.NoteText ?? ""
                });
            }
            return chiTiets;
        }

        // ======== helpers ========

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

        // ========= Size/variant picker (2-size: Size Chuẩn & Size L) =========
        private static SanPhamBienTheDto? PickVariantByNote(SanPhamDto sp, string? noteOrLine)
        {
            if (sp.BienThe == null || sp.BienThe.Count == 0) return null;

            var text = OrderTextCleaner.NormalizeNoDiacritics(noteOrLine ?? "");

            // Từ khóa nghiêng về L
            bool wantL = Regex.IsMatch(text, @"\b(size\s*l|sz\s*l|\bl\b|lon|to|bu|large|big)\b");
            bool forceStandard = Regex.IsMatch(text,
                @"\b(size\s*m|sz\s*m|\bm\b|chuan|thuong|vua|medium|regular|normal|size\s*chuan|"
              + @"size\s*s|sz\s*s|\bs\b|nho|small)\b");

            static string Norm(string s) => OrderTextCleaner.NormalizeNoDiacritics(s ?? "");
            static bool EqualsStd(string s) => Norm(s) == "size chuan" || Norm(s) == "chuan";
            static bool ContainsStd(string s) => Norm(s).Contains("size chuan") || Norm(s).Contains("chuan");
            static bool ContainsL(string s) => Norm(s).Contains("size l") || Norm(s).Contains("l");

            // 1) Nếu người dùng muốn L rõ ràng và không ép Chuẩn -> tìm theo tên "Size L"
            if (wantL && !forceStandard)
            {
                var byNameL = sp.BienThe.FirstOrDefault(v => !EqualsStd(v.TenBienThe ?? "") && ContainsL(v.TenBienThe ?? ""));
                if (byNameL != null) return byNameL;

                // Fallback: chọn biến thể có giá cao nhất coi như L
                return sp.BienThe.OrderByDescending(v => v.GiaBan).FirstOrDefault()
                       ?? sp.BienThe.FirstOrDefault(x => x.MacDinh)
                       ?? sp.BienThe.FirstOrDefault();
            }

            // 2) Ưu tiên đúng "Size Chuẩn" (khớp tên chính xác)
            var exactStd = sp.BienThe.FirstOrDefault(v => EqualsStd(v.TenBienThe ?? ""));
            if (exactStd != null) return exactStd;

            // 3) Nếu không có đúng tên, chọn biến thể có tên chứa "size chuan"/"chuan"
            var containStd = sp.BienThe.FirstOrDefault(v => ContainsStd(v.TenBienThe ?? ""));
            if (containStd != null) return containStd;

            // 4) Fallback: MacĐịnh → hoặc biến thể rẻ nhất coi như Chuẩn
            return sp.BienThe.FirstOrDefault(x => x.MacDinh)
                ?? sp.BienThe.OrderBy(v => v.GiaBan).FirstOrDefault()
                ?? sp.BienThe.FirstOrDefault();
        }
    }
}