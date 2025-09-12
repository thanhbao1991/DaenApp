using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
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
            DaThuHoacGhiNo = daThu > 0 || coNo,

            Id = entity.Id,
            MaHoaDon = entity.MaHoaDon,
            Ngay = entity.Ngay,
            BaoDon = entity.BaoDon,
            UuTien = entity.UuTien,
            NgayGio = entity.NgayGio,
            NgayShip = entity.NgayShip,
            NguoiShip = entity.NguoiShip,
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
            GhiChuShipper = entity.GhiChuShipper,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DaThu = daThu,
            ConLai = entity.ThanhTien - daThu,
            TrangThai = trangThai
        };
    }

    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto)
    {
        var now = DateTime.Now;

        var khachHang = await GetOrCreateKhachHangAsync(dto, now);
        dto.KhachHangId = khachHang?.Id;

        HoaDon entity = new HoaDon
        {
            Id = Guid.NewGuid(),
            MaHoaDon = string.IsNullOrWhiteSpace(dto.MaHoaDon)
                ? MaHoaDonGenerator.Generate()
                : dto.MaHoaDon,
            NgayRa = dto.NgayRa,
            PhanLoai = dto.PhanLoai,
            GhiChu = dto.GhiChu,
            GhiChuShipper = dto.GhiChuShipper,
            NgayShip = dto.NgayShip,
            NguoiShip = dto.NguoiShip,
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

        StringHelper.NormalizeAllStrings(entity);
        _context.HoaDons.Add(entity);

        var (tongTien, giamGia, thanhTien) = await AddChiTietAsync(entity.Id, dto, now);
        entity.TongTien = tongTien;
        entity.GiamGia = giamGia;
        entity.ThanhTien = thanhTien;

        if (string.IsNullOrWhiteSpace(entity.GhiChu))
        {
            var ghiChuTomTat = string.Join(", ",
                dto.ChiTietHoaDons.GroupBy(x => x.TenSanPham.Trim())
                   .Select(g => $"{g.Sum(x => x.SoLuong)} {g.Key}")
            );
            entity.GhiChu = ghiChuTomTat;
        }

        string tenBan = dto.TenBan;
        if (string.IsNullOrWhiteSpace(tenBan))
        {
            int stt = await _context.HoaDons
                .CountAsync(h => h.Ngay == now.Date && h.PhanLoai == dto.PhanLoai && !h.IsDeleted) + 1;

            tenBan = dto.PhanLoai switch
            {
                "Mv" => $"Mv {stt}",
                "Ship" => $"Ship {stt}",
                "App" => $"App {stt}",
                _ => entity?.TenBan ?? ""
            };
        }

        entity!.TenBan = tenBan;

        // 🟟 Dùng LoyaltyService thay vì công thức thủ công
        await AddTichDiemAsync(dto.KhachHangId, thanhTien, entity.Id, now);

        await _context.SaveChangesAsync();

        await DiscordService.SendAsync(
         DiscordEventType.HoaDonNew,
         $"{(entity.KhachHang?.Ten ?? entity.TenBan)} {entity.ThanhTien:N0} đ"
     );

        var after = ToDto(entity);
        return Result<HoaDonDto>.Success(after, "Đã thêm hóa đơn thành công.")
            .WithId(after.Id).WithAfter(after);
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

        entity.TenBan = dto.TenBan;
        entity.PhanLoai = dto.PhanLoai;
        entity.TenKhachHangText = dto.TenKhachHangText;
        entity.NgayShip = dto.NgayShip;
        entity.NguoiShip = dto.NguoiShip;
        entity.NgayHen = dto.NgayHen;
        entity.NgayRa = dto.NgayRa;
        entity.GhiChu = dto.GhiChu;
        entity.GhiChuShipper = dto.GhiChuShipper;
        entity.DiaChiText = dto.DiaChiText;
        entity.SoDienThoaiText = dto.SoDienThoaiText;
        entity.VoucherId = dto.VoucherId;
        entity.KhachHangId = dto.KhachHangId;
        entity.LastModified = now;

        // Xóa cứng dữ liệu con
        _context.ChiTietHoaDonToppings.RemoveRange(entity.ChiTietHoaDonToppings);
        _context.ChiTietHoaDonVouchers.RemoveRange(entity.ChiTietHoaDonVouchers);
        _context.ChiTietHoaDons.RemoveRange(entity.ChiTietHoaDons);

        var (tongTien, giamGia, thanhTien) = await AddChiTietAsync(entity.Id, dto, now);
        entity.TongTien = tongTien;
        entity.GiamGia = giamGia;
        entity.ThanhTien = thanhTien;

        if (string.IsNullOrWhiteSpace(entity.GhiChu))
        {
            var ghiChuTomTat = string.Join(", ",
                dto.ChiTietHoaDons.GroupBy(x => x.TenSanPham.Trim())
                   .Select(g => $"{g.Sum(x => x.SoLuong)} {g.Key}")
            );
            entity.GhiChu = ghiChuTomTat;
        }

        string tenBan = dto.TenBan;
        if (string.IsNullOrWhiteSpace(tenBan))
        {
            int stt = await _context.HoaDons
                .CountAsync(h => h.Ngay == now.Date && h.PhanLoai == dto.PhanLoai && !h.IsDeleted) + 1;

            tenBan = dto.PhanLoai switch
            {
                "Mv" => $"Mv {stt}",
                "Ship" => $"Ship {stt}",
                "App" => $"App {stt}",
                _ => entity?.TenBan ?? ""
            };
        }
        entity!.TenBan = tenBan;
        StringHelper.NormalizeAllStrings(entity);
        // 🟟 cập nhật điểm bằng LoyaltyService
        await UpdateTichDiemAsync(entity.KhachHangId, entity.Id, thanhTien, now);

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.")
            .WithId(id).WithBefore(before).WithAfter(after);
    }

    private Task AddTichDiemAsync(Guid? khachHangId, decimal thanhTien, Guid hoaDonId, DateTime now)
    {
        if (khachHangId == null) return Task.CompletedTask;

        int diemTichLuy = LoyaltyService.TinhDiemTuHoaDon(thanhTien); // 🟟 refactor

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

        return Task.CompletedTask;
    }

    private async Task UpdateTichDiemAsync(Guid? khachHangId, Guid hoaDonId, decimal thanhTien, DateTime now)
    {
        if (khachHangId == null) return;

        var oldLogs = await _context.ChiTietHoaDonPoints
            .Where(x => x.GhiChu.Contains(hoaDonId.ToString()))
            .ToListAsync();

        if (oldLogs.Any())
        {
            _context.ChiTietHoaDonPoints.RemoveRange(oldLogs);
        }

        await AddTichDiemAsync(khachHangId, thanhTien, hoaDonId, now);
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
                x.NguoiShip,
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
                x.GhiChuShipper,
                x.CreatedAt,
                x.LastModified,

                DaThu = x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => (decimal?)t.SoTien) ?? 0,
                Methods = x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Select(t => t.TenPhuongThucThanhToan).Distinct().ToList(),
                CoNo = x.ChiTietHoaDonNos.Any(n => !n.IsDeleted),


                ChiTiets = x.ChiTietHoaDons
                .Where(ct => !ct.IsDeleted)
                .OrderBy(ct => ct.CreatedAt) // 🟟 sắp xếp theo thời điểm tạo
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
                }).ToList(),

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
            NguoiShip = h.NguoiShip,
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

            GhiChuShipper = h.GhiChuShipper,
            CreatedAt = h.CreatedAt,
            LastModified = h.LastModified,
            DaThu = h.DaThu,
            ConLai = h.ThanhTien - h.DaThu,
            DaThuHoacGhiNo = h.DaThu > 0 || h.CoNo,

            TrangThai = ResolveTrangThai(h.ThanhTien, h.DaThu, h.CoNo, h.Methods),

            ChiTietHoaDons = h.ChiTiets,
            ChiTietHoaDonToppings = h.Toppings,
            ChiTietHoaDonVouchers = h.Vouchers,
        };

        // 🟟 gán topping vào từng chi tiết hóa đơn
        foreach (var ct in dto.ChiTietHoaDons)
        {
            ct.ToppingDtos = dto.ChiTietHoaDonToppings
                .Where(tp => tp.ChiTietHoaDonId == ct.Id)
                .Select(tp => new ToppingDto
                {
                    Id = tp.ToppingId,
                    Ten = tp.Ten,
                    Gia = tp.Gia,
                    SoLuong = tp.SoLuong
                })
                .ToList();

            if (string.IsNullOrEmpty(ct.ToppingText) && ct.ToppingDtos.Any())
            {
                ct.ToppingText = string.Join(", ", ct.ToppingDtos.Select(t => $"{t.Ten} x{t.SoLuong}"));
            }
        }

        // 🟟 tính điểm + công nợ qua LoyaltyService
        if (dto.KhachHangId != null)
        {
            var khId = dto.KhachHangId.Value;

            var duocNhanVoucher = await _context.KhachHangs
                .Where(k => k.Id == khId)
                .Select(k => k.DuocNhanVoucher)
                .FirstOrDefaultAsync();

            (int diemThangNay, int diemThangTruoc) =
                await LoyaltyService.TinhDiemThangAsync(_context, khId, DateTime.Now, duocNhanVoucher);

            dto.DiemThangNay = diemThangNay;
            dto.DiemThangTruoc = diemThangTruoc;

            dto.TongNoKhachHang = await LoyaltyService.TinhTongNoKhachHangAsync(_context, khId, dto.Id);
        }


        // đánh lại STT
        int stt = 1;
        foreach (var item in dto.ChiTietHoaDons)
        {
            item.Stt = stt++;
        }
        return dto;
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
        entity.NguoiShip = dto.NguoiShip;
        entity.NgayHen = dto.NgayHen;
        entity.BaoDon = dto.BaoDon;
        entity.UuTien = dto.UuTien;
        entity.LastModified = now;

        await _context.SaveChangesAsync();


        var after = ToDto(entity);

        if (before.NgayShip == null && after.NgayShip != null)
            await DiscordService.SendAsync(DiscordEventType.DangGiaoHang, $"{entity.TenKhachHangText} {entity.DiaChiText}");
        if (before.BaoDon == true && after.BaoDon == false)
            await DiscordService.SendAsync(DiscordEventType.NhanDon, $"{entity.TenKhachHangText} đã nhận đơn");
        if (before.NgayHen != null && after.NgayHen == null)
            await DiscordService.SendAsync(DiscordEventType.HenGio, $"{entity.TenKhachHangText} đã đến giờ hẹn");


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
            StringHelper.NormalizeAllStrings(khachHang);

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
                            DiaChi = StringHelper.CapitalizeEachWord(dto.DiaChiText.Trim()),
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

            //decimal donGia = ct.DonGia > 0 ? ct.DonGia : (bienThe?.GiaBan ?? 0);
            //decimal thanhTienSP = donGia * ct.SoLuong;

            // Trong AddChiTietAsync, ngay sau khi có bienThe và donGia
            decimal giaMacDinh = bienThe?.GiaBan ?? 0;
            decimal donGia = ct.DonGia > 0 ? ct.DonGia : giaMacDinh;
            decimal thanhTienSP = donGia * ct.SoLuong;

            // 🟟 Bổ sung: Nếu khách có giá riêng khác với mặc định → lưu/ cập nhật vào bảng KhachHangGiaBan
            if (dto.KhachHangId != null && bienThe != null && donGia != giaMacDinh)
            {
                var existingCustom = await _context.KhachHangGiaBans
                    .FirstOrDefaultAsync(x =>
                        x.KhachHangId == dto.KhachHangId.Value &&
                        x.SanPhamBienTheId == bienThe.Id &&
                        !x.IsDeleted);

                if (existingCustom == null)
                {
                    var newCustom = new KhachHangGiaBan
                    {
                        Id = Guid.NewGuid(),
                        KhachHangId = dto.KhachHangId.Value,
                        SanPhamBienTheId = bienThe.Id,
                        GiaBan = donGia,
                        CreatedAt = now,
                        LastModified = now,
                        IsDeleted = false
                    };
                    _context.KhachHangGiaBans.Add(newCustom);
                }
                else if (existingCustom.GiaBan != donGia)
                {
                    existingCustom.GiaBan = donGia;
                    existingCustom.LastModified = now;
                }
            }



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

                // ThanhTien = thanhTienSP,
                ThanhTien = thanhTienSP + tienToppingSP,

                TenSanPham = bienThe?.SanPham?.Ten ?? string.Empty,
                TenBienThe = bienThe?.TenBienThe ?? string.Empty,
                ToppingText = ct.ToppingText ?? "",
                NoteText = ct.NoteText,
                CreatedAt = ct.CreatedAt,
                LastModified = ct.LastModified,
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

            giamGia += DiscountHelper.TinhGiamGia(tongTien, voucher.KieuGiam, voucher.GiaTri, lamTron: true);

            _context.ChiTietHoaDonVouchers.Add(vvv);


        }

        if (giamGia > tongTien) giamGia = tongTien;
        decimal thanhTien = tongTien - giamGia;

        return (tongTien, giamGia, thanhTien);
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

        await DiscordService.SendAsync(
    DiscordEventType.HoaDonDel,
    $"🟟️ Đã xoá hoá đơn: {entity.MaHoaDon}\n" +
    $"Khách: {entity.KhachHang?.Ten ?? entity.TenBan}\n" +
    $"Tổng tiền: {entity.ThanhTien:N0} đ"
);


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
        var today = DateTime.Today;
        var fromDate = today.AddDays(-2); // 3 ngày gần đây (hôm nay + 2 ngày trước)

        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => !x.IsDeleted &&
                       (x.Ngay >= fromDate
                       //|| x.TrangThai == "Chưa thu" || x.TrangThai == "Thu một phần"
                       ))
            .Select(h => new
            {

                h.Id,
                h.MaHoaDon,
                h.Ngay,
                h.NgayGio,
                h.BaoDon,
                h.UuTien,
                h.NgayShip,
                h.NguoiShip,
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
                h.GhiChuShipper,
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
            NguoiShip = h.NguoiShip,
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
            GhiChuShipper = h.GhiChuShipper,
            CreatedAt = h.CreatedAt,
            LastModified = h.LastModified,
            DaThu = h.DaThu,
            ConLai = h.ThanhTien - h.DaThu,
            DaThuHoacGhiNo = h.DaThu > 0 || h.CoNo,
            TrangThai = ResolveTrangThai(h.ThanhTien, h.DaThu, h.CoNo, h.Methods)
        }).ToList();
    }






    public async Task<List<HoaDonDto>> GetForShipperAsync()
    {
        var today = DateTime.Today;

        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => !x.IsDeleted
                     && x.PhanLoai == "Ship"
                     && x.Ngay == today.AddDays(0)
                     && x.NgayShip != null
                     && x.NguoiShip == "Khánh")
            .OrderByDescending(x => x.NgayGio)
            .Select(x => new
            {
                x.Id,
                x.TenKhachHangText,
                x.DiaChiText,
                x.SoDienThoaiText,
                x.ThanhTien,
                x.GhiChu,
                x.GhiChuShipper,
                DaThu = x.ChiTietHoaDonThanhToans
                            .Where(t => !t.IsDeleted)
                            .Sum(t => (decimal?)t.SoTien) ?? 0,
                ConLai = x.ThanhTien - (
                            x.ChiTietHoaDonThanhToans
                              .Where(t => !t.IsDeleted)
                              .Sum(t => (decimal?)t.SoTien) ?? 0),
                x.NgayGio,
                x.NgayShip,
                x.NguoiShip,
                x.KhachHangId
            })
            .ToListAsync();

        var result = new List<HoaDonDto>();

        foreach (var h in list)
        {
            var dto = new HoaDonDto
            {
                Id = h.Id,
                TenKhachHangText = h.TenKhachHangText,
                DiaChiText = h.DiaChiText,
                SoDienThoaiText = h.SoDienThoaiText,
                ThanhTien = h.ThanhTien,
                DaThu = h.DaThu,
                ConLai = h.ConLai,
                NgayGio = h.NgayGio,
                NgayShip = h.NgayShip,
                NguoiShip = h.NguoiShip,
                GhiChu = h.GhiChu,
                GhiChuShipper = h.GhiChuShipper,
            };

            if (h.KhachHangId != null)
            {
                dto.TongNoKhachHang = await LoyaltyService
                    .TinhTongNoKhachHangAsync(_context, h.KhachHangId.Value, h.Id);
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<Result<HoaDonDto>> ThuTienMatAsync(Guid id)
    {
        var entity = await _context.HoaDons
         .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
            .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
              .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");
        if (!string.IsNullOrEmpty(entity.GhiChuShipper))
        {
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
        }
        var now = DateTime.Now;
        var before = ToDto(entity);

        // 1) Tính số tiền còn phải thu (không đụng DaThu/ConLai của HoaDon)
        var daThu = entity.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => t.SoTien);
        var soTienThu = entity.ThanhTien - daThu;

        if (soTienThu > 0)
        {
            // Lấy phương thức "Tiền mặt"
            var pm = await _context.PhuongThucThanhToans
                .Where(p => !p.IsDeleted && p.Ten == "Tiền mặt")
                .Select(p => new { p.Id, p.Ten })
                .FirstOrDefaultAsync();

            if (pm == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

            // Quyết định LoaiThanhToan: F1 hay F1a
            bool daCoNo = entity.ChiTietHoaDonNos.Any(n => !n.IsDeleted);
            var loai = daCoNo
                ? (entity.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày") // F1a
                : "Trong ngày";                                                       // F1

            var thanhToan = new ChiTietHoaDonThanhToan
            {
                Id = Guid.NewGuid(),
                HoaDonId = entity.Id,
                KhachHangId = entity.KhachHangId ?? Guid.Empty,
                Ngay = now.Date,
                NgayGio = now,
                SoTien = soTienThu,
                LoaiThanhToan = loai,
                PhuongThucThanhToanId = pm.Id,
                TenPhuongThucThanhToan = pm.Ten, // tránh lỗi NOT NULL
                GhiChu = "Shipper",
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false,
                ChiTietHoaDonNoId = daCoNo
                    ? entity.ChiTietHoaDonNos.Where(n => !n.IsDeleted)
                        .OrderByDescending(n => n.Ngay)
                        .Select(n => n.Id)
                        .FirstOrDefault()
                    : (Guid?)null
            };

            _context.ChiTietHoaDonThanhToans.Add(thanhToan);
            entity.LastModified = now;
        }

        // 2) Cập nhật ghi chú shipper (chuẩn F1)
        entity.GhiChuShipper = $"Tiền mặt: {soTienThu:N0} đ";
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        // 3) Discord như các flow khác
        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        // 4) Trả kết quả (Controller đã NotifyClients("updated", id) nên SignalR ok)
        return Result<HoaDonDto>.Success(after, "Cập nhật hoá đơn thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }
    public async Task<Result<HoaDonDto>> TraNoAsync(Guid id, decimal soTienKhachDua)
    {
        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
            .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

        var now = DateTime.Now;
        var before = ToDto(entity);

        if (entity.KhachHangId == null)
            return Result<HoaDonDto>.Failure("Hoá đơn này không có khách hàng, không thể trả nợ.");

        // 🟟 Tiền khách đưa nhập theo NGÀN đồng → đổi sang đồng
        decimal soTienThucTe = soTienKhachDua * 1000;

        // 🟟 Tính số tiền còn lại của đơn hôm nay (chỉ để kiểm tra, không tạo thanh toán cho đơn này nữa)
        decimal daThu = entity.ChiTietHoaDonThanhToans
            .Where(t => t.GhiChu == "Shipper")
            .Sum(t => t.SoTien);

        // Nếu khách đưa < Còn lại của đơn hôm nay ⇒ chặn
        decimal soTienTraNo = soTienThucTe - daThu;
        if (soTienTraNo <= 0)
            return Result<HoaDonDto>.Failure("Khách không đưa dư sau phần đã thu của đơn hôm nay.");

        var khId = entity.KhachHangId.Value;

        // Tính tổng nợ cũ (không tính chính hóa đơn hiện tại)
        var tongNoCu = await LoyaltyService.TinhTongNoKhachHangAsync(_context, khId, entity.Id);
        if (tongNoCu <= 0)
            return Result<HoaDonDto>.Failure("Khách hàng không còn nợ để trả.");

        decimal soTienCon = Math.Min(soTienTraNo, tongNoCu);
        decimal traNoCu = 0;

        // 🟟 Lấy phương thức "Tiền mặt"
        var pm = await _context.PhuongThucThanhToans
            .Where(p => !p.IsDeleted && p.Ten == "Tiền mặt")
            .Select(p => new { p.Id, p.Ten })
            .FirstOrDefaultAsync();

        if (pm == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

        // 🟟 Duyệt các dòng nợ cũ (FIFO) và cấn trừ
        var noConLaiList = await _context.ChiTietHoaDonNos
            .Where(n => !n.IsDeleted && n.KhachHangId == khId && n.HoaDonId != entity.Id)
            .Select(n => new
            {
                No = n,
                DaTra = _context.ChiTietHoaDonThanhToans
                    .Where(t => !t.IsDeleted && t.ChiTietHoaDonNoId == n.Id)
                    .Sum(t => (decimal?)t.SoTien) ?? 0
            })
            .OrderBy(x => x.No.NgayGio)
            .ToListAsync();

        foreach (var x in noConLaiList)
        {
            var soNoCon = x.No.SoTienNo - x.DaTra;
            if (soNoCon <= 0) continue;

            var tra = Math.Min(soTienCon, soNoCon);
            if (tra <= 0) break;

            _context.ChiTietHoaDonThanhToans.Add(new ChiTietHoaDonThanhToan
            {
                Id = Guid.NewGuid(),
                HoaDonId = x.No.HoaDonId, // gắn vào hóa đơn nợ cũ
                KhachHangId = khId,
                Ngay = now.Date,
                NgayGio = now,
                SoTien = tra,
                LoaiThanhToan = x.No.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                PhuongThucThanhToanId = pm.Id,
                TenPhuongThucThanhToan = pm.Ten, // luôn là Tiền mặt
                GhiChu = $"Shipper",
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false,
                ChiTietHoaDonNoId = x.No.Id
            });

            traNoCu += tra;
            soTienCon -= tra;

            if (soTienCon <= 0) break;
        }

        if (traNoCu <= 0)
        {
            return Result<HoaDonDto>.Failure("Khách đưa chỉ đủ trả đơn hôm nay, không có dư để trả nợ.");
        }

        // 🟟 Cập nhật GhiChuShipper cho hóa đơn hiện tại (chỉ để hiển thị)
        // 2️⃣ Cập nhật GhiChuShipper (giữ lại thông tin cũ + thêm trả nợ)
        var ghiChuCu = string.IsNullOrWhiteSpace(entity.GhiChuShipper) ? "" : entity.GhiChuShipper + " | ";
        entity.GhiChuShipper = $"{ghiChuCu}Trả nợ: {traNoCu:N0} đ";
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        // 🟟 Gửi thông báo Discord
        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        return Result<HoaDonDto>.Success(after, "Đã ghi nhận khách trả nợ.")
            .WithId(entity.Id)
            .WithBefore(before)
            .WithAfter(after);
    }
    public async Task<Result<HoaDonDto>> GhiNoAsync(Guid id)
    {
        var entity = await _context.HoaDons
         .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
            .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");
        if (!string.IsNullOrEmpty(entity.GhiChuShipper))
        {
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
        }
        var now = DateTime.Now;
        var before = ToDto(entity);

        // 1️⃣ Tính số tiền còn lại
        var daThu = entity.ChiTietHoaDonThanhToans
                        .Where(t => !t.IsDeleted)
                        .Sum(t => t.SoTien);
        var soTienNo = entity.ThanhTien - daThu;

        // 2️⃣ Chỉ tạo nợ nếu hóa đơn chưa từng có nợ
        if (soTienNo > 0 && !entity.ChiTietHoaDonNos.Any(x => !x.IsDeleted))
        {
            var no = new ChiTietHoaDonNo
            {
                Id = Guid.NewGuid(),
                HoaDonId = entity.Id,
                KhachHangId = entity.KhachHangId ?? Guid.Empty,
                Ngay = now.Date,
                NgayGio = now,
                SoTienNo = soTienNo,    // ✅ nợ toàn bộ số còn lại
                GhiChu = "Shipper",
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false
            };
            _context.ChiTietHoaDonNos.Add(no);
        }

        // 3️⃣ Cập nhật GhiChuShipper
        entity.GhiChuShipper = $"Ghi nợ: {soTienNo:N0} đ";
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        return Result<HoaDonDto>.Success(after, "Đã ghi nợ cho hóa đơn.")
            .WithId(id).WithBefore(before).WithAfter(after);
    }
    public async Task<Result<HoaDonDto>> ThuChuyenKhoanAsync(Guid id)
    {
        var entity = await _context.HoaDons
          .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
            .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
               .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");
        if (!string.IsNullOrEmpty(entity.GhiChuShipper))
        {
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
        }
        var now = DateTime.Now;
        var before = ToDto(entity);

        // 1) Tính số tiền còn phải thu từ các thanh toán
        var daThu = entity.ChiTietHoaDonThanhToans
                            .Where(t => !t.IsDeleted)
                            .Sum(t => t.SoTien);
        var soTienThu = entity.ThanhTien - daThu;

        if (soTienThu > 0)
        {
            var daCoNo = entity.ChiTietHoaDonNos.Any(x => !x.IsDeleted);

            var thanhToan = new ChiTietHoaDonThanhToan
            {
                Id = Guid.NewGuid(),
                HoaDonId = entity.Id,
                KhachHangId = entity.KhachHangId ?? Guid.Empty,
                Ngay = now.Date,
                NgayGio = now,
                SoTien = soTienThu,
                // 🟟 LoaiThanhToan logic:
                LoaiThanhToan = daCoNo
                    ? (entity.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày")
                    : "Trong ngày",
                // 🟟 Phương thức thanh toán: Chuyển khoản
                PhuongThucThanhToanId = Guid.Parse("2cf9a88f-3bc0-4eb5-940d-f8ffa4affa02"),
                TenPhuongThucThanhToan = "Chuyển khoản",
                GhiChu = "Shipper",
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false,
                ChiTietHoaDonNoId = daCoNo
                    ? entity.ChiTietHoaDonNos
                            .Where(x => !x.IsDeleted)
                            .OrderByDescending(x => x.Ngay)
                            .Select(x => x.Id)
                            .FirstOrDefault()
                    : (Guid?)null
            };

            _context.ChiTietHoaDonThanhToans.Add(thanhToan);
            entity.LastModified = now;
        }

        // 2) Cập nhật ghi chú shipper (chuẩn F4)
        entity.GhiChuShipper = $"Chuyển khoản: {soTienThu:N0} đ";
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        // 3) Gửi thông báo ra Discord
        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        // 4) Trả kết quả
        return Result<HoaDonDto>.Success(after, "Cập nhật hoá đơn thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }
    public async Task<Result<HoaDonDto>> TiNuaChuyenKhoanAsync(Guid id)
    {
        var entity = await _context.HoaDons
          .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");
        if (!string.IsNullOrEmpty(entity.GhiChuShipper))
        {
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
        }
        var now = DateTime.Now;
        var before = ToDto(entity);

        decimal daThu = await _context.ChiTietHoaDonThanhToans
    .Where(t => !t.IsDeleted && t.HoaDonId == entity.Id)
    .SumAsync(t => (decimal?)t.SoTien) ?? 0;

        decimal conLai = entity.ThanhTien - daThu;
        entity.GhiChuShipper = $"Tí nữa chuyển khoản: {conLai:N0} đ";
        entity.LastModified = now;
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

}