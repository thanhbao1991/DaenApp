using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services;

public class DoanhThuService : IDoanhThuService
{
    private readonly AppDbContext _context;
    public DoanhThuService(AppDbContext context) => _context = context;

    public async Task<List<DoanhThuHoaDonDto>> GetHoaDonKhachHangAsync(Guid khachHangId)
    {
        var list = await _context.HoaDons
            .Where(h => h.KhachHangId == khachHangId && !h.IsDeleted)
            .Select(h => new DoanhThuHoaDonDto
            {
                NgayHoaDon = h.NgayGio,
                Id = h.Id,
                ThongTinHoaDon = h.PhanLoai,
                TenKhachHangText = h.TenKhachHangText,
                TongTien = h.ThanhTien,
                ConLai = h.ConLai
            })
            .Where(h => h.ConLai > 0)
            .OrderByDescending(h => h.NgayHoaDon)
            .ToListAsync();

        return list;
    }

    public async Task<List<DoanhThuChiTietHoaDonDto>> GetChiTietHoaDonAsync(Guid hoaDonId)
    {
        var chiTiets = await _context.ChiTietHoaDons
            .AsNoTracking()
            .Where(x => x.HoaDonId == hoaDonId && !x.IsDeleted)
            .Select(x => new DoanhThuChiTietHoaDonDto
            {
                Id = x.Id,
                TenSanPham = x.TenSanPham,
                SoLuong = x.SoLuong,
                DonGia = x.DonGia,
                ThanhTien = x.ThanhTien,
                GhiChu = x.NoteText
            })
            .ToListAsync();

        return chiTiets;
    }

    public async Task<DoanhThuNgayDto> GetDoanhThuNgayAsync(DateTime ngay)
    {
        var ngayBatDau = ngay.Date;
        var ngayKetThuc = ngay.Date.AddDays(1);

        var list = await _context.HoaDons
            .Include(x => x.KhachHang)
            .Include(x => x.ChiTietHoaDonThanhToans)
            .Include(x => x.ChiTietHoaDonNos)
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= ngayBatDau && x.Ngay < ngayKetThuc)
            .ToListAsync();

        var hoaDonIds = list.Select(x => x.Id).ToList();

        var tongDoanhThu = list.Sum(x => x.ThanhTien);

