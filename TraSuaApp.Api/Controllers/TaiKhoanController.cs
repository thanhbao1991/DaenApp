using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Controllers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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
    {
        var data = await _service.GetAllAsync();
        return FromResult(Result.Success().WithAfter(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? FromResult(Result.Failure("Không tìm thấy tài khoản."))
            : FromResult(Result.Success().WithId(id).WithAfter(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaiKhoanDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaiKhoanDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return FromResult(result);
    }
}