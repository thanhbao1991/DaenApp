using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ThongKeController : ControllerBase
    {
        private readonly IThongKeService _service;
        public ThongKeController(IThongKeService service) => _service = service;


        [HttpGet("ngay")]
        public async Task<ActionResult<Result<ThongKeNgayDto>>> GetThongKeNgay(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);
            var dto = await _service.TinhNgayAsync(date);
            return Result<ThongKeNgayDto>.Success(dto);
        }
    }
}