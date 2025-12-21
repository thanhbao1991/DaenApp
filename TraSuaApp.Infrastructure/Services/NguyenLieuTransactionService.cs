using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class NguyenLieuTransactionService : INguyenLieuTransactionService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuTransaction"];

    public NguyenLieuTransactionService(AppDbContext context)
    {
        _context = context;
    }

    private NguyenLieuTransactionDto ToDto(NguyenLieuTransaction entity, NguyenLieuBanHang? nlb = null)
    {
        return new NguyenLieuTransactionDto
        {
            Id = entity.Id,

            NguyenLieuId = entity.NguyenLieuId,
            NgayGio = entity.NgayGio,
            Loai = entity.Loai,
            SoLuong = entity.SoLuong,
            DonGia = entity.DonGia,
            GhiChu = entity.GhiChu,
            ChiTieuHangNgayId = entity.ChiTieuHangNgayId,
            HoaDonId = entity.HoaDonId,

            TenNguyenLieu = nlb?.Ten,
            DonViTinh = nlb?.DonViTinh,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted
        };
    }

    public async Task<List<NguyenLieuTransactionDto>> GetAllAsync()
    {
        var list = await _context.NguyenLieuTransactions.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.NgayGio)
            .ToListAsync();

        if (!list.Any()) return new List<NguyenLieuTransactionDto>();

        var ids = list.Select(x => x.NguyenLieuId).Distinct().ToList();

        var nlbMap = await _context.NguyenLieuBanHangs.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => x);

        return list.Select(x => ToDto(x, nlbMap.TryGetValue(x.NguyenLieuId, out var n) ? n : null)).ToList();
    }

    public async Task<NguyenLieuTransactionDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.NguyenLieuTransactions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return null;

        var nlb = await _context.NguyenLieuBanHangs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.NguyenLieuId && !x.IsDeleted);

        return ToDto(entity, nlb);
    }

    public async Task<Result<NguyenLieuTransactionDto>> CreateAsync(NguyenLieuTransactionDto dto)
    {
        // ✅ Transaction thường do hệ thống tạo (nhập kho / bán / điều chỉnh).
        // Nếu vẫn muốn tạo tay từ UI, validate tối thiểu:
        if (dto.NguyenLieuId == Guid.Empty)
            return Result<NguyenLieuTransactionDto>.Failure("Vui lòng chọn nguyên liệu (NguyenLieuBanHang).");

        if (dto.SoLuong == 0)
            return Result<NguyenLieuTransactionDto>.Failure("Số lượng không được = 0.");

        var now = DateTime.Now;

        var entity = new NguyenLieuTransaction
        {
            Id = Guid.NewGuid(),
            NguyenLieuId = dto.NguyenLieuId,
            NgayGio = dto.NgayGio == default ? now : dto.NgayGio,

            Loai = dto.Loai,
            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,
            GhiChu = dto.GhiChu,

            ChiTieuHangNgayId = dto.ChiTieuHangNgayId,
            HoaDonId = dto.HoaDonId,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.NguyenLieuTransactions.Add(entity);
        await _context.SaveChangesAsync();

        var nlb = await _context.NguyenLieuBanHangs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.NguyenLieuId && !x.IsDeleted);

        var after = ToDto(entity, nlb);
        return Result<NguyenLieuTransactionDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuTransactionDto>> UpdateAsync(Guid id, NguyenLieuTransactionDto dto)
    {
        var entity = await _context.NguyenLieuTransactions
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<NguyenLieuTransactionDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<NguyenLieuTransactionDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var beforeNlb = await _context.NguyenLieuBanHangs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.NguyenLieuId && !x.IsDeleted);

        var before = ToDto(entity, beforeNlb);

        // Cho phép chỉnh tay ghi chú / ngày giờ / loại / số lượng nếu anh muốn
        entity.NgayGio = dto.NgayGio == default ? entity.NgayGio : dto.NgayGio;
        entity.Loai = dto.Loai;
        entity.SoLuong = dto.SoLuong;
        entity.DonGia = dto.DonGia;
        entity.GhiChu = dto.GhiChu;
        entity.ChiTieuHangNgayId = dto.ChiTieuHangNgayId;
        entity.HoaDonId = dto.HoaDonId;

        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var afterNlb = await _context.NguyenLieuBanHangs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.NguyenLieuId && !x.IsDeleted);

        var after = ToDto(entity, afterNlb);

        return Result<NguyenLieuTransactionDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuTransactionDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.NguyenLieuTransactions.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<NguyenLieuTransactionDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var nlb = await _context.NguyenLieuBanHangs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.NguyenLieuId && !x.IsDeleted);

        var before = ToDto(entity, nlb);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<NguyenLieuTransactionDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<NguyenLieuTransactionDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.NguyenLieuTransactions.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuTransactionDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<NguyenLieuTransactionDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var nlb = await _context.NguyenLieuBanHangs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.NguyenLieuId && !x.IsDeleted);

        var after = ToDto(entity, nlb);

        return Result<NguyenLieuTransactionDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<NguyenLieuTransactionDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.NguyenLieuTransactions.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        if (!list.Any()) return new List<NguyenLieuTransactionDto>();

        var ids = list.Select(x => x.NguyenLieuId).Distinct().ToList();

        var nlbMap = await _context.NguyenLieuBanHangs.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, x => x);

        return list.Select(x => ToDto(x, nlbMap.TryGetValue(x.NguyenLieuId, out var n) ? n : null)).ToList();
    }
}
