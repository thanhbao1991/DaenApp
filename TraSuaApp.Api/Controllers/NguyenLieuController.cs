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
public class NguyenLieuController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieu"];

    public NguyenLieuController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static NguyenLieuDto ToDto(NguyenLieu e)
    {
        return new NguyenLieuDto
        {
            Id = e.Id,
            Ten = e.Ten,
            DonViTinh = e.DonViTinh,
            GiaNhap = e.GiaNhap,
            DangSuDung = e.DangSuDung,
            NguyenLieuBanHangId = e.NguyenLieuBanHangId,
            HeSoQuyDoiBanHang = e.HeSoQuyDoiBanHang,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "NguyenLieu", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "NguyenLieu", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<NguyenLieuDto>>>> GetAll()
    {
        var list = await _context.NguyenLieus
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<NguyenLieuDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<NguyenLieuDto>>> GetById(Guid id)
    {
        var entity = await _context.NguyenLieus
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<NguyenLieuDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<NguyenLieuDto>>> Create(NguyenLieuDto dto)
    {
        var entity = new NguyenLieu
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DonViTinh = dto.DonViTinh,
            GiaNhap = dto.GiaNhap,
            DangSuDung = dto.DangSuDung,
            NguyenLieuBanHangId = dto.NguyenLieuBanHangId,
            HeSoQuyDoiBanHang = dto.HeSoQuyDoiBanHang,
            LastModified = DateTime.Now
        };

        _context.NguyenLieus.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("created", entity.Id);

        return Result<NguyenLieuDto>.Success(after, "Đã thêm nguyên liệu.")
            
            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<NguyenLieuDto>>> Update(Guid id, NguyenLieuDto dto)
    {
        var entity = await _context.NguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.DonViTinh = dto.DonViTinh;
        entity.GiaNhap = dto.GiaNhap;
        entity.DangSuDung = dto.DangSuDung;
        entity.NguyenLieuBanHangId = dto.NguyenLieuBanHangId;
        entity.HeSoQuyDoiBanHang = dto.HeSoQuyDoiBanHang;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("updated", id);

        return Result<NguyenLieuDto>.Success(after, "Cập nhật thành công.")
            
            
            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<NguyenLieuDto>>> Delete(Guid id)
    {
        var entity = await _context.NguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        _context.NguyenLieus.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<NguyenLieuDto>.Success(before, "Đã xoá.")
            
            ;
    }
}