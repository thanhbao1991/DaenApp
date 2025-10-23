using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class ChiTietHoaDonThanhToanTabModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        public List<ChiTietHoaDonThanhToanDto> Items { get; set; } = new();
        public decimal TotalThanhToan => Items.Sum(x => x.SoTien);

        public ChiTietHoaDonThanhToanTabModel(IHttpClientFactory factory)
        {
            _clientFactory = factory;
        }

        // 🟟 Load danh sách
        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("Api");
            var res = await client.GetFromJsonAsync<Result<List<ChiTietHoaDonThanhToanDto>>>("/api/ChiTietHoaDonThanhToan");
            if (res?.IsSuccess == true && res.Data != null)
                Items = res.Data!;
        }

        // 🟟 Xoá 1 bản ghi thanh toán
        public class DeleteRequest
        {
            public Guid Id { get; set; }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteRequest req)
        {
            var client = _clientFactory.CreateClient("Api");
            var res = await client.DeleteAsync($"/api/ChiTietHoaDonThanhToan/{req.Id}");
            var result = await res.Content.ReadFromJsonAsync<Result<ChiTietHoaDonThanhToanDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? (result?.IsSuccess == true
                            ? "✅ Đã xoá bản ghi thanh toán." : "Không thể xoá.")
            });
        }
    }
}