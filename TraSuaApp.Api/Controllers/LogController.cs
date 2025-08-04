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
        return Result<List<LogDto>>.Success("Danh sách Log", list).WithAfter(list);
    }

    [HttpGet("by-date")]
    public async Task<ActionResult<Result<List<LogDto>>>> GetByDate([FromQuery] DateTime ngay)
    {
        var list = await _service.GetByDateAsync(ngay.Date);
        return Result<List<LogDto>>.Success($"Log ngày {ngay:dd/MM/yyyy}", list).WithAfter(list);
    }

    [HttpGet("by-entity")]
    public async Task<ActionResult<Result<List<LogDto>>>> GetByEntity([FromQuery] Guid entityId)
    {
        var list = await _service.GetByEntityIdAsync(entityId);
        return Result<List<LogDto>>.Success($"Log của entity {entityId}", list).WithAfter(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<LogDto>>> GetById(Guid id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null
            ? Result<LogDto>.Failure("Không tìm thấy Log.")
            : Result<LogDto>.Success("Chi tiết Log", dto).WithId(id).WithAfter(dto);
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
        await Task.CompletedTask;
        // Không cho xoá log
        var response = new
        {
            status = 403,
            success = false,
            message = "Tính năng này bị khoá."
        };
        return StatusCode(StatusCodes.Status403Forbidden, response);
    }
}
