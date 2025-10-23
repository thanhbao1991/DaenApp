using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Services; // 🟟 thêm
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

        // ===== 🟟 XẾP HẠNG SẢN PHẨM (lọc theo năm) =====
        [HttpGet("xephang-sanpham")]
        public async Task<ActionResult<Result<List<SanPhamXepHangDto>>>> GetXepHangSanPham([FromQuery] int? year = null)
        {
            var y = (year is > 0 ? year.Value : DateTime.Today.Year);
            var start = new DateTime(y, 1, 1);
            var end = start.AddYears(1);

            var query = await (
                from ct in _db.ChiTietHoaDons.AsNoTracking()
                join h in _db.HoaDons.AsNoTracking() on ct.HoaDonId equals h.Id
                where !ct.IsDeleted && !h.IsDeleted
                      && h.NgayGio >= start && h.NgayGio < end
                group ct by ct.TenSanPham into g
                orderby g.Sum(x => x.SoLuong) descending
                select new SanPhamXepHangDto
                {
                    TenSanPham = g.Key,
                    TongSoLuong = g.Sum(x => x.SoLuong),
                    TongDoanhThu = g.Sum(x => x.SoLuong * x.DonGia)
                }
            ).ToListAsync();

            return Result<List<SanPhamXepHangDto>>.Success(query);
        }

        // ===== 🟟 XẾP HẠNG KHÁCH HÀNG (tính ở Hóa Đơn.ThanhTien, lọc theo năm) =====
        [HttpGet("xephang-khachhang")]
        public async Task<ActionResult<Result<List<KhachHangXepHangDto>>>> GetXepHangKhachHang([FromQuery] int? year = null)
        {
            var y = (year is > 0 ? year.Value : DateTime.Today.Year);
            var start = new DateTime(y, 1, 1);
            var end = start.AddYears(1);

            var list = await (
                from h in _db.HoaDons.AsNoTracking()
                join k in _db.KhachHangs.AsNoTracking() on h.KhachHangId equals k.Id
                where !h.IsDeleted
                      && h.NgayGio >= start && h.NgayGio < end
                group new { h, k } by new { k.Id, k.Ten } into g
                orderby g.Sum(x => x.h.ThanhTien) descending
                select new KhachHangXepHangDto
                {
                    KhachHangId = g.Key.Id,
                    TenKhachHang = g.Key.Ten ?? "",
                    TongSoDon = g.Select(x => x.h.Id).Distinct().Count(),
                    TongDoanhThu = g.Sum(x => x.h.ThanhTien),
                    LanCuoiMua = g.Max(x => x.h.NgayGio)
                }
            ).ToListAsync();

            return Result<List<KhachHangXepHangDto>>.Success(list);
        }

        // Dashboard 
        [HttpGet("lichsu-khachhang/{khachHangId}")]
        public async Task<ActionResult<DashboardDto>> GetLichSuKhachHang(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var history = await (
                from ct in _db.ChiTietHoaDons.AsNoTracking()
                join h in _db.HoaDons.AsNoTracking() on ct.HoaDonId equals h.Id
                where h.KhachHangId == khachHangId
                      && !h.IsDeleted
                orderby h.NgayGio descending, ct.CreatedAt descending
                select new ChiTietHoaDonDto
                {
                    Id = ct.Id,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    SanPhamIdBienThe = ct.SanPhamBienTheId,
                    SanPhamId = ct.SanPhamId,
                    HoaDonId = ct.HoaDonId,
                    NoteText = ct.NoteText,
                    ToppingText = ct.ToppingText,
                    CreatedAt = ct.CreatedAt,
                    DeletedAt = ct.DeletedAt,
                    IsDeleted = ct.IsDeleted,
                    LastModified = ct.LastModified,
                    TenBienThe = ct.TenBienThe,
                    TenSanPham = ct.TenSanPham,
                    NgayGio = h.NgayGio
                }
            ).ToListAsync();

            return new DashboardDto { History = history };
        }

        //hoadonedit
        [HttpGet("thongtin-khachhang/{khachHangId}")]
        public async Task<ActionResult<KhachHangFavoriteDto>> GetThongTinKhachHang(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var kh = await _db.KhachHangs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == khachHangId);

            if (kh == null) return NotFound("Không tìm thấy khách hàng.");

            (int diemThangNay, int diemThangTruoc) =
                await LoyaltyService.TinhDiemThangAsync(_db, khachHangId, DateTime.Now, kh.DuocNhanVoucher);

            var tongNo = await LoyaltyService.TinhTongNoKhachHangAsync(_db, khachHangId);

            bool daNhanVoucher = kh.DuocNhanVoucher
                ? await LoyaltyService.DaNhanVoucherTrongThangAsync(_db, khachHangId, DateTime.Now)
                : false;


            // ===== Kết quả =====
            return new KhachHangFavoriteDto
            {
                KhachHangId = kh.Id,

                DuocNhanVoucher = kh.DuocNhanVoucher,
                DaNhanVoucher = daNhanVoucher,
                DiemThangNay = diemThangNay,
                DiemThangTruoc = diemThangTruoc,
                TongNo = tongNo,

                // 🟟 Favorite trả về theo Id + tên
                MonYeuThich = kh.FavoriteMon
            };
        }

        // ===== 🟟 CHI TIÊU THEO NGUYÊN LIỆU =====
        [HttpGet("chitieubynguyenlieuid")]
        public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> GetChiTieuByNguyenLieuId(
            [FromQuery] int offset = 0,
            [FromQuery] Guid? nguyenLieuId = null)
        {
            var (start, end) = GetMonthRange(offset);

            var query = _db.ChiTieuHangNgays
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Ngay >= start && x.Ngay < end);

            if (nguyenLieuId != null && nguyenLieuId != Guid.Empty)
                query = query.Where(x => x.NguyenLieuId == nguyenLieuId);

            var list = await query
                .OrderByDescending(x => x.Ngay)
                .Select(x => new ChiTieuHangNgayDto
                {
                    Id = x.Id,
                    Ten = x.Ten,
                    DonGia = x.DonGia,
                    SoLuong = x.SoLuong,
                    GhiChu = x.GhiChu,
                    ThanhTien = x.ThanhTien,
                    Ngay = x.Ngay,
                    NgayGio = x.NgayGio,
                    NguyenLieuId = x.NguyenLieuId,
                    CreatedAt = x.CreatedAt,
                    LastModified = x.LastModified,
                    DeletedAt = x.DeletedAt,
                    IsDeleted = x.IsDeleted
                })
                .ToListAsync();

            return Result<List<ChiTieuHangNgayDto>>.Success(list);
        }

        // ===== 🟟 VOUCHER =====
        [HttpGet("voucher")]
        public async Task<ActionResult<Result<List<VoucherChiTraDto>>>> GetVoucherByOffset(
            [FromQuery] int offset = 0,
            [FromQuery] Guid? voucherId = null)
        {
            var (start, end) = GetMonthRange(offset);

            var query = from v in _db.ChiTietHoaDonVouchers.AsNoTracking()
                        where !v.IsDeleted && v.CreatedAt >= start && v.CreatedAt < end
                        join h0 in _db.HoaDons.AsNoTracking() on v.HoaDonId equals h0.Id into hj
                        from h in hj.DefaultIfEmpty()
                        join k0 in _db.KhachHangs.AsNoTracking() on h.KhachHangId equals k0.Id into kj
                        from k in kj.DefaultIfEmpty()
                        select new { v, h, k };

            if (voucherId != null && voucherId != Guid.Empty)
                query = query.Where(x => x.v.VoucherId == voucherId);

            var list = await query
                .OrderByDescending(x => x.v.CreatedAt)
                .Select(x => new VoucherChiTraDto
                {
                    Id = x.v.Id,
                    Ngay = x.v.CreatedAt,
                    TenVoucher = x.v.TenVoucher ?? "",
                    GiaTriApDung = x.v.GiaTriApDung,
                    HoaDonId = x.v.HoaDonId,
                    VoucherId = x.v.VoucherId,
                    TenKhachHang = (x.h.TenKhachHangText ?? x.k.Ten) ?? ""
                })
                .ToListAsync();

            return Result<List<VoucherChiTraDto>>.Success(list);
        }

        // ===== Helper =====
        private static (DateTime start, DateTime end) GetMonthRange(int offset)
        {
            var today = DateTime.Today;
            var first = new DateTime(today.Year, today.Month, 1);
            var start = first.AddMonths(offset);
            var end = start.AddMonths(1);
            return (start, end);
        }
    }
}