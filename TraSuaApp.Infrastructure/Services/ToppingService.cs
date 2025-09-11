using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class ToppingService : IToppingService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["Topping"];

    public ToppingService(AppDbContext context)
    {
        _context = context;
    }

    private static ToppingDto ToDto(Topping entity)
    {
        return new ToppingDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            Gia = entity.Gia,
            NgungBan = entity.NgungBan,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
            NhomSanPhams = entity.NhomSanPhams.Select(x => x.Id).ToList()
        };
    }

    public async Task<List<ToppingDto>> GetAllAsync()
    {
        return await _context.Toppings.AsNoTracking()
            .Include(x => x.NhomSanPhams)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<ToppingDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(x => x.NhomSanPhams)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<ToppingDto>> CreateAsync(ToppingDto dto)
    {
        bool daTonTai = await _context.Toppings
            .AnyAsync(x => x.Ten.ToLower() == dto.Ten.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<ToppingDto>.Failure($"{dto.Ten} đã tồn tại.");

        var nhoms = await _context.NhomSanPhams
            .Where(x => dto.NhomSanPhams.Contains(x.Id))
            .ToListAsync();

        var now = DateTime.Now;
        var entity = new Topping
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            Gia = dto.Gia,
            NgungBan = dto.NgungBan,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
            NhomSanPhams = nhoms
        };

        _context.Toppings.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ToppingDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto)
    {
        var entity = await _context.Toppings
            .Include(x => x.NhomSanPhams)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ToppingDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ToppingDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.Toppings
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == dto.Ten.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<ToppingDto>.Failure($"{dto.Ten} đã tồn tại.");

        var nhoms = await _context.NhomSanPhams
            .Where(x => dto.NhomSanPhams.Contains(x.Id))
            .ToListAsync();

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.Gia = dto.Gia;
        entity.NgungBan = dto.NgungBan;
        entity.LastModified = DateTime.Now;
        entity.NhomSanPhams = nhoms;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ToppingDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ToppingDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(x => x.NhomSanPhams)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<ToppingDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<ToppingDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<ToppingDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(x => x.NhomSanPhams)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ToppingDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<ToppingDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ToppingDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ToppingDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.Toppings.AsNoTracking()
            .Include(x => x.NhomSanPhams)
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}