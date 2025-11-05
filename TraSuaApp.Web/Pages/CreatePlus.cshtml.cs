using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TraSuaAppWeb.Pages.HoaDon
{
    // Cho phép POST AJAX không cần anti-forgery (dùng fetch)
    [IgnoreAntiforgeryToken]
    public class CreatePlusModel : PageModel
    {
        private readonly IHttpClientFactory _http;

        public CreatePlusModel(IHttpClientFactory http) => _http = http;

        // Seed xuống client (SP/GB/Voucher). KHJson = [] để không tải toàn bộ KH vào browser.
        public string KHJson { get; private set; } = "[]";
        public string SPJson { get; private set; } = "[]";
        public string GBJson { get; private set; } = "[]";
        public string VouchersJson { get; private set; } = "[]";

        public async Task OnGetAsync()
        {
            var api = _http.CreateClient("Api");

            // ✅ Dừng preload toàn bộ khách hàng
            KHJson = "[]";

            //  try { using var spRes = await api.GetAsync("/api/SanPham"); SPJson = await ExtractDataArrayAsync(spRes); } catch { 
            SPJson = "[]";
            // }
            try { using var gbRes = await api.GetAsync("/api/KhachHangGiaBan"); GBJson = await ExtractDataArrayAsync(gbRes); } catch { GBJson = "[]"; }
            try { using var vcRes = await api.GetAsync("/api/Voucher"); VouchersJson = await ExtractDataArrayAsync(vcRes); } catch { VouchersJson = "[]"; }
        }

        // ====== DTO (để ai muốn post qua /HoaDon/CreatePlus?handler=Save cũng OK) ======
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
            public string PhanLoai { get; set; } = "";
            public string? TenBan { get; set; }
            public Guid? KhachHangId { get; set; }
            public string? TenKhachHangText { get; set; }
            public string? SoDienThoaiText { get; set; }
            public string? DiaChiText { get; set; }
            public Guid? VoucherId { get; set; }
            public decimal GiamGiaFix { get; set; }
            public decimal CongNo { get; set; }
            public List<SaveLine> ChiTietHoaDons { get; set; } = new();

            public List<SaveVoucherReq> ChiTietHoaDonVouchers { get; set; } = new();
        }
        public class SaveVoucherReq
        {
            public Guid VoucherId { get; set; }
        }
        public async Task<IActionResult> OnPostSaveAsync([FromBody] System.Text.Json.JsonElement raw)
        {
            var api = _http.CreateClient("Api");
            var res = await api.PostAsync("/api/HoaDon",
                new StringContent(raw.GetRawText(), Encoding.UTF8, "application/json"));
            var s = await res.Content.ReadAsStringAsync();
            return Content(s, res.Content.Headers.ContentType?.MediaType ?? "application/json");
        }
        // POST: /HoaDon/CreatePlus?handler=Save
        //public async Task<IActionResult> OnPostSaveAsync([FromBody] SaveRequest req)
        //{
        //    var api = _http.CreateClient("Api");

        //    using var res = await api.PostAsJsonAsync("/api/HoaDon", req);
        //    var raw = await res.Content.ReadAsStringAsync();
        //    var contentType = res.Content.Headers.ContentType?.MediaType ?? "application/json";

        //    // ⬇️ Forward nguyên trạng mã lỗi & nội dung từ API
        //    return new ContentResult
        //    {
        //        Content = raw,
        //        ContentType = contentType,
        //        StatusCode = (int)res.StatusCode
        //    };
        //}
        // ====== SEARCH SẢN PHẨM (forward) ======
        // GET: /HoaDon/CreatePlus?handler=SearchSp&q=...&take=30
        public async Task<IActionResult> OnGetSearchSpAsync(string q, int take = 30)
        {
            var api = _http.CreateClient("Api");
            var url = $"/api/SanPham/search?q={Uri.EscapeDataString(q ?? "")}&take={take}";
            using var res = await api.GetAsync(url);
            var raw = await res.Content.ReadAsStringAsync();
            return Content(raw, "application/json");
        }

        // ====== SEARCH KHÁCH HÀNG (forward) ======
        // GET: /HoaDon/CreatePlus?handler=SearchKH&q=...&take=30
        public async Task<IActionResult> OnGetSearchKHAsync(string q, int take = 30)
        {
            var api = _http.CreateClient("Api");
            var url = $"/api/KhachHang/search?q={Uri.EscapeDataString(q ?? "")}&take={take}";
            using var res = await api.GetAsync(url);
            var raw = await res.Content.ReadAsStringAsync();
            return Content(raw, "application/json");
        }

        // ====== THÔNG TIN KH (điểm, nợ, v.v.) (forward) ======
        // GET: /HoaDon/CreatePlus?handler=KhInfo&id=...
        public async Task<IActionResult> OnGetKhInfoAsync(Guid id)
        {
            var api = _http.CreateClient("Api");
            using var res = await api.GetAsync($"/api/Dashboard/thongtin-khachhang/{id}");
            var raw = await res.Content.ReadAsStringAsync();
            return Content(raw, "application/json");
        }

        // ====== Helpers ======
        private static object? TryParseJson(string s)
        {
            try { return JsonSerializer.Deserialize<JsonElement>(s); }
            catch { return s; }
        }

        // Bóc mảng data từ Result<T> hoặc mảng thuần
        private static async Task<string> ExtractDataArrayAsync(HttpResponseMessage res)
        {
            if (!res.IsSuccessStatusCode) return "[]";

            using var s = await res.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(s);

            // Kiểu Result<T> { data: [...] }
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Array)
            {
                return data.GetRawText();
            }

            // Kiểu mảng thuần [...]
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.GetRawText();

            return "[]";
        }
    }
}