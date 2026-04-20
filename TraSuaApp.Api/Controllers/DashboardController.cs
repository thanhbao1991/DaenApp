using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;

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
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var query = await _db.HoaDonNos
                    .AsNoTracking()
                    .Where(x =>
                        (x.NgayGio >= today && x.NgayGio < tomorrow) ||
                        (x.NgayGio < today &&
                         x.ThanhTien > x.DaThu &&
                         x.NgayNo == null))
                    .OrderByDescending(x => x.NgayGio)
                    .Select(x => new HoaDonNoDto
                    {
                        Id = x.Id,
                        TenKhachHangText = x.TenKhachHangText ?? "",
                        KhachHangId = x.KhachHangId,
                        VoucherId = x.VoucherId,
                        ThanhTien = x.ThanhTien,
                        DaThu = x.DaThu,
                        ConLai = x.ConLai,
                        NgayGio = x.NgayGio,
                        NgayShip = x.NgayShip,
                        NgayNo = x.NgayNo,
                        NgayIn = x.NgayIn,
                        NguoiShip = x.NguoiShip ?? "",
                        GhiChu = x.GhiChu ?? "",
                        GhiChuShipper = x.GhiChuShipper ?? "",
                        PhanLoai = x.PhanLoai ?? "",
                        IsBank = x.IsBank,
                        LastModified = x.LastModified,
                        Stt = x.Stt
                    })
                    .ToListAsync();

                return Result<List<HoaDonNoDto>>.Success(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<List<HoaDonNoDto>>.Failure(ex.ToString()));
            }
        }
        //public async Task<ActionResult<Result<List<HoaDonNoDto>>>> GetHoaDon()
        //{

        //    var today = DateTime.Today;
        //    var tomorrow = today.AddDays(1);

        //    var baseQuery = _db.HoaDonNos.AsNoTracking();

        //    var todayQuery = baseQuery
        //        .Where(x =>
        //            x.NgayGio >= today &&
        //            x.NgayGio < tomorrow
        //        );

        //    var oldQuery = baseQuery
        //        .Where(x =>
        //            x.NgayGio < today &&
        //            x.ThanhTien > x.DaThu &&
        //            x.NgayNo == null
        //        );

        //    var query = await todayQuery
        //        .Union(oldQuery)
        //        .OrderByDescending(x => x.NgayGio)
        //        .Select(SelectHoaDonNoDto)
        //        .ToListAsync();

        //    return Result<List<HoaDonNoDto>>.Success(query);

        //}  

        [HttpGet("get-hoa-don/{id}")]
        public async Task<ActionResult<Result<HoaDonNoDto>>> GetHoaDonNoById(Guid id)
        {
            try
            {
                var item = await _db.HoaDonNos
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(SelectHoaDonNoDto)
                    .FirstOrDefaultAsync();

                if (item == null)
                    return Result<HoaDonNoDto>.Failure("Không tìm thấy hoá đơn.");

                return Result<HoaDonNoDto>.Success(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<HoaDonNoDto>.Failure(ex.ToString()));
            }
        }
        //   [HttpGet("get-khach-hang-info/{khachHangId}")]
        //   public async Task<ActionResult<KhachHangInfoDto>> GetKhachHangInÌnfo(Guid khachHangId)
        //   {
        //       if (khachHangId == Guid.Empty)
        //           return BadRequest("KhachHangId không hợp lệ.");

        //       var now = DateTime.Now;

        //       var kh = await _db.KhachHangs
        //           .AsNoTracking()
        //           .Where(x => x.Id == khachHangId)
        //           .Select(x => new
        //           {
        //               x.Id,
        //               x.DuocNhanVoucher,
        //               x.FavoriteMon
        //           })
        //           .FirstOrDefaultAsync(HttpContext.RequestAborted);

        //       if (kh == null)
        //           return NotFound("Không tìm thấy khách hàng.");

        //       int diemThangNay = -1;
        //       int diemThangTruoc = -1;

        //       if (kh.DuocNhanVoucher)
        //       {
        //           var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
        //           var firstDayPrev = firstDayCurrent.AddMonths(-1);
        //           var firstDayNext = firstDayCurrent.AddMonths(1);

        //           var agg = await _db.HoaDons
        //     .AsNoTracking()
        //     .Where(h =>
        //         !h.IsDeleted &&
        //         h.KhachHangId == khachHangId &&
        //         h.Ngay >= firstDayPrev &&
        //         h.Ngay < firstDayNext
        //     )
        //     .GroupBy(h => h.Ngay >= firstDayCurrent ? 1 : 0)
        //     .Select(g => new
        //     {
        //         IsCurrent = g.Key == 1,
        //         Sum = g.Sum(h => (int?)Math.Floor(h.ThanhTien * 0.01m)) ?? 0
        //     })
        //     .ToListAsync(HttpContext.RequestAborted);

        //           diemThangNay = agg
        //               .Where(x => x.IsCurrent)
        //               .Select(x => x.Sum)
        //               .FirstOrDefault();

        //           diemThangTruoc = agg
        //               .Where(x => !x.IsCurrent)
        //               .Select(x => x.Sum)
        //               .FirstOrDefault();
        //       }

        //       var firstDayCurrent2 = new DateTime(now.Year, now.Month, 1);
        //       var firstDayNext2 = firstDayCurrent2.AddMonths(1);

        //       bool daNhanVoucher = await (
        //    from hd in _db.HoaDons.AsNoTracking()
        //    where hd.KhachHangId == khachHangId
        //          && !hd.IsDeleted
        //          && hd.VoucherId != null
        //          && hd.LastModified >= firstDayCurrent2
        //          && hd.LastModified < firstDayNext2
        //    select hd.Id
        //).AnyAsync(HttpContext.RequestAborted);
        //       var tongNo = await _db.HoaDonNos
        //           .Where(x =>
        //               x.KhachHangId == khachHangId &&
        //               x.NgayNo != null &&
        //               x.ConLai > 0)
        //           .SumAsync(x => (decimal?)x.ConLai, HttpContext.RequestAborted) ?? 0;

        //       var donKhac = await _db.HoaDonNos
        //           .Where(x =>
        //               x.KhachHangId == khachHangId &&
        //               x.ConLai > 0 &&
        //               x.NgayNo == null)
        //           .SumAsync(x => (decimal?)x.ConLai, HttpContext.RequestAborted) ?? 0;

        //       return new KhachHangInfoDto
        //       {
        //           KhachHangId = kh.Id,
        //           DuocNhanVoucher = kh.DuocNhanVoucher,
        //           DaNhanVoucher = daNhanVoucher,
        //           DiemThangNay = diemThangNay,
        //           DiemThangTruoc = diemThangTruoc,
        //           TongNo = tongNo,
        //           DonKhac = donKhac,
        //           MonYeuThich = kh.FavoriteMon
        //       };
        //   }



        //web only
        [HttpGet("get-chi-tieu-hang-ngay")]
        public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> GetChiTieuHangNgay([FromQuery] int thang, [FromQuery] int nam)
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
                        x.Ngay < end

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

        [HttpGet("get-voucher-thang")]
        public async Task<ActionResult<Result<List<VoucherThangDto>>>> GetVoucherThang([FromQuery] int thang, [FromQuery] int nam)
        {
            try
            {
                if (thang < 1 || thang > 12)
                    return BadRequest(Result<List<VoucherThangDto>>.Failure("Tháng không hợp lệ"));

                var start = new DateTime(nam, thang, 1);
                var end = start.AddMonths(1);

                var query = await _db.HoaDons
                    .AsNoTracking()
                    .Where(x =>
                        x.Ngay >= start &&
                        x.Ngay < end &&

                        x.VoucherId != null &&
                        x.VoucherId != AppConstants.VoucherIdMuaHo
                    )
                    .OrderByDescending(x => x.NgayGio)
                    .Select(x => new VoucherThangDto
                    {
                        NgayGio = x.NgayGio,
                        TenKhachHangText = x.TenKhachHangText,
                        GhiChu = x.GhiChu,
                        GiamGia = x.GiamGia
                    })
                    .ToListAsync();

                return Result<List<VoucherThangDto>>.Success(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<List<VoucherThangDto>>.Failure(ex.ToString()));
            }
        }

        [HttpGet("get-chiet-khau-thang")]
        public async Task<ActionResult<Result<List<VoucherThangDto>>>> GetChietKhauThang([FromQuery] int thang, [FromQuery] int nam)
        {
            try
            {
                if (thang < 1 || thang > 12)
                    return BadRequest(Result<List<VoucherThangDto>>.Failure("Tháng không hợp lệ"));

                var start = new DateTime(nam, thang, 1);
                var end = start.AddMonths(1);

                var query = await _db.HoaDons
                    .AsNoTracking()
                    .Where(x =>
                        x.Ngay >= start &&
                        x.Ngay < end &&

                        x.VoucherId != null &&
                        x.VoucherId == AppConstants.VoucherIdMuaHo
                    )
                    .OrderByDescending(x => x.NgayGio)
                    .Select(x => new VoucherThangDto
                    {
                        NgayGio = x.NgayGio,
                        TenKhachHangText = x.TenKhachHangText,
                        GhiChu = x.GhiChu,
                        GiamGia = x.GiamGia
                    })
                    .ToListAsync();

                return Result<List<VoucherThangDto>>.Success(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<List<VoucherThangDto>>.Failure(ex.ToString()));
            }
        }

        [HttpGet("get-chi-tiet-thang")]
        public async Task<ActionResult<Result<List<ChiTietThangDto>>>> GetChiTietThang(
    [FromQuery] int thang,
    [FromQuery] int nam)
        {
            try
            {
                if (thang < 1 || thang > 12)
                    return BadRequest(Result<List<ChiTietThangDto>>.Failure("Tháng không hợp lệ"));

                var start = new DateTime(nam, thang, 1);
                var end = start.AddMonths(1);

                var query = await _db.ChiTietHoaDons
                    .AsNoTracking()
                    .Where(x =>

                        x.HoaDon.Ngay >= start &&
                        x.HoaDon.Ngay < end
                    )
                    .OrderByDescending(x => x.HoaDon.NgayGio)
                    .Select(x => new ChiTietThangDto
                    {
                        NgayGio = x.HoaDon.NgayGio,
                        TenKhachHang = x.HoaDon.TenKhachHangText ?? "Khách lẻ",
                        TenSanPham = x.TenSanPham,
                        TenBienThe = x.NoteText,
                        SoLuong = x.SoLuong
                    })
                    .ToListAsync();

                return Result<List<ChiTietThangDto>>.Success(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<List<ChiTietThangDto>>.Failure(ex.ToString()));
            }
        }
    }
}