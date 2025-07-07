using System.Diagnostics;
using System.Text;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;

public class LogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LogMiddleware> _logger;

    public LogMiddleware(RequestDelegate next, ILogger<LogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILogService logService)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        var logEntry = new LogEntry
        {
            Id = Guid.NewGuid(),
            ThoiGian = DateTime.Now,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            IP = context.Connection.RemoteIpAddress?.ToString()
        };

        // Đọc RequestBody nếu có
        if (request.Method != HttpMethods.Get && request.ContentLength > 0 && request.Body.CanRead)
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            logEntry.RequestBody = await reader.ReadToEndAsync();

            request.Body.Position = 0;
        }

        // Ghi đè Response.Body để capture lại dữ liệu trả về
        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context); // tiếp tục pipeline

            logEntry.StatusCode = context.Response.StatusCode;

            responseBody.Seek(0, SeekOrigin.Begin);
            logEntry.ResponseBody = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            logEntry.StatusCode = 500;
            logEntry.ExceptionMessage = ex.ToString();
            throw;
        }
        finally
        {
            stopwatch.Stop();
            logEntry.DurationMs = stopwatch.ElapsedMilliseconds;

            // Lấy thông tin người dùng
            if (context.User.Identity?.IsAuthenticated == true)
            {
                logEntry.UserName = context.User.Identity.Name;
                logEntry.UserId = context.User.Claims
                    .FirstOrDefault(x => x.Type == "sub" || x.Type.Contains("nameidentifier"))?.Value;
            }

            await logService.LogAsync(logEntry);
        }
    }
}