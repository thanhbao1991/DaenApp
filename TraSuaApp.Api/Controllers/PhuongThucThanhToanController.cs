using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PhuongThucThanhToanController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["PhuongThucThanhToan"];

    public PhuongThucThanhToanController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static PhuongThucThanhToanDto ToDto(PhuongThucThanhToan e)
    {
        return new PhuongThucThanhToanDto
        {
            Id = e.Id,
            Ten = e.Ten,
            DangSuDung = e.DangSuDung,
            LastModified = e.LastModified,
        };
    }

    private async Task NotifyClients(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "PhuongThucThanhToan", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "PhuongThucThanhToan", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<PhuongThucThanhToanDto>>>> GetAll()
    {
        var list = await _context.PhuongThucThanhToans
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<PhuongThucThanhToanDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> GetById(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<PhuongThucThanhToanDto>.Success(ToDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Create(PhuongThucThanhToanDto dto)
    {
        var ten = (dto.Ten ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ten))
            return Result<PhuongThucThanhToanDto>.Failure($"Tên {_friendlyName} không được để trống.");

        bool exist = await _context.PhuongThucThanhToans
            .AnyAsync(x => x.Ten.ToLower() == ten.ToLower());

        if (exist)
            return Result<PhuongThucThanhToanDto>.Failure($"{_friendlyName} {ten} đã tồn tại.");

        var now = DateTime.Now;

        var entity = new PhuongThucThanhToan
        {
            Id = Guid.NewGuid(),
            Ten = ten,
            DangSuDung = dto.DangSuDung,
            LastModified = now
        };

        _context.PhuongThucThanhToans.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        if (after.Id != Guid.Empty)
            await NotifyClients("created", after.Id);

        return Result<PhuongThucThanhToanDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            ;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Update(Guid id, PhuongThucThanhToanDto dto)
    {
        var entity = await _context.PhuongThucThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var ten = (dto.Ten ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ten))
            return Result<PhuongThucThanhToanDto>.Failure($"Tên {_friendlyName} không được để trống.");

        bool exist = await _context.PhuongThucThanhToans
            .AnyAsync(x => x.Id != id && x.Ten.ToLower() == ten.ToLower());

        if (exist)
            return Result<PhuongThucThanhToanDto>.Failure($"{_friendlyName} {ten} đã tồn tại.");

        var before = ToDto(entity);

        entity.Ten = ten;
        entity.DangSuDung = dto.DangSuDung;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await NotifyClients("updated", id);

        return Result<PhuongThucThanhToanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")


            ;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Delete(Guid id)
    {
        var entity = await _context.PhuongThucThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        _context.PhuongThucThanhToans.Remove(entity);
        await _context.SaveChangesAsync();

        await NotifyClients("deleted", id);

        return Result<PhuongThucThanhToanDto>.Success(before, $"Đã xoá {_friendlyName.ToLower()} thành công.")

            ;
    }
}