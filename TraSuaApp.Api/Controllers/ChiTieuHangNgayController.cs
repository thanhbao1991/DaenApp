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
public class ChiTieuHangNgayController : BaseApiController
{
    private readonly IChiTieuHangNgayService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["ChiTieuHangNgay"];

    public ChiTieuHangNgayController(IChiTieuHangNgayService service, IHubContext<SignalRHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    [HttpGet("nguyenlieu/{year:int}/{month:int}")]
    public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> GetByNguyenLieuMonth(int year, int month)
    {
        Guid nguyenLieuId = Guid.Parse("7995B334-44D1-4768-89C7-280E6B0413AE");
        var list = await _service.GetByNguyenLieuInMonth(nguyenLieuId, year, month);
        return Result<List<ChiTieuHangNgayDto>>.Success(list);
    }
    private async Task NotifyClients(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "ChiTieuHangNgay", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "ChiTieuHangNgay", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<ChiTieuHangNgayDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null)
            return Result<ChiTieuHangNgayDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<ChiTieuHangNgayDto>.Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Create(ChiTieuHangNgayDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Update(Guid id, ChiTieuHangNgayDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess)
            await NotifyClients("deleted", id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess)
            await NotifyClients("restored", id);

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<ChiTieuHangNgayDto>>.Success(list);
    }
}
