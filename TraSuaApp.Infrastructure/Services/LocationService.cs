using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class LocationService : ILocationService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["Location"];
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY_ALL = "LOCATION_ALL_V1";

    public LocationService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private static LocationDto ToDto(Location e) => new()
    {
        Id = e.Id,
        // nội dung
        StartAddress = e.StartAddress,
        StartLat = e.StartLat,
        StartLong = e.StartLong,
        DistanceKm = e.DistanceKm,
        MoneyDistance = e.MoneyDistance,
        Matrix = e.Matrix,
        // meta
        CreatedAt = e.CreatedAt,
        LastModified = e.LastModified,
        DeletedAt = e.DeletedAt,
        IsDeleted = e.IsDeleted
    };

    public async Task<List<LocationDto>> GetAllAsync()
    {
        var data = await _cache.GetOrCreateAsync(CACHE_KEY_ALL, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            entry.SlidingExpiration = TimeSpan.FromSeconds(30);

            var list = await _context.Locations.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.LastModified ?? x.CreatedAt)
                .Select(x => ToDto(x))
                .ToListAsync();

            return list;
        });

        return data ?? new List<LocationDto>();
    }

    private void BustCache() => _cache.Remove(CACHE_KEY_ALL);

    public async Task<LocationDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<LocationDto>> CreateAsync(LocationDto dto)
    {
        // Upsert theo StartLat + StartLong
        var now = DateTime.Now;

        // Nếu có lat/long -> kiểm tra trùng
        if (dto.StartLat.HasValue && dto.StartLong.HasValue)
        {
            var dup = await _context.Locations.FirstOrDefaultAsync(x =>
                !x.IsDeleted &&
                x.StartLat == dto.StartLat &&
                x.StartLong == dto.StartLong);

            if (dup != null)
            {
                // Cập nhật “nhẹ” khi có dữ liệu mới
                if (!string.IsNullOrWhiteSpace(dto.StartAddress))
                    dup.StartAddress = dto.StartAddress.Trim();

                if (dto.DistanceKm.HasValue)
                    dup.DistanceKm = dto.DistanceKm;

                if (dto.MoneyDistance.HasValue)
                    dup.MoneyDistance = dto.MoneyDistance;

                if (!string.IsNullOrWhiteSpace(dto.Matrix))
                    dup.Matrix = dto.Matrix!.Trim();

                dup.LastModified = now;

                await _context.SaveChangesAsync();
                BustCache();

                var after = ToDto(dup);
                return Result<LocationDto>.Success(after, "Đã cập nhật bản ghi Location trùng lat/long (không tạo mới).")
                    .WithId(after.Id)
                    .WithAfter(after);
            }
        }

        // Không trùng -> tạo mới
        var entity = new Location
        {
            Id = Guid.NewGuid(),
            StartAddress = (dto.StartAddress ?? string.Empty).Trim(),
            StartLat = dto.StartLat,
            StartLong = dto.StartLong,
            DistanceKm = dto.DistanceKm,
            MoneyDistance = dto.MoneyDistance,
            Matrix = dto.Matrix?.Trim(),
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.Locations.Add(entity);
        await _context.SaveChangesAsync();
        BustCache();

        var created = ToDto(entity);
        return Result<LocationDto>.Success(created, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(created.Id)
            .WithAfter(created);
    }

    public async Task<Result<LocationDto>> UpdateAsync(Guid id, LocationDto dto)
    {
        var entity = await _context.Locations
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<LocationDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<LocationDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        // Gán toàn bộ trường nội dung
        entity.StartAddress = (dto.StartAddress ?? string.Empty).Trim();
        entity.StartLat = dto.StartLat;
        entity.StartLong = dto.StartLong;
        entity.DistanceKm = dto.DistanceKm;
        entity.MoneyDistance = dto.MoneyDistance;
        entity.Matrix = dto.Matrix?.Trim();

        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        BustCache();

        var after = ToDto(entity);
        return Result<LocationDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithAfter(after);
    }

    public async Task<Result<LocationDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.Locations.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<LocationDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();
        BustCache();

        return Result<LocationDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<LocationDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.Locations.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<LocationDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<LocationDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        BustCache();

        var after = ToDto(entity);
        return Result<LocationDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<LocationDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.Locations.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}
