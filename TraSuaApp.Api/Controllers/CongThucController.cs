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
public class CongThucController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["CongThuc"];

    public CongThucController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static CongThucDto ToDto(CongThuc e)
    {
        return new CongThucDto
        {
            Id = e.Id,
            Ten = e.Ten,
            Loai = e.Loai,
            IsDefault = e.IsDefault,
            SanPhamBienTheId = e.SanPhamBienTheId,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "CongThuc", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "CongThuc", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<CongThucDto>>>> GetAll()
    {
        var list = await _context.CongThucs
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<CongThucDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<CongThucDto>>> GetById(Guid id)
    {
        var entity = await _context.CongThucs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongThucDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<CongThucDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<CongThucDto>>> Create(CongThucDto dto)
    {
        var entity = new CongThuc
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten?.Trim(),
            Loai = dto.Loai,
            IsDefault = dto.IsDefault,
            SanPhamBienTheId = dto.SanPhamBienTheId,
            LastModified = DateTime.Now
        };

        _context.CongThucs.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("created", entity.Id);

        return Result<CongThucDto>.Success(after, "Đã thêm công thức.")

            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<CongThucDto>>> Update(Guid id, CongThucDto dto)
    {
        var entity = await _context.CongThucs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongThucDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten?.Trim();
        entity.Loai = dto.Loai;
        entity.IsDefault = dto.IsDefault;
        entity.SanPhamBienTheId = dto.SanPhamBienTheId;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("updated", id);

        return Result<CongThucDto>.Success(after, "Cập nhật thành công.")


            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<CongThucDto>>> Delete(Guid id)
    {
        var entity = await _context.CongThucs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongThucDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        _context.CongThucs.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<CongThucDto>.Success(before, "Đã xoá.")

            ;
    }
}