using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class TaiKhoanService : ITaiKhoanService
{
    private readonly AppDbContext _context;

    public TaiKhoanService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaiKhoanDto>> GetAllAsync()
    {
        return await _context.TaiKhoans
            .Select(x => new TaiKhoanDto
            {
                Id = x.Id,
                TenDangNhap = x.TenDangNhap,
                TenHienThi = x.TenHienThi,
                VaiTro = x.VaiTro,
                IsActive = x.IsActive,
                ThoiGianTao = x.ThoiGianTao
            })
            .ToListAsync();
    }

    public async Task<TaiKhoanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.TaiKhoans.FindAsync(id);
        if (entity == null) return null;

        return new TaiKhoanDto
        {
            Id = entity.Id,
            TenDangNhap = entity.TenDangNhap,
            TenHienThi = entity.TenHienThi,
            VaiTro = entity.VaiTro,
            IsActive = entity.IsActive,
            ThoiGianTao = entity.ThoiGianTao
        };
    }

    public async Task<Result> CreateAsync(TaiKhoanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
            return Result.Failure("Tên đăng nhập và mật khẩu là bắt buộc.");

        var trung = await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == dto.TenDangNhap);
        if (trung)
            return Result.Failure("Tên đăng nhập đã tồn tại.");

        var entity = new TaiKhoan
        {
            Id = Guid.NewGuid(),
            TenDangNhap = dto.TenDangNhap,
            MatKhau = PasswordHelper.HashPassword(dto.MatKhau),
            TenHienThi = dto.TenHienThi,
            VaiTro = dto.VaiTro,
            IsActive = dto.IsActive,
            ThoiGianTao = DateTime.Now
        };

        _context.TaiKhoans.Add(entity);
        await _context.SaveChangesAsync();
        return Result.Success("Đã thêm tài khoản.");
    }

    public async Task<Result> UpdateAsync(Guid id, TaiKhoanDto dto)
    {
        var entity = await _context.TaiKhoans.FindAsync(id);
        if (entity == null)
            return Result.Failure("Tài khoản không tồn tại.");

        // ✅ Kiểm tra trùng tên đăng nhập với tài khoản khác
        var exists = await _context.TaiKhoans
            .AnyAsync(x => x.TenDangNhap == dto.TenDangNhap && x.Id != id);

        if (exists)
            return Result.Failure("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");

        // Cập nhật thông tin
        entity.TenDangNhap = dto.TenDangNhap;
        entity.TenHienThi = dto.TenHienThi;
        entity.VaiTro = dto.VaiTro;
        entity.IsActive = dto.IsActive;

        // Chỉ cập nhật mật khẩu nếu có nhập
        if (!string.IsNullOrWhiteSpace(dto.MatKhau))
            entity.MatKhau = PasswordHelper.HashPassword(dto.MatKhau);

        await _context.SaveChangesAsync();
        return Result.Success("Đã cập nhật tài khoản.");
    }
    public async Task<Result> DeleteAsync(Guid id)
    {
        var taiKhoan = await _context.TaiKhoans.FindAsync(id);
        if (taiKhoan == null)
            return Result.Failure("Tài khoản không tồn tại.");

        _context.TaiKhoans.Remove(taiKhoan);
        await _context.SaveChangesAsync();

        return Result.Success("Đã xoá thành công.");
    }
}