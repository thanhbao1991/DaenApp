using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoanhThuController : ControllerBase
{
    private readonly IDoanhThuService _service;
    public DoanhThuController(IDoanhThuService service)
    {
        _service = service;
    }

    [HttpGet("ngay")]
    public async Task<ActionResult<Result<DoanhThuNgayDto>>> GetDoanhThuNgay(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var dto = await _service.GetDoanhThuNgayAsync(date);
        return Result<DoanhThuNgayDto>.Success(dto);
    }

    [HttpGet("thang")]
    public async Task<ActionResult<Result<List<DoanhThuThangItemDto>>>> GetDoanhThuThang(int thang, int nam)
    {
        var dto = await _service.GetDoanhThuThangAsync(thang, nam);
        return Result<List<DoanhThuThangItemDto>>.Success(dto);
    }
}