using System.Diagnostics;
using System.Text;
using System.Text.Json;
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

        var logEntry = new Log
        {
            Id = Guid.NewGuid(),
            ThoiGian = DateTime.Now,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            IP = context.Connection.RemoteIpAddress?.ToString()
        };

        // Đọc RequestBody nếu hợp lệ
        if (request.Method != HttpMethods.Get &&
            request.ContentLength > 0 &&
            request.Body.CanRead &&
            request.Body.CanSeek)
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            logEntry.RequestBody = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);
        }

        // Ghi đè response body
        var originalBody = context.Response.Body;
        await using var newResponseBody = new MemoryStream();
        context.Response.Body = newResponseBody;

        try
        {
            await _next(context); // tiếp tục pipeline

            logEntry.StatusCode = context.Response.StatusCode;

            newResponseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(newResponseBody).ReadToEndAsync();

            // Tóm tắt nếu là mảng JSON
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(responseText);
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    logEntry.ResponseBody = $"[{jsonElement.GetArrayLength()} dòng]";
                }
                else
                {
                    logEntry.ResponseBody = responseText;
                }
            }
            catch
            {
                logEntry.ResponseBody = responseText;
            }

            newResponseBody.Seek(0, SeekOrigin.Begin);
            await newResponseBody.CopyToAsync(originalBody);
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

            // Ghi nhận thông tin user nếu có
            if (context.User.Identity?.IsAuthenticated == true)
            {
                logEntry.UserName = context.User.Identity.Name;
                logEntry.UserId = context.User.Claims
                    .FirstOrDefault(c => c.Type == "sub" || c.Type.EndsWith("nameidentifier"))?.Value;
            }

            await logService.LogAsync(logEntry);
        }
    }
}