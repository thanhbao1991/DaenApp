using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.Ordering
{
    /// <summary>
    /// SERVICE ổn định: giữ API cho UI (MessengerTab, …) luôn giống nhau.
    /// - OCR ảnh (OpenAI Vision qua Chat Completions)
    /// - Ráp hoá đơn từ text/ảnh
    /// - Dùng QuickOrderEngine để suy món (engine có thể chỉnh thoải mái)
    /// </summary>
    public class QuickOrderService
    {
        private readonly HttpClient _http;           // OCR ảnh online
        private readonly QuickOrderEngine _engine;   // Engine linh hoạt

        public QuickOrderService(string apiKey)
        {
            _engine = new QuickOrderEngine(apiKey);

            _http = new HttpClient { BaseAddress = new Uri("https://api.openai.com/") };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        // ===== Helpers ảnh / data-url =====
        private static bool IsDataUrl(string s) => s.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        private static bool LooksLikeBase64(string s)
            => s.Length > 100 && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[A-Za-z0-9+/=\s]+$");

        // ===== OCR ONLINE (gpt-4o-mini qua Chat Completions) =====
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
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    ".heic" => "image/heic",
                    _ => "image/png"
                };
                imageUrlOrData = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            else if (IsDataUrl(inputOrUrl))
            {
                imageUrlOrData = inputOrUrl;
            }
            else if (Uri.IsWellFormedUriString(inputOrUrl, UriKind.Absolute))
            {
                imageUrlOrData = inputOrUrl;
            }
            else if (LooksLikeBase64(inputOrUrl))
            {
                imageUrlOrData = "data:image/png;base64," + inputOrUrl;
            }
            else
            {
                throw new ArgumentException("Đầu vào ảnh không hợp lệ (không phải path/URL/base64/data-url).");
            }

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
            {
                contentText = contentEl.GetString();
            }
            else if (msg.TryGetProperty("content", out contentEl) && contentEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in contentEl.EnumerateArray())
                {
                    if (part.TryGetProperty("type", out var t) && t.GetString() == "text" &&
                        part.TryGetProperty("text", out var txt))
                    {
                        contentText = txt.GetString();
                        break;
                    }
                }
            }

            return contentText?.Trim() ?? "";
        }

        // ===== API ổn định cho UI: Build từ text/ảnh =====
        public async Task<(HoaDonDto? HoaDon, string RawInput)> BuildHoaDonAsync(string inputOrUrl, bool isImage = false, string? shortMenu = "")
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
                }, text);
            }

            var chiTiets = await _engine.MapToChiTietAsync(text, shortMenu);

            return (new HoaDonDto
            {
                Id = Guid.Empty,
                Ngay = DateTime.Now.Date,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                ChiTietHoaDons = chiTiets,
                ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>(),
                ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>()
            }, text);
        }
    }
}