using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SanPhamController : BaseApiController
{
    private readonly ISanPhamService _service;

    public SanPhamController(ISanPhamService service)
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
            ? NotFound(new { Message = "Không tìm thấy sản phẩm." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SanPhamDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsSuccess
            ? Ok(new { result.Message, Id = dto.Id }) // ✅ Trả về Id để middleware log EntityId
            : BadRequest(new { result.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SanPhamDto dto)
    {
        dto.Id = id; // ✅ Gán lại id cho chắc chắn
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