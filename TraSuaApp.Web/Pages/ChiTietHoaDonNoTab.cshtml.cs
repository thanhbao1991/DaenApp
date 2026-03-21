using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
namespace TraSuaAppWeb.Pages
{
    [IgnoreAntiforgeryToken]
    public class ChiTietHoaDonNoTabModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public List<HoaDonNoDto> Items { get; set; } = new();

        public ChiTietHoaDonNoTabModel(IHttpClientFactory factory)
        {
            _clientFactory = factory;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var client = _clientFactory.CreateClient("Api");

                // ✅ FIX: dùng đúng API giống WPF
                var res = await client.GetFromJsonAsync<Result<List<HoaDonNoDto>>>(
                    "/api/dashboard/get-cong-no"
                );

                if (res?.IsSuccess == true && res.Data != null)
                {
                    Items = res.Data;
                }
                else
                {
                    Items = new();
                }
            }
            catch
            {
                Items = new();
            }
        }

        public class PayRequest
        {
            public Guid Id { get; set; }
            public string Type { get; set; } = "";
            public decimal? Amount { get; set; }
            public string? Note { get; set; }
        }

        public async Task<IActionResult> OnPostPayAsync([FromBody] PayRequest req)
        {
            try
            {
                var client = _clientFactory.CreateClient("Api");

                var response = await client.PostAsJsonAsync(
                    $"/api/ChiTietHoaDonNo/{req.Id}/pay",
                    new
                    {
                        type = req.Type,
                        amount = req.Amount,
                        note = req.Note
                    }
                );

                var result = await response.Content.ReadFromJsonAsync<Result<object>>();

                return new JsonResult(new
                {
                    success = result?.IsSuccess ?? false,
                    message = result?.Message
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}