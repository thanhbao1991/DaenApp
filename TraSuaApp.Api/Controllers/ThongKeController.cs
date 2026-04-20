using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ThongKeController : ControllerBase
{
    private readonly AppDbContext _db;

    public ThongKeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("chi-tieu-ngay")]
    public async Task<ActionResult<Result<ThongKeChiTieuDto>>> GetThongKeChiTieuNgay(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var baseQuery = _db.ChiTieuHangNgays
            .AsNoTracking()
           ;

        var startNgay = date.Date;
        var endNgay = startNgay.AddDays(1);

        var chiTieuNgayQ = baseQuery
            .Where(x =>
                !x.BillThang &&
                x.Ngay >= startNgay &&
                x.Ngay < endNgay)
            .OrderBy(x => x.NgayGio);

        var chiTieuNgay = await chiTieuNgayQ
            .SumAsync(x => (decimal?)x.ThanhTien) ?? 0;

        var danhSachNgay = await chiTieuNgayQ
            .GroupBy(x => x.Ten ?? "(khác)")
            .Select(g => new ChiTieuItemDto
            {
                Ten = g.Key,
                SoTien = g.Sum(x => x.ThanhTien)
            })
            .OrderByDescending(x => x.SoTien)
            .ToListAsync();

        var chiTieuThangQ = baseQuery
            .Where(x =>
                x.BillThang &&
                x.Ngay >= startNgay &&
                x.Ngay < endNgay)
            .OrderBy(x => x.NgayGio);

        var chiTieuThang = await chiTieuThangQ
            .SumAsync(x => (decimal?)x.ThanhTien) ?? 0;

        var danhSachThang = await chiTieuThangQ
            .GroupBy(x => x.Ten ?? "(khác)")
            .Select(g => new ChiTieuItemDto
            {
                Ten = g.Key,
                SoTien = g.Sum(x => x.ThanhTien)
            })
            .OrderByDescending(x => x.SoTien)
            .ToListAsync();

        return Result<ThongKeChiTieuDto>.Success(new ThongKeChiTieuDto
        {
            TongChiTieu = chiTieuNgay + chiTieuThang,
            ChiTieuNgay = chiTieuNgay,
            DanhSachChiTieuNgay = danhSachNgay,
            ChiTieuThang = chiTieuThang,
            DanhSachChiTieuThang = danhSachThang
        });
    }

    [HttpGet("cong-no-ngay")]
    public async Task<ActionResult<Result<ThongKeCongNoDto>>> GetThongKeCongNoNgay(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var start = date.Date;
        var end = start.AddDays(1);

        var data = await _db.HoaDonNos
            .AsNoTracking()
            .Where(x => x.NgayNo >= start && x.NgayNo < end)
            .OrderBy(x => x.NgayNo)
            .Select(x => new
            {
                Ten = x.TenKhachHangText ?? "",
                x.ConLai
            })
            .ToListAsync();

        var result = new ThongKeCongNoDto
        {
            TongCongNoNgay = data.Sum(x => x.ConLai),
            DanhSachCongNoNgay = data
                .GroupBy(x => x.Ten)
                .Select(g => new CongNoItemDto
                {
                    TenKhachHang = g.Key,
                    SoTienNo = g.Sum(x => x.ConLai)
                })
                .OrderByDescending(x => x.SoTienNo)
                .ToList()
        };

        return Result<ThongKeCongNoDto>.Success(result);
    }

    [HttpGet("thanh-toan-ngay")]
    public async Task<ActionResult<Result<ThongKeThanhToanDto>>> GetThanhToanNgay(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var start = date.Date;
        var end = start.AddDays(1);

        var data = await _db.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x =>

                x.NgayGio >= start &&
                x.NgayGio < end)
            .Select(x => new
            {
                x.PhuongThucThanhToanId,
                x.LoaiThanhToan,
                x.GhiChu,
                x.SoTien
            })
            .ToListAsync();

        var donChuaThanhToan = await _db.HoaDonNos
            .AsNoTracking()
            .Where(x =>
                x.ConLai > 0 &&
                x.NgayNo == null &&
                x.NgayGio >= start &&
                x.NgayGio < end)
            .SumAsync(x => (decimal?)x.ConLai) ?? 0;

        var tongChuyenKhoan = data
            .Where(x => x.PhuongThucThanhToanId == AppConstants.ChuyenKhoanId)
            .Sum(x => x.SoTien);

        var tienMat = data
            .Where(x => x.PhuongThucThanhToanId == AppConstants.TienMatId)
            .ToList();

        var traNoTaiQuan = tienMat
            .Where(x => x.LoaiThanhToan == "Trả nợ qua ngày" && x.GhiChu != "Shipper")
            .Sum(x => x.SoTien);

        var traNoQuaShipper = tienMat
            .Where(x => x.LoaiThanhToan == "Trả nợ qua ngày" && x.GhiChu == "Shipper")
            .Sum(x => x.SoTien);

        var thuTaiQuan = tienMat
            .Where(x => x.LoaiThanhToan == "Thanh toán")
            .Sum(x => x.SoTien);

        var thuQuaShipper = tienMat
            .Where(x => x.LoaiThanhToan == "Trong ngày" && x.GhiChu == "Shipper")
            .Sum(x => x.SoTien);

        var danhSachTienMat = new List<ThanhToanItemDto>
        {
            new() { Ten = "Tiền mặt Nhã", SoTien = thuTaiQuan },
            new() { Ten = "Trả nợ Nhã ", SoTien = traNoTaiQuan },
            new() { Ten = "Tiền mặt Khánh", SoTien = thuQuaShipper },
            new() { Ten = "Trả nợ Khánh", SoTien = traNoQuaShipper },
        };

        var result = new ThongKeThanhToanDto
        {
            TongTienMat = danhSachTienMat.Sum(x => x.SoTien),
            TongChuyenKhoan = tongChuyenKhoan,
            DanhSachTienMat = danhSachTienMat,
        };

        return Result<ThongKeThanhToanDto>.Success(result);
    }

    [HttpGet("doanh-thu-ngay")]
    public async Task<ActionResult<Result<ThongKeDoanhThuNgayDto>>> GetDoanhThuNgay(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var start = date.Date;
        var end = start.AddDays(1);

        var data = await _db.HoaDonNos
            .AsNoTracking()
            .Where(x =>
                x.NgayGio >= start &&
                x.NgayGio < end)
            .Select(x => new
            {
                Ten = x.PhanLoai == "Mv"
                    ? (x.VoucherId == AppConstants.VoucherIdMuaHo
                        ? "Mua hộ"
                        : "Mua về")
                    : (x.PhanLoai ?? "(khác)"),
                x.ThanhTien
            })
            .ToListAsync();

        var danhSach = data
            .GroupBy(x => x.Ten)
            .Select(g => new DoanhThuTheoLoaiItemDto
            {
                Ten = g.Key,
                DoanhThu = g.Sum(x => x.ThanhTien)
            })
            .OrderByDescending(x => x.DoanhThu)
            .ToList();

        var result = new ThongKeDoanhThuNgayDto
        {
            TongDoanhThu = data.Sum(x => x.ThanhTien),
            DanhSach = danhSach
        };

        return Result<ThongKeDoanhThuNgayDto>.Success(result);
    }

    [HttpGet("tra-no-ngay")]
    public async Task<ActionResult<Result<ThongKeTraNoNgayDto>>> GetTraNoNgay(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var start = date.Date;
        var end = start.AddDays(1);

        var data = await _db.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x =>

                x.LoaiThanhToan == "Trả nợ qua ngày" &&
                x.PhuongThucThanhToanId == AppConstants.TienMatId &&
                x.NgayGio >= start &&
                x.NgayGio < end)
            .Select(x => new
            {
                Ten = x.HoaDon.TenKhachHangText ?? "(khách)",
                x.GhiChu,
                x.SoTien
            })
            .ToListAsync();

        var taiQuan = data
            .Where(x => x.GhiChu != "Shipper")
            .GroupBy(x => x.Ten)
            .Select(g => new KhachTraNoItemDto
            {
                TenKhachHang = g.Key,
                SoTien = g.Sum(x => x.SoTien)
            })
            .OrderByDescending(x => x.SoTien)
            .ToList();

        var shipper = data
            .Where(x => x.GhiChu == "Shipper")
            .GroupBy(x => x.Ten)
            .Select(g => new KhachTraNoItemDto
            {
                TenKhachHang = g.Key,
                SoTien = g.Sum(x => x.SoTien)
            })
            .OrderByDescending(x => x.SoTien)
            .ToList();

        var result = new ThongKeTraNoNgayDto
        {
            TongTraNoTaiQuan = taiQuan.Sum(x => x.SoTien),
            TongTraNoShipper = shipper.Sum(x => x.SoTien),
            TraNoTaiQuan = taiQuan,
            TraNoShipper = shipper
        };

        return Result<ThongKeTraNoNgayDto>.Success(result);
    }

    [HttpGet("don-chua-thanh-toan")]
    public async Task<ActionResult<Result<ThongKeDonChuaThanhToanDto>>> GetDonChuaThanhToan(int ngay, int thang, int nam)
    {
        var date = new DateTime(nam, thang, ngay);
        var start = date.Date;
        var end = start.AddDays(1);

        var data = await _db.HoaDonNos
            .AsNoTracking()
            .Where(x =>
                x.ConLai > 0 &&
                x.NgayNo == null &&
                x.NgayGio >= start &&
                x.NgayGio < end)
            .Select(x => new
            {
                Ten = x.TenKhachHangText ?? "(khách)",
                x.ConLai
            })
            .ToListAsync();

        var result = new ThongKeDonChuaThanhToanDto
        {
            TongChuaThanhToan = data.Sum(x => x.ConLai),
            DanhSach = data
                .GroupBy(x => x.Ten)
                .Select(g => new DonChuaThanhToanItemDto
                {
                    TenKhachHang = g.Key,
                    SoTien = g.Sum(x => x.ConLai)
                })
                .OrderByDescending(x => x.SoTien)
                .ToList()
        };

        return Result<ThongKeDonChuaThanhToanDto>.Success(result);
    }
}