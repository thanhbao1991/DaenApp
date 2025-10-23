using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaAppWeb.Pages
{
    public class ChiTieuHangNgayTabModel : PageModel
    {
        private readonly IHttpClientFactory _factory;
        public List<ChiTieuHangNgayDto> Items { get; set; } = new();

        public ChiTieuHangNgayTabModel(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // 🟟 Load danh sách hôm nay
        public async Task OnGetAsync()
        {
            var client = _factory.CreateClient("Api");
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var res = await client.GetFromJsonAsync<Result<List<ChiTieuHangNgayDto>>>(
                $"/api/ChiTieuHangNgay?date={today}"
            );

            if (res?.IsSuccess == true && res.Data != null)
                Items = res.Data!.Where(x => !x.IsDeleted && !x.BillThang)
                                 .OrderByDescending(x => x.NgayGio)
                                 .ToList();
        }

        // ➕ Thêm nhanh
        public class AddRequest
        {
            public string Ten { get; set; } = "";
            public decimal ThanhTien { get; set; }
            public string? GhiChu { get; set; }
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] AddRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Ten))
                return new JsonResult(new { success = false, message = "Tên chi tiêu không được trống." });

            var client = _factory.CreateClient("Api");
            var dto = new ChiTieuHangNgayDto
            {
                Ten = req.Ten.Trim(),
                ThanhTien = req.ThanhTien,
                GhiChu = req.GhiChu ?? "",
                Ngay = DateTime.Today,
                NgayGio = DateTime.Now
            };

            var response = await client.PostAsJsonAsync("/api/ChiTieuHangNgay", dto);
            var result = await response.Content.ReadFromJsonAsync<Result<ChiTieuHangNgayDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? (result?.IsSuccess == true ? "✅ Đã thêm chi tiêu." : "Không thể thêm.")
            });
        }

        // ✏️ Sửa ghi chú
        public class EditRequest
        {
            public Guid Id { get; set; }
            public string? GhiChu { get; set; }
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] EditRequest req)
        {
            var client = _factory.CreateClient("Api");
            var get = await client.GetFromJsonAsync<Result<ChiTieuHangNgayDto>>($"/api/ChiTieuHangNgay/{req.Id}");
            if (get?.Data == null)
                return new JsonResult(new { success = false, message = "Không tìm thấy chi tiêu." });

            var dto = get.Data;
            dto.GhiChu = req.GhiChu ?? dto.GhiChu;

            var res = await client.PutAsJsonAsync($"/api/ChiTieuHangNgay/{req.Id}", dto);
            var result = await res.Content.ReadFromJsonAsync<Result<ChiTieuHangNgayDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? "Không thể cập nhật."
            });
        }

        // ❌ Xoá chi tiêu
        public class DeleteRequest
        {
            public Guid Id { get; set; }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteRequest req)
        {
            var client = _factory.CreateClient("Api");
            var res = await client.DeleteAsync($"/api/ChiTieuHangNgay/{req.Id}");
            var result = await res.Content.ReadFromJsonAsync<Result<ChiTieuHangNgayDto>>();

            return new JsonResult(new
            {
                success = result?.IsSuccess ?? false,
                message = result?.Message ?? (result?.IsSuccess == true ? "✅ Đã xoá chi tiêu." : "Không thể xoá.")
            });
        }
    }
}