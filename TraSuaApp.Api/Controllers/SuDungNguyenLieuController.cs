using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Api.Hubs;

using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SuDungNguyenLieuController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;

    public SuDungNguyenLieuController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static SuDungNguyenLieuDto ToDto(SuDungNguyenLieu e)
    {
        return new SuDungNguyenLieuDto
        {
            Id = e.Id,
            CongThucId = e.CongThucId,
            NguyenLieuId = e.NguyenLieuId,
            SoLuong = e.SoLuong,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        await _hub.Clients.All.SendAsync("EntityChanged", "SuDungNguyenLieu", action, id.ToString(), "");
    }

    [HttpGet]
    public async Task<Result<List<SuDungNguyenLieuDto>>> GetAll()
    {
        var list = await _context.SuDungNguyenLieus
            .AsNoTracking()
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<SuDungNguyenLieuDto>>.Success(list);
    }

    [HttpPost]
    public async Task<Result<SuDungNguyenLieuDto>> Create(SuDungNguyenLieuDto dto)
    {
        var entity = new SuDungNguyenLieu
        {
            Id = Guid.NewGuid(),
            CongThucId = dto.CongThucId,
            NguyenLieuId = dto.NguyenLieuId,
            SoLuong = dto.SoLuong,
            LastModified = DateTime.Now
        };

        _context.SuDungNguyenLieus.Add(entity);
        await _context.SaveChangesAsync();

        await Notify("created", entity.Id);

        return Result<SuDungNguyenLieuDto>.Success(ToDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<Result<SuDungNguyenLieuDto>> Update(Guid id, SuDungNguyenLieuDto dto)
    {
        var entity = await _context.SuDungNguyenLieus.FindAsync(id);
        if (entity == null) return Result<SuDungNguyenLieuDto>.Failure("Không tìm thấy");

        entity.SoLuong = dto.SoLuong;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();
        await Notify("updated", id);

        return Result<SuDungNguyenLieuDto>.Success(ToDto(entity));
    }

    [HttpDelete("{id}")]
    public async Task<Result<SuDungNguyenLieuDto>> Delete(Guid id)
    {
        var entity = await _context.SuDungNguyenLieus.FindAsync(id);
        if (entity == null) return Result<SuDungNguyenLieuDto>.Failure("Không tìm thấy");

        _context.SuDungNguyenLieus.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<SuDungNguyenLieuDto>.Success(null);
    }
}