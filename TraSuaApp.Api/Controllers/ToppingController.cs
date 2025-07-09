using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Controllers;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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
    public async Task<ActionResult<Result<List<ToppingDto>>>> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Result<List<ToppingDto>>.Success("Danh sách topping", data).WithAfter(data);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ToppingDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result<ToppingDto>.Failure("Không tìm thấy topping.")
            : Result<ToppingDto>.Success("Chi tiết topping", dto).WithId(id).WithAfter(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<ToppingDto>>> Create([FromBody] ToppingDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ToppingDto>>> Update(Guid id, [FromBody] ToppingDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ToppingDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result;
    }
}