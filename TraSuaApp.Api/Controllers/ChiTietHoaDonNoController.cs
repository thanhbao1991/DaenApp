using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Dtos.Requests;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChiTietHoaDonNoController : BaseApiController
{
    private readonly IChiTietHoaDonNoService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonNo"];

    public ChiTietHoaDonNoController(IChiTietHoaDonNoService service, IHubContext<SignalRHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    // ==============================
    // 🟟 Helpers phát tín hiệu
    // ==============================

    // Generic notifier cho Guid (non-nullable)
    private Task Notify(string entity, string action, Guid id)
    {
        if (id == Guid.Empty) return Task.CompletedTask;

        if (!string.IsNullOrEmpty(ConnectionId))
        {
            return _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", entity, action, id.ToString(), ConnectionId);
        }

        return _hub.Clients.All
            .SendAsync("EntityChanged", entity, action, id.ToString(), ConnectionId ?? "");
    }

    // Generic notifier an toàn cho Guid? (nullable)
    private Task NotifyNullable(string entity, string action, Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty) return Task.CompletedTask;
        return Notify(entity, action, id.Value);
    }

    // Back-compat: notifier chuyên cho ChiTietHoaDonNo
    private Task NotifyClients(string action, Guid id) => Notify("ChiTietHoaDonNo", action, id);

    // ==============================
    // 🟟 Actions
    // ==============================

    [HttpPost("{id}/pay")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Pay(Guid id, PayDebtRequest req)
    {
        var result = await _service.PayDebtAsync(id, req);

        if (result.IsSuccess)
        {
            // 1️⃣ Cập nhật công nợ
            await NotifyClients("updated", id);

            // 2️⃣ Tạo thanh toán mới + cập nhật hóa đơn liên quan
            if (result.Data != null)
            {
                var thanhToanId = result.Data.Id;        // Guid (non-null)
                var hoaDonId = result.Data.HoaDonId;  // Guid hoặc Guid?

                await Notify("ChiTietHoaDonThanhToan", "created", thanhToanId);
                await NotifyNullable("HoaDon", "updated", hoaDonId);
            }
        }

        return result;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<ChiTietHoaDonNoDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<ChiTietHoaDonNoDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonNoDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<ChiTietHoaDonNoDto>.Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<ChiTietHoaDonNoDto>>> Create(ChiTietHoaDonNoDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonNoDto>>> Update(Guid id, ChiTietHoaDonNoDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonNoDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess)
            await NotifyClients("deleted", id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<ChiTietHoaDonNoDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess)
            await NotifyClients("restored", id);

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<ChiTietHoaDonNoDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<ChiTietHoaDonNoDto>>.Success(list);
    }
}