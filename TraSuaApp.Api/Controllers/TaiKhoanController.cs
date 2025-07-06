using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaiKhoanController : ControllerBase
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
        return result.ThanhCong
            ? Ok(new { result.Message })
            : BadRequest(new { result.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaiKhoanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.ThanhCong
            ? Ok(new { result.Message })
            : BadRequest(new { result.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result.ThanhCong
            ? Ok(new { result.Message })
            : BadRequest(new { result.Message });
    }
}