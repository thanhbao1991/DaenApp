using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services
{
    public class ThongKeService : IThongKeService
    {
        private readonly AppDbContext _db;
        public ThongKeService(AppDbContext db) => _db = db;

        public async Task<ThongKeNgayDto> TinhNgayAsync(DateTime ngay)
        {
            var start = ngay;
            var end = start.AddDays(1);

            // ====== QUERY CƠ SỞ ======
            var hoaDonQ = _db.HoaDons.AsNoTracking()
                             .Where(h => !h.IsDeleted && h.Ngay >= start && h.Ngay < end);

            // 🟟 Lấy chi tiết theo NGÀY HÓA ĐƠN (giống DashboardController)
            var chiTietQuery = _db.ChiTietHoaDons.AsNoTracking()
                                 .Where(c => !c.IsDeleted
                                          && !c.HoaDon.IsDeleted
                                          && c.HoaDon.Ngay >= start
                                          && c.HoaDon.Ngay < end);

            var thanhToanQ = _db.ChiTietHoaDonThanhToans.AsNoTracking()
                              .Where(t => !t.IsDeleted && t.Ngay >= start && t.Ngay < end);

            var noQ = _db.ChiTietHoaDonNos.AsNoTracking()
                         .Where(n => !n.IsDeleted && n.Ngay >= start && n.Ngay < end);

            var chiTieuQ = _db.ChiTieuHangNgays.AsNoTracking()
                           .Where(c => !c.IsDeleted && c.Ngay >= start && c.Ngay < end);

            // ====== DOANH THU & PHÂN RÃ (Ship / Tại chỗ / App) ======
            decimal doanhThu = await hoaDonQ.SumAsync(x => (decimal?)x.ThanhTien) ?? 0m;
            decimal ship = await hoaDonQ.Where(h => h.PhanLoai == "Ship").SumAsync(h => (decimal?)h.ThanhTien) ?? 0m;
            decimal app = await hoaDonQ.Where(h => h.PhanLoai == "App").SumAsync(h => (decimal?)h.ThanhTien) ?? 0m;
            decimal taiCho = doanhThu - ship - app;

            // ====== ĐÃ THU / CHƯA THU ======
            decimal daThu = await thanhToanQ.SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            // tách theo phương thức + "Khánh" (ghi chú Shipper)
            decimal ttTienMat_All = await thanhToanQ.Where(t => t.TenPhuongThucThanhToan == "Tiền mặt")
                                                      .SumAsync(t => (decimal?)t.SoTien) ?? 0m;
            decimal ttTienMat_Khanh = await thanhToanQ.Where(t => t.TenPhuongThucThanhToan == "Tiền mặt" && t.GhiChu == "Shipper")
                                                      .SumAsync(t => (decimal?)t.SoTien) ?? 0m;
            decimal ttTienMat_KhongKhanh = ttTienMat_All - ttTienMat_Khanh;

            decimal ttBanking = await thanhToanQ
                                .Where(t => t.TenPhuongThucThanhToan == "Chuyển khoản" || t.TenPhuongThucThanhToan == "Banking Nhã")
                                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal chuaThu = doanhThu - daThu;

            // ====== CHI TIÊU ======
            decimal chiTieu = await chiTieuQ.SumAsync(c => (decimal?)c.ThanhTien) ?? 0m;
            var chiTieuChiTiet = await chiTieuQ
                .GroupBy(c => c.Ten ?? "(khác)")
                .Select(g => new LabelValueDto { Ten = g.Key, GiaTri = g.Sum(x => x.ThanhTien) })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // ====== CÔNG NỢ (ghi nợ tạo trong ngày) ======
            decimal congNo = await noQ.SumAsync(n => (decimal?)n.SoTienNo) ?? 0m;
            var congNoChiTiet = await
                (from n in noQ
                 join kh in _db.KhachHangs.AsNoTracking() on n.KhachHangId equals kh.Id into khj
                 from kh in khj.DefaultIfEmpty()
                 orderby n.SoTienNo descending
                 select new LabelValueDto
                 {
                     Ten = (kh != null ? kh.Ten : null) ?? "(không tên)",
                     GiaTri = n.SoTienNo
                 }).ToListAsync();

            // ====== TRẢ NỢ ======
            var traNoQ = thanhToanQ.Where(t => t.LoaiThanhToan.Contains("Trả nợ"));
            decimal traNoTien = await traNoQ.Where(t => t.TenPhuongThucThanhToan == "Tiền mặt").SumAsync(t => (decimal?)t.SoTien) ?? 0m;
            decimal traNoBank = await traNoQ.Where(t => t.TenPhuongThucThanhToan != "Tiền mặt").SumAsync(t => (decimal?)t.SoTien) ?? 0m;
            decimal traNoKhanh = await traNoQ.Where(t => t.GhiChu == "Shipper").SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            var traNoTienChiTiet = await
                (from t in traNoQ.Where(x => x.TenPhuongThucThanhToan == "Tiền mặt")
                 join h in _db.HoaDons.AsNoTracking() on t.HoaDonId equals h.Id
                 join kh in _db.KhachHangs.AsNoTracking() on h.KhachHangId equals kh.Id into khj
                 from kh in khj.DefaultIfEmpty()
                 group t by (kh != null ? kh.Ten : h.TenKhachHangText) into g
                 select new LabelValueDto { Ten = g.Key ?? "(không tên)", GiaTri = g.Sum(x => x.SoTien) })
                .OrderByDescending(x => x.GiaTri).ToListAsync();

            var traNoBankChiTiet = await
                (from t in traNoQ.Where(x => x.TenPhuongThucThanhToan != "Tiền mặt")
                 join h in _db.HoaDons.AsNoTracking() on t.HoaDonId equals h.Id
                 join kh in _db.KhachHangs.AsNoTracking() on h.KhachHangId equals kh.Id into khj
                 from kh in khj.DefaultIfEmpty()
                 group t by (kh != null ? kh.Ten : h.TenKhachHangText) into g
                 select new LabelValueDto { Ten = g.Key ?? "(không tên)", GiaTri = g.Sum(x => x.SoTien) })
                .OrderByDescending(x => x.GiaTri).ToListAsync();

            // ====== TỔNG SỐ ĐƠN / TỔNG SỐ LY ======
            int tongSoDon = await hoaDonQ.CountAsync();
            int tongSoLy = (int)((await chiTietQuery.SumAsync(c => (decimal?)c.SoLuong) ?? 0m));

            // ====== TOP SẢN PHẨM BÁN CHẠY (chuẩn theo DashboardController) ======
            var tempTop = await chiTietQuery
                .GroupBy(c => new { c.TenSanPham, Ngay = c.HoaDon.Ngay.Date })
                .Select(g => new
                {
                    Ngay = g.Key.Ngay,
                    TenSanPham = g.Key.TenSanPham ?? "",
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToListAsync();

            var topSanPhams = tempTop
                .Select((x, i) => new TopSanPhamDto
                {
                    Stt = i + 1,
                    TenSanPham = x.TenSanPham,
                    SoLuong = x.SoLuong,
                    DoanhThu = x.DoanhThu
                })
                .ToList();

            // ====== MANG VỀ (theo công thức cũ) ======
            decimal mangVe = doanhThu - chiTieu - congNo - ttBanking - ttTienMat_Khanh;

            // ====== DTO ======
            return new ThongKeNgayDto
            {
                Ngay = start,

                DoanhThu = doanhThu,
                DaThu = daThu,
                DaThu_TienMat = ttTienMat_KhongKhanh,
                DaThu_Banking = ttBanking,
                DaThu_Khanh = ttTienMat_Khanh,
                ChuaThu = chuaThu,
                ChiTieu = chiTieu,
                CongNo = congNo,
                MangVe = mangVe,
                TraNoTien = traNoTien,
                TraNoKhanh = traNoKhanh,
                TraNoBank = traNoBank,
                TongSoDon = tongSoDon,
                TongSoLy = tongSoLy,

                DoanhThuChiTiet = new List<LabelValueDto>
                {
                    new LabelValueDto { Ten = "Ship",    GiaTri = ship },
                    new LabelValueDto { Ten = "Tại chỗ", GiaTri = taiCho },
                    new LabelValueDto { Ten = "App",     GiaTri = app }
                },
                ChiTieuChiTiet = chiTieuChiTiet,
                CongNoChiTiet = congNoChiTiet,
                TraNoTienChiTiet = traNoTienChiTiet,
                TraNoBankChiTiet = traNoBankChiTiet,
                DaThuChiTiet = new List<LabelValueDto>
                {
                    new LabelValueDto { Ten = "Tiền mặt",     GiaTri = ttTienMat_All },
                    new LabelValueDto { Ten = "Chuyển khoản", GiaTri = ttBanking }
                },
                TopSanPhams = topSanPhams
            };
        }


    }
}