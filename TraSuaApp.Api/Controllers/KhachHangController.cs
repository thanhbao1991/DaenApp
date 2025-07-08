using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Controllers;
using TraSuaApp.Api.Extensions;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KhachHangController : BaseApiController
{
    private readonly IKhachHangService _service;

    public KhachHangController(IKhachHangService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result.Success().WithAfter(list).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result.Failure("Không tìm thấy khách hàng.").ToActionResult()
            : Result.Success().WithId(id).WithAfter(dto).ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KhachHangDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] KhachHangDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.ToActionResult();
    }
}