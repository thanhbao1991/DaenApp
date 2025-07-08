using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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
        return Result.Success().WithAfter(result).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLogDetail(Guid id)
    {
        var log = await _logService.GetLogByIdAsync(id);
        return log == null
            ? Result.Failure("Không tìm thấy log.").ToActionResult()
            : Result.Success().WithId(id).WithAfter(log).ToActionResult();
    }
}