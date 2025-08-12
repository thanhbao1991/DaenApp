using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class CongViecNoiBoService : ICongViecNoiBoService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["CongViecNoiBo"];

    public CongViecNoiBoService(AppDbContext context)
    {
        _context = context;
    }

    private CongViecNoiBoDto ToDto(CongViecNoiBo entity)
    {
        return new CongViecNoiBoDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            DaHoanThanh = entity.DaHoanThanh,
            NgayGio = entity.NgayGio,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<CongViecNoiBoDto>> GetAllAsync()
    {
        var list = await _context.CongViecNoiBos.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<CongViecNoiBoDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.CongViecNoiBos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<CongViecNoiBoDto>> CreateAsync(CongViecNoiBoDto dto)
    {
        var entity = new CongViecNoiBo
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DaHoanThanh = dto.DaHoanThanh,
            NgayGio = dto.NgayGio,
            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now,
            IsDeleted = false,
        };

        _context.CongViecNoiBos.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<CongViecNoiBoDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<CongViecNoiBoDto>> UpdateAsync(Guid id, CongViecNoiBoDto dto)
    {
        var entity = await _context.CongViecNoiBos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<CongViecNoiBoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<CongViecNoiBoDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.DaHoanThanh = dto.DaHoanThanh;
        entity.NgayGio = dto.NgayGio;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<CongViecNoiBoDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<CongViecNoiBoDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.CongViecNoiBos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<CongViecNoiBoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<CongViecNoiBoDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<CongViecNoiBoDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.CongViecNoiBos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongViecNoiBoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<CongViecNoiBoDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<CongViecNoiBoDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<CongViecNoiBoDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.CongViecNoiBos.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
                 .OrderByDescending(x => x.LastModified) // ✅ THÊM DÒNG NÀY
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}
