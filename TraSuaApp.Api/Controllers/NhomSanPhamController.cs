using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NhomSanPhamController : ControllerBase
{
    private readonly AppDbContext _context;

    public NhomSanPhamController(AppDbContext context)
    {
        _context = context;
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<Result<List<NhomSanPhamDto>>> GetAll()
    {
        var list = await _context.NhomSanPhams
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => new NhomSanPhamDto
            {
                Id = x.Id,
                Ten = x.Ten,
                LastModified = x.LastModified
            })
            .ToListAsync();

        return Result<List<NhomSanPhamDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<Result<NhomSanPhamDto>> GetById(Guid id)
    {
        var entity = await _context.NhomSanPhams
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NhomSanPhamDto>.Failure("Không tìm thấy nhóm.");

        return Result<NhomSanPhamDto>.Success(new NhomSanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            LastModified = entity.LastModified
        });
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<Result<NhomSanPhamDto>> Create(NhomSanPhamDto dto)
    {
        var ten = dto.Ten.Trim();

        bool exist = await _context.NhomSanPhams
            .AnyAsync(x => x.Ten.ToLower() == ten.ToLower());

        if (exist)
            return Result<NhomSanPhamDto>.Failure($"Nhóm {ten} đã tồn tại.");

        var now = DateTime.Now;

        var entity = new NhomSanPham
        {
            Id = Guid.NewGuid(),
            Ten = ten,
            LastModified = now
        };

        _context.NhomSanPhams.Add(entity);
        await _context.SaveChangesAsync();

        var result = new NhomSanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            LastModified = entity.LastModified
        };

        return Result<NhomSanPhamDto>.Success(result, "Thêm nhóm thành công.")
            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<Result<NhomSanPhamDto>> Update(Guid id, NhomSanPhamDto dto)
    {
        var entity = await _context.NhomSanPhams
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NhomSanPhamDto>.Failure("Không tìm thấy nhóm.");

        var ten = dto.Ten.Trim();

        bool exist = await _context.NhomSanPhams
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == ten.ToLower());

        if (exist)
            return Result<NhomSanPhamDto>.Failure($"Nhóm {ten} đã tồn tại.");

        var before = new NhomSanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            LastModified = entity.LastModified
        };

        entity.Ten = ten;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = new NhomSanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            LastModified = entity.LastModified
        };

        return Result<NhomSanPhamDto>.Success(after, "Cập nhật thành công.")


            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<Result<NhomSanPhamDto>> Delete(Guid id)
    {
        var entity = await _context.NhomSanPhams
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NhomSanPhamDto>.Failure("Không tìm thấy nhóm.");

        var before = new NhomSanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            LastModified = entity.LastModified
        };

        _context.NhomSanPhams.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<NhomSanPhamDto>.Success(before, "Đã xoá.")

            ;
    }
}