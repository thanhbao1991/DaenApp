using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class PhuongThucThanhToanService : IPhuongThucThanhToanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["PhuongThucThanhToan"];
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY_ALL = "PTTT_ALL_V1";

    public PhuongThucThanhToanService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private static PhuongThucThanhToanDto ToDto(PhuongThucThanhToan e) => new()
    {
        Id = e.Id,
        Ten = e.Ten,
        DangSuDung = e.DangSuDung,
        CreatedAt = e.CreatedAt,
        LastModified = e.LastModified,
        DeletedAt = e.DeletedAt,
        IsDeleted = e.IsDeleted
    };

    public async Task<List<PhuongThucThanhToanDto>> GetAllAsync()
    {
        var data = await _cache.GetOrCreateAsync(CACHE_KEY_ALL, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            entry.SlidingExpiration = TimeSpan.FromSeconds(30);

            var list = await _context.PhuongThucThanhToans.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.LastModified)
                .Select(x => ToDto(x))
                .ToListAsync();

            return list;
        });

        return data ?? new List<PhuongThucThanhToanDto>();
    }

    private void BustCache() => _cache.Remove(CACHE_KEY_ALL);

    public async Task<PhuongThucThanhToanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<PhuongThucThanhToanDto>> CreateAsync(PhuongThucThanhToanDto dto)
    {
        bool daTonTai = await _context.PhuongThucThanhToans
            .AnyAsync(x => x.Ten.ToLower() == dto.Ten.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<PhuongThucThanhToanDto>.Failure($"{_friendlyName} {dto.Ten} đã tồn tại.");

        var now = DateTime.Now;
        var entity = new PhuongThucThanhToan
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DangSuDung = dto.DangSuDung,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.PhuongThucThanhToans.Add(entity);
        await _context.SaveChangesAsync();
        BustCache(); // ✅ Clear cache sau khi ghi

        var after = ToDto(entity);
        return Result<PhuongThucThanhToanDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<PhuongThucThanhToanDto>> UpdateAsync(Guid id, PhuongThucThanhToanDto dto)
    {
        var entity = await _context.PhuongThucThanhToans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<PhuongThucThanhToanDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.PhuongThucThanhToans
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == dto.Ten.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<PhuongThucThanhToanDto>.Failure($"{_friendlyName} {dto.Ten} đã tồn tại.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.DangSuDung = dto.DangSuDung;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        BustCache(); // ✅ Clear cache sau khi cập nhật

        var after = ToDto(entity);
        return Result<PhuongThucThanhToanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<PhuongThucThanhToanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();
        BustCache(); // ✅ Clear cache sau khi xoá

        return Result<PhuongThucThanhToanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<PhuongThucThanhToanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<PhuongThucThanhToanDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        BustCache(); // ✅ Clear cache sau khi khôi phục

        var after = ToDto(entity);
        return Result<PhuongThucThanhToanDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<PhuongThucThanhToanDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.PhuongThucThanhToans.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}