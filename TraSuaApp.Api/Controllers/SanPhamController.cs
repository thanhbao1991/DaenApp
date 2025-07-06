using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class SanPhamController : ControllerBase
{
    private readonly ISanPhamService _service;

    public SanPhamController(ISanPhamService service)
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
            ? NotFound(new { Message = "Không tìm thấy sản phẩm." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SanPhamDto dto)
        => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SanPhamDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success
            ? Ok(new { Message = "Xóa thành công." })
            : NotFound(new { Message = "Không tìm thấy sản phẩm." });
    }
}