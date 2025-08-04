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
public class VoucherController : BaseApiController
{
    private readonly IVoucherService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["Voucher"];

    public VoucherController(IVoucherService service, IHubContext<SignalRHub> hub)
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
                .SendAsync("EntityChanged", "Voucher", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "Voucher", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<VoucherDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<VoucherDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<VoucherDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null)
            return Result<VoucherDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<VoucherDto>.Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<VoucherDto>>> Create(VoucherDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<VoucherDto>>> Update(Guid id, VoucherDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<VoucherDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess)
            await NotifyClients("deleted", id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<VoucherDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess)
            await NotifyClients("restored", id);

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<VoucherDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<VoucherDto>>.Success(list);
    }
}
