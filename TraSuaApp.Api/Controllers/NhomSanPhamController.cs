using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NhomSanPhamController : BaseApiController
{
    private readonly INhomSanPhamService _service;

    public NhomSanPhamController(INhomSanPhamService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return FromResult(Result.Success().WithAfter(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? FromResult(Result.Failure("Không tìm thấy nhóm sản phẩm."))
            : FromResult(Result.Success().WithId(id).WithAfter(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NhomSanPhamDto dto)
        => FromResult(await _service.CreateAsync(dto));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NhomSanPhamDto dto)
        => FromResult(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => FromResult(await _service.DeleteAsync(id));
}