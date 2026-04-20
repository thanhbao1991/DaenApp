using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Shared.Config;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaiKhoanController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["TaiKhoan"];

    public TaiKhoanController(AppDbContext context)
    {
        _context = context;
    }

    private static TaiKhoanDto ToDto(TaiKhoan e)
    {
        return new TaiKhoanDto
        {
            Id = e.Id,
            TenDangNhap = e.TenDangNhap,
            TenHienThi = e.TenHienThi,
            VaiTro = e.VaiTro,
            IsActive = e.IsActive,
            LastModified = e.LastModified
        };
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<TaiKhoanDto>>>> GetAll()
    {
        var list = await _context.TaiKhoans
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<TaiKhoanDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<TaiKhoanDto>>> GetById(Guid id)
    {
        var entity = await _context.TaiKhoans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<TaiKhoanDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<TaiKhoanDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<TaiKhoanDto>>> Create(TaiKhoanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDangNhap) || string.IsNullOrWhiteSpace(dto.MatKhau))
            return Result<TaiKhoanDto>.Failure("Thiếu thông tin.");

        bool exist = await _context.TaiKhoans
            .AnyAsync(x => x.TenDangNhap == dto.TenDangNhap);

        if (exist)
            return Result<TaiKhoanDto>.Failure("Tài khoản đã tồn tại.");

        var entity = new TaiKhoan
        {
            Id = Guid.NewGuid(),
            TenDangNhap = dto.TenDangNhap,
            MatKhau = dto.MatKhau, // 🟟 nếu có hash thì thay ở đây
            TenHienThi = dto.TenHienThi,
            VaiTro = dto.VaiTro,
            IsActive = dto.IsActive,
            LastModified = DateTime.Now
        };

        _context.TaiKhoans.Add(entity);
        await _context.SaveChangesAsync();

        return Result<TaiKhoanDto>.Success(ToDto(entity), "Đã thêm tài khoản.")
            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<TaiKhoanDto>>> Update(Guid id, TaiKhoanDto dto)
    {
        var entity = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<TaiKhoanDto>.Failure("Không tìm thấy.");

        entity.TenHienThi = dto.TenHienThi;
        entity.VaiTro = dto.VaiTro;
        entity.IsActive = dto.IsActive;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<TaiKhoanDto>.Success(ToDto(entity), "Đã cập nhật.")
            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<TaiKhoanDto>>> Delete(Guid id)
    {
        var entity = await _context.TaiKhoans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<TaiKhoanDto>.Failure("Không tìm thấy.");

        _context.TaiKhoans.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<TaiKhoanDto>.Success(null, "Đã xoá.")
            ;
    }
}