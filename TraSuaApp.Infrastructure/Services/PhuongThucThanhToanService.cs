using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class PhuongThucThanhToanService : IPhuongThucThanhToanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["PhuongThucThanhToan"];

    public PhuongThucThanhToanService(AppDbContext context)
    {
        _context = context;
    }

    private PhuongThucThanhToanDto ToDto(PhuongThucThanhToan entity)
    {
        return new PhuongThucThanhToanDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            DangSuDung = entity.DangSuDung,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<PhuongThucThanhToanDto>> GetAllAsync()
    {
        var list = await _context.PhuongThucThanhToans.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<PhuongThucThanhToanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<PhuongThucThanhToanDto>> CreateAsync(PhuongThucThanhToanDto dto)
    {
        var entity = new PhuongThucThanhToan
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DangSuDung = dto.DangSuDung,

            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now,
            IsDeleted = false,
        };

        _context.PhuongThucThanhToans.Add(entity);
        await _context.SaveChangesAsync();

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

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.DangSuDung = dto.DangSuDung;

        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<PhuongThucThanhToanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<PhuongThucThanhToanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<PhuongThucThanhToanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<PhuongThucThanhToanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<PhuongThucThanhToanDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<PhuongThucThanhToanDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<PhuongThucThanhToanDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.PhuongThucThanhToans.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}
