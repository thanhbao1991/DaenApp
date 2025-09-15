using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services;

public class DoanhThuService : IDoanhThuService
{
    private readonly AppDbContext _context;
    public DoanhThuService(AppDbContext context) => _context = context;

    public async Task<DoanhThuNgayDto> GetDoanhThuNgayAsync(DateTime ngay)
    {
        var list = await _context.HoaDons
            .Include(x => x.KhachHang)
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay == ngay)
            .ToListAsync();

        var hoaDonIds = list
        .Select(x => x.Id)
        .ToList();

        var tongDoanhThu = list.Sum(x => x.ThanhTien);

        var tongDaThu = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
    .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && !ct.IsDeleted)
    .SumAsync(ct => (decimal?)ct.SoTien) ?? 0;

        var tongChuyenKhoan = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
      .Where(ct => hoaDonIds.Contains(ct.HoaDonId) && ct.TenPhuongThucThanhToan.ToLower() != "Tiền Mặt" && !ct.IsDeleted)
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
                NgayHoaDon = x.Ngay,
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
        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay.Month == thang && x.Ngay.Year == nam)
            .GroupBy(x => x.Ngay.Date)
            .Select(g => new DoanhThuThangItemDto
            {
                Ngay = g.Key,
                //   SoDon = g.Count(x => x.PhanLoai is "Mv" or "Ship" or "App" or "TaiCho"),
                //   TongTien = (double)g.Where(x => x.PhanLoai is "Mv" or "Ship" or "App" or "TaiCho").Sum(x => x.TongTien),
                //     ChiTieu = (double)g.Where(x => x.PhanLoai == "ChiTieu").Sum(x => x.TongTien),
                //  //  TienBank = (double)g.Where(x => x.NgayBank == g.Key).Sum(x => x.TienBank),
                //     TienNo = (double)g.Where(x => x.PhanLoai is "Mv" or "Ship" or "TaiCho")
                //                     .Where(x => x.NgayNo == g.Key && (x.NgayTra == null || x.NgayTra != g.Key))
                //0                      .Sum(x => x.TienNo),
                TaiCho = g.Where(x => x.PhanLoai == "TaiCho").Sum(x => x.TongTien),
                MuaVe = g.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
                DiShip = g.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
                AppShipping = g.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),
            })
            .OrderBy(g => g.Ngay)
            .ToListAsync();

        foreach (var item in list)
        {
            item.TongTienMat = item.TongTien - item.ChiTieu - item.TienBank - item.TienNo;
        }

        return list;
    }
}