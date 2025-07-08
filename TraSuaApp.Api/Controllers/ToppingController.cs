using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Controllers;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

[Authorize]
[Route("api/[controller]")]
public class ToppingController : BaseApiController
{
    private readonly IToppingService _service;

    public ToppingController(IToppingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllAsync();
        return FromResult(Result<List<ToppingDto>>.Success("Danh sách topping", data).WithAfter(data));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? FromResult(Result<ToppingDto>.Failure("Không tìm thấy topping."))
            : FromResult(Result<ToppingDto>.Success("Chi tiết topping", dto).WithId(id).WithAfter(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ToppingDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return FromResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ToppingDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return FromResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return FromResult(result);
    }
}