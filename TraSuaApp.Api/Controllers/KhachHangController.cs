using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Controllers;

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
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { Message = "Không tìm thấy khách hàng." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KhachHangDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return result.IsSuccess
                ? Ok(new { result.Message, Id = dto.Id }) // ✅ Trả về Id
                : BadRequest(new { result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] KhachHangDto dto)
    {
        dto.Id = id; // Đảm bảo đúng Id
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