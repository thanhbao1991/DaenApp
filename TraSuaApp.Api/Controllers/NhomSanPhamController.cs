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
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
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
        var result = await _service.CreateAsync(dto);
        return result.IsSuccess
            ? Ok(new { result.Message, Id = dto.Id }) // ✅ Trả về Id để middleware log được EntityId
            : BadRequest(new { result.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NhomSanPhamDto dto)
    {
        dto.Id = id; // Gán lại để đảm bảo ID đúng
        var result = await _service.UpdateAsync(id, dto);
        return result.IsSuccess
            ? Ok(new { result.Message, Id = id }) // ✅ Trả về Id
            : BadRequest(new { result.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess
            ? Ok(new { result.Message, Id = id }) // ✅ Trả về Id
            : BadRequest(new { result.Message });
    }
}