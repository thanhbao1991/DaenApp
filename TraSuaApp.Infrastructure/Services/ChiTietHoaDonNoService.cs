using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Dtos.Requests;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Infrastructure.Services;

public class ChiTietHoaDonNoService : IChiTietHoaDonNoService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonNo"];


    public ChiTietHoaDonNoService(AppDbContext context)
    {
        _context = context;
    }

    private ChiTietHoaDonNoDto ToDto(ChiTietHoaDonNo entity)
    {
        return new ChiTietHoaDonNoDto
        {
            Id = entity.Id,
            SoTienNo = entity.SoTienNo,
            SoTienConLai = entity.SoTienConLai,
            NgayGio = entity.NgayGio,
            GhiChu = entity.GhiChu,
            Ngay = entity.Ngay,
            HoaDonId = entity.HoaDonId,
            KhachHangId = entity.KhachHangId,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
            LastModified = entity.LastModified
        };
    }

    public async Task<Result<ChiTietHoaDonNoDto>> CreateAsync(ChiTietHoaDonNoDto dto)
    {
        var entity = new ChiTietHoaDonNo
        {
            Id = Guid.NewGuid(),
            SoTienNo = dto.SoTienNo,
            SoTienConLai = dto.SoTienNo,
            NgayGio = dto.NgayGio,
            GhiChu = dto.GhiChu,
            Ngay = dto.Ngay,
            HoaDonId = dto.HoaDonId,
            KhachHangId = dto.KhachHangId,

            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now,
            IsDeleted = false,
        };

        _context.ChiTietHoaDonNos.Add(entity);

        await _context.SaveChangesAsync();
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        await DiscordService.SendAsync(
            DiscordEventType.GhiNo,
            $"{dto.Ten} {dto.SoTienNo:N0} đ"
        );

        var after = ToDto(entity);
        return Result<ChiTietHoaDonNoDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> UpdateAsync(Guid id, ChiTietHoaDonNoDto dto)
    {
        var entity = await _context.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ChiTietHoaDonNoDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var before = ToDto(entity);

        var daTra = Math.Max(0, entity.SoTienNo - entity.SoTienConLai); // phần đã trả trước khi sửa
        entity.SoTienNo = dto.SoTienNo;
        // giữ nguyên phần đã trả, tính lại phần còn lại theo tổng nợ mới
        entity.SoTienConLai = Math.Max(0, dto.SoTienNo - daTra);

        entity.NgayGio = dto.NgayGio;
        entity.Ngay = dto.Ngay;
        entity.GhiChu = dto.GhiChu;
        entity.KhachHangId = dto.KhachHangId;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await DiscordService.SendAsync(
    DiscordEventType.GhiNo,
    $"[Chỉnh sửa] {after.Ten} {after.SoTienNo:N0} đ"
);

        return Result<ChiTietHoaDonNoDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> PayDebtAsync(Guid id, PayDebtRequest req)
    {
        var no = await _context.ChiTietHoaDonNos
            .Include(x => x.HoaDon).Include(x => x.KhachHang)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (no == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Không tìm thấy công nợ.");

        if (no.SoTienConLai <= 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Công nợ đã thanh toán.");

        var now = DateTime.Now;

        // ✅ Lấy số còn lại & xác định số tiền sẽ thu
        var soConLai = no.SoTienConLai;
        var soThu = req.Amount ?? soConLai;        // null => thu đủ
        if (soThu <= 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không hợp lệ.");
        if (soThu > soConLai)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Số tiền vượt quá số còn lại ({soConLai:N0}).");

        // GUID phương thức — giống WPF
        var tienMatId = Guid.Parse("0121FC04-0469-4908-8B9A-7002F860FB5C");
        var chuyenKhoanId = Guid.Parse("2cf9a88f-3bc0-4d4b-940d-f8ffa4affa02");
        var isTienMat = string.Equals(req.Type, "TienMat", StringComparison.OrdinalIgnoreCase);

        // ✅ Ghi chú: ưu tiên Note gửi lên; nếu trống thì tự sinh
        var note = string.IsNullOrWhiteSpace(req.Note)
            ? (soThu < soConLai ? "Thanh toán thiếu" : "Thanh toán đủ")
            : req.Note;

        var dto = new ChiTietHoaDonThanhToanDto
        {
            ChiTietHoaDonNoId = no.Id,
            HoaDonId = no.HoaDonId,
            KhachHangId = no.KhachHangId,
            SoTien = soThu,                              // ✅ dùng số tiền đã tính
            Ngay = now.Date,
            NgayGio = now,
            LoaiThanhToan = (no.Ngay == now.Date) ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
            TenPhuongThucThanhToan = isTienMat ? "Tiền mặt" : "Chuyển khoản",
            PhuongThucThanhToanId = isTienMat ? tienMatId : chuyenKhoanId,
            GhiChu = note                                // ✅ dùng ghi chú mới
        };

        // Tận dụng service hiện có để giữ toàn bộ nghiệp vụ (recalc, discord…)
        var result = await new ChiTietHoaDonThanhToanService(_context).CreateAsync(dto);
        return result;
    }

    public async Task<Result<ChiTietHoaDonNoDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<ChiTietHoaDonNoDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTietHoaDonNoDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }


    public async Task<List<ChiTietHoaDonNoDto>> GetAllAsync()
    {
        var fromDate = DateTime.Today.AddDays(0);

        return await _context.ChiTietHoaDonNos
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.SoTienConLai > 0
            //  || x.Ngay >= fromDate
            )
            .OrderByDescending(x => x.LastModified)
            .Select(x => new ChiTietHoaDonNoDto
            {
                Id = x.Id,
                Ngay = x.Ngay,
                NgayGio = x.NgayGio,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                Ten = x.KhachHang != null
                        ? x.KhachHang.Ten
                        : (x.HoaDon != null ? x.HoaDon.TenBan : "(không tên)"),

                SoTienNo = x.SoTienNo,
                SoTienConLai = x.SoTienConLai,
                GhiChu = x.GhiChu,
                CreatedAt = x.CreatedAt,
                LastModified = x.LastModified,
                IsDeleted = x.IsDeleted
            })
            .ToListAsync();
    }


    public async Task<ChiTietHoaDonNoDto?> GetByIdAsync(Guid id)
    {
        return await _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ChiTietHoaDonNoDto
            {
                Id = x.Id,
                MaHoaDon = x.HoaDon != null ? x.HoaDon.MaHoaDon : null,
                SoTienNo = x.SoTienNo,
                SoTienConLai = x.SoTienConLai,
                NgayGio = x.NgayGio,
                GhiChu = x.GhiChu,
                Ngay = x.Ngay,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                Ten = x.KhachHang != null ? x.KhachHang.Ten : null,
                CreatedAt = x.CreatedAt,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                LastModified = x.LastModified
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Result<ChiTietHoaDonNoDto>> DeleteAsync(Guid id)
    {
        var before = await _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ChiTietHoaDonNoDto
            {
                Id = x.Id,
                MaHoaDon = x.HoaDon != null ? x.HoaDon.MaHoaDon : null,
                SoTienNo = x.SoTienNo,
                SoTienConLai = x.SoTienConLai,
                NgayGio = x.NgayGio,
                GhiChu = x.GhiChu,
                Ngay = x.Ngay,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                Ten = x.KhachHang != null ? x.KhachHang.Ten : null,
                CreatedAt = x.CreatedAt,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                LastModified = x.LastModified
            })
            .FirstOrDefaultAsync();

        if (before == null || before.IsDeleted)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var entity = await _context.ChiTietHoaDonNos.FirstAsync(x => x.Id == id);
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        await DiscordService.SendAsync(
    DiscordEventType.GhiNo,
    $"[Xoá] {before.Ten} {before.SoTienNo:N0} đ"
);

        return Result<ChiTietHoaDonNoDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<List<ChiTietHoaDonNoDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => new ChiTietHoaDonNoDto
            {
                Id = x.Id,
                MaHoaDon = x.HoaDon != null ? x.HoaDon.MaHoaDon : null,
                SoTienNo = x.SoTienNo,
                SoTienConLai = x.SoTienConLai,
                NgayGio = x.NgayGio,
                GhiChu = x.GhiChu,
                Ngay = x.Ngay,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                Ten = x.KhachHang != null ? x.KhachHang.Ten : null,
                CreatedAt = x.CreatedAt,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                LastModified = x.LastModified
            })
            .ToListAsync();
    }


}
