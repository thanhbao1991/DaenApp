using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Extensions;
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
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLogDetail(Guid id)
    {
        var result = await _logService.GetLogByIdAsync(id);
        return result.ToActionResult();
    }
}