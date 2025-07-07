using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;

[ApiController]
[Route("api/logs")]
public class LogController : ControllerBase
{
    private readonly ILogService _logService;

    public LogController(ILogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] LogFilterDto filter)
    {
        var result = await _logService.GetLogsAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLogDetail(Guid id)
    {
        var log = await _logService.GetLogByIdAsync(id);
        if (log == null) return NotFound();
        return Ok(log);
    }
}