using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class NguyenLieuService : INguyenLieuService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieu"];

    public NguyenLieuService(AppDbContext context)
    {
        _context = context;
    }

    private static NguyenLieuDto ToDto(NguyenLieu entity)
    {
        return new NguyenLieuDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            DonViTinh = entity.DonViTinh,
            GiaNhap = entity.GiaNhap,
            TonKho = entity.TonKho,
            DangSuDung = entity.DangSuDung,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }


    public async Task<Result<NguyenLieuDto>> CreateAsync(NguyenLieuDto dto)
    {
        bool daTonTai = await _context.NguyenLieus
            .AnyAsync(x => x.Ten.ToLower() == dto.Ten.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<NguyenLieuDto>.Failure($"{dto.Ten} đã tồn tại.");

        var now = DateTime.Now;
        var entity = new NguyenLieu
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DangSuDung = dto.DangSuDung,
            DonViTinh = dto.DonViTinh,
            TonKho = dto.TonKho,
            GiaNhap = dto.GiaNhap,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.NguyenLieus.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NguyenLieuDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuDto>> UpdateAsync(Guid id, NguyenLieuDto dto)
    {
        var entity = await _context.NguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<NguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<NguyenLieuDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.NguyenLieus
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == dto.Ten.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<NguyenLieuDto>.Failure($"{dto.Ten} đã tồn tại.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.Ten = dto.Ten.Trim();
        entity.GiaNhap = dto.GiaNhap;
        entity.TonKho = dto.TonKho;
        entity.DonViTinh = dto.DonViTinh;
        entity.DangSuDung = dto.DangSuDung;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NguyenLieuDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.NguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<NguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<NguyenLieuDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<NguyenLieuDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.NguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<NguyenLieuDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NguyenLieuDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<NguyenLieuDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.NguyenLieus.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
    public async Task<List<NguyenLieuDto>> GetAllAsync()
    {
        return await _context.NguyenLieus.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<NguyenLieuDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.NguyenLieus
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

}
