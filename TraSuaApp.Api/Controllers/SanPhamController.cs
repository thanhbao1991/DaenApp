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
public class SanPhamController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["SanPham"];

    public SanPhamController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet("search")]
    public async Task<ActionResult> Search(string? q, int take = 30)
    {
        q = (q ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new { data = new List<SanPhamDto>() });

        take = Math.Clamp(take, 1, 50);
        var nx = StringHelper.MyNormalizeText(q);

        var data = await _context.SanPhams
            .AsNoTracking()
            .Where(x => !x.NgungBan)
            .Where(x => EF.Functions.Like(x.TimKiem!, $"%{nx}%"))
            .OrderByDescending(x => x.ThuTu)
            .ThenBy(x => x.Ten)
            .Take(take)
            .Select(entity => new SanPhamDto
            {
                Id = entity.Id,
                Ten = entity.Ten,
                VietTat = entity.VietTat,
                TenKhongVietTat = entity.TenKhongVietTat,
                ThuTu = entity.ThuTu,
                NgungBan = entity.NgungBan,
                TichDiem = entity.TichDiem,
                NhomSanPhamId = entity.NhomSanPhamId,
                TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
                LastModified = entity.LastModified,

                BienThe = entity.SanPhamBienThes
                    .OrderBy(bt => bt.GiaBan)
                    .Select(bt => new SanPhamBienTheDto
                    {
                        Id = bt.Id,
                        SanPhamId = bt.SanPhamId,
                        TenBienThe = bt.TenBienThe,
                        GiaBan = bt.GiaBan,
                        DinhLuong = bt.DinhLuong,
                        MacDinh = bt.MacDinh
                    }).ToList()
            })
            .ToListAsync();

        return Ok(new { data });
    }

    private async Task NotifyClients(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "SanPham", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "SanPham", action, id.ToString(), ConnectionId ?? "");
        }
    }

    private static SanPhamDto ToDto(SanPham entity)
    {
        return new SanPhamDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            VietTat = entity.VietTat,
            TenKhongVietTat = entity.TenKhongVietTat,
            ThuTu = entity.ThuTu,
            NgungBan = entity.NgungBan,
            TichDiem = entity.TichDiem,
            NhomSanPhamId = entity.NhomSanPhamId,
            TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
            LastModified = entity.LastModified,

            BienThe = entity.SanPhamBienThes
                .OrderBy(bt => bt.GiaBan)
                .Select(bt => new SanPhamBienTheDto
                {
                    Id = bt.Id,
                    SanPhamId = bt.SanPhamId,
                    TenBienThe = bt.TenBienThe,
                    GiaBan = bt.GiaBan,
                    DinhLuong = bt.DinhLuong,
                    MacDinh = bt.MacDinh
                })
                .ToList()
        };
    }

    private static string BuildTimKiem(SanPhamDto dto)
    {
        var raw = dto.Ten?.Trim() ?? string.Empty;
        var nx = StringHelper.MyNormalizeText(raw);

        var compact = nx.Replace(" ", string.Empty);
        var initials = string.Join(string.Empty, nx.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w[0]));
        var spaced = nx;

        var vt = StringHelper.MyNormalizeText(dto.VietTat?.Trim() ?? string.Empty);

        return string.Join(";", new[] { compact, initials, spaced, vt }
            .Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToLower();
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<SanPhamDto>>>> GetAll()
    {
        var list = await _context.SanPhams
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Select(entity => new SanPhamDto
            {
                Id = entity.Id,
                Ten = entity.Ten,
                VietTat = entity.VietTat,
                TenKhongVietTat = entity.TenKhongVietTat,
                ThuTu = entity.ThuTu,
                NgungBan = entity.NgungBan,
                TichDiem = entity.TichDiem,
                NhomSanPhamId = entity.NhomSanPhamId,
                TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
                LastModified = entity.LastModified,

                BienThe = entity.SanPhamBienThes
                    .OrderBy(bt => bt.GiaBan)
                    .Select(b => new SanPhamBienTheDto
                    {
                        Id = b.Id,
                        SanPhamId = b.SanPhamId,
                        TenBienThe = b.TenBienThe,
                        GiaBan = b.GiaBan,
                        MacDinh = b.MacDinh,
                        DinhLuong = b.DinhLuong,
                    }).ToList()
            })
            .ToListAsync();

        return Result<List<SanPhamDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<SanPhamDto?>>> GetById(Guid id)
    {
        var result = await _context.SanPhams
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(entity => new SanPhamDto
            {
                Id = entity.Id,
                Ten = entity.Ten,
                VietTat = entity.VietTat,
                TenKhongVietTat = entity.TenKhongVietTat,
                ThuTu = entity.ThuTu,
                NgungBan = entity.NgungBan,
                TichDiem = entity.TichDiem,
                NhomSanPhamId = entity.NhomSanPhamId,
                TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
                LastModified = entity.LastModified,

                BienThe = entity.SanPhamBienThes
                    .OrderBy(bt => bt.GiaBan)
                    .Select(b => new SanPhamBienTheDto
                    {
                        Id = b.Id,
                        SanPhamId = b.SanPhamId,
                        TenBienThe = b.TenBienThe,
                        GiaBan = b.GiaBan,
                        MacDinh = b.MacDinh,
                        DinhLuong = b.DinhLuong,
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        return result == null
            ? Result<SanPhamDto?>.Failure($"Không tìm thấy {_friendlyName}.")
            : Result<SanPhamDto?>.Success(data: result);
    }

    [HttpPut("{id}/single")]
    public async Task<ActionResult<Result<SanPhamDto>>> UpdateSingle(Guid id, SanPhamDto dto)
    {
        var entity = await _context.SanPhams
            .Include(x => x.SanPhamBienThes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<SanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.ThuTu = dto.ThuTu;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        var result = Result<SanPhamDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")


            ;

        await NotifyClients("updated", id);
        return result;
    }

    [HttpPost]
    public async Task<ActionResult<Result<SanPhamDto>>> Create(SanPhamDto dto)
    {
        var bienThe = dto.BienThe ?? new List<SanPhamBienTheDto>();

        var entity = new SanPham
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            VietTat = dto.VietTat?.Trim(),
            TenKhongVietTat = string.IsNullOrWhiteSpace(dto.TenKhongVietTat)
                ? dto.Ten.ToLower()
                : dto.TenKhongVietTat,
            TimKiem = BuildTimKiem(dto),
            ThuTu = dto.ThuTu,
            NgungBan = dto.NgungBan,
            TichDiem = dto.TichDiem,
            NhomSanPhamId = dto.NhomSanPhamId,
            LastModified = DateTime.Now,
            SanPhamBienThes = bienThe.Select(b => new SanPhamBienThe
            {
                Id = Guid.NewGuid(),
                TenBienThe = b.TenBienThe,
                GiaBan = b.GiaBan,
                MacDinh = b.MacDinh,
                SanPhamId = Guid.Empty,
                DinhLuong = b.DinhLuong,
            }).ToList()
        };

        foreach (var bt in entity.SanPhamBienThes)
            bt.SanPhamId = entity.Id;

        _context.SanPhams.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        var result = Result<SanPhamDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            ;

        await NotifyClients("created", after.Id);
        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<SanPhamDto>>> Update(Guid id, SanPhamDto dto)
    {
        var entity = await _context.SanPhams
            .Include(x => x.SanPhamBienThes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<SanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten.Trim();
        entity.VietTat = dto.VietTat?.Trim();
        entity.TenKhongVietTat = string.IsNullOrWhiteSpace(dto.TenKhongVietTat)
            ? StringHelper.MyNormalizeText(dto.Ten).ToLower()
            : dto.TenKhongVietTat;

        entity.TimKiem = BuildTimKiem(dto);
        entity.ThuTu = dto.ThuTu;
        entity.NgungBan = dto.NgungBan;
        entity.TichDiem = dto.TichDiem;
        entity.NhomSanPhamId = dto.NhomSanPhamId;
        entity.LastModified = DateTime.Now;

        dto.BienThe ??= new List<SanPhamBienTheDto>();

        var removedVariants = entity.SanPhamBienThes
            .Where(x => !dto.BienThe.Any(d => d.Id == x.Id))
            .ToList();

        foreach (var v in removedVariants)
            _context.SanPhamBienThes.Remove(v);

        foreach (var variant in entity.SanPhamBienThes)
        {
            var dtoVariant = dto.BienThe.FirstOrDefault(x => x.Id == variant.Id);
            if (dtoVariant == null)
                continue;

            variant.TenBienThe = dtoVariant.TenBienThe;
            variant.GiaBan = dtoVariant.GiaBan;
            variant.MacDinh = dtoVariant.MacDinh;
            variant.DinhLuong = dtoVariant.DinhLuong; // 🟟 FIX

            variant.LastModified = DateTime.Now;
        }

        var newVariants = dto.BienThe.Where(x => x.Id == Guid.Empty);
        foreach (var b in newVariants)
        {
            entity.SanPhamBienThes.Add(new SanPhamBienThe
            {
                Id = Guid.NewGuid(),
                SanPhamId = entity.Id,
                TenBienThe = b.TenBienThe,
                GiaBan = b.GiaBan,
                MacDinh = b.MacDinh,
                DinhLuong = b.DinhLuong,
                LastModified = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        var result = Result<SanPhamDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")


            ;

        await NotifyClients("updated", id);
        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<SanPhamDto>>> Delete(Guid id)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var entity = await _context.SanPhams
                .Include(x => x.SanPhamBienThes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<SanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

            var hasInvoice = await _context.ChiTietHoaDons
                .AnyAsync(x => x.SanPhamId == id);

            if (hasInvoice)
                return Result<SanPhamDto>.Failure($"{_friendlyName} đã phát sinh hoá đơn. Không thể xoá.");

            var before = ToDto(entity);

            _context.SanPhamBienThes.RemoveRange(entity.SanPhamBienThes);
            _context.SanPhams.Remove(entity);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var result = Result<SanPhamDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")

                ;

            await NotifyClients("deleted", id);
            //git
            return result;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result<SanPhamDto>.Failure(ex.Message);
        }
    }
}
