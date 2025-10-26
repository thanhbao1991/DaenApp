using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
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

    private HoaDonDto ToDto(HoaDon entity)
    {
        var pays = entity.ChiTietHoaDonThanhToans?.Where(t => !t.IsDeleted).ToList() ?? new List<ChiTietHoaDonThanhToan>();
        bool coTienMat = pays.Any(t => (t.TenPhuongThucThanhToan ?? "").Contains("Tiền mặt"));
        bool coChuyenKhoan = pays.Any(t => (t.TenPhuongThucThanhToan ?? "").Contains("Chuyển khoản"));

        var trangThai = HoaDonHelper.ResolveTrangThai(entity.ThanhTien, entity.ConLai, entity.HasDebt, coTienMat, coChuyenKhoan);



        return new HoaDonDto
        {
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
            ConLai = entity.ConLai,
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
            Id = dto.Id,
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

        // ✅ đảm bảo trạng thái ban đầu
        entity.ConLai = entity.ThanhTien;    // mới tạo/ghi lại chi tiết → chưa thu gì
        entity.HasDebt = false;

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
            var start = now.Date; var end = start.AddDays(1);
            int stt = await _context.HoaDons
                .CountAsync(h => !h.IsDeleted && h.PhanLoai == dto.PhanLoai
                              && h.Ngay >= start && h.Ngay < end) + 1;

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
            var start = now.Date; var end = start.AddDays(1);
            int stt = await _context.HoaDons
                .CountAsync(h => !h.IsDeleted && h.PhanLoai == dto.PhanLoai
                              && h.Ngay >= start && h.Ngay < end) + 1;

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

        // ✅ đồng bộ lại theo thực tế thanh toán/nợ (nếu đã có)
        await _context.SaveChangesAsync();

        await HoaDonHelper.RecalcConLaiAsync(_context, entity.Id);
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
                x.ConLai,
                x.HasDebt,

                CoTienMat = _context.ChiTietHoaDonThanhToans.Any(t =>
            !t.IsDeleted && t.HoaDonId == x.Id &&
            t.TenPhuongThucThanhToan != null &&
            t.TenPhuongThucThanhToan.Contains("Tiền mặt")),

                CoChuyenKhoan = _context.ChiTietHoaDonThanhToans.Any(t =>
                    !t.IsDeleted && t.HoaDonId == x.Id &&
                    t.TenPhuongThucThanhToan != null &&
                    t.TenPhuongThucThanhToan.Contains("Chuyển khoản")),


                ChiTiets = x.ChiTietHoaDons
                .Where(ct => !ct.IsDeleted)
                .OrderBy(ct => ct.Stt) // 🟟 sắp xếp theo thời điểm tạo
                .Select(ct => new ChiTietHoaDonDto
                {
                    Id = ct.Id,
                    HoaDonId = ct.HoaDonId,
                    SanPhamIdBienThe = ct.SanPhamBienTheId,
                    SanPhamId = ct.SanPhamId,
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
            ConLai = h.ConLai,
            HasDebt = h.HasDebt,

            TrangThai = HoaDonHelper.ResolveTrangThai(h.ThanhTien, h.ConLai, h.HasDebt, h.CoTienMat, h.CoChuyenKhoan),

            ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(h.ChiTiets),
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
        var phone = (dto.SoDienThoaiText ?? "").Trim();
        var addr = (dto.DiaChiText ?? "").Trim();
        var name = string.IsNullOrWhiteSpace(dto.TenKhachHangText) ? "Khách lẻ" : dto.TenKhachHangText.Trim();

        KhachHang? kh = null;

        if (dto.KhachHangId != null)
        {
            kh = await _context.KhachHangs.FirstOrDefaultAsync(x => x.Id == dto.KhachHangId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(phone))
        {
            var khId = await _context.KhachHangPhones
                .AsNoTracking()
                .Where(p => p.SoDienThoai == phone)
                .Select(p => (Guid?)p.KhachHangId)
                .FirstOrDefaultAsync();

            if (khId != null)
                kh = await _context.KhachHangs.FirstOrDefaultAsync(x => x.Id == khId.Value);
        }

        if (kh == null && (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(phone)))
        {
            kh = new KhachHang
            {
                Id = Guid.NewGuid(),
                Ten = name,
                DuocNhanVoucher = true,
                CreatedAt = now,
                LastModified = now
            };
            StringHelper.NormalizeAllStrings(kh);
            _context.KhachHangs.Add(kh);

            if (!string.IsNullOrWhiteSpace(phone))
            {
                _context.KhachHangPhones.Add(new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = kh.Id,
                    SoDienThoai = phone,
                    IsDefault = true
                });
            }

            if (!string.IsNullOrWhiteSpace(addr))
            {
                _context.KhachHangAddresses.Add(new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = kh.Id,
                    DiaChi = StringHelper.CapitalizeEachWord(addr),
                    IsDefault = true
                });
            }
            return kh;
        }

        if (kh != null)
        {
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var hasPhone = await _context.KhachHangPhones
                    .AnyAsync(p => p.KhachHangId == kh.Id && p.SoDienThoai == phone);
                if (!hasPhone)
                {
                    _context.KhachHangPhones.Add(new KhachHangPhone
                    {
                        Id = Guid.NewGuid(),
                        KhachHangId = kh.Id,
                        SoDienThoai = phone,
                        IsDefault = false
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(addr))
            {
                var addrLower = addr.ToLower();
                var hasAddr = await _context.KhachHangAddresses
                    .AnyAsync(a => a.KhachHangId == kh.Id && a.DiaChi.ToLower() == addrLower);
                if (!hasAddr)
                {
                    _context.KhachHangAddresses.Add(new KhachHangAddress
                    {
                        Id = Guid.NewGuid(),
                        KhachHangId = kh.Id,
                        DiaChi = addr,
                        IsDefault = false
                    });
                }
            }
        }

        return kh;
    }

    private async Task<(decimal tongTien, decimal giamGia, decimal thanhTien)> AddChiTietAsync(Guid hoaDonId, HoaDonDto dto, DateTime now)
    {
        decimal tongTien = 0;

        var toppingLookup = dto.ChiTietHoaDonToppings
            .GroupBy(tp => tp.ChiTietHoaDonId)
            .ToDictionary(g => g.Key, g => g.ToList());
        int autoStt = 1;
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


            int stt = ct.Stt > 0 ? ct.Stt : autoStt++;  // ⬅️ ưu tiên DTO, fallback tự tăng

            _context.ChiTietHoaDons.Add(new ChiTietHoaDonEntity
            {
                Stt = stt,
                Id = chiTietId,
                HoaDonId = hoaDonId,
                SanPhamBienTheId = ct.SanPhamIdBienThe,
                SanPhamId = bienThe?.SanPhamId ?? Guid.Empty,

                SoLuong = ct.SoLuong,
                DonGia = donGia,

                // ThanhTien = thanhTienSP,
                ThanhTien = thanhTienSP + tienToppingSP,

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
                // GiaTriApDung = voucher.GiaTri,
                GiaTriApDung = DiscountHelper.TinhGiamGia(tongTien, voucher.KieuGiam, voucher.GiaTri, lamTron: true),
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false
            };

            giamGia += vvv.GiaTriApDung;

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
            ct.DeletedAt = now;
            ct.LastModified = now;
        }
        foreach (var tp in entity.ChiTietHoaDonToppings)
        {
            tp.IsDeleted = true;
            tp.DeletedAt = now;
            tp.LastModified = now;
        }
        foreach (var v in entity.ChiTietHoaDonVouchers)
        {
            v.IsDeleted = true;
            v.DeletedAt = now;
            v.LastModified = now;
        }
        foreach (var tt in entity.ChiTietHoaDonThanhToans)
        {
            tt.IsDeleted = true;
            tt.DeletedAt = now;
            tt.LastModified = now;
        }
        foreach (var no in entity.ChiTietHoaDonNos)
        {
            no.IsDeleted = true;
            no.DeletedAt = now;
            no.LastModified = now;
        }
        foreach (var p in entity.ChiTietHoaDonPoints)
        {
            p.IsDeleted = true;
            p.DeletedAt = now;
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
        var fromDate = today.AddDays(0);

        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => !x.IsDeleted &&
                       (x.Ngay >= fromDate
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
                h.ConLai,
                h.HasDebt,
                CoTienMat = _context.ChiTietHoaDonThanhToans.Any(t =>
            !t.IsDeleted && t.HoaDonId == h.Id &&
            t.TenPhuongThucThanhToan != null &&
            t.TenPhuongThucThanhToan.Contains("Tiền mặt")),

                CoChuyenKhoan = _context.ChiTietHoaDonThanhToans.Any(t =>
                    !t.IsDeleted && t.HoaDonId == h.Id &&
                    t.TenPhuongThucThanhToan != null &&
                    t.TenPhuongThucThanhToan.Contains("Chuyển khoản")),

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
            ConLai = h.ConLai,
            HasDebt = h.HasDebt,
            TrangThai = HoaDonHelper.ResolveTrangThai(h.ThanhTien, h.ConLai, h.HasDebt, h.CoTienMat, h.CoChuyenKhoan)

        }).ToList();
    }


}