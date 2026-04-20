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
public class CongViecNoiBoController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["CongViecNoiBo"];

    public CongViecNoiBoController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static CongViecNoiBoDto ToDto(CongViecNoiBo e)
    {
        return new CongViecNoiBoDto
        {
            Id = e.Id,
            Ten = e.Ten,
            DaHoanThanh = e.DaHoanThanh,
            NgayGio = e.NgayGio,
            NgayCanhBao = e.NgayCanhBao,
            XNgayCanhBao = e.XNgayCanhBao,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "CongViecNoiBo", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "CongViecNoiBo", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<CongViecNoiBoDto>>>> GetAll()
    {
        var list = await _context.CongViecNoiBos
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<CongViecNoiBoDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<CongViecNoiBoDto>>> GetById(Guid id)
    {
        var entity = await _context.CongViecNoiBos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongViecNoiBoDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<CongViecNoiBoDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<CongViecNoiBoDto>>> Create(CongViecNoiBoDto dto)
    {
        var entity = new CongViecNoiBo
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DaHoanThanh = dto.DaHoanThanh,
            NgayGio = dto.NgayGio,
            NgayCanhBao = dto.NgayCanhBao,
            XNgayCanhBao = dto.XNgayCanhBao,
            LastModified = DateTime.Now
        };

        _context.CongViecNoiBos.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("created", entity.Id);

        return Result<CongViecNoiBoDto>.Success(after, "Đã thêm công việc.")
            
            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<CongViecNoiBoDto>>> Update(Guid id, CongViecNoiBoDto dto)
    {
        var entity = await _context.CongViecNoiBos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongViecNoiBoDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.DaHoanThanh = dto.DaHoanThanh;
        entity.NgayGio = dto.NgayGio;
        entity.NgayCanhBao = dto.NgayCanhBao;
        entity.XNgayCanhBao = dto.XNgayCanhBao;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("updated", id);

        return Result<CongViecNoiBoDto>.Success(after, "Cập nhật thành công.")
            
            
            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<CongViecNoiBoDto>>> Delete(Guid id)
    {
        var entity = await _context.CongViecNoiBos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongViecNoiBoDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        _context.CongViecNoiBos.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<CongViecNoiBoDto>.Success(before, "Đã xoá.")
            
            ;
    }
}