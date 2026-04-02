using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        private static readonly Expression<Func<HoaDonNoDto, HoaDonNoDto>> SelectHoaDonNoDto =
    x => new HoaDonNoDto
    {
        Id = x.Id,
        TenKhachHangText = x.TenKhachHangText ?? "",
        GhiChu = x.GhiChu ?? "",
        GhiChuShipper = x.GhiChuShipper ?? "",
        IsBank = x.IsBank,
        NguoiShip = x.NguoiShip ?? "",
        PhanLoai = x.PhanLoai ?? "",
        NgayNo = x.NgayNo,
        NgayGio = x.NgayGio,
        NgayShip = x.NgayShip,
        NgayIn = x.NgayIn,
        LastModified = x.LastModified,
        ThanhTien = x.ThanhTien,
        DaThu = x.DaThu,
        ConLai = x.ConLai,
        KhachHangId = x.KhachHangId,
        VoucherId = x.VoucherId,
    };







        [HttpGet("get-cong-no")]
        public async Task<ActionResult<Result<List<HoaDonNoDto>>>> GetCongNo()
        {
            try
            {
                var query = await _db.HoaDonNos
                    .AsNoTracking()
                    .Where(x =>
                        x.NgayNo != null &&
                        x.ThanhTien > x.DaThu
                    )
                    .OrderByDescending(x => x.NgayNo)
                    .Select(SelectHoaDonNoDto)
                    .ToListAsync();

                return Result<List<HoaDonNoDto>>.Success(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<List<HoaDonNoDto>>.Failure(ex.ToString()));
            }
        }

        [HttpGet("get-hoa-don")]
        public async Task<ActionResult<Result<List<HoaDonNoDto>>>> GetHoaDon()
        {

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var baseQuery = _db.HoaDonNos.AsNoTracking();

            var todayQuery = baseQuery
                .Where(x =>
                    x.NgayGio >= today &&
                    x.NgayGio < tomorrow
                );

            var oldQuery = baseQuery
                .Where(x =>
                    x.NgayGio < today &&
                    x.ThanhTien > x.DaThu &&
                    x.NgayNo == null
                );

            var query = await todayQuery
                .Union(oldQuery)
                .OrderByDescending(x => x.NgayGio)
                .Select(SelectHoaDonNoDto)
                .ToListAsync();

            return Result<List<HoaDonNoDto>>.Success(query);

        }

        [HttpGet("get-khach-hang-info/{khachHangId}")]
        public async Task<ActionResult<KhachHangInfoDto>> GetKhachHangInÌnfo(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var now = DateTime.Now;

            var kh = await _db.KhachHangs
                .AsNoTracking()
                .Where(x => x.Id == khachHangId)
                .Select(x => new
                {
                    x.Id,
                    x.DuocNhanVoucher,
                    x.FavoriteMon
                })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            if (kh == null)
                return NotFound("Không tìm thấy khách hàng.");

            int diemThangNay = -1;
            int diemThangTruoc = -1;

            if (kh.DuocNhanVoucher)
            {
                var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
                var firstDayPrev = firstDayCurrent.AddMonths(-1);
                var firstDayNext = firstDayCurrent.AddMonths(1);

                var agg = await _db.ChiTietHoaDonPoints
                    .AsNoTracking()
                    .Where(p =>
                        !p.IsDeleted &&
                        p.KhachHangId == khachHangId &&
                        p.Ngay >= firstDayPrev &&
                        p.Ngay < firstDayNext)
                    .GroupBy(p => p.Ngay >= firstDayCurrent ? 1 : 0)
                    .Select(g => new
                    {
                        IsCurrent = g.Key == 1,
                        Sum = g.Sum(p => (int?)p.DiemThayDoi) ?? 0
                    })
                    .ToListAsync(HttpContext.RequestAborted);

                diemThangNay = agg.Where(x => x.IsCurrent).Select(x => x.Sum).FirstOrDefault();
                diemThangTruoc = agg.Where(x => !x.IsCurrent).Select(x => x.Sum).FirstOrDefault();
            }

            var firstDayCurrent2 = new DateTime(now.Year, now.Month, 1);
            var firstDayNext2 = firstDayCurrent2.AddMonths(1);

            bool daNhanVoucher = await (
                from v in _db.ChiTietHoaDonVouchers.AsNoTracking()
                join hd in _db.HoaDons.AsNoTracking() on v.HoaDonId equals hd.Id
                where hd.KhachHangId == khachHangId
                      && !hd.IsDeleted
                      && !v.IsDeleted
                      && v.LastModified >= firstDayCurrent2
                      && v.LastModified < firstDayNext2
                select v.Id
            ).AnyAsync(HttpContext.RequestAborted);

            var tongNo = await _db.HoaDonNos
                .Where(x =>
                    x.KhachHangId == khachHangId &&
                    x.NgayNo != null &&
                    x.ConLai > 0)
                .SumAsync(x => (decimal?)x.ConLai, HttpContext.RequestAborted) ?? 0;

            var donKhac = await _db.HoaDonNos
                .Where(x =>
                    x.KhachHangId == khachHangId &&
                    x.ConLai > 0 &&
                    x.NgayNo == null)
                .SumAsync(x => (decimal?)x.ConLai, HttpContext.RequestAborted) ?? 0;

            return new KhachHangInfoDto
            {
                KhachHangId = kh.Id,
                DuocNhanVoucher = kh.DuocNhanVoucher,
                DaNhanVoucher = daNhanVoucher,
                DiemThangNay = diemThangNay,
                DiemThangTruoc = diemThangTruoc,
                TongNo = tongNo,
                DonKhac = donKhac,
                MonYeuThich = kh.FavoriteMon
            };
        }

        [HttpGet("get-chi-tieu-hang-ngay")]
        public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> GetChiTieuHangNgay(
    [FromQuery] int thang,
    [FromQuery] int nam)
        {
            try
            {
                if (thang < 1 || thang > 12)
                    return BadRequest(Result<List<ChiTieuHangNgayDto>>.Failure("Tháng không hợp lệ"));

                var start = new DateTime(nam, thang, 1);
                var end = start.AddMonths(1);

                var query = await _db.ChiTieuHangNgays
                    .AsNoTracking()
                    .Where(x =>
                        x.Ngay >= start &&
                        x.Ngay < end &&
                        !x.IsDeleted
                    )
                    .OrderByDescending(x => x.NgayGio)
                    .Select(x => new ChiTieuHangNgayDto
                    {
                        Id = x.Id,
                        SoLuong = x.SoLuong,
                        DonGia = x.DonGia,
                        ThanhTien = x.ThanhTien,
                        BillThang = x.BillThang,
                        Ten = x.Ten,
                        GhiChu = x.GhiChu,
                        Ngay = x.Ngay,
                        NgayGio = x.NgayGio,
                        NguyenLieuId = x.NguyenLieuId,
                    })
                    .ToListAsync();

                return Result<List<ChiTieuHangNgayDto>>.Success(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<List<ChiTieuHangNgayDto>>.Failure(ex.ToString()));
            }
        }
    }
}