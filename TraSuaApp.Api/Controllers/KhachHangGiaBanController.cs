using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Shared.Config;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KhachHangGiaBanController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHangGiaBan"];

    public KhachHangGiaBanController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static KhachHangGiaBanDto ToDto(KhachHangGiaBan e)
    {
        return new KhachHangGiaBanDto
        {
            Id = e.Id,
            KhachHangId = e.KhachHangId,
            SanPhamBienTheId = e.SanPhamBienTheId,
            GiaBan = e.GiaBan,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "KhachHangGiaBan", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "KhachHangGiaBan", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<KhachHangGiaBanDto>>>> GetAll()
    {
        var list = await _context.KhachHangGiaBans
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<KhachHangGiaBanDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<KhachHangGiaBanDto>>> GetById(Guid id)
    {
        var entity = await _context.KhachHangGiaBans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangGiaBanDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<KhachHangGiaBanDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<KhachHangGiaBanDto>>> Create(KhachHangGiaBanDto dto)
    {
        var entity = new KhachHangGiaBan
        {
            Id = Guid.NewGuid(),
            KhachHangId = dto.KhachHangId,
            SanPhamBienTheId = dto.SanPhamBienTheId,
            GiaBan = dto.GiaBan,
            LastModified = DateTime.Now
        };

        _context.KhachHangGiaBans.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("created", entity.Id);

        return Result<KhachHangGiaBanDto>.Success(after, "Đã thêm giá bán.")
            
            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<KhachHangGiaBanDto>>> Update(Guid id, KhachHangGiaBanDto dto)
    {
        var entity = await _context.KhachHangGiaBans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangGiaBanDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        entity.GiaBan = dto.GiaBan;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("updated", id);

        return Result<KhachHangGiaBanDto>.Success(after, "Cập nhật thành công.")
            
            
            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<KhachHangGiaBanDto>>> Delete(Guid id)
    {
        var entity = await _context.KhachHangGiaBans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangGiaBanDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        _context.KhachHangGiaBans.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<KhachHangGiaBanDto>.Success(before, "Đã xoá.")
            
            ;
    }
}