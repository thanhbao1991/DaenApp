using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Api.Filters;

public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        => _logger = logger;

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception in API");

        string combined = context.Exception.ToString();

        try
        {
            await DiscordService.SendAsync(
                DiscordEventType.Admin,
                $"❌ **API Exception**\n```{combined}```"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiExceptionFilter] Gửi Discord thất bại");
        }

        // Duplicate key
        if (context.Exception is DbUpdateException dbEx
            && dbEx.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            var result = Result<object>.Failure("Đối tượng đã tồn tại.");
            context.Result = new BadRequestObjectResult(result);
            context.ExceptionHandled = true;
            return;
        }

        // Default
        var failureResult = Result<object>.Failure(context.Exception.Message);
        context.Result = new ObjectResult(failureResult)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
    }
}