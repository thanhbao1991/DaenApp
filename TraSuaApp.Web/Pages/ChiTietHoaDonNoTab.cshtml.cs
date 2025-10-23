using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Dtos.Requests;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    // BỎ Anti-Forgery cho các POST AJAX trong trang này
    [IgnoreAntiforgeryToken]
    public class ChiTietHoaDonNoTabModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public List<ChiTietHoaDonNoDto> Items { get; set; } = new();
        public decimal TotalConLai => Items.Sum(x => x.SoTienConLai);

        public ChiTietHoaDonNoTabModel(IHttpClientFactory factory)
        {
            _clientFactory = factory;
        }

        // Load danh sách công nợ
        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("Api");
            var res = await client.GetFromJsonAsync<Result<List<ChiTietHoaDonNoDto>>>("/api/ChiTietHoaDonNo");
            if (res?.IsSuccess == true && res.Data != null)
                Items = res.Data!;
        }

        // Payload nhận từ JS
        public class PayRequest
        {
            public Guid Id { get; set; }
            public string Type { get; set; } = "";   // "TienMat" | "ChuyenKhoan"
            public decimal? Amount { get; set; }     // null => thu đủ (SoTienConLai)
            public string? Note { get; set; }        // ghi chú
        }

        // POST: /ChiTietHoaDonNoTab?handler=Pay
        public async Task<IActionResult> OnPostPayAsync([FromBody] PayRequest req)
        {
            var client = _clientFactory.CreateClient("Api");

            // Gửi kèm Amount/Note xuống API để cho phép thu thiếu / thu đủ
            var payload = new PayDebtRequest
            {
                Type = req.Type,
                Amount = req.Amount,   // null => server tự lấy SoTienConLai (thu đủ)
                Note = req.Note
            };

            var response = await client.PostAsJsonAsync($"/api/ChiTietHoaDonNo/{req.Id}/pay", payload);
            var result = await response.Content.ReadFromJsonAsync<Result<ChiTietHoaDonThanhToanDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? (result?.IsSuccess == true
                           ? "✅ Đã ghi nhận thanh toán công nợ." : "Không thể ghi nhận.")
            });
        }
    }
}