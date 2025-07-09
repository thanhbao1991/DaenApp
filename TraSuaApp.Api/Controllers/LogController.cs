using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Api.Extensions;
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
        // Gọi service trả về kết quả
        var resultData = await _logService.GetLogsAsync(filter);

        // Trả về kết quả gói trong Result<T>
        return Result<PagedResultDto<LogDto>>
            .Success("Danh sách log", null)
            .WithAfter(resultData)
            .ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLogDetail(Guid id)
    {
        var logData = await _logService.GetLogByIdAsync(id);

        if (logData == null)
        {

            return Result<LogDto>.Failure("Không tìm thấy log.")
                .ToActionResult();
        }

        return Result<LogDto>
            .Success("Chi tiết log", null)
            .WithId(id)
            .WithAfter(logData)
            .ToActionResult();
    }
}