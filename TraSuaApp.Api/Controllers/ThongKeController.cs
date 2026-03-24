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


        [HttpGet("chi-tieu-ngay")]
        public async Task<ActionResult<Result<ThongKeChiTieuDto>>> GetThongKeChiTieuNgay(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);
            var dto = await _service.TinhChiTieuNgayAsync(date);
            return Result<ThongKeChiTieuDto>.Success(dto);
        }

        [HttpGet("cong-no-ngay")]
        public async Task<ActionResult<Result<ThongKeCongNoDto>>> GetThongKeCongNoNgay(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);
            var dto = await _service.TinhCongNoNgayAsync(date);
            return Result<ThongKeCongNoDto>.Success(dto);
        }

        [HttpGet("thanh-toan-ngay")]
        public async Task<ActionResult<Result<ThongKeThanhToanDto>>> GetThanhToanNgay(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);

            var dto = await _service.TinhThanhToanNgayAsync(date);

            return Result<ThongKeThanhToanDto>.Success(dto);
        }

        [HttpGet("doanh-thu-ngay")]
        public async Task<ActionResult<Result<ThongKeDoanhThuNgayDto>>> GetDoanhThuNgay(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);

            var dto = await _service.TinhDoanhThuNgayAsync(date);

            return Result<ThongKeDoanhThuNgayDto>.Success(dto);
        }

        [HttpGet("tra-no-ngay")]
        public async Task<ActionResult<Result<ThongKeTraNoNgayDto>>> GetTraNoNgay(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);

            var dto = await _service.TinhTraNoNgayAsync(date);

            return Result<ThongKeTraNoNgayDto>.Success(dto);
        }

        [HttpGet("don-chua-thanh-toan")]
        public async Task<ActionResult<Result<ThongKeDonChuaThanhToanDto>>> GetDonChuaThanhToan(int ngay, int thang, int nam)
        {
            var date = new DateTime(nam, thang, ngay);

            var dto = await _service.TinhDonChuaThanhToanAsync(date);

            return Result<ThongKeDonChuaThanhToanDto>.Success(dto);
        }
    }
}