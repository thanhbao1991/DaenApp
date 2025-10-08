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

            return new DashboardDto
            {
                History = history
            };
        }


        [HttpGet("thongtin-khachhang/{khachHangId}")]
        public async Task<ActionResult<KhachHangFavoriteDto>> GetThongTinKhachHang(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var kh = await _db.KhachHangs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == khachHangId);

            if (kh == null) return NotFound("Không tìm thấy khách hàng.");

            // 🟟 Top chi tiết
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);

            //var topChiTiets = await (
            //    from ct in _db.ChiTietHoaDons.AsNoTracking()
            //    join h in _db.HoaDons.AsNoTracking() on ct.HoaDonId equals h.Id
            //    join bt in _db.SanPhamBienThes.AsNoTracking() on ct.SanPhamBienTheId equals bt.Id
            //    join sp in _db.SanPhams.AsNoTracking() on bt.SanPhamId equals sp.Id
            //    where h.KhachHangId == khachHangId
            //          && !h.IsDeleted
            //          && !ct.IsDeleted
            //          && h.NgayGio >= threeMonthsAgo
            //    group ct by new { bt.Id, sp.Ten, bt.TenBienThe, bt.GiaBan } into g
            //    orderby g.Sum(x => x.SoLuong) descending
            //    select new ChiTietHoaDonDto
            //    {
            //        SanPhamIdBienThe = g.Key.Id,
            //        TenSanPham = g.Key.Ten ?? "",
            //        TenBienThe = g.Key.TenBienThe ?? "",
            //        DonGia = g.Key.GiaBan,
            //        SoLuong = 0
            //    }
            //)
            //.Take(2)
            //.ToListAsync();

            // 🟟 Tính điểm thưởng
            (int diemThangNay, int diemThangTruoc) =
                await LoyaltyService.TinhDiemThangAsync(_db, khachHangId, DateTime.Now, kh.DuocNhanVoucher);

            // 🟟 Tính tổng nợ
            var tongNo = await LoyaltyService.TinhTongNoKhachHangAsync(_db, khachHangId);

            // 🟟 Kiểm tra khách hàng đã nhận voucher trong tháng này chưa
            bool daNhanVoucher = kh.DuocNhanVoucher
                ? await LoyaltyService.DaNhanVoucherTrongThangAsync(_db, khachHangId, DateTime.Now)
                : false;

            // 🟟 Trả về DTO gộp
            return new KhachHangFavoriteDto
            {
                KhachHangId = kh.Id,
                DuocNhanVoucher = kh.DuocNhanVoucher,
                DaNhanVoucher = daNhanVoucher,
                DiemThangNay = diemThangNay,
                DiemThangTruoc = diemThangTruoc,
                TongNo = tongNo,
                //TopChiTiets = topChiTiets
            };
        }


        [HttpGet("topmenu-quickorder/{khachHangId}")]
        public async Task<ActionResult<string>> GetTopMenuForQuickOrder(Guid khachHangId)
        {
            if (khachHangId == Guid.Empty)
                return BadRequest("KhachHangId không hợp lệ.");

            var threeMonthsAgo = DateTime.Now.AddMonths(-3);

            // 🟟 Top 20 theo khách trong 3 tháng gần nhất
            var topByCustomer = await (
                from ct in _db.ChiTietHoaDons.AsNoTracking()
                join h in _db.HoaDons.AsNoTracking() on ct.HoaDonId equals h.Id
                join sp in _db.SanPhams.AsNoTracking() on ct.SanPhamId equals sp.Id
                where h.KhachHangId == khachHangId
                      && !h.IsDeleted
                      && !ct.IsDeleted
                      && !sp.IsDeleted
                      && !sp.NgungBan
                      && h.NgayGio >= threeMonthsAgo
                group new { ct, sp } by new { sp.Id, sp.TenKhongVietTat } into g
                orderby g.Sum(x => x.ct.SoLuong) descending
                select new
                {
                    Id = g.Key.Id,
                    Ten = g.Key.TenKhongVietTat ?? "",
                    TongSoLuong = g.Sum(x => x.ct.SoLuong)
                }
            ).Take(20).ToListAsync();


            // 🟟 Chuẩn định dạng cho Engine: "Id<TAB>Tên (normalized)"
            var lines = topByCustomer
                .Select(x => $"{x.Id}\t{(x.Ten)}");

            var text = string.Join("\n", lines);
            return Ok(text);
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