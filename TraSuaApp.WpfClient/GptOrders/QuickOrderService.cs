using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using TraSuaApp.Shared.Dtos;
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

        public async Task<(HoaDonDto? HoaDon, string RawInput, List<QuickOrderDto> Predictions)> BuildHoaDonAsync(
            string inputOrUrl, bool isImage = false, string? shortMenuFromHistory = "", Guid? khachHangId = null)

        {
            string text = inputOrUrl;

            if (isImage ||
                IsDataUrl(inputOrUrl) ||
                Uri.IsWellFormedUriString(inputOrUrl, UriKind.Absolute) ||
                LooksLikeBase64(inputOrUrl) ||
                File.Exists(inputOrUrl))
            {
                text = await ExtractTextFromImageAsync(inputOrUrl);
            }

            var menu = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();

            // DÙNG HÀM MỚI: BuildShortlistForPrompt (không cần await)
            string learnedShort = QuickGptLearningStore.Instance.BuildShortlistForPrompt(
                customerId: khachHangId,
                currentMenu: menu,
                serverTopForCustomer: null,
                topK: 12
            );
            string combinedShort = string.Join("\n",
                new[] { shortMenuFromHistory ?? "", learnedShort }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(text))
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
                }, text, new List<QuickOrderDto>());
            }

            var preds = await _engine.ParseQuickOrderAsync(text, combinedShort, khachHangId);
            var chiTiets = await _engine.MapToChiTietAsync(text, combinedShort, khachHangId);
            return (new HoaDonDto
            {
                Id = Guid.Empty,
                Ngay = DateTime.Now.Date,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                ChiTietHoaDons = chiTiets,
                ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>(),
                ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>()
            }, text, preds);
        }
    }
}