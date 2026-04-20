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
public class VoucherController : ControllerBase
{
    private readonly AppDbContext _context;

    public VoucherController(AppDbContext context)
    {
        _context = context;
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<Result<List<VoucherDto>>> GetAll()
    {
        var list = await _context.Vouchers
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => new VoucherDto
            {
                Id = x.Id,
                Ten = x.Ten,
                GiaTri = x.GiaTri,
                KieuGiam = x.KieuGiam,
                LastModified = x.LastModified
            })
            .ToListAsync();

        return Result<List<VoucherDto>>.Success(list);
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<Result<VoucherDto>> Create(VoucherDto dto)
    {
        var entity = new Voucher
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            GiaTri = dto.GiaTri,
            KieuGiam = dto.KieuGiam,
            LastModified = DateTime.Now
        };

        _context.Vouchers.Add(entity);
        await _context.SaveChangesAsync();

        return Result<VoucherDto>.Success(new VoucherDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            GiaTri = entity.GiaTri,
            KieuGiam = entity.KieuGiam,
            LastModified = entity.LastModified
        }, "Thêm thành công");
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<Result<VoucherDto>> Update(Guid id, VoucherDto dto)
    {
        var entity = await _context.Vouchers.FindAsync(id);

        if (entity == null)
            return Result<VoucherDto>.Failure("Không tìm thấy");

        entity.Ten = dto.Ten;
        entity.GiaTri = dto.GiaTri;
        entity.KieuGiam = dto.KieuGiam;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<VoucherDto>.Success(new VoucherDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            GiaTri = entity.GiaTri,
            KieuGiam = entity.KieuGiam,
            LastModified = entity.LastModified
        }, "Cập nhật thành công");
    }

    // ======================
    // DELETE
    // ======================
    [HttpDelete("{id}")]
    public async Task<Result<VoucherDto>> Delete(Guid id)
    {
        var entity = await _context.Vouchers.FindAsync(id);

        if (entity == null)
            return Result<VoucherDto>.Failure("Không tìm thấy");

        _context.Vouchers.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<VoucherDto>.Success(new VoucherDto
        {
            Id = entity.Id,
            Ten = entity.Ten
        }, "Đã xoá");
    }
}