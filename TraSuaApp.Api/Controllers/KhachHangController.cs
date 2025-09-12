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
public class KhachHangController : BaseApiController
{
    private readonly IKhachHangService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

    public KhachHangController(IKhachHangService service, IHubContext<SignalRHub> hub)
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
                .SendAsync("EntityChanged", "khachhang", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "khachhang", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<KhachHangDto>>>> GetAll()
        => Result<List<KhachHangDto>>.Success(data: await _service.GetAllAsync());
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<KhachHangDto?>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? Result<KhachHangDto?>.Failure($"KhÃ´ng tÃ¬m tháº¥y {_friendlyName}.")
            : Result<KhachHangDto?>.Success(data: result);
    }
    [HttpPost]
    public async Task<ActionResult<Result<KhachHangDto>>> Create(KhachHangDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);
        return result;
    }
    [HttpPut("{id}/single")]
    public async Task<ActionResult<Result<KhachHangDto>>> UpdateSingle(Guid id, KhachHangDto dto)
    {
        var result = await _service.UpdateSingleAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<KhachHangDto>>> Update(Guid id, KhachHangDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id);
        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<KhachHangDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("deleted", result.Data.Id);
        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<KhachHangDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("restored", result.Data.Id);
        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<KhachHangDto>>>> Sync(DateTime lastSync)
    {
        var data = await _service.GetUpdatedSince(lastSync);
        return Result<List<KhachHangDto>>.Success(data);
    }
}
