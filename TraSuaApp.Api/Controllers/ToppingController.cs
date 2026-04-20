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
public class ToppingController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["Topping"];

    public ToppingController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private async Task NotifyClients(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "Topping", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "Topping", action, id.ToString(), ConnectionId ?? "");
        }
    }

    private static ToppingDto ToDto(Topping entity)
    {
        return new ToppingDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            Gia = entity.Gia,
            NgungBan = entity.NgungBan,
            LastModified = entity.LastModified,

            NhomSanPhams = entity.NhomSanPhams.Select(x => x.Id).ToList()
        };
    }

    private async Task<List<NhomSanPham>> LoadNhomSanPhamsAsync(IEnumerable<Guid>? ids)
    {
        var list = ids?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (list.Count == 0)
            return new List<NhomSanPham>();

        return await _context.NhomSanPhams
            .Where(x => list.Contains(x.Id))
            .ToListAsync();
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<ToppingDto>>>> GetAll()
    {
        var list = await _context.Toppings
            .AsNoTracking()
            .Include(x => x.NhomSanPhams)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Result<List<ToppingDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ToppingDto>>> GetById(Guid id)
    {
        var entity = await _context.Toppings
            .AsNoTracking()
            .Include(x => x.NhomSanPhams)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ToppingDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<ToppingDto>.Success(ToDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<Result<ToppingDto>>> Create(ToppingDto dto)
    {
        var ten = (dto.Ten ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ten))
            return Result<ToppingDto>.Failure("Tên topping không được để trống.");

        var exists = await _context.Toppings
            .AnyAsync(x => x.Ten.ToLower() == ten.ToLower());

        if (exists)
            return Result<ToppingDto>.Failure($"Topping {ten} đã tồn tại.");

        var now = DateTime.Now;
        var nhoms = await LoadNhomSanPhamsAsync(dto.NhomSanPhams);

        var entity = new Topping
        {
            Id = Guid.NewGuid(),
            Ten = ten,
            Gia = dto.Gia,
            NgungBan = dto.NgungBan,
            LastModified = now,
            NhomSanPhams = nhoms
        };

        _context.Toppings.Add(entity);
        await _context.SaveChangesAsync();

        entity = await _context.Toppings
            .AsNoTracking()
            .Include(x => x.NhomSanPhams)
            .FirstAsync(x => x.Id == entity.Id);

        var after = ToDto(entity);
        return Result<ToppingDto>.Success(after, $"Thêm {_friendlyName.ToLower()} thành công.")

            ;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ToppingDto>>> Update(Guid id, ToppingDto dto)
    {
        var entity = await _context.Toppings
            .Include(x => x.NhomSanPhams)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ToppingDto>.Failure($"Không tìm thấy {_friendlyName}.");

        var ten = (dto.Ten ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(ten))
            return Result<ToppingDto>.Failure("Tên topping không được để trống.");

        var exists = await _context.Toppings
            .AnyAsync(x => x.Id != id && x.Ten.ToLower() == ten.ToLower());

        if (exists)
            return Result<ToppingDto>.Failure($"Topping {ten} đã tồn tại.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.Ten = ten;
        entity.Gia = dto.Gia;
        entity.NgungBan = dto.NgungBan;
        entity.LastModified = now;

        var nhoms = await LoadNhomSanPhamsAsync(dto.NhomSanPhams);
        entity.NhomSanPhams.Clear();
        foreach (var nhom in nhoms)
            entity.NhomSanPhams.Add(nhom);

        await _context.SaveChangesAsync();

        entity = await _context.Toppings
            .AsNoTracking()
            .Include(x => x.NhomSanPhams)
            .FirstAsync(x => x.Id == id);

        var after = ToDto(entity);
        return Result<ToppingDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")


            ;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ToppingDto>>> Delete(Guid id)
    {
        var entity = await _context.Toppings
            .Include(x => x.NhomSanPhams)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ToppingDto>.Failure($"Không tìm thấy {_friendlyName}.");

        var before = ToDto(entity);

        _context.Toppings.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<ToppingDto>.Success(before, $"Đã xoá {_friendlyName.ToLower()}.")

            ;
    }
}
