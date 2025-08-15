using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class HoaDonService : IHoaDonService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames[("HoaDon")];

    public HoaDonService(AppDbContext context)
    {
        _context = context;
    }

    private HoaDonDto ToDto(HoaDon entity)
    {
        var tongDaThu = entity.ChiTietHoaDonThanhToans?
        .Where(x => !x.IsDeleted)
        .Sum(x => x.SoTien) ?? 0;

        return new HoaDonDto
        {
            Id = entity.Id,
            PhanLoai = entity.PhanLoai,
            Ten = entity.KhachHang != null ? entity.KhachHang.Ten + " / " + entity.DiaChiText : entity.TenBan,
            MaHoaDon = entity.MaHoaDon,
            TrangThai = entity.TrangThai,
            Ngay = entity.Ngay,
            NgayGio = entity.NgayGio,
            TenBan = entity.TenBan,
            DiaChiText = entity.DiaChiText,
            SoDienThoaiText = entity.SoDienThoaiText,
            VoucherId = entity.VoucherId,
            KhachHangId = entity.KhachHangId,
            TongTien = entity.TongTien,
            GiamGia = entity.GiamGia,
            ThanhTien = entity.ThanhTien,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DaThu = tongDaThu,
            ConLai = entity.ThanhTien - tongDaThu,

            ChiTietHoaDons = entity.ChiTietHoaDons?
                .Where(x => !x.IsDeleted)
                .Select(ct => new ChiTietHoaDonDto
                {
                    Id = ct.Id,
                    HoaDonId = ct.HoaDonId,
                    SanPhamIdBienThe = ct.SanPhamBienTheId,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    TenSanPham = ct.TenSanPham ?? "",
                    TenBienThe = ct.TenBienThe,
                    ToppingText = ct.ToppingText,
                    NoteText = ct.NoteText,
                    CreatedAt = ct.CreatedAt,
                    LastModified = ct.LastModified,
                })
                .ToList() ?? new List<ChiTietHoaDonDto>(),

            ChiTietHoaDonToppings = entity.ChiTietHoaDonToppings?
                .Where(x => !x.IsDeleted)
                .Select(tp => new ChiTietHoaDonToppingDto
                {
                    Id = tp.Id,
                    HoaDonId = tp.HoaDonId,
                    ChiTietHoaDonId = tp.ChiTietHoaDonId,
                    ToppingId = tp.ToppingId,
                    Ten = tp.TenTopping,
                    SoLuong = tp.SoLuong,
                    Gia = tp.Gia,
                    CreatedAt = tp.CreatedAt,
                    LastModified = tp.LastModified,
                })
                .ToList() ?? new List<ChiTietHoaDonToppingDto>(),

            ChiTietHoaDonVouchers = entity.ChiTietHoaDonVouchers?
                .Where(x => !x.IsDeleted)
                .Select(v => new ChiTietHoaDonVoucherDto
                {
                    Id = v.Id,
                    HoaDonId = v.HoaDonId,
                    VoucherId = v.VoucherId,
                    Ten = v.TenVoucher,
                    GiaTriApDung = v.GiaTriApDung,
                    CreatedAt = v.CreatedAt,
                    LastModified = v.LastModified,
                })
                .ToList() ?? new List<ChiTietHoaDonVoucherDto>()
        };
    }

    public async Task<List<HoaDonDto>> GetAllAsync()
    {
        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Ngay >= DateTime.Today.AddDays(-1))
            //.Include(h => h.ChiTietHoaDons)
            .Include(h => h.KhachHang)
            .Include(h => h.ChiTietHoaDonThanhToans)
                .ThenInclude(ct => ct.PhuongThucThanhToan)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<HoaDonDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .Include(x => x.ChiTietHoaDonToppings)
            .Include(x => x.ChiTietHoaDonVouchers)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto)
    {
        var now = DateTime.Now;

        var khachHang = await GetOrCreateKhachHangAsync(dto, now);
        dto.KhachHangId = khachHang?.Id;

        var entity = new HoaDon
        {
            Id = Guid.NewGuid(),
            MaHoaDon = MaHoaDonGenerator.Generate(),
            TrangThai = dto.TrangThai,
            TenBan = dto.TenBan,
            DiaChiText = dto.DiaChiText,
            SoDienThoaiText = dto.SoDienThoaiText,
            VoucherId = dto.VoucherId,
            KhachHangId = dto.KhachHangId,
            Ngay = now.Date,
            NgayGio = now,
            LastModified = now,
            CreatedAt = now,
            IsDeleted = false
        };

        _context.HoaDons.Add(entity);

        var (tongTien, giamGia, thanhTien) = await AddChiTietAsync(entity.Id, dto, now);
        entity.TongTien = tongTien;
        entity.GiamGia = giamGia;
        entity.ThanhTien = thanhTien;

        await AddTichDiemAsync(dto.KhachHangId, thanhTien, entity.Id, now);

        await _context.SaveChangesAsync();
        var after = ToDto(entity);
        return Result<HoaDonDto>.Success(after, "Đã thêm hóa đơn thành công.").WithId(after.Id).WithAfter(after);
    }

    public async Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto)
    {
        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .Include(x => x.ChiTietHoaDonToppings)
            .Include(x => x.ChiTietHoaDonVouchers)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

        if (dto.LastModified < entity.LastModified)
            return Result<HoaDonDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var now = DateTime.Now;
        var before = ToDto(entity);

        var khachHang = await GetOrCreateKhachHangAsync(dto, now);
        dto.KhachHangId = khachHang?.Id;

        // entity.MaHoaDon = dto.MaHoaDon;
        entity.TrangThai = dto.TrangThai;
        entity.TenBan = dto.TenBan;
        entity.DiaChiText = dto.DiaChiText;
        entity.SoDienThoaiText = dto.SoDienThoaiText;
        entity.VoucherId = dto.VoucherId;
        entity.KhachHangId = dto.KhachHangId;
        entity.LastModified = now;

        // ✅ Xóa cứng dữ liệu bảng con trước khi thêm mới
        _context.ChiTietHoaDonToppings.RemoveRange(entity.ChiTietHoaDonToppings);
        _context.ChiTietHoaDonVouchers.RemoveRange(entity.ChiTietHoaDonVouchers);
        _context.ChiTietHoaDons.RemoveRange(entity.ChiTietHoaDons);

        var (tongTien, giamGia, thanhTien) = await AddChiTietAsync(entity.Id, dto, now);
        entity.TongTien = tongTien;
        entity.GiamGia = giamGia;
        entity.ThanhTien = thanhTien;

        // ✅ Cập nhật lại điểm
        await UpdateTichDiemAsync(entity.KhachHangId, entity.Id, thanhTien, now);

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    private async Task<KhachHang?> GetOrCreateKhachHangAsync(HoaDonDto dto, DateTime now)
    {
        KhachHang? khachHang = null;

        if (dto.KhachHangId != null)
        {
            khachHang = await _context.KhachHangs
                .Include(kh => kh.KhachHangAddresses)
                .Include(kh => kh.KhachHangPhones)
                .FirstOrDefaultAsync(kh => kh.Id == dto.KhachHangId);
        }
        else if (!string.IsNullOrWhiteSpace(dto.SoDienThoaiText))
        {
            khachHang = await _context.KhachHangs
                .Include(kh => kh.KhachHangAddresses)
                .Include(kh => kh.KhachHangPhones)
                .FirstOrDefaultAsync(kh => kh.KhachHangPhones.Any(d => d.SoDienThoai == dto.SoDienThoaiText));
        }

        if (khachHang == null &&
            (!string.IsNullOrWhiteSpace(dto.TenKhachHang) || !string.IsNullOrWhiteSpace(dto.SoDienThoaiText)))
        {
            khachHang = new KhachHang
            {
                Id = Guid.NewGuid(),
                Ten = dto.TenKhachHang?.Trim() ?? "Khách lẻ",
                DuocNhanVoucher = true,
                CreatedAt = now,
                LastModified = now
            };

            if (!string.IsNullOrWhiteSpace(dto.SoDienThoaiText))
            {
                khachHang.KhachHangPhones = new List<KhachHangPhone>
                {
                    new KhachHangPhone
                    {
                        Id = Guid.NewGuid(),
                        SoDienThoai = dto.SoDienThoaiText.Trim(),
                        IsDefault = true
                    }
                };
            }

            if (!string.IsNullOrWhiteSpace(dto.DiaChiText))
            {
                khachHang.KhachHangAddresses = new List<KhachHangAddress>
                {
                    new KhachHangAddress
                    {
                        Id = Guid.NewGuid(),
                        DiaChi = dto.DiaChiText.Trim(),
                        IsDefault = true
                    }
                };
            }

            _context.KhachHangs.Add(khachHang);
        }
        else if (khachHang != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.SoDienThoaiText) &&
                !khachHang.KhachHangPhones.Any(d => d.SoDienThoai == dto.SoDienThoaiText.Trim()))
            {
                khachHang.KhachHangPhones.Add(new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = khachHang.Id,
                    SoDienThoai = dto.SoDienThoaiText.Trim(),
                    IsDefault = false
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.DiaChiText) &&
                !khachHang.KhachHangAddresses.Any(d => d.DiaChi.ToLower() == dto.DiaChiText.Trim().ToLower()))
            {
                khachHang.KhachHangAddresses.Add(new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = khachHang.Id,
                    DiaChi = dto.DiaChiText.Trim(),
                    IsDefault = false
                });
            }
        }

        return khachHang;
    }

    private async Task<(decimal tongTien, decimal giamGia, decimal thanhTien)> AddChiTietAsync(Guid hoaDonId, HoaDonDto dto, DateTime now)
    {
        decimal tongTien = 0;

        var toppingLookup = dto.ChiTietHoaDonToppings
            .GroupBy(tp => tp.ChiTietHoaDonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var ct in dto.ChiTietHoaDons)
        {
            var bienThe = await _context.SanPhamBienThes
                .Include(bt => bt.SanPham)
                .FirstOrDefaultAsync(bt => bt.Id == ct.SanPhamIdBienThe);

            decimal donGia = bienThe?.GiaBan ?? ct.DonGia;
            decimal thanhTienSP = donGia * ct.SoLuong;

            decimal tienToppingSP = 0;
            Guid chiTietId = Guid.NewGuid();

            if (toppingLookup.TryGetValue(ct.Id, out var tpList))
            {
                foreach (var tp in tpList)
                {
                    var topping = await _context.Toppings.FirstOrDefaultAsync(x => x.Id == tp.ToppingId);
                    decimal giaTopping = topping?.Gia ?? tp.Gia;
                    decimal tienTP = giaTopping * tp.SoLuong;
                    tienToppingSP += tienTP;

                    _context.ChiTietHoaDonToppings.Add(new ChiTietHoaDonTopping
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = hoaDonId,
                        ChiTietHoaDonId = chiTietId,
                        TenTopping = tp.Ten,
                        ToppingId = tp.ToppingId,
                        SoLuong = tp.SoLuong,
                        Gia = giaTopping,
                        CreatedAt = now,
                        LastModified = now,
                        IsDeleted = false
                    });
                }
            }

            tongTien += thanhTienSP + tienToppingSP;

            _context.ChiTietHoaDons.Add(new ChiTietHoaDonEntity
            {
                Id = chiTietId,
                HoaDonId = hoaDonId,
                SanPhamBienTheId = ct.SanPhamIdBienThe,
                SoLuong = ct.SoLuong,
                DonGia = donGia,
                ThanhTien = thanhTienSP,
                TenSanPham = bienThe?.SanPham?.Ten ?? string.Empty,
                TenBienThe = bienThe?.TenBienThe ?? string.Empty,
                ToppingText = ct.ToppingText ?? "",
                NoteText = ct.NoteText,
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false
            });
        }

        decimal giamGia = 0;

        foreach (var v in dto.ChiTietHoaDonVouchers ?? Enumerable.Empty<ChiTietHoaDonVoucherDto>())
        {
            if (v.VoucherId == Guid.Empty) continue;

            var voucher = await _context.Vouchers.FindAsync(v.VoucherId);
            if (voucher == null) continue;

            var vvv = new ChiTietHoaDonVoucher
            {
                Id = Guid.NewGuid(),
                HoaDonId = hoaDonId,
                VoucherId = v.VoucherId,
                TenVoucher = voucher.Ten,
                GiaTriApDung = voucher.GiaTri,
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false
            };

            if (voucher.KieuGiam == "%")
            {
                var mucGiam = tongTien * (voucher.GiaTri / 100m);
                giamGia += Math.Min(mucGiam, tongTien);
            }
            else
            {
                giamGia += voucher.GiaTri;
            }
            _context.ChiTietHoaDonVouchers.Add(vvv);
        }

        if (giamGia > tongTien) giamGia = tongTien;
        decimal thanhTien = tongTien - giamGia;

        return (tongTien, giamGia, thanhTien);
    }

    private async Task AddTichDiemAsync(Guid? khachHangId, decimal thanhTien, Guid hoaDonId, DateTime now)
    {
        if (khachHangId == null) return;

        int diemTichLuy = (int)Math.Floor(thanhTien * 0.01m);

        _context.ChiTietHoaDonPoints.Add(new ChiTietHoaDonPoint
        {
            Id = Guid.NewGuid(),
            HoaDonId = hoaDonId,
            KhachHangId = khachHangId.Value,
            Ngay = now.Date,
            NgayGio = now,
            DiemThayDoi = diemTichLuy,
            GhiChu = $"Tích điểm từ hoá đơn {hoaDonId}",
            CreatedAt = now,
            LastModified = now
        });

        var point = await _context.KhachHangPoints.FirstOrDefaultAsync(x => x.KhachHangId == khachHangId.Value);
        if (point == null)
        {
            _context.KhachHangPoints.Add(new KhachHangPoint
            {
                Id = Guid.NewGuid(),
                KhachHangId = khachHangId.Value,
                TongDiem = diemTichLuy,
                CreatedAt = now,
                LastModified = now,

            });
        }
        else
        {
            point.TongDiem += diemTichLuy;
            point.LastModified = now;
        }
    }

    private async Task UpdateTichDiemAsync(Guid? khachHangId, Guid hoaDonId, decimal thanhTien, DateTime now)
    {
        if (khachHangId == null) return;

        var oldLogs = await _context.ChiTietHoaDonPoints
            .Where(x => x.GhiChu.Contains(hoaDonId.ToString()))
            .ToListAsync();

        if (oldLogs.Any())
        {
            int oldPoints = oldLogs.Sum(l => l.DiemThayDoi);
            var point = await _context.KhachHangPoints.FirstOrDefaultAsync(x => x.KhachHangId == khachHangId);
            if (point != null)
            {
                point.TongDiem -= oldPoints;
                if (point.TongDiem < 0) point.TongDiem = 0;
                point.LastModified = now;
            }
            _context.ChiTietHoaDonPoints.RemoveRange(oldLogs);
        }

        await AddTichDiemAsync(khachHangId, thanhTien, hoaDonId, now);
    }

    public async Task<Result<HoaDonDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .Include(x => x.ChiTietHoaDonToppings)
            .Include(x => x.ChiTietHoaDonVouchers)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<HoaDonDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        // ✅ Xóa mềm bản ghi liên quan
        foreach (var ct in entity.ChiTietHoaDons) { ct.IsDeleted = true; ct.LastModified = now; }
        foreach (var tp in entity.ChiTietHoaDonToppings) { tp.IsDeleted = true; tp.LastModified = now; }
        foreach (var v in entity.ChiTietHoaDonVouchers) { v.IsDeleted = true; v.LastModified = now; }

        // ✅ Trừ điểm tích lũy
        var logs = await _context.ChiTietHoaDonPoints
            .Where(l => l.GhiChu.Contains(id.ToString()))
            .ToListAsync();

        if (logs.Any())
        {
            int points = logs.Sum(l => l.DiemThayDoi);
            var diemKH = await _context.KhachHangPoints.FirstOrDefaultAsync(x => x.KhachHangId == entity.KhachHangId);
            if (diemKH != null)
            {
                diemKH.TongDiem -= points;
                if (diemKH.TongDiem < 0) diemKH.TongDiem = 0;
                diemKH.LastModified = now;
            }
            _context.ChiTietHoaDonPoints.RemoveRange(logs);
        }

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<HoaDonDto>.Success(before, $"Xoá hóa đơn thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<HoaDonDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .Include(x => x.ChiTietHoaDonToppings)
            .Include(x => x.ChiTietHoaDonVouchers)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<HoaDonDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        var now = DateTime.Now;

        // ✅ Khôi phục hóa đơn
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = now;

        // ✅ Khôi phục các bảng con
        foreach (var ct in entity.ChiTietHoaDons) { ct.IsDeleted = false; ct.LastModified = now; }
        foreach (var tp in entity.ChiTietHoaDonToppings) { tp.IsDeleted = false; tp.LastModified = now; }
        foreach (var v in entity.ChiTietHoaDonVouchers) { v.IsDeleted = false; v.LastModified = now; }

        // ✅ Cộng lại điểm tích lũy
        if (entity.KhachHangId != null)
        {
            int diemTichLuy = (int)Math.Floor(entity.ThanhTien * 0.01m);

            _context.ChiTietHoaDonPoints.Add(new ChiTietHoaDonPoint
            {
                Id = Guid.NewGuid(),
                KhachHangId = entity.KhachHangId.Value,
                Ngay = now.Date,
                NgayGio = now,

                DiemThayDoi = diemTichLuy,
                GhiChu = $"Tích điểm từ hoá đơn {entity.Id}",
                CreatedAt = now,
                LastModified = now
            });

            var point = await _context.KhachHangPoints
                .FirstOrDefaultAsync(x => x.KhachHangId == entity.KhachHangId);

            if (point == null)
            {
                _context.KhachHangPoints.Add(new KhachHangPoint
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = entity.KhachHangId.Value,
                    TongDiem = diemTichLuy,
                    CreatedAt = now,
                    LastModified = now
                });
            }
            else
            {
                point.TongDiem += diemTichLuy;
                point.LastModified = now;
            }
        }

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<HoaDonDto>.Success(after, $"Khôi phục hóa đơn thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<HoaDonDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}