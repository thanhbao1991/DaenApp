using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class CongViecNoiBoTabModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        public List<CongViecNoiBoDto> Items { get; set; } = new();

        public CongViecNoiBoTabModel(IHttpClientFactory factory)
        {
            _clientFactory = factory;
        }

        // 🟟 Load danh sách công việc
        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("Api");
            var res = await client.GetFromJsonAsync<Result<List<CongViecNoiBoDto>>>("/api/CongViecNoiBo");
            if (res?.IsSuccess == true && res.Data != null)
                Items = res.Data!
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.DaHoanThanh)
                    .ThenByDescending(x => x.LastModified)
                    .ToList();
        }

        // 🟟 Thêm nhanh 1 công việc
        public class AddRequest
        {
            public string Ten { get; set; } = "";
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] AddRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Ten))
                return new JsonResult(new { success = false, message = "Tên công việc không được để trống." });

            var client = _clientFactory.CreateClient("Api");
            var response = await client.PostAsJsonAsync("/api/CongViecNoiBo", new { Ten = req.Ten });
            var result = await response.Content.ReadFromJsonAsync<Result<CongViecNoiBoDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? (result?.IsSuccess == true
                    ? "✅ Đã thêm công việc mới." : "Không thể thêm.")
            });
        }

        // ✅ Toggle hoàn thành
        public class ToggleRequest
        {
            public Guid Id { get; set; }
        }

        public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleRequest req)
        {
            var client = _clientFactory.CreateClient("Api");
            var res = await client.PostAsync($"/api/CongViecNoiBo/{req.Id}/toggle", null);
            var result = await res.Content.ReadFromJsonAsync<Result<CongViecNoiBoDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? "Không thể cập nhật trạng thái."
            });
        }

        // ❌ Xoá công việc
        public class DeleteRequest
        {
            public Guid Id { get; set; }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteRequest req)
        {
            var client = _clientFactory.CreateClient("Api");
            var res = await client.DeleteAsync($"/api/CongViecNoiBo/{req.Id}");
            var result = await res.Content.ReadFromJsonAsync<Result<CongViecNoiBoDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? (result?.IsSuccess == true
                            ? "✅ Đã xoá công việc." : "Không thể xoá.")
            });
        }
    }
}