        var tongDaThu = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTien) ?? 0m;

        var tongChuyenKhoan = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) &&
                         ct.TenPhuongThucThanhToan.ToLower() != "tiền mặt" &&
                         !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTien) ?? 0m;

        // Nợ còn lại của các hoá đơn trong ngày (đã ghi nợ): dùng SoTienConLai
        var tongNoTuNoLines = await _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTienConLai) ?? 0m;

        // Phần CHƯA THU của các hóa đơn chưa ghi nợ
        var chuaThuKhongNo = await _context.HoaDons.AsNoTracking()
            .Where(h => hoaDonIds.Contains(h.Id) && !h.IsDeleted && !h.ChiTietHoaDonNos.Any(n => !n.IsDeleted))
            .Select(h => (decimal?)(
                h.ThanhTien - (h.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted)
                                .Sum(t => (decimal?)t.SoTien) ?? 0m)))
            .SumAsync() ?? 0m;

        var tongConLai = await _context.HoaDons.AsNoTracking()
      .Where(h => !h.IsDeleted && h.Ngay >= ngayBatDau && h.Ngay < ngayKetThuc)
      .SumAsync(h => (decimal?)h.ConLai) ?? 0m;

        var tongChiTieu = await _context.ChiTieuHangNgays.AsNoTracking()
        .Where(ct => !ct.IsDeleted && !ct.BillThang
                  && ct.Ngay >= ngayBatDau && ct.Ngay < ngayKetThuc)
        .SumAsync(ct => (decimal?)ct.ThanhTien) ?? 0m;

        string viecChuaLam = string.Join(", ",
            await _context.CongViecNoiBos
                .AsNoTracking()
                .Where(ct => !ct.DaHoanThanh && !ct.IsDeleted)
                .Select(ct => ct.Ten)
                .ToListAsync()
        );

        var dto = new DoanhThuNgayDto
        {
            Ngay = ngay,
            TongSoDon = list.Count(),
            TongDoanhThu = tongDoanhThu,
            TongDaThu = tongDaThu,
            TongConLai = tongConLai,
            TongChiTieu = tongChiTieu,
            TongChuyenKhoan = tongChuyenKhoan,
            TongTienNo = tongNoTuNoLines, // nợ còn lại hôm nay (đã ghi nợ)
            TongCongNo = tongNoTuNoLines,

            TaiCho = list.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => x.ThanhTien),
            MuaVe = list.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
            DiShip = list.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
            AppShipping = list.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),
            ViecChuaLam = viecChuaLam,

            // chi tiết theo hoá đơn (ưu tiên SoTienConLai)
            HoaDons = list.Select(h => new DoanhThuHoaDonDto
            {
                Id = h.Id,
                IdKhachHang = h.KhachHangId,
                ThongTinHoaDon = h.TenBan,
                TenKhachHangText = h.TenKhachHangText,
                DiaChi = h.DiaChiText,
                DiaChiShip = h.DiaChiText,
                PhanLoai = h.PhanLoai,
                NgayHoaDon = h.NgayGio,
                NgayShip = h.NgayShip,
                BaoDon = h.BaoDon,
                TongTien = h.ThanhTien,
                ConLai = h.ConLai,
            }).ToList()
        };

        dto.TongTienMat = -1;// dto.TongDoanhThu - dto.TongChiTieu - dto.TongChuyenKhoan - dto.TongTienNo;
        return dto;
    }

    public async Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam)
    {
        var monthStart = new DateTime(nam, thang, 1);
        var monthEnd = monthStart.AddMonths(1);

        var hoaDons = await _context.HoaDons
            .Include(x => x.KhachHang)
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= monthStart && x.Ngay < monthEnd)
            .ToListAsync();

        var chiTieus = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(ct => !ct.IsDeleted && !ct.BillThang
                      && ct.Ngay >= monthStart && ct.Ngay < monthEnd)
            .ToListAsync();

        var hoaDonIds = hoaDons.Select(x => x.Id).ToList();

        var thanhToans = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .ToListAsync();

        var noList = await _context.ChiTietHoaDonNos
            .AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .Select(ct => new { ct.HoaDonId, ct.SoTienConLai })
            .ToListAsync();


        var result = hoaDons
            .GroupBy(x => x.Ngay.Date)
            .Select(g =>
            {
                var idsTrongNgay = g.Select(x => x.Id).ToList();
                var tongDoanhThu = g.Sum(x => x.ThanhTien);

                var tongChuyenKhoan = thanhToans
                    .Where(ct => idsTrongNgay.Contains(ct.HoaDonId) && ct.TenPhuongThucThanhToan.ToLower() != "tiền mặt")
                    .Sum(ct => ct.SoTien);

                var tongTienNo = noList
                    .Where(ct => idsTrongNgay.Contains(ct.HoaDonId))
                    .Sum(ct => ct.SoTienConLai);

                var tongChiTieu = chiTieus
                    .Where(ct => ct.Ngay.Date == g.Key)
                    .Sum(ct => ct.ThanhTien);

                return new DoanhThuThangItemDto
                {
                    Ngay = g.Key,
                    SoDon = g.Count(),
                    TongTien = tongDoanhThu,
                    ChiTieu = tongChiTieu,
                    TienBank = tongChuyenKhoan,
                    TienNo = tongTienNo,
                    TongTienMat = tongDoanhThu - tongChiTieu - tongChuyenKhoan - tongTienNo,
                    TaiCho = g.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => x.ThanhTien),
                    MuaVe = g.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
                    DiShip = g.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
                    AppShipping = g.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),
                    ThuongNha = tongDoanhThu * 0.005m,
                    ThuongKhanh = tongDoanhThu * 0.005m
                };
            })
            .OrderBy(x => x.Ngay)
            .ToList();

        return result;
    }
}