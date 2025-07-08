using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

    public async Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
            return Result<TaiKhoanDto>.Failure("Tên đăng nhập và mật khẩu là bắt buộc.");

        var exists = await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == dto.TenDangNhap);
        if (exists)
            return Result<TaiKhoanDto>.Failure("Tên đăng nhập đã tồn tại.");

        var entity = _mapper.Map<TaiKhoan>(dto);
        entity.Id = Guid.NewGuid();
        entity.MatKhau = PasswordHelper.HashPassword(dto.MatKhau);
        entity.ThoiGianTao = DateTime.Now;

        _context.TaiKhoans.Add(entity);
        await _context.SaveChangesAsync();

        var resultDto = _mapper.Map<TaiKhoanDto>(entity);
        resultDto.MatKhau = null;

        return Result<TaiKhoanDto>.Success("Đã thêm tài khoản.", resultDto)
            .WithId(entity.Id)
            .WithAfter(resultDto);
    }

    public async Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto)
    {
        var entity = await _context.TaiKhoans.FindAsync(id);
        if (entity == null)
            return Result<TaiKhoanDto>.Failure("Tài khoản không tồn tại.");

        var trungTen = await _context.TaiKhoans
            .AnyAsync(x => x.TenDangNhap == dto.TenDangNhap && x.Id != id);
        if (trungTen)
            return Result<TaiKhoanDto>.Failure("Tên đăng nhập đã tồn tại.");

        var before = _mapper.Map<TaiKhoanDto>(entity);
        before.MatKhau = null;

        var oldPassword = entity.MatKhau;
        _mapper.Map(dto, entity);
        entity.MatKhau = string.IsNullOrWhiteSpace(dto.MatKhau)
            ? oldPassword
            : PasswordHelper.HashPassword(dto.MatKhau);

        await _context.SaveChangesAsync();

        var after = _mapper.Map<TaiKhoanDto>(entity);
        after.MatKhau = null;

        return Result<TaiKhoanDto>.Success("Đã cập nhật tài khoản.", after)
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<TaiKhoanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.TaiKhoans.FindAsync(id);
        if (entity == null)
            return Result<TaiKhoanDto>.Failure("Tài khoản không tồn tại.");

        var before = _mapper.Map<TaiKhoanDto>(entity);
        before.MatKhau = null;

        _context.TaiKhoans.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<TaiKhoanDto>.Success("Đã xoá tài khoản.", before)
            .WithId(id)
            .WithBefore(before);
    }
}