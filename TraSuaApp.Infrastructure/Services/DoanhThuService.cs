using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
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
                DaThu = h.ChiTietHoaDonThanhToans
                            .Where(t => !t.IsDeleted)
                            .Sum(t => (decimal?)t.SoTien) ?? 0,
                ConLai = h.ThanhTien - (
                            h.ChiTietHoaDonThanhToans
                              .Where(t => !t.IsDeleted)
                              .Sum(t => (decimal?)t.SoTien) ?? 0),
            })
            .Where(h => h.ConLai > 0) // 🟟 chỉ lấy hoá đơn còn nợ thực sự
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
        var list = await _context.HoaDons
            .Include(x => x.KhachHang)
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay == ngay)
            .ToListAsync();

        var hoaDonIds = list.Select(x => x.Id).ToList();
        var tongDoanhThu = list.Sum(x => x.ThanhTien);

        var tongDaThu = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTien) ?? 0;

        var tongChuyenKhoan = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && ct.TenPhuongThucThanhToan.ToLower() != "tiền mặt" && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTien) ?? 0;

        var tongTienNo = await _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTienNo) ?? 0;

        var tongTienTraNoTrongNgay = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && ct.LoaiThanhToan == "Trả nợ trong ngày" && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.SoTien) ?? 0;

        tongTienNo -= tongTienTraNoTrongNgay;

        var tongChiTieu = await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(ct => ct.Ngay == ngay && !ct.BillThang && !ct.IsDeleted)
            .SumAsync(ct => (decimal?)ct.ThanhTien) ?? 0;

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
            TongConLai = tongDoanhThu - tongDaThu,
            TongChiTieu = tongChiTieu,
            TongChuyenKhoan = tongChuyenKhoan,
            TongTienNo = tongTienNo,
            TongCongNo = -1,
            TaiCho = list.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => x.ThanhTien),
            MuaVe = list.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
            DiShip = list.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
            AppShipping = list.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),
            ViecChuaLam = viecChuaLam,
            HoaDons = list.Select(x => new DoanhThuHoaDonDto
            {
                Id = x.Id,
                IdKhachHang = x.KhachHangId,
                DiaChi = x.DiaChiText,
                ThongTinHoaDon = x.KhachHangId != null ? x.TenKhachHangText : x.TenBan,
                DiaChiShip = x.DiaChiText,
                PhanLoai = x.PhanLoai,
                NgayHoaDon = x.NgayGio,
                NgayShip = x.NgayShip,
                BaoDon = x.BaoDon,
                TongTien = x.TongTien,
            }).ToList()
        };

        dto.TongTienMat = dto.TongDoanhThu - dto.TongChiTieu - dto.TongChuyenKhoan - dto.TongTienNo;
        return dto;
    }

    public async Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam)
    {
        var hoaDons = await _context.HoaDons
            .Include(x => x.KhachHang)
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay.Month == thang && x.Ngay.Year == nam)
            .ToListAsync();

        var hoaDonIds = hoaDons.Select(x => x.Id).ToList();

        var thanhToans = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .ToListAsync();

        var noList = await _context.ChiTietHoaDonNos
            .AsNoTracking()
            .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
            .ToListAsync();

        var chiTieus = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(ct => ct.Ngay.Month == thang && ct.Ngay.Year == nam && !ct.BillThang && !ct.IsDeleted)
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
                    .Sum(ct => ct.SoTienNo);

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
                    //  ThuongKhanh = g.Where(x => x.PhanLoai == "Ship" && x.NguoiShip == "Khánh").Sum(x => x.ThanhTien) * 0.01m,
                    ThuongNha = tongDoanhThu * 0.005m,
                    ThuongKhanh = tongDoanhThu * 0.005m
                };
            })
            .OrderBy(x => x.Ngay)
            .ToList();

        return result;
    }
}