using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TraSuaAppWeb.Pages.HoaDon
{
    [IgnoreAntiforgeryToken]
    public class CreatePlusModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public CreatePlusModel(IHttpClientFactory http) => _http = http;

        public string KHJson { get; private set; } = "[]";
        public string SPJson { get; private set; } = "[]";
        public string GBJson { get; private set; } = "[]";
        public string VouchersJson { get; private set; } = "[]";

        public async Task OnGetAsync()
        {
            var api = _http.CreateClient("Api");

            try { using var khRes = await api.GetAsync("/api/KhachHang"); KHJson = await ExtractDataArrayAsync(khRes); } catch { KHJson = "[]"; }
            try { using var spRes = await api.GetAsync("/api/SanPham"); SPJson = await ExtractDataArrayAsync(spRes); } catch { SPJson = "[]"; }
            try { using var gbRes = await api.GetAsync("/api/KhachHangGiaBan"); GBJson = await ExtractDataArrayAsync(gbRes); } catch { GBJson = "[]"; }
            try { using var vcRes = await api.GetAsync("/api/Voucher"); VouchersJson = await ExtractDataArrayAsync(vcRes); } catch { VouchersJson = "[]"; }
        }

        // ========= NHẬN PAYLOAD TỪ JS & FORWARD VỀ API =========
        public class SaveLine
        {
            public Guid SanPhamId { get; set; }
            public Guid SanPhamIdBienThe { get; set; }
            public string? TenSanPham { get; set; }
            public string? TenBienThe { get; set; }
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public string? NoteText { get; set; }
            public List<object>? ToppingDtos { get; set; } = new();
        }
        public class SaveRequest
        {
            public string LoaiDon { get; set; } = "";
            public string? TenBan { get; set; }
            public Guid? KhachHangId { get; set; }
            public string? KhachHangText { get; set; }
            public string? DienThoaiText { get; set; }
            public string? DiaChiText { get; set; }
            public Guid? VoucherId { get; set; }
            public decimal GiamGiaFix { get; set; }
            public decimal CongNo { get; set; }
            public List<SaveLine> ChiTietHoaDons { get; set; } = new();
        }

        // POST: /HoaDon/CreatePlus?handler=Save
        public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest req)
        {
            var api = _http.CreateClient("Api");

            // Điều chỉnh đường dẫn theo API của bạn
            var forwardPath = "/api/HoaDon";               // A
            // var forwardPath = "/api/HoaDon/Create";     // B

            var response = await api.PostAsJsonAsync(forwardPath, req);
            var text = await response.Content.ReadAsStringAsync();

            return new JsonResult(new
            {
                success = response.IsSuccessStatusCode,
                status = (int)response.StatusCode,
                raw = TryParseJson(text)
            });
        }

        private static object? TryParseJson(string s)
        {
            try { return JsonSerializer.Deserialize<JsonElement>(s); }
            catch { return s; }
        }

        // Helper: bóc mảng data từ Result<T> hoặc mảng thuần
        private static async Task<string> ExtractDataArrayAsync(HttpResponseMessage res)
        {
            if (!res.IsSuccessStatusCode) return "[]";
            using var s = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(s);

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Array)
                return data.GetRawText();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.GetRawText();

            return "[]";
        }
    }
}