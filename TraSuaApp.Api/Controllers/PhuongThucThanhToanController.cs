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
public class PhuongThucThanhToanController : BaseApiController
{
    private readonly IPhuongThucThanhToanService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["PhuongThucThanhToan"];

    public PhuongThucThanhToanController(IPhuongThucThanhToanService service, IHubContext<SignalRHub> hub)
    {
        _service = service;
        _hub = hub;
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
            await _hub.Clients.All.SendAsync("EntityChanged", "PhuongThucThanhToan", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<PhuongThucThanhToanDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<PhuongThucThanhToanDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null)
            return Result<PhuongThucThanhToanDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<PhuongThucThanhToanDto>.Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Create(PhuongThucThanhToanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Update(Guid id, PhuongThucThanhToanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess)
            await NotifyClients("deleted", id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<PhuongThucThanhToanDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess)
            await NotifyClients("restored", id);

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<PhuongThucThanhToanDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<PhuongThucThanhToanDto>>.Success(list);
    }
}
