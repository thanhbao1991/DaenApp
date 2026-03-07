using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services;

public class DoanhThuService : IDoanhThuService
{
    private readonly AppDbContext _context;
    public DoanhThuService(AppDbContext context) => _context = context;

    public async Task<List<DoanhThuNamItemDto>> GetDoanhThuNamAsync(int nam)
    {
        var yearStart = new DateTime(nam, 1, 1);
        var yearEnd = yearStart.AddYears(1);

        var hoaDons = await (
            from h in _context.vHoaDonPaymentMasks.AsNoTracking()


            where
                   h.NgayGio >= yearStart
                  && h.NgayGio < yearEnd
                  && !(

                   h.TongSoLanNhanVoucher >= 10 &&
    h.PaymentMethodMask == 1 &&
    (h.PhanLoai == "Ship" || h.PhanLoai == "Mv" || h.PhanLoai == "Tại Chỗ")

                  )

            select new
            {
                Thang = h.NgayGio.Month,
                h.ThanhTien
            }
        ).ToListAsync();

        var doanhThuTheoThang = hoaDons
            .GroupBy(x => x.Thang)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    SoDon = g.Count(),
                    TongTien = g.Sum(x => x.ThanhTien)
                });

        var result = new List<DoanhThuNamItemDto>();

        for (int thang = 1; thang <= 12; thang++)
        {
            if (doanhThuTheoThang.TryGetValue(thang, out var data))
            {
                result.Add(new DoanhThuNamItemDto
                {
                    Thang = thang,
                    SoDon = data.SoDon,
                    TongTien = data.TongTien
                });
            }
            else
            {
                result.Add(new DoanhThuNamItemDto
                {
                    Thang = thang,
                    SoDon = 0,
                    TongTien = 0
                });
            }
        }

        return result.OrderBy(x => x.Thang).ToList();
    }
    public async Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam)
    {
        var monthStart = new DateTime(nam, thang, 1);
        var monthEnd = monthStart.AddMonths(1);

        var hoaDons = await _context.vHoaDonPaymentMasks
            .AsNoTracking()
            .Where(h =>
                h.NgayGio >= monthStart &&
                h.NgayGio < monthEnd &&
                !(
   h.TongSoLanNhanVoucher >= 10 &&
    h.PaymentMethodMask == 1 &&
    (h.PhanLoai == "Ship" || h.PhanLoai == "Mv" || h.PhanLoai == "Tại Chỗ")

                )
            )
            .Select(h => new
            {
                Ngay = h.NgayGio.Date,
                h.ThanhTien
            })
            .ToListAsync();

        var doanhThuTheoNgay = hoaDons
            .GroupBy(x => x.Ngay)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    SoDon = g.Count(),
                    TongTien = g.Sum(x => x.ThanhTien)
                });

        var totalDays = (monthEnd - monthStart).Days;
        var result = new List<DoanhThuThangItemDto>();

        for (int i = 0; i < totalDays; i++)
        {
            var day = monthStart.AddDays(i).Date;

            if (doanhThuTheoNgay.TryGetValue(day, out var data))
            {
                result.Add(new DoanhThuThangItemDto
                {
                    Ngay = day,
                    SoDon = data.SoDon,
                    TongTien = data.TongTien
                });
            }
            else
            {
                result.Add(new DoanhThuThangItemDto
                {
                    Ngay = day,
                    SoDon = 0,
                    TongTien = 0
                });
            }
        }

        return result.OrderBy(x => x.Ngay).ToList();
    }

    //public async Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam)
    //{
    //    var monthStart = new DateTime(nam, thang, 1);
    //    var monthEnd = monthStart.AddMonths(1);

    //    var hoaDons = await (
    //        from h in _context.HoaDons.AsNoTracking()
    //        join k in _context.KhachHangs.AsNoTracking()
    //            on h.KhachHangId equals k.Id into kh
    //        from k in kh.DefaultIfEmpty()

    //        where !h.IsDeleted
    //              && h.Ngay >= monthStart
    //              && h.Ngay < monthEnd
    //              && !(k != null && k.TongLanNhanVoucher >= 1 && h.PaymentMethodMask <= 1 && h.PhanLoai != "App")

    //        select new
    //        {
    //            Ngay = h.Ngay.Date,
    //            h.ThanhTien
    //        }
    //    ).ToListAsync();

    //    var doanhThuTheoNgay = hoaDons
    //        .GroupBy(x => x.Ngay)
    //        .ToDictionary(
    //            g => g.Key,
    //            g => new
    //            {
    //                SoDon = g.Count(),
    //                TongTien = g.Sum(x => x.ThanhTien)
    //            });

    //    var totalDays = (monthEnd - monthStart).Days;
    //    var result = new List<DoanhThuThangItemDto>();

    //    for (int i = 0; i < totalDays; i++)
    //    {
    //        var day = monthStart.AddDays(i).Date;

    //        if (doanhThuTheoNgay.TryGetValue(day, out var data))
    //        {
    //            result.Add(new DoanhThuThangItemDto
    //            {
    //                Ngay = day,
    //                SoDon = data.SoDon,
    //                TongTien = data.TongTien
    //            });
    //        }
    //        else
    //        {
    //            result.Add(new DoanhThuThangItemDto
    //            {
    //                Ngay = day,
    //                SoDon = 0,
    //                TongTien = 0
    //            });
    //        }
    //    }

    //    return result.OrderBy(x => x.Ngay).ToList();
    //}

    //public async Task<List<DoanhThuNamItemDto>> GetDoanhThuNamAsync(int nam)
    //{
    //    var yearStart = new DateTime(nam, 1, 1);
    //    var yearEnd = yearStart.AddYears(1);

    //    var hoaDons = await (
    //        from h in _context.HoaDons.AsNoTracking()
    //        join k in _context.KhachHangs.AsNoTracking()
    //            on h.KhachHangId equals k.Id into kh
    //        from k in kh.DefaultIfEmpty()

    //        where !h.IsDeleted
    //              && h.Ngay >= yearStart
    //              && h.Ngay < yearEnd
    //              && !(k != null && k.TongLanNhanVoucher >= 1 && h.PaymentMethodMask <= 1 && h.PhanLoai != "App")

    //        select new
    //        {
    //            Thang = h.Ngay.Month,
    //            h.ThanhTien
    //        }
    //    ).ToListAsync();

    //    var doanhThuTheoThang = hoaDons
    //        .GroupBy(x => x.Thang)
    //        .ToDictionary(
    //            g => g.Key,
    //            g => new
    //            {
    //                SoDon = g.Count(),
    //                TongTien = g.Sum(x => x.ThanhTien)
    //            });

    //    var result = new List<DoanhThuNamItemDto>();

    //    for (int thang = 1; thang <= 12; thang++)
    //    {
    //        if (doanhThuTheoThang.TryGetValue(thang, out var data))
    //        {
    //            result.Add(new DoanhThuNamItemDto
    //            {
    //                Thang = thang,
    //                SoDon = data.SoDon,
    //                TongTien = data.TongTien
    //            });
    //        }
    //        else
    //        {
    //            result.Add(new DoanhThuNamItemDto
    //            {
    //                Thang = thang,
    //                SoDon = 0,
    //                TongTien = 0
    //            });
    //        }
    //    }

    //    return result.OrderBy(x => x.Thang).ToList();
    //}

    //public async Task<List<DoanhThuNamItemDto>> GetDoanhThuNamAsync(int nam)
    //{
    //    var yearStart = new DateTime(nam, 1, 1);
    //    var yearEnd = yearStart.AddYears(1);

    //    var hoaDons = await _context.HoaDons
    //        .AsNoTracking()
    //        .Where(h => !h.IsDeleted && h.Ngay >= yearStart && h.Ngay < yearEnd)
    //        .Select(h => new
    //        {
    //            h.Id,
    //            Thang = h.Ngay.Month,
    //            h.ThanhTien,
    //            h.PhanLoai
    //        })
    //        .ToListAsync();

    //    var hoaDonIds = hoaDons.Select(x => x.Id).ToList();

    //    var thanhToans = await _context.ChiTietHoaDonThanhToans
    //        .AsNoTracking()
    //        .Where(t => hoaDonIds.Contains(t.HoaDonId) && !t.IsDeleted)
    //        .Select(t => new
    //        {
    //            t.HoaDonId,
    //            t.SoTien,
    //            t.TenPhuongThucThanhToan
    //        })
    //        .ToListAsync();

    //    var noLines = await _context.ChiTietHoaDonNos
    //        .AsNoTracking()
    //        .Where(n => hoaDonIds.Contains(n.HoaDonId) && !n.IsDeleted)
    //        .Select(n => new
    //        {
    //            n.HoaDonId,
    //            n.SoTienConLai
    //        })
    //        .ToListAsync();

    //    var chiTieus = await _context.ChiTieuHangNgays
    //        .AsNoTracking()
    //        .Where(ct => !ct.IsDeleted && ct.Ngay >= yearStart && ct.Ngay < yearEnd)
    //        .Select(ct => new
    //        {
    //            Thang = ct.Ngay.Month,
    //            ct.ThanhTien
    //        })
    //        .ToListAsync();

    //    static bool IsTienMat(string? ten)
    //    {
    //        if (string.IsNullOrWhiteSpace(ten)) return false;
    //        var s = ten.Trim().ToLower();
    //        return s.Contains("tiền mặt") || s.Contains("tien mat");
    //    }

    //    var thanhToanLookup = thanhToans
    //        .GroupBy(x => x.HoaDonId)
    //        .ToDictionary(g => g.Key, g => g.ToList());

    //    var noLookup = noLines
    //        .GroupBy(x => x.HoaDonId)
    //        .ToDictionary(g => g.Key, g => g.Sum(x => x.SoTienConLai));

    //    var result = new List<DoanhThuNamItemDto>();

    //    for (int thang = 1; thang <= 12; thang++)
    //    {
    //        var dsHoaDon = hoaDons.Where(x => x.Thang == thang).ToList();

    //        decimal tongDoanhThu = 0;
    //        decimal tongChuyenKhoan = 0;
    //        decimal tongTienNo = 0;

    //        foreach (var h in dsHoaDon)
    //        {
    //            tongDoanhThu += h.ThanhTien;

    //            if (thanhToanLookup.TryGetValue(h.Id, out var tts))
    //            {
    //                tongChuyenKhoan += tts
    //                    .Where(t => !IsTienMat(t.TenPhuongThucThanhToan))
    //                    .Sum(t => t.SoTien);
    //            }

    //            if (noLookup.TryGetValue(h.Id, out var no))
    //            {
    //                tongTienNo += no;
    //            }
    //        }

    //        var tongChiTieu = chiTieus
    //            .Where(x => x.Thang == thang)
    //            .Sum(x => x.ThanhTien);

    //        result.Add(new DoanhThuNamItemDto
    //        {
    //            Thang = thang,
    //            SoDon = dsHoaDon.Count,
    //            TongTien = tongDoanhThu,
    //            ChiTieu = tongChiTieu,
    //            TienChuyển khoản = tongChuyenKhoan,
    //            TienNo = tongTienNo,
    //            TongTienMat = tongDoanhThu - tongChuyenKhoan - tongTienNo,

    //            TaiCho = dsHoaDon.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => x.ThanhTien),
    //            MuaVe = dsHoaDon.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
    //            DiShip = dsHoaDon.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
    //            AppShipping = dsHoaDon.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),

    //            ThuongNha = tongDoanhThu * 0.003m,
    //            ThuongKhanh = tongDoanhThu * 0.003m
    //        });
    //    }

    //    return result;
    //}

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

        // ===== 1️⃣ HÓA ĐƠN =====
        var hoaDons = await _context.HoaDons
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= ngayBatDau && x.Ngay < ngayKetThuc)
            .Select(h => new HoaDonLite
            {
                Id = h.Id,
                KhachHangId = h.KhachHangId,
                TenKhachHangText = h.TenKhachHangText,
                DiaChiText = h.DiaChiText,
                PhanLoai = h.PhanLoai,
                TenBan = h.TenBan,
                NgayGio = h.NgayGio,
                NgayShip = h.NgayShip,
                BaoDon = h.BaoDon,
                ThanhTien = h.ThanhTien,
                ConLai = h.ConLai
            })
            .ToListAsync();

        var hoaDonIds = hoaDons.Select(x => x.Id).ToList();

        // ===== 2️⃣ THANH TOÁN =====
        var thanhToans = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => hoaDonIds.Contains(x.HoaDonId) && !x.IsDeleted)
            .Select(x => new ThanhToanLite
            {
                HoaDonId = x.HoaDonId,
                SoTien = x.SoTien,
                TenPhuongThuc = x.TenPhuongThucThanhToan,
                NgayGio = x.NgayGio
            })
            .ToListAsync();

        // ===== 3️⃣ NỢ =====
        var noLines = await _context.ChiTietHoaDonNos
            .AsNoTracking()
            .Where(x => hoaDonIds.Contains(x.HoaDonId) && !x.IsDeleted)
            .Select(x => new NoLite
            {
                HoaDonId = x.HoaDonId,
                SoTienConLai = x.SoTienConLai,
                NgayGio = x.NgayGio
            })
            .ToListAsync();

        var thanhToanLookup = thanhToans
            .GroupBy(x => x.HoaDonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var noLookup = noLines
            .GroupBy(x => x.HoaDonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        static bool IsTienMat(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return false;
            ten = ten.ToLower();
            return ten.Contains("tiền mặt") || ten.Contains("tien mat");
        }

        decimal tongDoanhThu = 0;
        decimal tongDaThu = 0;
        decimal tongChuyenKhoan = 0;
        decimal tongTienMat = 0;
        decimal tongTienNo = 0;
        decimal tongConLai = 0;

        var hoaDonDtos = new List<DoanhThuHoaDonDto>();

        foreach (var h in hoaDons)
        {
            var tt = thanhToanLookup.TryGetValue(h.Id, out var tts)
                ? tts
                : new List<ThanhToanLite>();

            var nos = noLookup.TryGetValue(h.Id, out var ns)
                ? ns
                : new List<NoLite>();

            var tienMat = tt.Where(t => IsTienMat(t.TenPhuongThuc)).Sum(t => t.SoTien);
            var tienBank = tt.Where(t => !IsTienMat(t.TenPhuongThuc)).Sum(t => t.SoTien);
            var tienNo = nos.Sum(n => n.SoTienConLai);

            var tongTien = Math.Max(0, h.ThanhTien);
            var conLai = Math.Max(0, h.ConLai);
            var daThu = tienMat + tienBank;

            DateTime? ngayNo = nos.OrderByDescending(x => x.NgayGio).Select(x => x.NgayGio).FirstOrDefault();
            DateTime? ngayTra = tt.OrderByDescending(x => x.NgayGio).Select(x => x.NgayGio).FirstOrDefault();

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
                TenKhachHangText = h.TenKhachHangText,
                DiaChi = h.DiaChiText,
                DiaChiShip = h.DiaChiText,
                PhanLoai = h.PhanLoai,
                ThongTinHoaDon = h.TenBan,
                NgayHoaDon = h.NgayGio,
                NgayShip = h.NgayShip,
                NgayNo = ngayNo,
                NgayTra = ngayTra,
                BaoDon = h.BaoDon,
                DaThanhToan = conLai <= 0,
                TongTien = tongTien,
                DaThu = daThu,
                ConLai = conLai,
                TienBank = tienBank,
                TienMat = tienMat,
                TienNo = tienNo
            });
        }

        var tongChiTieu = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(ct => !ct.IsDeleted
            && !ct.BillThang
            && ct.Ngay >= ngayBatDau
            && ct.Ngay < ngayKetThuc)
            .SumAsync(ct => (decimal?)ct.ThanhTien) ?? 0;

        var viecChuaLam = string.Join(", ",
            await _context.CongViecNoiBos
                .AsNoTracking()
                .Where(x => !x.IsDeleted && !x.DaHoanThanh)
                .Select(x => x.Ten)
                .ToListAsync()
        );

        return new DoanhThuNgayDto
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
            TaiCho = hoaDons.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => x.ThanhTien),
            MuaVe = hoaDons.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
            DiShip = hoaDons.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
            AppShipping = hoaDons.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),
            ViecChuaLam = viecChuaLam,
            HoaDons = hoaDonDtos
                .OrderByDescending(x => x.PhanLoai == "Ship" && x.NgayShip == null)
                .ThenByDescending(x => x.NgayHoaDon)
                .ToList()
        };
    }

    //public async Task<List<DoanhThuThangItemDto>> GetDoanhThuThangAsync(int thang, int nam)
    //{
    //    var monthStart = new DateTime(nam, thang, 1);
    //    var monthEnd = monthStart.AddMonths(1);

    //    // ===== 1️⃣ HÓA ĐƠN =====
    //    var hoaDons = await _context.HoaDons
    //        .AsNoTracking()
    //        .Where(h => !h.IsDeleted && h.Ngay >= monthStart && h.Ngay < monthEnd)
    //        .Select(h => new HoaDonTempDto
    //        {
    //            Id = h.Id,
    //            Ngay = h.Ngay.Date,
    //            ThanhTien = h.ThanhTien,
    //            PhanLoai = h.PhanLoai
    //        })
    //        .ToListAsync();

    //    var hoaDonIds = hoaDons.Select(x => x.Id).ToList();

    //    // ===== 2️⃣ THANH TOÁN =====
    //    var thanhToans = await _context.ChiTietHoaDonThanhToans
    //        .AsNoTracking()
    //        .Where(t => hoaDonIds.Contains(t.HoaDonId) && !t.IsDeleted)
    //        .Select(t => new ThanhToanTempDto
    //        {
    //            HoaDonId = t.HoaDonId,
    //            SoTien = t.SoTien,
    //            TenPhuongThucThanhToan = t.TenPhuongThucThanhToan
    //        })
    //        .ToListAsync();

    //    // ===== 3️⃣ CÔNG NỢ =====
    //    var noLines = await _context.ChiTietHoaDonNos
    //        .AsNoTracking()
    //        .Where(n => hoaDonIds.Contains(n.HoaDonId) && !n.IsDeleted)
    //        .Select(n => new NoTempDto
    //        {
    //            HoaDonId = n.HoaDonId,
    //            SoTienConLai = n.SoTienConLai
    //        })
    //        .ToListAsync();

    //    // ===== 4️⃣ CHI TIÊU (giống Dashboard) =====
    //    var chiTieus = await _context.ChiTieuHangNgays
    //        .AsNoTracking()
    //        .Where(ct => !ct.IsDeleted
    //            && ct.Ngay >= monthStart
    //            && ct.Ngay < monthEnd)
    //        .Select(ct => new
    //        {
    //            Ngay = ct.Ngay.Date,
    //            ct.ThanhTien
    //        })
    //        .ToListAsync();

    //    // ===== 5️⃣ HELPER =====
    //    static bool IsTienMat(string? ten)
    //    {
    //        if (string.IsNullOrWhiteSpace(ten)) return false;
    //        var s = ten.Trim().ToLower();
    //        return s.Contains("tiền mặt") || s.Contains("tien mat");
    //    }

    //    // ===== 6️⃣ LOOKUP =====
    //    var thanhToanLookup = thanhToans
    //        .GroupBy(x => x.HoaDonId)
    //        .ToDictionary(g => g.Key, g => g.ToList());

    //    var noLookup = noLines
    //        .GroupBy(x => x.HoaDonId)
    //        .ToDictionary(g => g.Key, g => g.Sum(x => x.SoTienConLai));

    //    var chiTieuLookup = chiTieus
    //        .GroupBy(x => x.Ngay)
    //        .ToDictionary(g => g.Key, g => g.Sum(x => x.ThanhTien));

    //    var hoaDonLookup = hoaDons
    //        .GroupBy(x => x.Ngay)
    //        .ToDictionary(g => g.Key, g => g.ToList());

    //    // ===== 7️⃣ TẠO FULL DANH SÁCH NGÀY =====
    //    var totalDays = (monthEnd - monthStart).Days;
    //    var result = new List<DoanhThuThangItemDto>();

    //    for (int i = 0; i < totalDays; i++)
    //    {
    //        var day = monthStart.AddDays(i).Date;

    //        if (!hoaDonLookup.TryGetValue(day, out var dsHoaDon))
    //        {
    //            dsHoaDon = new List<HoaDonTempDto>();
    //        }

    //        decimal tongDoanhThu = 0;
    //        decimal tongChuyenKhoan = 0;
    //        decimal tongTienNo = 0;

    //        foreach (var h in dsHoaDon)
    //        {
    //            tongDoanhThu += h.ThanhTien;

    //            if (thanhToanLookup.TryGetValue(h.Id, out var tts))
    //            {
    //                tongChuyenKhoan += tts
    //                    .Where(t => !IsTienMat(t.TenPhuongThucThanhToan))
    //                    .Sum(t => t.SoTien);
    //            }

    //            if (noLookup.TryGetValue(h.Id, out var no))
    //            {
    //                tongTienNo += no;
    //            }
    //        }

    //        chiTieuLookup.TryGetValue(day, out var tongChiTieu);

    //        result.Add(new DoanhThuThangItemDto
    //        {
    //            Ngay = day,
    //            SoDon = dsHoaDon.Count,
    //            TongTien = tongDoanhThu,
    //            ChiTieu = tongChiTieu,
    //            TienBank = tongChuyenKhoan,
    //            TienNo = tongTienNo,
    //            TongTienMat = tongDoanhThu - tongChuyenKhoan - tongTienNo,

    //            TaiCho = dsHoaDon.Where(x => x.PhanLoai == "Tại Chỗ").Sum(x => x.ThanhTien),
    //            MuaVe = dsHoaDon.Where(x => x.PhanLoai == "Mv").Sum(x => x.ThanhTien),
    //            DiShip = dsHoaDon.Where(x => x.PhanLoai == "Ship").Sum(x => x.ThanhTien),
    //            AppShipping = dsHoaDon.Where(x => x.PhanLoai == "App").Sum(x => x.ThanhTien),

    //            ThuongNha = tongDoanhThu * 0.003m,
    //            ThuongKhanh = tongDoanhThu * 0.003m
    //        });
    //    }

    //    return result.OrderBy(x => x.Ngay).ToList();
    //}
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
    private class HoaDonLite
    {
        public Guid Id { get; set; }
        public Guid? KhachHangId { get; set; }
        public string? TenKhachHangText { get; set; }
        public string? DiaChiText { get; set; }
        public string? PhanLoai { get; set; }
        public string? TenBan { get; set; }
        public DateTime NgayGio { get; set; }
        public DateTime? NgayShip { get; set; }
        public bool BaoDon { get; set; }
        public decimal ThanhTien { get; set; }
        public decimal ConLai { get; set; }
    }
    private class ThanhToanLite
    {
        public Guid HoaDonId { get; set; }
        public decimal SoTien { get; set; }
        public string? TenPhuongThuc { get; set; }
        public DateTime NgayGio { get; set; }
    }
    private class NoLite
    {
        public Guid HoaDonId { get; set; }
        public decimal SoTienConLai { get; set; }
        public DateTime NgayGio { get; set; }
    }

    public class HoaDonTempDto
    {
        public Guid Id { get; set; }
        public DateTime Ngay { get; set; }
        public decimal ThanhTien { get; set; }
        public string? PhanLoai { get; set; }
    }

    public class ThanhToanTempDto
    {
        public Guid HoaDonId { get; set; }
        public decimal SoTien { get; set; }
        public string? TenPhuongThucThanhToan { get; set; }
    }

    public class NoTempDto
    {
        public Guid HoaDonId { get; set; }
        public decimal SoTienConLai { get; set; }
    }
    public class HoaDonPaymentMaskView
    {
        public Guid Id { get; set; }

        public DateTime NgayGio { get; set; }

        public decimal ThanhTien { get; set; }

        public int TongSoLanNhanVoucher { get; set; }

        public int PaymentMethodMask { get; set; }
        public string PhanLoai { get; internal set; }
        public Guid VoucherId { get; internal set; }
        public DateTime NgayShip { get; internal set; }
        public DateTime NgayRa { get; internal set; }
    }
}
