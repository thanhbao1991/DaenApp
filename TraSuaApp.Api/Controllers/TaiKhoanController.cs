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
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound(new { Message = "Không tìm thấy tài khoản." });

        return Ok(result);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaiKhoanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result.ThanhCong)
            return BadRequest(new { result.Message });

        return Ok(new { result.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaiKhoanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (!result.ThanhCong)
            return BadRequest(new { result.Message });

        return Ok(new { result.Message });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.ThanhCong)
            return BadRequest(new { result.Message });

        return Ok(new { result.Message });
    }

}