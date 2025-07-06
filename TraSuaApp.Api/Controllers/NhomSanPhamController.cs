using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NhomSanPhamController : ControllerBase
{
    private readonly INhomSanPhamService _service;

    public NhomSanPhamController(INhomSanPhamService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { Message = "Không tìm thấy nhóm sản phẩm." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NhomSanPhamDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NhomSanPhamDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);
        return ok
            ? NoContent()
            : NotFound(new { Message = "Không tìm thấy nhóm sản phẩm để cập nhật." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        return ok
            ? NoContent()
            : NotFound(new { Message = "Không tìm thấy nhóm sản phẩm để xoá." });
    }
}