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

    private static TaiKhoanDto ToDto(TaiKhoan entity)
    {
        return new TaiKhoanDto
        {
            Id = entity.Id,
            IsActive = entity.IsActive,
            TenDangNhap = entity.TenDangNhap,
            TenHienThi = entity.TenHienThi,
            VaiTro = entity.VaiTro,
            MatKhau = null, // ❌ không bao giờ trả mật khẩu

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<TaiKhoanDto>> GetAllAsync()
    {
        return await _context.TaiKhoans.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<TaiKhoanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.TaiKhoans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto)
    {
        bool daTonTai = await _context.TaiKhoans
            .AnyAsync(x => x.TenDangNhap.ToLower() == dto.TenDangNhap.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<TaiKhoanDto>.Failure($"Tên đăng nhập {dto.TenDangNhap} đã tồn tại.");

        var now = DateTime.Now;
        var entity = new TaiKhoan
        {
            Id = Guid.NewGuid(),
            IsActive = dto.IsActive,
            TenDangNhap = dto.TenDangNhap.Trim(),
            TenHienThi = dto.TenHienThi.Trim(),
            VaiTro = dto.VaiTro,
            MatKhau = PasswordHelper.HashPassword(dto.MatKhau ?? ""),

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.TaiKhoans.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
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

        bool daTonTai = await _context.TaiKhoans
            .AnyAsync(x => x.Id != id &&
                           x.TenDangNhap.ToLower() == dto.TenDangNhap.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<TaiKhoanDto>.Failure($"Tên đăng nhập {dto.TenDangNhap} đã tồn tại.");

        var before = ToDto(entity);

        entity.TenDangNhap = dto.TenDangNhap.Trim();
        entity.TenHienThi = dto.TenHienThi.Trim();
        entity.VaiTro = dto.VaiTro;
        entity.IsActive = dto.IsActive;

        if (!string.IsNullOrWhiteSpace(dto.MatKhau))
        {
            entity.MatKhau = PasswordHelper.HashPassword(dto.MatKhau);
        }

        entity.LastModified = DateTime.Now;
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<TaiKhoanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<TaiKhoanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<TaiKhoanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<TaiKhoanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<TaiKhoanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Id == id);

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
        return await _context.TaiKhoans.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}