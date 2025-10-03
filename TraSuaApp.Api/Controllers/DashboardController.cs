using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Services; // 🟟 thêm
using TraSuaApp.Shared.Dtos;

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

    }
}