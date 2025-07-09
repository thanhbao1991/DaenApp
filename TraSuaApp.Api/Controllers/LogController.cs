using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogController : BaseApiController
{
    private readonly ILogService _service;

    public LogController(ILogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<LogDto>>>> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Result<List<LogDto>>.Success("Danh sách nhóm sản phẩm", list).WithAfter(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<LogDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result<LogDto>.Failure("Không tìm thấy nhóm sản phẩm.")
            : Result<LogDto>.Success("Chi tiết nhóm sản phẩm", dto).WithId(id).WithAfter(dto);
    }

    [HttpPost]
    public async Task<ActionResult<Result<LogDto>>> Create([FromBody] LogDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result<LogDto>>> Update(Guid id, [FromBody] LogDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<LogDto>>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result;
    }
}