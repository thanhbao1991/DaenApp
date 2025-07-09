using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Api.Middleware;

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
        var request = context.Request;

        // ❌ Bỏ qua log nếu là GET hoặc truy cập /api/logs hoặc /api/auth
        if (request.Method == HttpMethods.Get ||
            request.Path.StartsWithSegments("/api/logs", StringComparison.OrdinalIgnoreCase) ||
            request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        var logEntry = new Log
        {
            Id = Guid.NewGuid(),
            ThoiGian = DateTime.Now,
            Method = request.Method,
            Path = request.Path,
            Ip = context.Connection.RemoteIpAddress?.ToString()
        };

        // Ghi request body ngắn gọn
        try
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);

            logEntry.RequestBodyShort = requestBody.Length > 1000
                ? requestBody.Substring(0, 1000) + "... [truncated]"
                : requestBody;

            _logger.LogInformation($"[LogMiddleware] {request.Method} {request.Path} Body: {logEntry.RequestBodyShort}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc request body");
        }

        // Ghi response body
        var originalBody = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        string responseBody = "";

        try
        {
            await _next(context);
            logEntry.StatusCode = context.Response.StatusCode;

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBody);

            logEntry.ResponseBodyShort = responseBody.Length > 1000
                ? responseBody.Substring(0, 1000) + "... [truncated]"
                : responseBody;

            // ✅ Gán EntityId theo yêu cầu:
            if (request.Method == HttpMethods.Put || request.Method == HttpMethods.Delete)
            {
                // Lấy từ URL
                var lastSegment = request.Path.Value?.Split('/').LastOrDefault();
                if (Guid.TryParse(lastSegment, out var idFromPath))
                {
                    logEntry.EntityId = idFromPath;
                }
            }
            else if (request.Method == HttpMethods.Post)
            {
                // Lấy từ response body: entityId
                try
                {
                    var json = JsonDocument.Parse(responseBody);
                    if (json.RootElement.TryGetProperty("entityId", out var idProp) &&
                        Guid.TryParse(idProp.ToString(), out var idFromResponse))
                    {
                        logEntry.EntityId = idFromResponse;
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            logEntry.StatusCode = 500;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            logEntry.DurationMs = stopwatch.ElapsedMilliseconds;

            // Ghi thông tin người dùng nếu có
            if (context.User.Identity?.IsAuthenticated == true)
            {
                logEntry.UserName = context.User.Identity.Name;
                logEntry.UserId = context.User.Claims
    .FirstOrDefault(c =>
    c.Type == "Id")
    ?.Value;
            }

            try
            {
                await logService.LogAsync(logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogMiddleware] Ghi log vào DB thất bại");
            }
        }
    }
}