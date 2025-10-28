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

            // ✅ KHJson = [] — dừng load toàn bộ khách hàng về browser
            KHJson = "[]";

            try { using var spRes = await api.GetAsync("/api/SanPham"); SPJson = await ExtractDataArrayAsync(spRes); } catch { SPJson = "[]"; }
            try { using var gbRes = await api.GetAsync("/api/KhachHangGiaBan"); GBJson = await ExtractDataArrayAsync(gbRes); } catch { GBJson = "[]"; }
            try { using var vcRes = await api.GetAsync("/api/Voucher"); VouchersJson = await ExtractDataArrayAsync(vcRes); } catch { VouchersJson = "[]"; }
        }

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

            var response = await api.PostAsJsonAsync("/api/HoaDon", req);
            var text = await response.Content.ReadAsStringAsync();

            return new JsonResult(new
            {
                success = response.IsSuccessStatusCode,
                raw = TryParseJson(text)
            });
        }
        // GET: /HoaDon/CreatePlus?handler=SearchSP&q=...&take=30
        public async Task<IActionResult> OnGetSearchSpAsync(string q, int take = 30)
        {
            var api = _http.CreateClient("Api");
            var url = $"/api/SanPham/search?q={Uri.EscapeDataString(q ?? "")}&take={take}";
            var res = await api.GetAsync(url);
            var raw = await res.Content.ReadAsStringAsync();
            return Content(raw, "application/json");
        }
        // ✅ Đồng bộ cách gọi như nơi khác — Web forward đến Api
        public async Task<IActionResult> OnGetSearchKHAsync(string q, int take = 30)
        {
            var api = _http.CreateClient("Api");
            var url = $"/api/KhachHang/search?q={Uri.EscapeDataString(q ?? "")}&take={take}";
            var res = await api.GetAsync(url);

            var raw = await res.Content.ReadAsStringAsync();
            return Content(raw, "application/json");
        }
        // GET: /HoaDon/CreatePlus?handler=KhInfo&id=...
        public async Task<IActionResult> OnGetKhInfoAsync(Guid id)
        {
            var api = _http.CreateClient("Api");
            var res = await api.GetAsync($"/api/Dashboard/thongtin-khachhang/{id}");
            var raw = await res.Content.ReadAsStringAsync();
            return Content(raw, "application/json");
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