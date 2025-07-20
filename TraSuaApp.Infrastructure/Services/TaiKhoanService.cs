using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class TaiKhoanService : ITaiKhoanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["TaiKhoan"];

    public TaiKhoanService(AppDbContext context)
    {
        _context = context;
    }

    private TaiKhoanDto ToDto(TaiKhoan entity)
    {
        return new TaiKhoanDto
        {
            Id = entity.Id,
            IsActive = entity.IsActive,
            MatKhau = null,
            TenDangNhap = entity.TenDangNhap,
            TenHienThi = entity.TenHienThi,
            VaiTro = entity.VaiTro,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<TaiKhoanDto>> GetAllAsync()
    {
        var list = await _context.TaiKhoans
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<TaiKhoanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto)
    {
        var entity = new TaiKhoan
        {
            Id = Guid.NewGuid(),
            IsActive = dto.IsActive,
            MatKhau = PasswordHelper.HashPassword(dto.MatKhau ?? ""),
            TenDangNhap = dto.TenDangNhap,
            TenHienThi = dto.TenHienThi,
            VaiTro = dto.VaiTro,

            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now,
            IsDeleted = false,
        };

        _context.TaiKhoans.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        after.MatKhau = null;
        return Result<TaiKhoanDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto)
    {
        var entity = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<TaiKhoanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<TaiKhoanDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");


        var before = ToDto(entity);

        entity.TenDangNhap = dto.TenDangNhap;
        entity.TenHienThi = dto.TenHienThi;
        entity.VaiTro = dto.VaiTro;
        entity.IsActive = dto.IsActive;
        if (!string.IsNullOrWhiteSpace(dto.MatKhau))
        {
            entity.MatKhau = PasswordHelper.HashPassword(dto.MatKhau);
        }

        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        after.MatKhau = null;

        return Result<TaiKhoanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<TaiKhoanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<TaiKhoanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<TaiKhoanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<TaiKhoanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<TaiKhoanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<TaiKhoanDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<TaiKhoanDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<TaiKhoanDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.TaiKhoans
            .Where(x => x.LastModified > lastSync)
                 .OrderByDescending(x => x.LastModified) // ✅ THÊM DÒNG NÀY
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}