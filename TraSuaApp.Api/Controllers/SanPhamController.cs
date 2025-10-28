using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SanPhamController : BaseApiController
{
    private readonly ISanPhamService _service;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["SanPham"];

    public SanPhamController(ISanPhamService service, IHubContext<SignalRHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    [HttpGet("search")]
    public async Task<ActionResult> Search(string? q, int take = 30)
    {
        var data = await _service.SearchAsync(q ?? string.Empty, take);
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

    [HttpGet]
    public async Task<ActionResult<Result<List<SanPhamDto>>>> GetAll()
        => Result<List<SanPhamDto>>.Success(data: await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<SanPhamDto?>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? Result<SanPhamDto?>.Failure($"Không tìm thấy {_friendlyName}.")
            : Result<SanPhamDto?>.Success(data: result);
    }

    // Cập nhật nhanh 1 trường (ví dụ: ThuTu) cho 1 sản phẩm
    [HttpPut("{id}/single")]
    public async Task<ActionResult<Result<SanPhamDto>>> UpdateSingle(Guid id, SanPhamDto dto)
    {
        var result = await _service.UpdateSingleAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<Result<SanPhamDto>>> Create(SanPhamDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<SanPhamDto>>> Update(Guid id, SanPhamDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<SanPhamDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("deleted", result.Data.Id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<SanPhamDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("restored", result.Data.Id);

        return result;
    }

    // API đồng bộ theo mốc thời gian
    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<SanPhamDto>>>> Sync(DateTime lastSync)
    {
        var data = await _service.GetUpdatedSince(lastSync);
        return Result<List<SanPhamDto>>.Success(data);
    }
}