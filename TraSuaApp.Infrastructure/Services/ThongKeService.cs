using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services
{
    public class ThongKeService : IThongKeService
    {
        private readonly AppDbContext _db;

        public ThongKeService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ThongKeChiTieuDto> TinhChiTieuNgayAsync(DateTime ngay)
        {
            var startNgay = ngay.Date;
            var endNgay = startNgay.AddDays(1);

            var baseQuery = _db.ChiTieuHangNgays
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

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

            // ===== Chi tiêu tháng (nhưng vẫn lọc theo ngày) =====
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

            return new ThongKeChiTieuDto
            {
                TongChiTieu = chiTieuNgay + chiTieuThang,

                ChiTieuNgay = chiTieuNgay,
                DanhSachChiTieuNgay = danhSachNgay,

                ChiTieuThang = chiTieuThang,
                DanhSachChiTieuThang = danhSachThang
            };
        }

        public async Task<ThongKeCongNoDto> TinhCongNoNgayAsync(DateTime ngay)
        {
            var start = ngay.Date;
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

            return result;
        }

        public async Task<ThongKeThanhToanDto> TinhThanhToanNgayAsync(DateTime ngay)
        {
            var start = ngay.Date;
            var end = start.AddDays(1);

            var data = await _db.ChiTietHoaDonThanhToans
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
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

            // ===== TỔNG CHUYỂN KHOẢN =====
            var tongChuyenKhoan = data
                .Where(x => x.PhuongThucThanhToanId == AppConstants.ChuyenKhoanId)
                .Sum(x => x.SoTien);

            // ===== TIỀN MẶT =====
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
                .Where(x => x.LoaiThanhToan == "Thanh toán" && x.GhiChu.StartsWith("Thanh toán"))
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

            var tongTienMat = danhSachTienMat.Sum(x => x.SoTien);

            return new ThongKeThanhToanDto
            {
                TongTienMat = tongTienMat,
                TongChuyenKhoan = tongChuyenKhoan,
                DanhSachTienMat = danhSachTienMat
            };
        }

        public async Task<ThongKeDoanhThuNgayDto> TinhDoanhThuNgayAsync(DateTime ngay)
        {
            var start = ngay.Date;
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

            return new ThongKeDoanhThuNgayDto
            {
                TongDoanhThu = data.Sum(x => x.ThanhTien),
                DanhSach = danhSach
            };
        }

        public async Task<ThongKeTraNoNgayDto> TinhTraNoNgayAsync(DateTime ngay)
        {
            var start = ngay.Date;
            var end = start.AddDays(1);

            var data = await _db.ChiTietHoaDonThanhToans
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
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

            // ===== trả nợ tại quán =====
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

            // ===== trả nợ qua shipper =====
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

            return new ThongKeTraNoNgayDto
            {
                TongTraNoTaiQuan = taiQuan.Sum(x => x.SoTien),
                TongTraNoShipper = shipper.Sum(x => x.SoTien),

                TraNoTaiQuan = taiQuan,
                TraNoShipper = shipper
            };
        }


    }
}