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
public class NguyenLieuBanHangController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuBanHang"];

    public NguyenLieuBanHangController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static NguyenLieuBanHangDto ToDto(NguyenLieuBanHang e)
    {
        return new NguyenLieuBanHangDto
        {
            Id = e.Id,
            Ten = e.Ten,
            DonViTinh = e.DonViTinh,
            TonKho = e.TonKho,
            DangSuDung = e.DangSuDung,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "NguyenLieuBanHang", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "NguyenLieuBanHang", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<NguyenLieuBanHangDto>>>> GetAll()
    {
        var list = await _context.NguyenLieuBanHangs
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<NguyenLieuBanHangDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<NguyenLieuBanHangDto>>> GetById(Guid id)
    {
        var entity = await _context.NguyenLieuBanHangs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuBanHangDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<NguyenLieuBanHangDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<NguyenLieuBanHangDto>>> Create(NguyenLieuBanHangDto dto)
    {
        var entity = new NguyenLieuBanHang
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DonViTinh = dto.DonViTinh,
            TonKho = dto.TonKho, // 🟟 giữ nguyên tồn đầu
            DangSuDung = dto.DangSuDung,
            LastModified = DateTime.Now
        };

        _context.NguyenLieuBanHangs.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("created", entity.Id);

        return Result<NguyenLieuBanHangDto>.Success(after, "Đã thêm nguyên liệu bán hàng.")
            
            ;
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<NguyenLieuBanHangDto>>> Update(Guid id, NguyenLieuBanHangDto dto)
    {
        var entity = await _context.NguyenLieuBanHangs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuBanHangDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.DonViTinh = dto.DonViTinh;

        // 🟟 cập nhật tồn trực tiếp (cho phép âm)
        entity.TonKho = dto.TonKho;

        entity.DangSuDung = dto.DangSuDung;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await Notify("updated", id);

        return Result<NguyenLieuBanHangDto>.Success(after, "Cập nhật thành công.")
            
            
            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<NguyenLieuBanHangDto>>> Delete(Guid id)
    {
        var entity = await _context.NguyenLieuBanHangs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuBanHangDto>.Failure("Không tìm thấy.");

        var before = ToDto(entity);

        _context.NguyenLieuBanHangs.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<NguyenLieuBanHangDto>.Success(before, "Đã xoá.")
            
            ;
    }
}