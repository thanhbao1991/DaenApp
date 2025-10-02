using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
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

            // ====== DOANH THU & PHÂN RÃ (theo PhanLoai) ======
            decimal doanhThu = await hoaDonQ.SumAsync(x => (decimal?)x.ThanhTien) ?? 0m;

            var doanhThuChiTiet = await hoaDonQ
                .GroupBy(h => h.PhanLoai ?? "(khác)")
                .Select(g => new LabelValueDto
                {
                    Ten = g.Key,
                    GiaTri = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // ====== ĐÃ THU / CHƯA THU ======
            var daThuQ = thanhToanQ
    .Where(t => t.LoaiThanhToan == "Trong ngày" || t.LoaiThanhToan == "Trả nợ trong ngày");
            decimal daThu = await daThuQ.SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal ttTienMat_Khanh = await daThuQ
                .Where(t => t.TenPhuongThucThanhToan == "Tiền mặt" && t.GhiChu == "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal ttTienMat_KhongKhanh = await daThuQ
                .Where(t => t.TenPhuongThucThanhToan == "Tiền mặt" && t.GhiChu != "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal ttBanking = await daThuQ
                .Where(t => t.TenPhuongThucThanhToan != "Tiền mặt"
                )
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            var DaThuChiTiet = new List<LabelValueDto>
            {
                new LabelValueDto { Ten = "Tiền mặt", GiaTri = ttTienMat_KhongKhanh },
                new LabelValueDto { Ten = "Tiền shipper", GiaTri = ttTienMat_Khanh },
                new LabelValueDto { Ten = "Chuyển khoản", GiaTri = ttBanking }
            };

            // ====== CHƯA THU (chỉ các hoá đơn CHƯA ghi nợ) ======
            var chuaThuList = await (
               from h in hoaDonQ
               where h.ConLai > 0 && !h.HasDebt           // ✅ đơn chưa thu và chưa ghi nợ
               select new
               {
                   h.Id,
                   h.KhachHangId,
                   h.TenKhachHangText,
                   h.TenBan,
                   ConLai = h.ConLai                      // ✅ dùng cột
               }
           ).ToListAsync();

            decimal chuaThu = chuaThuList.Sum(x => x.ConLai);

            var chuaThuChiTiet = chuaThuList
                .GroupBy(x => x.KhachHangId ?? Guid.Empty)
                .Select(g => new LabelValueDto
                {
                    Ten = g.First().KhachHangId != null
                            ? (g.First().TenKhachHangText ?? "(không tên)")
                            : (g.First().TenBan ?? "(không tên)"),
                    GiaTri = g.Sum(x => x.ConLai)
                })
                .OrderByDescending(x => x.GiaTri)
                .ToList();

            // ====== CHI TIÊU ======
            decimal chiTieu = await chiTieuQ
                .Where(c => !c.BillThang)
                .SumAsync(c => (decimal?)c.ThanhTien) ?? 0m;

            var chiTieuChiTiet = await chiTieuQ
                .Where(c => !c.BillThang)
                .GroupBy(c => c.Ten ?? "(khác)")
                .Select(g => new LabelValueDto { Ten = g.Key, GiaTri = g.Sum(x => x.ThanhTien) })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // ====== CÔNG NỢ (ghi nợ phát sinh trong ngày) — DÙNG SoTienConLai ======
            decimal congNo = await noQ.SumAsync(n => (decimal?)n.SoTienConLai) ?? 0m;

            var congNoChiTiet = await noQ
                .GroupBy(n => n.KhachHangId ?? Guid.Empty)
                .Select(g => new LabelValueDto
                {
                    Ten = g.First().KhachHangId != null
                        ? (g.First().HoaDon.TenKhachHangText ?? "(không tên)")
                        : (g.First().HoaDon.TenBan ?? "(không tên)"),
                    GiaTri = g.Sum(x => x.SoTienConLai)
                })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // ====== TRẢ NỢ ======
            var traNoQ = thanhToanQ.Where(t => t.LoaiThanhToan == "Trả nợ qua ngày");

            decimal traNoTien = await traNoQ
                .Where(t => t.TenPhuongThucThanhToan == "Tiền mặt")
                .Where(t => t.GhiChu != "Shipper")

                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal traNoBank = await traNoQ
                .Where(t => t.TenPhuongThucThanhToan != "Tiền mặt")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal traNoKhanh = await traNoQ
                .Where(t => t.GhiChu == "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            // Chi tiết Trả nợ Tiền mặt
            var traNoTienChiTiet = await (
                from t in traNoQ.Where(x => x.TenPhuongThucThanhToan == "Tiền mặt" &&
                x.GhiChu != "Shipper")
                join h in _db.HoaDons.AsNoTracking() on t.HoaDonId equals h.Id
                group t by (h.KhachHangId ?? Guid.Empty) into g
                select new LabelValueDto
                {
                    Ten = g.First().HoaDon.KhachHangId != null
                        ? (g.First().HoaDon.TenKhachHangText ?? "(không tên)")
                        : (g.First().HoaDon.TenBan ?? "(không tên)"),
                    GiaTri = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // Chi tiết Trả nợ Bank
            var traNoBankChiTiet = await (
                from t in traNoQ.Where(x => x.TenPhuongThucThanhToan != "Tiền mặt")
                join h in _db.HoaDons.AsNoTracking() on t.HoaDonId equals h.Id
                group t by (h.KhachHangId ?? Guid.Empty) into g
                select new LabelValueDto
                {
                    Ten = g.First().HoaDon.KhachHangId != null
                        ? (g.First().HoaDon.TenKhachHangText ?? "(không tên)")
                        : (g.First().HoaDon.TenBan ?? "(không tên)"),
                    GiaTri = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // ====== TỔNG SỐ ĐƠN / TỔNG SỐ LY ======
            int tongSoDon = await hoaDonQ.CountAsync();

            var tongSoLy = await chiTietQuery
               .Include(ct => ct.SanPhamBienThe)
                   .ThenInclude(bt => bt.SanPham)
                       .ThenInclude(sp => sp.NhomSanPham)
               .Where(ct => ct.SanPhamBienThe != null &&
                            ct.SanPhamBienThe.SanPham != null &&
                            ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Thuốc lá" &&
                            ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Ăn vặt" &&
                            ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Nước lon"
                   )
               .SumAsync(ct => (int?)ct.SoLuong) ?? 0;

            // ====== TOP SẢN PHẨM BÁN CHẠY ======
            var tempTop = await chiTietQuery
                .GroupBy(c => c.TenSanPham)
                .Select(g => new
                {
                    TenSanPham = g.Key ?? "",
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.SoLuong)
                .ThenByDescending(x => x.DoanhThu)
                .Take(50)
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
            decimal mangVe = ttTienMat_KhongKhanh - chiTieu;

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

                DoanhThuChiTiet = doanhThuChiTiet,
                ChiTieuChiTiet = chiTieuChiTiet,
                CongNoChiTiet = congNoChiTiet,
                TraNoTienChiTiet = traNoTienChiTiet,
                TraNoBankChiTiet = traNoBankChiTiet,
                ChuaThuChiTiet = chuaThuChiTiet,
                DaThuChiTiet = DaThuChiTiet,
                TopSanPhams = topSanPhams
            };
        }

    }
}