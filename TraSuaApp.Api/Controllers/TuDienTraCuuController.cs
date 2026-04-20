using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TuDienTraCuuController : ControllerBase
{
    private readonly AppDbContext _context;

    public TuDienTraCuuController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<Result<List<TuDienTraCuuDto>>> GetAll()
    {
        var list = await _context.TuDienTraCuus
            .AsNoTracking()
            .Select(x => new TuDienTraCuuDto
            {
                Id = x.Id,
                Ten = x.Ten,
                TenPhienDich = x.TenPhienDich
            })
            .ToListAsync();

        return Result<List<TuDienTraCuuDto>>.Success(list);
    }

    [HttpPost]
    public async Task<Result<TuDienTraCuuDto>> Create(TuDienTraCuuDto dto)
    {
        var entity = new TuDienTraCuu
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            TenPhienDich = dto.TenPhienDich,
            LastModified = DateTime.Now
        };

        _context.TuDienTraCuus.Add(entity);
        await _context.SaveChangesAsync();

        return Result<TuDienTraCuuDto>.Success(dto);
    }

    [HttpDelete("{id}")]
    public async Task<Result<TuDienTraCuuDto>> Delete(Guid id)
    {
        var entity = await _context.TuDienTraCuus.FindAsync(id);
        if (entity == null) return Result<TuDienTraCuuDto>.Failure("Không tìm thấy");

        _context.TuDienTraCuus.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<TuDienTraCuuDto>.Success(null);
    }
}