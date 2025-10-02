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
public class HoaDonController : BaseApiController
{
    private readonly IHoaDonService _service;
    private readonly IHubContext<SignalRHub> _hub;
    string _friendlyName = TuDien._tableFriendlyNames["HoaDon"];

    public HoaDonController(IHoaDonService service, IHubContext<SignalRHub> hub)
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
                .SendAsync("EntityChanged", "HoaDon", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "HoaDon", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<HoaDonDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<HoaDonDto>>.Success(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<HoaDonDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        if (dto == null)
            return Result<HoaDonDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<HoaDonDto>.Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<HoaDonDto>>> Create(HoaDonDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("created", result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<HoaDonDto>>> Update(Guid id, HoaDonDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<HoaDonDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (result.IsSuccess)
            await NotifyClients("deleted", id);

        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<Result<HoaDonDto>>> Restore(Guid id)
    {
        var result = await _service.RestoreAsync(id);
        if (result.IsSuccess)
            await NotifyClients("restored", id);

        return result;
    }

    [HttpPut("{id}/single")]
    public async Task<ActionResult<Result<HoaDonDto>>> UpdateSingle(Guid id, HoaDonDto dto)
    {
        var result = await _service.UpdateSingleAsync(id, dto);
        if (result.IsSuccess)
            await NotifyClients("updated", id);

        return result;
    }

    [HttpGet("sync")]
    public async Task<ActionResult<Result<List<HoaDonDto>>>> Sync(DateTime lastSync)
    {
        var list = await _service.GetUpdatedSince(lastSync);
        return Result<List<HoaDonDto>>.Success(list);
    }








    // 🟟 Lấy danh sách hóa đơn dành cho shipper
    [HttpGet("shipper")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<List<HoaDonDto>>>> GetForShipper()
    {
        var list = await _service.GetForShipperAsync();
        return Result<List<HoaDonDto>>.Success(list);
    }






    // 🟟 Thu tiền mặt
    [HttpPost("shipperf1/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> ThuTienMat(Guid id)
    {
        var result = await _service.ThuTienMatAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id);   // bắn signal cập nhật
        return result;
    }

    // 🟟 Thu chuyển khoản
    [HttpPost("shipperf4/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> ThuChuyenKhoan(Guid id)
    {
        var result = await _service.ThuChuyenKhoanAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id);   // bắn signal cập nhật
        return result;
    }

    // 🟟 Thu chuyển khoản
    [HttpPost("shipper55/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> TiNuaChuyenKhoan(Guid id)
    {
        var result = await _service.TiNuaChuyenKhoanAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id);   // bắn signal cập nhật
        return result;
    }


    // 🟟 Ghi nợ
    [HttpPost("shipper12/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> GhiNo(Guid id)
    {
        var result = await _service.GhiNoAsync(id);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id);   // bắn signal cập nhật
        return result;
    }

    // 🟟 Khách đã trả nợ
    [HttpPost("shipper99/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> TraNo(Guid id, [FromBody] decimal soTien)
    {
        var result = await _service.TraNoAsync(id, soTien);
        if (result.IsSuccess && result.Data != null)
            await NotifyClients("updated", result.Data.Id); // bắn signal cập nhật

        return result;
    }
}
