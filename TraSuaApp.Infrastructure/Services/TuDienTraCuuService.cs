using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class TuDienTraCuuService : ITuDienTraCuuService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["TuDienTraCuu"];

    public TuDienTraCuuService(AppDbContext context)
    {
        _context = context;
    }

    private static TuDienTraCuuDto ToDto(TuDienTraCuu entity)
    {
        return new TuDienTraCuuDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            TenPhienDich = entity.TenPhienDich,
            DangSuDung = entity.DangSuDung,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<TuDienTraCuuDto>> GetAllAsync()
    {
        return await _context.TuDienTraCuus.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<TuDienTraCuuDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.TuDienTraCuus
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<TuDienTraCuuDto>> CreateAsync(TuDienTraCuuDto dto)
    {
        bool daTonTai = await _context.TuDienTraCuus
            .AnyAsync(x => x.Ten.ToLower() == dto.Ten.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<TuDienTraCuuDto>.Failure($"{_friendlyName} {dto.Ten} đã tồn tại.");

        var now = DateTime.Now;
        var entity = new TuDienTraCuu
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.MyNormalizeText(),
            TenPhienDich = dto.TenPhienDich.Trim(),
            DangSuDung = dto.DangSuDung,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.TuDienTraCuus.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<TuDienTraCuuDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<TuDienTraCuuDto>> UpdateAsync(Guid id, TuDienTraCuuDto dto)
    {
        var entity = await _context.TuDienTraCuus
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<TuDienTraCuuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<TuDienTraCuuDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.TuDienTraCuus
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == dto.Ten.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<TuDienTraCuuDto>.Failure($"{_friendlyName} {dto.Ten} đã tồn tại.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.MyNormalizeText();
        entity.TenPhienDich = dto.TenPhienDich.Trim();
        entity.DangSuDung = dto.DangSuDung;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<TuDienTraCuuDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<TuDienTraCuuDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.TuDienTraCuus.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<TuDienTraCuuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<TuDienTraCuuDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<TuDienTraCuuDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.TuDienTraCuus.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<TuDienTraCuuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<TuDienTraCuuDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<TuDienTraCuuDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<TuDienTraCuuDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.TuDienTraCuus.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}