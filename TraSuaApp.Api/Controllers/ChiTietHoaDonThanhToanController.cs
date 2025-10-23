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
public class ChiTietHoaDonThanhToanController : BaseApiController
{
    private readonly IChiTietHoaDonThanhToanService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"];

    public ChiTietHoaDonThanhToanController(
        IChiTietHoaDonThanhToanService service,
        IHubContext<SignalRHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    // ==============================
    // 🟟 Helper phát tín hiệu chung
    // ==============================
    private async Task Notify(string entity, string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", entity, action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", entity, action, id.ToString(), ConnectionId ?? "");
        }
    }

    // ✅ Helper an toàn cho Guid? (tránh .Value khi null)
    private Task NotifyNullable(string entity, string action, Guid? id)
    {
        if (id.HasValue && id.Value != Guid.Empty)
            return Notify(entity, action, id.Value);
        return Task.CompletedTask;
    }

    // ==============================
    // 🟟 CRUD
    // ==============================

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
        {
            var data = result.Data;
            await Notify("ChiTietHoaDonThanhToan", "created", data.Id);

            // ✅ an toàn với null
            await NotifyNullable("ChiTietHoaDonNo", "updated", data.ChiTietHoaDonNoId);

            if (data.HoaDonId != Guid.Empty)
                await Notify("HoaDon", "updated", data.HoaDonId);
        }

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Update(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        if (result.IsSuccess)
        {
            await Notify("ChiTietHoaDonThanhToan", "updated", id);

            if (result.Data != null)
            {
                await NotifyNullable("ChiTietHoaDonNo", "updated", result.Data.ChiTietHoaDonNoId);

                if (result.Data.HoaDonId != Guid.Empty)
                    await Notify("HoaDon", "updated", result.Data.HoaDonId);
            }
        }

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (result.IsSuccess)
        {
            await Notify("ChiTietHoaDonThanhToan", "deleted", id);

            if (result.Data != null)
            {
                await NotifyNullable("ChiTietHoaDonNo", "updated", result.Data.ChiTietHoaDonNoId);

                if (result.Data.HoaDonId != Guid.Empty)
                    await Notify("HoaDon", "updated", result.Data.HoaDonId);
            }
        }

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);

        if (result.IsSuccess)
        {
            await Notify("ChiTietHoaDonThanhToan", "restored", id);

            if (result.Data != null)
            {
                await NotifyNullable("ChiTietHoaDonNo", "updated", result.Data.ChiTietHoaDonNoId);

                if (result.Data.HoaDonId != Guid.Empty)
                    await Notify("HoaDon", "updated", result.Data.HoaDonId);
            }
        }

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<ChiTietHoaDonThanhToanDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<ChiTietHoaDonThanhToanDto>>.Success(list);
    }
}