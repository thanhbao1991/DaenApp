using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services
{
    public class ThongKeService : IThongKeService
    {
        private readonly AppDbContext _db;
        public ThongKeService(AppDbContext db) => _db = db;
        public Task<ThongKeNgayDto> TinhNgayAsync(DateTime ngay)
        {
            return TinhNgayCoreAsync(ngay, anShipKhanh: false);
        }
        public Task<ThongKeNgayDto> TinhNgay_AnShipKhanhAsync(DateTime ngay)
        {
            return TinhNgayCoreAsync(ngay, anShipKhanh: true);
        }
        private async Task<ThongKeNgayDto> TinhNgayCoreAsync(
            DateTime ngay,
            bool anShipKhanh)
        {
            var start = ngay;
            var end = start.AddDays(1);

            // ====== QUERY HÓA ĐƠN GỐC ======
            var hoaDonQ = _db.HoaDons.AsNoTracking()
                .Where(h => !h.IsDeleted
                         && h.Ngay >= start
                         && h.Ngay < end);

            if (anShipKhanh)
                hoaDonQ = hoaDonQ.Where(h => h.NguoiShip != "Khánh");

            // ====== CHI TIẾT HÓA ĐƠN ======
            var chiTietQuery = _db.ChiTietHoaDons.AsNoTracking()
                .Where(c => !c.IsDeleted
                         && !c.HoaDon.IsDeleted
                         && c.HoaDon.Ngay >= start
                         && c.HoaDon.Ngay < end);

            if (anShipKhanh)
                chiTietQuery = chiTietQuery.Where(c => c.HoaDon.NguoiShip != "Khánh");

            // ====== THANH TOÁN ======
            var thanhToanQ = _db.ChiTietHoaDonThanhToans.AsNoTracking()
                .Where(t => !t.IsDeleted
                         && t.Ngay >= start
                         && t.Ngay < end);

            if (anShipKhanh)
                thanhToanQ = thanhToanQ.Where(t => t.HoaDon.NguoiShip != "Khánh");

            // ====== CÔNG NỢ ======
            var noQ = _db.HoaDonNos.AsNoTracking()
                .Where(n =>
                          n.NgayNo >= start
                         && n.NgayNo < end);

            //if (anShipKhanh)
            //    noQ = noQ.Where(n => n.hoa.NguoiShip != "Khánh");

            // ====== CHI TIÊU ======
            var chiTieuQ = _db.ChiTieuHangNgays.AsNoTracking()
                .Where(c => !c.IsDeleted
                         && c.Ngay >= start
                         && c.Ngay < end);

            // ====== DOANH THU ======
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

            // ====== ĐÃ THU ======
            var daThuQ = thanhToanQ
                .Where(t => t.LoaiThanhToan == "Trong ngày"
                         || t.LoaiThanhToan == "Trả nợ trong ngày");

            decimal daThu = await daThuQ.SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal ttTienMat_Khanh = await daThuQ
                .Where(t => t.PhuongThucThanhToanId == AppConstants.TienMatId
                         && t.GhiChu == "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal ttTienMat_KhongKhanh = await daThuQ
                .Where(t => t.PhuongThucThanhToanId == AppConstants.TienMatId
                         && t.GhiChu != "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal ckChung = await daThuQ
      .Where(t => t.PhuongThucThanhToanId == AppConstants.ChuyenKhoanId)
    .SumAsync(t => (decimal?)t.SoTien) ?? 0;


            var daThuChiTiet = new List<LabelValueDto>
    {
        new() { Ten = "Tiền mặt", GiaTri = ttTienMat_KhongKhanh },
        new() { Ten = "Tiền shipper", GiaTri = ttTienMat_Khanh },
        new() { Ten = "Chuyển khoản", GiaTri = ckChung },
    };

            // ====== CHƯA THU ======
            var chuaThuList = await (
                from h in hoaDonQ
                where h.ConLai > 0 && !h.HasDebt
                select new
                {
                    h.Id,
                    h.KhachHangId,
                    h.TenKhachHangText,
                    h.TenBan,
                    h.ConLai
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
                .Select(g => new LabelValueDto
                {
                    Ten = g.Key,
                    GiaTri = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(x => x.GiaTri)
                .ToListAsync();

            // ====== CÔNG NỢ ======
            decimal congNo = await noQ.SumAsync(n => (decimal?)n.ConLai) ?? 0m;

            //        var congNoChiTiet = await noQ
            //.Where(n => n.ConLai != null)
            //.GroupBy(n => n.KhachHangId ?? Guid.Empty)
            //.Select(g => new LabelValueDto
            //{
            //    Ten = g.First().HoaDon!.KhachHangId != null
            //        ? (g.First().HoaDon!.TenKhachHangText ?? "(không tên)")
            //        : (g.First().HoaDon!.TenBan ?? "(không tên)"),
            //    GiaTri = g.Sum(x => x.SoTienConLai)
            //})
            //.OrderByDescending(x => x.GiaTri)
            //.ToListAsync();
            //var congNoChiTiet = await noQ
            //    .GroupBy(n => n.KhachHangId ?? Guid.Empty)
            //    .Select(g => new LabelValueDto
            //    {
            //        Ten = g.First().HoaDon.KhachHangId != null
            //            ? (g.First().HoaDon.TenKhachHangText ?? "(không tên)")
            //            : (g.First().HoaDon.TenBan ?? "(không tên)"),
            //        GiaTri = g.Sum(x => x.SoTienConLai)
            //    })
            //    .OrderByDescending(x => x.GiaTri)
            //    .ToListAsync();

            // ====== TRẢ NỢ ======
            var traNoQ = thanhToanQ
                .Where(t => t.LoaiThanhToan == "Trả nợ qua ngày");

            decimal traNoTien = await traNoQ
                .Where(t => t.PhuongThucThanhToanId == AppConstants.TienMatId
                         && t.GhiChu != "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal traNoBank = await traNoQ
                .Where(t => t.PhuongThucThanhToanId != AppConstants.TienMatId)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal traNoKhanh = await traNoQ
                .Where(t => t.GhiChu == "Shipper")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            // ====== TỔNG ĐƠN / LY ======
            int tongSoDon = await hoaDonQ.CountAsync();

            //            int tongSoLy = await chiTietQuery
            //                .Include(ct => ct.SanPhamBienThe)
            //                    .ThenInclude(bt => bt.SanPham)
            //                        .ThenInclude(sp => sp.NhomSanPham)
            //         .Where(ct =>
            //    ct.SanPhamBienThe != null &&
            //    ct.SanPhamBienThe.SanPham != null &&
            //    ct.SanPhamBienThe.SanPham.NhomSanPham != null &&   // ✅ BẮT BUỘC
            //    ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Thuốc lá" &&
            //    ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Ăn vặt" &&
            //    ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Nước lon"
            //)
            //                .SumAsync(ct => (int?)ct.SoLuong) ?? 0;
            int tongSoLy = await chiTietQuery
                .Where(ct =>
                    ct.SanPhamBienThe != null &&
                    ct.SanPhamBienThe.SanPham != null &&
                    ct.SanPhamBienThe.SanPham.NhomSanPham != null &&
                    ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Thuốc lá" &&
                    ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Ăn vặt" &&
                    ct.SanPhamBienThe.SanPham.NhomSanPham.Ten != "Nước lon"
                )
                .Select(ct => (int?)ct.SoLuong)
                .SumAsync() ?? 0;
            // ====== TOP SẢN PHẨM ======
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
            var traNoTienChiTiet = await (
    from t in traNoQ
    where t.PhuongThucThanhToanId == AppConstants.TienMatId
          && t.GhiChu != "Shipper"
    join h in _db.HoaDons.AsNoTracking().Where(h => !h.IsDeleted)
        on t.HoaDonId equals h.Id
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

            var traNoBankChiTiet = await (
                from t in traNoQ
                where t.PhuongThucThanhToanId != AppConstants.TienMatId
                join h in _db.HoaDons.AsNoTracking().Where(h => !h.IsDeleted)
                    on t.HoaDonId equals h.Id
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
            var topSanPhams = tempTop
                .Select((x, i) => new TopSanPhamDto
                {
                    Stt = i + 1,
                    TenSanPham = x.TenSanPham,
                    SoLuong = x.SoLuong,
                    DoanhThu = x.DoanhThu
                })
                .ToList();
            // ====== NGUYÊN LIỆU SẮP HẾT (LẤY THẲNG BẢNG) ======
            var rawNguyenLieu = await _db.NguyenLieuBanHangs
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DangSuDung)
                .OrderBy(x => x.TonKho)   // ít lên trước
                .ThenBy(x => x.Ten)
                .Take(50)
                .Select(x => new
                {
                    x.Ten,
                    x.TonKho
                })
                .ToListAsync(); // ✅ kết thúc LINQ to Entities

            var SoLuongNguyenLieuBanHangs = rawNguyenLieu
                .Select((x, i) => new SoLuongNguyenLieuBanHangDto
                {
                    Stt = i + 1,              // ✅ LINQ to Objects → OK
                    TenNguyenLieu = x.Ten,
                    SoLuong = x.TonKho
                })
                .ToList();
            decimal mangVe = ttTienMat_KhongKhanh - chiTieu;

            return new ThongKeNgayDto
            {
                Ngay = start,

                DoanhThu = doanhThu,
                DaThu = daThu,
                DaThu_TienMat = ttTienMat_KhongKhanh,
                DaThu_CK_Chung = ckChung,
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
                //CongNoChiTiet = congNoChiTiet,
                TraNoTienChiTiet = traNoTienChiTiet,
                TraNoBankChiTiet = traNoBankChiTiet,

                ChuaThuChiTiet = chuaThuChiTiet,
                DaThuChiTiet = daThuChiTiet,
                TopSanPhams = topSanPhams,
                SoLuongNguyenLieuBanHangs = SoLuongNguyenLieuBanHangs

            };
        }

    }
}