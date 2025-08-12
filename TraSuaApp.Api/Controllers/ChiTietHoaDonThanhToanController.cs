using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChiTietHoaDonThanhToanController : BaseApiController
{
    private readonly IChiTietHoaDonThanhToanService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"];

    public ChiTietHoaDonThanhToanController(IChiTietHoaDonThanhToanService service, IHubContext<SignalRHub> hub)
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
                .SendAsync("EntityChanged", "ChiTietHoaDonThanhToan", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "ChiTietHoaDonThanhToan", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<ChiTietHoaDonThanhToanDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<ChiTietHoaDonThanhToanDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<ChiTietHoaDonThanhToanDto>.Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Create(ChiTietHoaDonThanhToanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Update(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess)
            await NotifyClients("deleted", id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess)
            await NotifyClients("restored", id);

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<ChiTietHoaDonThanhToanDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<ChiTietHoaDonThanhToanDto>>.Success(list);
    }
}
