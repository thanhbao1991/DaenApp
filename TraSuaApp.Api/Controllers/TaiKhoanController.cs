using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Controllers;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class TaiKhoanController : BaseApiController
{
    private readonly ITaiKhoanService _service;

    public TaiKhoanController(ITaiKhoanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null
            ? NotFound(new { Message = "Không tìm thấy tài khoản." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaiKhoanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsSuccess
            ? Ok(new { result.Message, Id = dto.Id }) // ✅ Trả về Id để middleware lấy EntityId
            : BadRequest(new { result.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaiKhoanDto dto)
    {
        dto.Id = id; // ✅ Đảm bảo dto có Id đúng
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