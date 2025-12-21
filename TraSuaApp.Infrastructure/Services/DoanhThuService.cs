using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
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
        var ngayKetThuc = ngayBatDau.AddDays(1);

        // Lấy toàn bộ hóa đơn trong ngày + các line thanh toán + các line nợ
        var hoaDons = await _context.HoaDons
            .Include(x => x.KhachHang)
            .Include(x => x.ChiTietHoaDonThanhToans)
            .Include(x => x.ChiTietHoaDonNos)
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= ngayBatDau && x.Ngay < ngayKetThuc)
            .ToListAsync();

        // ===== Helper: normalize payment method =====
        static bool IsTienMat(string? tenPhuongThuc)
        {
            if (string.IsNullOrWhiteSpace(tenPhuongThuc)) return false;
            var s = tenPhuongThuc.Trim().ToLower();
            // chấp nhận vài biến thể hay gặp
            return s == "tiền mặt" || s == "tien mat" || s.Contains("tiền mặt") || s.Contains("tien mat");
        }

        // ===== Tính tổng & map từng hóa đơn =====
        decimal tongDoanhThu = 0m;
        decimal tongDaThu = 0m;
        decimal tongChuyenKhoan = 0m;
        decimal tongTienMat = 0m;
        decimal tongTienNo = 0m;
        decimal tongConLai = 0m;

        var hoaDonDtos = new List<DoanhThuHoaDonDto>();

        foreach (var h in hoaDons)
        {
            var thanhToans = (h.ChiTietHoaDonThanhToans ?? new List<ChiTietHoaDonThanhToan>())
                .Where(t => !t.IsDeleted)
                .ToList();

            var noLines = (h.ChiTietHoaDonNos ?? new List<ChiTietHoaDonNo>())
                .Where(n => !n.IsDeleted)
                .ToList();

            var tienMat = thanhToans
                .Where(t => IsTienMat(t.TenPhuongThucThanhToan))
                .Sum(t => (decimal)t.SoTien);

            var tienBank = thanhToans
                .Where(t => !IsTienMat(t.TenPhuongThucThanhToan))
                .Sum(t => (decimal)t.SoTien);

            // Nợ còn lại (để tô đỏ)
            var tienNo = noLines.Sum(n => (decimal?)n.SoTienConLai) ?? 0m;

            // ConLai lấy từ HoaDon để đúng logic hệ thống (anh đang dùng rồi)
            var conLai = (decimal)h.ConLai;
            if (conLai < 0) conLai = 0;

            var tongTien = (decimal)h.ThanhTien;
            if (tongTien < 0) tongTien = 0;

            var daThu = tienMat + tienBank;
            var daThanhToan = conLai <= 0;

            // Ngày nợ/Ngày trả (nếu entity có NgayGio)
            DateTime? ngayNo = null;
            DateTime? ngayTra = null;

            // Nếu entity ChiTietHoaDonNo có NgayGio, lấy cái mới nhất
            // (Nếu tên field khác, anh đổi lại đúng field)
            if (noLines.Count > 0)
            {
                // giả định entity có property NgayGio
                ngayNo = noLines
                    .Select(n => (DateTime?)n.NgayGio)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();
            }

            if (thanhToans.Count > 0)
            {
                // giả định entity có property NgayGio
                ngayTra = thanhToans
                    .Select(t => (DateTime?)t.NgayGio)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();
            }

            // Cộng tổng
            tongDoanhThu += tongTien;
            tongDaThu += daThu;
            tongChuyenKhoan += tienBank;
            tongTienMat += tienMat;
            tongTienNo += tienNo;
            tongConLai += conLai;

            hoaDonDtos.Add(new DoanhThuHoaDonDto
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

                NgayNo = ngayNo,
                NgayTra = ngayTra,

                DaThanhToan = daThanhToan,
                BaoDon = h.BaoDon,

                TongTien = tongTien,
                DaThu = daThu,
                ConLai = conLai,
                TienBank = tienBank,
                TienMat = tienMat,
                TienNo = tienNo
            });
        }

        // Chi tiêu trong ngày
        var tongChiTieu = await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(ct => !ct.IsDeleted && !ct.BillThang && ct.Ngay >= ngayBatDau && ct.Ngay < ngayKetThuc)
            .SumAsync(ct => (decimal?)ct.ThanhTien) ?? 0m;

        // Việc chưa làm
        var viecChuaLam = string.Join(", ",
            await _context.CongViecNoiBos
                .AsNoTracking()
                .Where(ct => !ct.DaHoanThanh && !ct.IsDeleted)
                .Select(ct => ct.Ten)
                .ToListAsync()
        );

        // DTO trả về
        var dto = new DoanhThuNgayDto
        {
            Ngay = ngayBatDau,
            TongSoDon = hoaDons.Count,
            TongDoanhThu = tongDoanhThu,
            TongDaThu = tongDaThu,
            TongConLai = tongConLai,
            TongChiTieu = tongChiTieu,

            TongChuyenKhoan = tongChuyenKhoan,
            TongTienMat = tongTienMat,
            TongTienNo = tongTienNo,
            TongCongNo = tongTienNo,

            TaiCho = hoaDons.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => (decimal)x.ThanhTien),
            MuaVe = hoaDons.Where(x => x.PhanLoai == "Mv").Sum(x => (decimal)x.ThanhTien),
            DiShip = hoaDons.Where(x => x.PhanLoai == "Ship").Sum(x => (decimal)x.ThanhTien),
            AppShipping = hoaDons.Where(x => x.PhanLoai == "App").Sum(x => (decimal)x.ThanhTien),

            ViecChuaLam = viecChuaLam,
            HoaDons = hoaDonDtos
                .OrderByDescending(x => x.PhanLoai == "Ship" && x.NgayShip == null)
                .ThenByDescending(x => x.NgayHoaDon)
                .ToList()
        };

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
            .Where(ct => !ct.IsDeleted && !ct.BillThang && ct.Ngay >= monthStart && ct.Ngay < monthEnd)
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

    // THEO GIỜ trong THÁNG: trả cả SoDon và DoanhThu (để backward-compatible)
    public async Task<List<DoanhThuHourBucketDto>> GetSoDonTheoGioTrongThangAsync(int thang, int nam, int startHour = 6, int endHour = 22)
    {
        if (startHour < 0) startHour = 0;
        if (endHour > 23) endHour = 23;
        if (endHour < startHour) (startHour, endHour) = (0, 23);

        var monthStart = new DateTime(nam, thang, 1);
        var monthEnd = monthStart.AddMonths(1);

        var grouped = await _context.HoaDons
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.Ngay >= monthStart && h.Ngay < monthEnd)
            .GroupBy(h => h.NgayGio.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                SoDon = g.Count(),
                DoanhThu = g.Sum(x => x.ThanhTien)
            })
            .ToListAsync();

        // Fill đủ range giờ
        var dict = Enumerable.Range(startHour, endHour - startHour + 1)
            .ToDictionary(
                h => h,
                h => new DoanhThuHourBucketDto
                {
                    Hour = h,
                    SoDon = 0,
                    DoanhThu = 0m
                }
            );

        foreach (var g in grouped)
        {
            if (g.Hour >= startHour && g.Hour <= endHour)
            {
                dict[g.Hour].SoDon = g.SoDon;
                dict[g.Hour].DoanhThu = g.DoanhThu;
            }
        }

        return dict.Values.OrderBy(x => x.Hour).ToList();
    }
}