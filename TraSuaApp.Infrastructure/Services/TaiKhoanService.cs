using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class TaiKhoanService : ITaiKhoanService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public TaiKhoanService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<TaiKhoanDto>> GetAllAsync()
    {
        var list = await _context.TaiKhoans.ToListAsync();
        return _mapper.Map<List<TaiKhoanDto>>(list);
    }

    public async Task<TaiKhoanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.TaiKhoans.FindAsync(id);
        return entity == null ? null : _mapper.Map<TaiKhoanDto>(entity);
    }

    public async Task<Result> CreateAsync(TaiKhoanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
            return Result.Failure("Tên đăng nhập và mật khẩu là bắt buộc.");

        var trung = await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == dto.TenDangNhap);
        if (trung)
            return Result.Failure("Tên đăng nhập đã tồn tại.");

        var entity = _mapper.Map<TaiKhoan>(dto);
        entity.Id = Guid.NewGuid();
        entity.MatKhau = PasswordHelper.HashPassword(dto.MatKhau);
        entity.ThoiGianTao = DateTime.Now;

        _context.TaiKhoans.Add(entity);
        await _context.SaveChangesAsync();
        return Result.Success("Đã thêm tài khoản.");
    }

    public async Task<Result> UpdateAsync(Guid id, TaiKhoanDto dto)
    {
        var entity = await _context.TaiKhoans.FindAsync(id);
        if (entity == null)
            return Result.Failure("Tài khoản không tồn tại.");

        var exists = await _context.TaiKhoans
            .AnyAsync(x => x.TenDangNhap == dto.TenDangNhap && x.Id != id);

        if (exists)
            return Result.Failure("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");

        // Giữ lại mật khẩu cũ nếu dto không có
        var oldPassword = entity.MatKhau;

        _mapper.Map(dto, entity);

        entity.MatKhau = string.IsNullOrWhiteSpace(dto.MatKhau)
            ? oldPassword
            : PasswordHelper.HashPassword(dto.MatKhau);

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