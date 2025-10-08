using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Services;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AiOrdering
{
    public class QuickOrderService
    {
        private readonly HttpClient _http;
        private readonly QuickOrderEngine _engine;

        public QuickOrderService(string apiKey)
        {
            _engine = new QuickOrderEngine(apiKey);
            _http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/") };
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }

        private static bool IsDataUrl(string s) => s.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        private static bool LooksLikeBase64(string s)
            => s.Length > 100 && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[A-Za-z0-9+/=\s]+$");

        private async Task<string> ExtractTextFromImageAsync(string inputOrUrl)
        {
            string imageUrlOrData;
            if (File.Exists(inputOrUrl))
            {
                var bytes = await File.ReadAllBytesAsync(inputOrUrl);
                var ext = Path.GetExtension(inputOrUrl).ToLowerInvariant();
                var mime = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    ".heic" => "image/heic",
                    _ => "image/png"
                };
                imageUrlOrData = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            else if (IsDataUrl(inputOrUrl)) imageUrlOrData = inputOrUrl;
            else if (Uri.IsWellFormedUriString(inputOrUrl, UriKind.Absolute)) imageUrlOrData = inputOrUrl;
            else if (LooksLikeBase64(inputOrUrl)) imageUrlOrData = "data:image/png;base64," + inputOrUrl;
            else throw new ArgumentException("Đầu vào ảnh không hợp lệ.");

            var body = new
            {
                model = "gpt-4o-mini",
                temperature = 0,
                messages = new object[]
                {
                    new { role = "system", content = "Bạn là công cụ OCR, trả về nguyên văn tiếng Việt/Anh trong ảnh, không thêm diễn giải." },
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Đọc chính xác text trong ảnh đơn hàng này:" },
                            new { type = "image_url", image_url = new { url = imageUrlOrData } }
                        }
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json")
            };

            using var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
            string? contentText = null;

            if (msg.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                contentText = contentEl.GetString();
            else if (msg.TryGetProperty("content", out contentEl) && contentEl.ValueKind == JsonValueKind.Array)
                foreach (var part in contentEl.EnumerateArray())
                    if (part.TryGetProperty("type", out var t) && t.GetString() == "text" &&
                        part.TryGetProperty("text", out var txt)) { contentText = txt.GetString(); break; }

            return contentText?.Trim() ?? "";
        }

        /// <summary>
        /// Trả về:
        /// - HoaDon (map từ dự đoán)
        /// - RawInput: CHUỖI ĐÃ LỌC (OrderTextCleaner.PreClean) để hiển thị trong UI/ghi log
        /// - Predictions: list dự đoán dòng ↔ sản phẩm (để hiển thị/learn theo Line)
        /// </summary>
        public async Task<(HoaDonDto? HoaDon, string RawInput, List<QuickOrderDto> Predictions)> BuildHoaDonAsync(
            string inputOrUrl, bool isImage = false, string? shortMenuFromHistory = "", Guid? khachHangId = null)
        {
            List<string> baoCao = new List<string>();
            // 1) Lấy text gốc (từ ảnh hoặc text)
            string sourceText = inputOrUrl;

            if (isImage ||
                IsDataUrl(inputOrUrl) ||
                Uri.IsWellFormedUriString(inputOrUrl, UriKind.Absolute) ||
                LooksLikeBase64(inputOrUrl) ||
                File.Exists(inputOrUrl))
            {
                sourceText = await ExtractTextFromImageAsync(inputOrUrl);
            }

            // 2) Chuỗi hiển thị (đã lọc rác, giữ note) — phục vụ UI & logging
            string cleanedForDisplay = OrderTextCleaner.PreClean(sourceText);

            var menu = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();

            // SHORTLIST học máy (theo khách + global) + lịch sử server
            string learnedShort = QuickGptLearningStore.Instance.BuildShortlistForPrompt(
                customerId: khachHangId,
                currentMenu: menu,
                serverTopForCustomer: null,
                topK: 12
            );
            string combinedShort = string.Join("\n",
                new[] { shortMenuFromHistory ?? "", learnedShort }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return (new HoaDonDto
                {
                    Id = Guid.Empty,
                    Ngay = DateTime.Now.Date,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(),
                    ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>(),
                    ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>()
                }, cleanedForDisplay, new List<QuickOrderDto>());
            }

            // 3) Gọi engine (Engine tự PreClean + Normalize lại nội bộ nên không lo double)
            var preds = await _engine.ParseQuickOrderAsync(sourceText, combinedShort, khachHangId);
            var chiTiets = await _engine.MapToChiTietAsync(sourceText, combinedShort, khachHangId);


            // 4) Hoá đơn kết quả (mở form kể cả khi rỗng)
            var hd = new HoaDonDto
            {
                Id = Guid.Empty,
                Ngay = DateTime.Now.Date,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                ChiTietHoaDons = chiTiets,
                ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>(),
                ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>()
            };

            baoCao.Add("sourceText"); baoCao.Add(sourceText);
            baoCao.Add("cleanedForDisplay"); baoCao.Add(cleanedForDisplay);
            baoCao.Add("shortMenuFromHistory"); baoCao.Add(shortMenuFromHistory);
            baoCao.Add("learnedShort"); baoCao.Add(learnedShort);
            await DiscordService.SendAsync(
     Shared.Enums.DiscordEventType.Admin,
     string.Join("\n", baoCao)
 );
            // Lưu ý: Trả về cleanedForDisplay để gắn vào HoaDonEdit.GptInputText → nhìn đẹp, dễ đọc.
            return (hd, cleanedForDisplay, preds);
        }
    }
}