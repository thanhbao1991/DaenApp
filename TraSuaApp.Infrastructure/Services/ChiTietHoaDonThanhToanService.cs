using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Infrastructure.Services;

public class ChiTietHoaDonThanhToanService : IChiTietHoaDonThanhToanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"];

    public ChiTietHoaDonThanhToanService(AppDbContext context)
    {
        _context = context;
    }

    private ChiTietHoaDonThanhToanDto ToDto(ChiTietHoaDonThanhToan entity)
    {
        return new ChiTietHoaDonThanhToanDto
        {
            Ten = entity.KhachHangId != null
                ? entity.KhachHang?.Ten
                : entity.HoaDon?.TenBan,

            Id = entity.Id,
            LoaiThanhToan = entity.LoaiThanhToan,
            ChiTietHoaDonNoId = entity.ChiTietHoaDonNoId,
            SoTien = entity.SoTien,
            NgayGio = entity.NgayGio,
            Ngay = entity.Ngay,
            HoaDonId = entity.HoaDonId,
            KhachHangId = entity.KhachHangId,
            PhuongThucThanhToanId = entity.PhuongThucThanhToanId,
            TenPhuongThucThanhToan = entity.TenPhuongThucThanhToan,
            GhiChu = entity.GhiChu,

            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
            LastModified = entity.LastModified
        };
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto)
    {
        // Kiểm tra số tiền hợp lệ
        if (dto.SoTien < 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không được âm.");

        var hoaDon = await _context.HoaDons.FindAsync(dto.HoaDonId);
        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == dto.HoaDonId && !x.IsDeleted)
            .SumAsync(x => x.SoTien);

        var soTienConLai = hoaDon.ThanhTien - tongDaThanhToan;

        if (dto.SoTien > soTienConLai)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Số tiền còn lại cần thu: {soTienConLai.ToString("N0")}.");

        string GhiChu = dto.SoTien == soTienConLai ? "Thanh toán đủ" : "Thanh toán thiếu";

        var now = DateTime.Now;
        var ngayGio = dto.NgayGio == default ? now : dto.NgayGio;
        var ngay = dto.Ngay == default ? now.Date : dto.Ngay;

        var entity = new ChiTietHoaDonThanhToan
        {
            Id = Guid.NewGuid(),
            SoTien = dto.SoTien,
            LoaiThanhToan = dto.LoaiThanhToan,
            ChiTietHoaDonNoId = dto.ChiTietHoaDonNoId,
            NgayGio = ngayGio,
            TenPhuongThucThanhToan = dto.TenPhuongThucThanhToan,
            Ngay = ngay,
            HoaDonId = dto.HoaDonId,
            KhachHangId = dto.KhachHangId,
            PhuongThucThanhToanId = dto.PhuongThucThanhToanId,
            GhiChu = GhiChu,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.ChiTietHoaDonThanhToans.Add(entity);

        // Cập nhật công nợ (nếu có)
        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, -dto.SoTien);

        // 🟟 FLUSH TRƯỚC
        await _context.SaveChangesAsync();

        // Recalc ConLai/HasDebt
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        var afterEntity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstAsync(x => x.Id == entity.Id);

        var after = ToDto(afterEntity);

        if (entity.ChiTietHoaDonNoId != null)
            await DiscordService.SendAsync(
                DiscordEventType.TraNo,
                $"{entity.SoTien:N0}đ {dto.Ten}"
            );
        // else
        //     await DiscordService.SendAsync(
        //         DiscordEventType.ThanhToan,
        //         $"{entity.SoTien:N0}đ {dto.Ten}"
        //     );

        return Result<ChiTietHoaDonThanhToanDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        // Kiểm tra số tiền hợp lệ
        if (dto.SoTien < 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không được âm.");

        var hoaDon = await _context.HoaDons.FindAsync(dto.HoaDonId);
        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
         .Where(x => x.HoaDonId == dto.HoaDonId && !x.IsDeleted && x.Id != id) // ✅
         .SumAsync(x => x.SoTien);

        var soTienConLai = hoaDon.ThanhTien - tongDaThanhToan;

        if (dto.SoTien > soTienConLai)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Số tiền còn lại cần thu: {soTienConLai.ToString("N0")}.");

        string GhiChu = dto.SoTien == soTienConLai ? "Thanh toán đủ" : "Thanh toán thiếu";

        var before = ToDto(entity);

        // Giữ lại thông tin cũ để recalc đúng
        var oldHoaDonId = entity.HoaDonId;
        var oldSoTien = entity.SoTien;
        var oldChiTietHoaDonNoId = entity.ChiTietHoaDonNoId;

        // Cập nhật entity
        entity.SoTien = dto.SoTien;
        entity.ChiTietHoaDonNoId = dto.ChiTietHoaDonNoId;
        entity.LoaiThanhToan = dto.LoaiThanhToan;
        entity.KhachHangId = dto.KhachHangId;
        entity.HoaDonId = dto.HoaDonId;
        entity.PhuongThucThanhToanId = dto.PhuongThucThanhToanId;
        entity.TenPhuongThucThanhToan = dto.TenPhuongThucThanhToan;
        entity.GhiChu = GhiChu;
        entity.LastModified = DateTime.Now;

        // Hoàn tác công nợ cũ + áp dụng công nợ mới (nếu có)
        await NoHelper.UpdateSoTienConLaiAsync(_context, oldChiTietHoaDonNoId, oldSoTien);
        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, -dto.SoTien);

        // 🟟 FLUSH TRƯỚC
        await _context.SaveChangesAsync();

        // Nếu đổi hóa đơn, recalc cả hai; nếu không, recalc hóa đơn hiện tại
        if (oldHoaDonId != entity.HoaDonId)
        {
            await HoaDonHelper.RecalcConLaiAsync(_context, oldHoaDonId);
        }
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);

        // LƯU KẾT QUẢ RECALC
        await _context.SaveChangesAsync();

        var afterEntity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstAsync(x => x.Id == id);

        var after = ToDto(afterEntity);

        if (entity.ChiTietHoaDonNoId != null)
            await DiscordService.SendAsync(
                DiscordEventType.TraNo,
                $"[Chỉnh sửa] {entity.SoTien:N0}đ {dto.Ten}"
            );
        else
            await DiscordService.SendAsync(
                DiscordEventType.ThanhToan,
                $"[Chỉnh sửa] {entity.SoTien:N0}đ {dto.Ten}"
            );

        return Result<ChiTietHoaDonThanhToanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        // Trả lại công nợ nếu trước đó cấn trừ vào nợ
        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, entity.SoTien);

        // 🟟 FLUSH TRƯỚC
        await _context.SaveChangesAsync();

        // Recalc hóa đơn
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        if (entity.ChiTietHoaDonNoId != null)
            await DiscordService.SendAsync(
                DiscordEventType.TraNo,
                $"[Xoá] {entity.SoTien:N0}đ {entity.Id}"
            );
        else
            await DiscordService.SendAsync(
                DiscordEventType.ThanhToan,
                $"[Xoá] {entity.SoTien:N0}đ {entity.Id} "
            );

        return Result<ChiTietHoaDonThanhToanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        // Cấn lại công nợ khi khôi phục
        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, -entity.SoTien);

        // 🟟 FLUSH TRƯỚC
        await _context.SaveChangesAsync();

        // Recalc hóa đơn
        await HoaDonHelper.RecalcConLaiAsync(_context, entity.HoaDonId);
        await _context.SaveChangesAsync();

        var afterEntity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstAsync(x => x.Id == id);

        var after = ToDto(afterEntity);
        return Result<ChiTietHoaDonThanhToanDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ChiTietHoaDonThanhToanDto>> GetAllAsync()
    {
        var today = DateTime.Today;
        var fromDate = today.AddDays(0);

        return await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= fromDate)
            .OrderByDescending(x => x.LastModified)
            .Select(x => new ChiTietHoaDonThanhToanDto
            {
                Id = x.Id,
                Ten = x.KhachHangId != null ? x.KhachHang!.Ten : x.HoaDon!.TenBan,
                LoaiThanhToan = x.LoaiThanhToan,
                ChiTietHoaDonNoId = x.ChiTietHoaDonNoId,
                SoTien = x.SoTien,
                NgayGio = x.NgayGio,
                Ngay = x.Ngay,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                PhuongThucThanhToanId = x.PhuongThucThanhToanId,
                TenPhuongThucThanhToan = x.TenPhuongThucThanhToan,
                GhiChu = x.GhiChu,
                CreatedAt = x.CreatedAt,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                LastModified = x.LastModified
            })
            .ToListAsync();
    }

    public async Task<ChiTietHoaDonThanhToanDto?> GetByIdAsync(Guid id)
    {
        return await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ChiTietHoaDonThanhToanDto
            {
                Id = x.Id,
                Ten = x.KhachHangId != null ? x.KhachHang!.Ten : x.HoaDon!.TenBan,
                LoaiThanhToan = x.LoaiThanhToan,
                ChiTietHoaDonNoId = x.ChiTietHoaDonNoId,
                SoTien = x.SoTien,
                NgayGio = x.NgayGio,
                Ngay = x.Ngay,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                PhuongThucThanhToanId = x.PhuongThucThanhToanId,
                TenPhuongThucThanhToan = x.TenPhuongThucThanhToan,
                GhiChu = x.GhiChu,
                CreatedAt = x.CreatedAt,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                LastModified = x.LastModified
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ChiTietHoaDonThanhToanDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => new ChiTietHoaDonThanhToanDto
            {
                Id = x.Id,
                Ten = x.KhachHangId != null ? x.KhachHang!.Ten : x.HoaDon!.TenBan,
                LoaiThanhToan = x.LoaiThanhToan,
                ChiTietHoaDonNoId = x.ChiTietHoaDonNoId,
                SoTien = x.SoTien,
                NgayGio = x.NgayGio,
                Ngay = x.Ngay,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                PhuongThucThanhToanId = x.PhuongThucThanhToanId,
                TenPhuongThucThanhToan = x.TenPhuongThucThanhToan,
                GhiChu = x.GhiChu,
                CreatedAt = x.CreatedAt,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                LastModified = x.LastModified
            })
            .ToListAsync();
    }
}