using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DoanhThuController : ControllerBase
{
    private readonly IDoanhThuService _service;
    public DoanhThuController(IDoanhThuService service)
    {
        _service = service;
    }

    [HttpGet("chitiet")]
    public async Task<ActionResult<Result<List<DoanhThuChiTietHoaDonDto>>>> GetChiTietHoaDon(Guid hoaDonId)
    {
        var dto = await _service.GetChiTietHoaDonAsync(hoaDonId);
        return Result<List<DoanhThuChiTietHoaDonDto>>.Success(dto);
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

    // Danh sách hóa đơn của 1 khách
    [HttpGet("danhsach")]
    public async Task<ActionResult<Result<List<DoanhThuHoaDonDto>>>> GetDanhSachHoaDon(Guid khachHangId)
    {
        var dto = await _service.GetHoaDonKhachHangAsync(khachHangId);
        return Result<List<DoanhThuHoaDonDto>>.Success(dto);
    }

    // Tổng hợp THEO GIỜ trong THÁNG (giữ route cũ, trả thêm DoanhThu)
    // GET /api/DoanhThu/thang-by-hour?thang=10&nam=2025&startHour=6&endHour=22
    [HttpGet("thang-by-hour")]
    public async Task<ActionResult<Result<List<DoanhThuHourBucketDto>>>> GetThangByHour(
        int thang, int nam, int startHour = 6, int endHour = 22)
    {
        var dto = await _service.GetSoDonTheoGioTrongThangAsync(thang, nam, startHour, endHour);
        return Result<List<DoanhThuHourBucketDto>>.Success(dto);
    }
}