using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Infrastructure.Services;

public class HoaDonService : IHoaDonService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames[("HoaDon")];

    public HoaDonService(AppDbContext context)
    {
        _context = context;
    }

    // ✅ Dùng chung cho mọi nơi
    private static string ResolveTrangThai(decimal thanhTien, decimal daThu, bool coNo, IList<string> methods)
    {
        if (daThu >= thanhTien)
        {
            if (methods.Count > 1) return $"{methods[0]} + {methods[1]}";
            if (methods.Count == 1) return methods[0];
            return "Đã thu";
        }
        if (daThu > 0 && daThu < thanhTien) return coNo ? "Nợ một phần" : "Thu một phần";
        return coNo ? "Ghi nợ" : "Chưa thu";
    }

    // ✅ Dùng cho Create/Update/Delete
    private HoaDonDto ToDto(HoaDon entity)
    {
        var daThu = entity.ChiTietHoaDonThanhToans?.Where(x => !x.IsDeleted).Sum(x => x.SoTien) ?? 0;
        bool coNo = entity.ChiTietHoaDonNos?.Any(x => !x.IsDeleted) == true;
        var methods = entity.ChiTietHoaDonThanhToans?
            .Where(x => !x.IsDeleted)
            .Select(x => x.TenPhuongThucThanhToan)
            .Distinct()
            .ToList() ?? new List<string>();

        var trangThai = ResolveTrangThai(entity.ThanhTien, daThu, coNo, methods);

        return new HoaDonDto
        {
            Id = entity.Id,
            MaHoaDon = entity.MaHoaDon,
            Ngay = entity.Ngay,
            BaoDon = entity.BaoDon,
            UuTien = entity.UuTien,
            NgayGio = entity.NgayGio,
            NgayShip = entity.NgayShip,
            NgayHen = entity.NgayHen,
            NgayRa = entity.NgayRa,
            PhanLoai = entity.PhanLoai,
            TenBan = entity.TenBan,
            TenKhachHangText = !string.IsNullOrWhiteSpace(entity.TenKhachHangText) ? entity.TenKhachHangText : entity.TenBan,
            DiaChiText = entity.DiaChiText,
            SoDienThoaiText = entity.SoDienThoaiText,
            VoucherId = entity.VoucherId,
            KhachHangId = entity.KhachHangId,
            TongTien = entity.TongTien,
            GiamGia = entity.GiamGia,
            ThanhTien = entity.ThanhTien,
            GhiChu = entity.GhiChu,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DaThu = daThu,
            ConLai = entity.ThanhTien - daThu,
            TrangThai = trangThai
        };
    }

    // ✅ Danh sách nhanh

    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto)
    {
        var now = DateTime.Now;

        var khachHang = await GetOrCreateKhachHangAsync(dto, now);
        dto.KhachHangId = khachHang?.Id;

        var entity = new HoaDon
        {
            Id = Guid.NewGuid(),
            MaHoaDon = MaHoaDonGenerator.Generate(),
            //TrangThai = dto.TrangThai,
            NgayRa = dto.NgayRa,
            GhiChu = dto.GhiChu,
            NgayShip = dto.NgayShip,
            NgayHen = dto.NgayHen,
            TenBan = dto.TenBan,
            TenKhachHangText = dto.TenKhachHangText,
            DiaChiText = dto.DiaChiText,
            SoDienThoaiText = dto.SoDienThoaiText,
            VoucherId = dto.VoucherId,
            KhachHangId = dto.KhachHangId,
            Ngay = now.Date,
            BaoDon = dto.BaoDon,
            UuTien = dto.UuTien,
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
        // entity.TrangThai = dto.TrangThai;
        entity.TenBan = dto.TenBan;
        entity.TenKhachHangText = dto.TenKhachHangText;
        entity.NgayShip = dto.NgayShip;
        entity.NgayHen = dto.NgayHen;
        entity.NgayRa = dto.NgayRa;
        entity.GhiChu = dto.GhiChu;

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
    public async Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto)
    {
        var entity = await _context.HoaDons
            // .Include(x => x.KhachHang)
            //      .Include(x => x.ChiTietHoaDonToppings)
            //    .Include(x => x.ChiTietHoaDonVouchers)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

        if (dto.LastModified < entity.LastModified)
            return Result<HoaDonDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var now = DateTime.Now;
        var before = ToDto(entity);

        entity.NgayShip = dto.NgayShip;
        entity.NgayHen = dto.NgayHen;
        entity.BaoDon = dto.BaoDon;
        entity.UuTien = dto.UuTien;
        entity.LastModified = now;

        await _context.SaveChangesAsync();


        var after = ToDto(entity);

        if (before.NgayShip == null && after.NgayShip != null)
        {
            await DiscordService.DiShipAsync(
               $"{(entity.KhachHangId != null ? entity.TenKhachHangText + " / " + before.DiaChiText : entity.TenBan)}\n{entity.GhiChu}"
           );
        }
        if (before.BaoDon == true && after.BaoDon == false)
        {
            await DiscordService.NhanDonAsync(
                   $"{(entity.KhachHangId != null ? entity.TenKhachHangText + " / " + before.DiaChiText : entity.TenBan)}\n{entity.GhiChu}"

         );
        }

        if (before.NgayHen != null && after.NgayHen == null)
        {
            await DiscordService.HenGioAsync(
               $"{(entity.KhachHangId != null ? entity.TenKhachHangText + " / " + before.DiaChiText : entity.TenBan)}\n{entity.GhiChu}"
           );
        }
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
            (!string.IsNullOrWhiteSpace(dto.TenKhachHangText) || !string.IsNullOrWhiteSpace(dto.SoDienThoaiText)))
        {
            khachHang = new KhachHang
            {
                Id = Guid.NewGuid(),
                Ten = dto.TenKhachHangText?.Trim() ?? "Khách lẻ",
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
            .Include(x => x.ChiTietHoaDonThanhToans)
            .Include(x => x.ChiTietHoaDonNos)
            .Include(x => x.ChiTietHoaDonPoints) // ✅ dùng FK HoaDonId
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<HoaDonDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        // ✅ Xoá mềm tất cả chi tiết
        foreach (var ct in entity.ChiTietHoaDons)
        {
            ct.IsDeleted = true;
            ct.LastModified = now;
        }
        foreach (var tp in entity.ChiTietHoaDonToppings)
        {
            tp.IsDeleted = true;
            tp.LastModified = now;
        }
        foreach (var v in entity.ChiTietHoaDonVouchers)
        {
            v.IsDeleted = true;
            v.LastModified = now;
        }
        foreach (var tt in entity.ChiTietHoaDonThanhToans)
        {
            tt.IsDeleted = true;
            tt.LastModified = now;
        }
        foreach (var no in entity.ChiTietHoaDonNos)
        {
            no.IsDeleted = true;
            no.LastModified = now;
        }
        foreach (var p in entity.ChiTietHoaDonPoints)
        {
            p.IsDeleted = true;
            p.LastModified = now;
        }

        // ✅ Xoá mềm hóa đơn chính
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<HoaDonDto>.Success(before, "Xoá hóa đơn thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }
    public async Task<Result<HoaDonDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .Include(x => x.ChiTietHoaDonToppings)
            .Include(x => x.ChiTietHoaDonVouchers)
            .Include(x => x.ChiTietHoaDonThanhToans)
            .Include(x => x.ChiTietHoaDonNos)
            .Include(x => x.ChiTietHoaDonPoints)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<HoaDonDto>.Failure("Hóa đơn chưa bị xoá, không cần khôi phục.");

        var now = DateTime.Now;

        // ✅ Khôi phục chi tiết
        foreach (var ct in entity.ChiTietHoaDons)
        {
            ct.IsDeleted = false;
            ct.LastModified = now;
        }
        foreach (var tp in entity.ChiTietHoaDonToppings)
        {
            tp.IsDeleted = false;
            tp.LastModified = now;
        }
        foreach (var v in entity.ChiTietHoaDonVouchers)
        {
            v.IsDeleted = false;
            v.LastModified = now;
        }
        foreach (var tt in entity.ChiTietHoaDonThanhToans)
        {
            tt.IsDeleted = false;
            tt.LastModified = now;
        }
        foreach (var no in entity.ChiTietHoaDonNos)
        {
            no.IsDeleted = false;
            no.LastModified = now;
        }
        foreach (var p in entity.ChiTietHoaDonPoints)
        {
            p.IsDeleted = false;
            p.LastModified = now;
        }

        // ✅ Khôi phục hóa đơn chính
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        return Result<HoaDonDto>.Success(after, "Khôi phục hóa đơn thành công.")
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
    public async Task<List<HoaDonDto>> GetAllAsync()
    {
        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(h => new
            {
                h.Id,
                h.MaHoaDon,
                h.Ngay,
                h.NgayGio,
                h.BaoDon,
                h.UuTien,
                h.NgayShip,
                h.NgayHen,
                h.NgayRa,
                h.PhanLoai,
                h.TenBan,
                h.TenKhachHangText,
                h.DiaChiText,
                h.SoDienThoaiText,
                h.VoucherId,
                h.KhachHangId,
                h.TongTien,
                h.GiamGia,
                h.ThanhTien,
                h.GhiChu,
                h.CreatedAt,
                h.LastModified,

                DaThu = h.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => (decimal?)t.SoTien) ?? 0,
                Methods = h.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Select(t => t.TenPhuongThucThanhToan).Distinct().ToList(),
                CoNo = h.ChiTietHoaDonNos.Any(n => !n.IsDeleted)
            })
            .OrderByDescending(h => h.LastModified)
            .ToListAsync();

        return list.Select(h => new HoaDonDto
        {
            Id = h.Id,
            MaHoaDon = h.MaHoaDon,
            Ngay = h.Ngay,
            NgayGio = h.NgayGio,
            BaoDon = h.BaoDon,
            UuTien = h.UuTien,
            NgayShip = h.NgayShip,
            NgayHen = h.NgayHen,
            NgayRa = h.NgayRa,
            PhanLoai = h.PhanLoai,
            TenBan = h.TenBan,
            Ten = h.KhachHangId != null ? $"{h.TenKhachHangText} - {h.DiaChiText}" : h.TenBan,
            TenKhachHangText = h.TenKhachHangText,
            DiaChiText = h.DiaChiText,
            SoDienThoaiText = h.SoDienThoaiText,
            VoucherId = h.VoucherId,
            KhachHangId = h.KhachHangId,
            TongTien = h.TongTien,
            GiamGia = h.GiamGia,
            ThanhTien = h.ThanhTien,
            GhiChu = h.GhiChu,
            CreatedAt = h.CreatedAt,
            LastModified = h.LastModified,
            DaThu = h.DaThu,
            ConLai = h.ThanhTien - h.DaThu,
            TrangThai = ResolveTrangThai(h.ThanhTien, h.DaThu, h.CoNo, h.Methods)
        }).ToList();
    }
    public async Task<HoaDonDto?> GetByIdAsync(Guid id)
    {
        var h = await _context.HoaDons.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.MaHoaDon,
                x.Ngay,
                x.NgayGio,
                x.BaoDon,
                x.UuTien,
                x.NgayShip,
                x.NgayHen,
                x.NgayRa,
                x.PhanLoai,
                x.TenBan,
                x.TenKhachHangText,
                x.DiaChiText,
                x.SoDienThoaiText,
                x.VoucherId,
                x.KhachHangId,
                x.TongTien,
                x.GiamGia,
                x.ThanhTien,
                x.GhiChu,
                x.CreatedAt,
                x.LastModified,

                DaThu = x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => (decimal?)t.SoTien) ?? 0,
                Methods = x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Select(t => t.TenPhuongThucThanhToan).Distinct().ToList(),
                CoNo = x.ChiTietHoaDonNos.Any(n => !n.IsDeleted),

                ChiTiets = x.ChiTietHoaDons.Where(ct => !ct.IsDeleted).Select(ct => new ChiTietHoaDonDto
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
                    LastModified = ct.LastModified
                }).ToList(),

                Toppings = x.ChiTietHoaDonToppings.Where(tp => !tp.IsDeleted).Select(tp => new ChiTietHoaDonToppingDto
                {
                    Id = tp.Id,
                    HoaDonId = tp.HoaDonId,
                    ChiTietHoaDonId = tp.ChiTietHoaDonId,
                    ToppingId = tp.ToppingId,
                    Ten = tp.TenTopping,
                    SoLuong = tp.SoLuong,
                    Gia = tp.Gia,
                    CreatedAt = tp.CreatedAt,
                    LastModified = tp.LastModified
                }).ToList(),

                Vouchers = x.ChiTietHoaDonVouchers.Where(v => !v.IsDeleted).Select(v => new ChiTietHoaDonVoucherDto
                {
                    Id = v.Id,
                    HoaDonId = v.HoaDonId,
                    VoucherId = v.VoucherId,
                    Ten = v.TenVoucher,
                    GiaTriApDung = v.GiaTriApDung,
                    CreatedAt = v.CreatedAt,
                    LastModified = v.LastModified
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (h == null) return null;

        var dto = new HoaDonDto
        {
            Id = h.Id,
            MaHoaDon = h.MaHoaDon,
            Ngay = h.Ngay,
            NgayGio = h.NgayGio,
            BaoDon = h.BaoDon,
            UuTien = h.UuTien,
            NgayShip = h.NgayShip,
            NgayHen = h.NgayHen,
            NgayRa = h.NgayRa,
            PhanLoai = h.PhanLoai,
            TenBan = h.TenBan,
            Ten = !string.IsNullOrWhiteSpace(h.TenKhachHangText) ? h.TenKhachHangText : h.TenBan,
            TenKhachHangText = h.TenKhachHangText,
            DiaChiText = h.DiaChiText,
            SoDienThoaiText = h.SoDienThoaiText,
            VoucherId = h.VoucherId,
            KhachHangId = h.KhachHangId,
            TongTien = h.TongTien,
            GiamGia = h.GiamGia,
            ThanhTien = h.ThanhTien,
            GhiChu = h.GhiChu,
            CreatedAt = h.CreatedAt,
            LastModified = h.LastModified,
            DaThu = h.DaThu,
            ConLai = h.ThanhTien - h.DaThu,
            TrangThai = ResolveTrangThai(h.ThanhTien, h.DaThu, h.CoNo, h.Methods),

            ChiTietHoaDons = h.ChiTiets,
            ChiTietHoaDonToppings = h.Toppings,
            ChiTietHoaDonVouchers = h.Vouchers
        };

        // ✅ Tính điểm khách hàng
        if (dto.KhachHangId != null)
        {
            var khId = dto.KhachHangId.Value;
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);

            dto.DiemTrongThang = await _context.ChiTietHoaDonPoints.AsNoTracking()
                .Where(p => p.KhachHangId == khId && p.Ngay >= firstDay && p.Ngay <= now.Date)
                .SumAsync(p => (int?)p.DiemThayDoi) ?? 0;


            dto.TongNoKhachHang = await TinhTongNoKhachHangAsync(khId, dto.Id);
        }
        return dto;
    }
    public async Task<decimal> TinhTongNoKhachHangAsync(Guid khachHangId, Guid? excludeHoaDonId = null)
    {
        var congNoQuery = _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(h => h.KhachHangId == khachHangId && !h.IsDeleted);

        if (excludeHoaDonId.HasValue)
            congNoQuery = congNoQuery.Where(h => h.HoaDonId != excludeHoaDonId.Value);

        var result = await congNoQuery
            .Select(h => new
            {
                ConLai = h.SoTienNo - (_context.ChiTietHoaDonThanhToans
                                    .Where(t => t.ChiTietHoaDonNoId == h.Id && !t.IsDeleted)
                                    .Sum(t => (decimal?)t.SoTien) ?? 0)
            })
            .ToListAsync();

        return result.Sum(x => x.ConLai > 0 ? x.ConLai : 0);
    }

}