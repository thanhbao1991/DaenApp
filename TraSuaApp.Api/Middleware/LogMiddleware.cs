using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TraSuaApp.Application.Interfaces;
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
            QueryString = request.QueryString.ToString(),
            Ip = context.Connection.RemoteIpAddress?.ToString()
        };

        // Đọc request body
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

            // ✅ Gán EntityId từ body nếu có
            try
            {
                var json = JsonDocument.Parse(logEntry.RequestBodyShort ?? "{}");
                if (json.RootElement.TryGetProperty("id", out var idProp) &&
                    Guid.TryParse(idProp.ToString(), out var entityGuid))
                {
                    logEntry.EntityId = entityGuid;
                }
            }
            catch { }

            _logger.LogInformation($"[LogMiddleware] Method: {request.Method}, Body: {logEntry.RequestBodyShort}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể đọc request body");
        }

        // Nếu chưa có EntityId từ body, thử lấy từ URL
        if (logEntry.EntityId == Guid.Empty)
        {
            var lastSegment = request.Path.Value?.Split('/').LastOrDefault();
            if (Guid.TryParse(lastSegment, out var urlId))
            {
                logEntry.EntityId = urlId;
            }
        }

        // Ghi response body nếu không phải GET
        var originalBody = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        string responseBody = "";

        try
        {
            await _next(context);
            logEntry.StatusCode = context.Response.StatusCode;

            if (request.Method != HttpMethods.Get)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                _logger.LogInformation("ResponseBody: " + responseBody);


                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBody);

                if (responseBody.StartsWith("["))
                {
                    try
                    {
                        var array = JsonDocument.Parse(responseBody).RootElement;
                        logEntry.ResponseBodyShort = $"[{array.GetArrayLength()} dòng]";
                    }
                    catch
                    {
                        logEntry.ResponseBodyShort = responseBody;
                    }
                }
                else
                {
                    logEntry.ResponseBodyShort = responseBody.Length > 1000
                        ? responseBody.Substring(0, 1000) + "... [truncated]"
                        : responseBody;
                }

                // ✅ Nếu là POST và chưa có EntityId, lấy từ response
                if (logEntry.Method == HttpMethods.Post && logEntry.EntityId == Guid.Empty)
                {
                    try
                    {
                        var json = JsonDocument.Parse(responseBody);
                        if (json.RootElement.TryGetProperty("id", out var idProp) &&
                            Guid.TryParse(idProp.ToString(), out var responseGuid))
                        {
                            logEntry.EntityId = responseGuid;
                        }
                    }
                    catch { }
                }
            }
            else
            {
                // Nếu là GET, vẫn copy body ra ngoài
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBody);
            }
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

            // Ghi thông tin người dùng nếu có
            if (context.User.Identity?.IsAuthenticated == true)
            {
                logEntry.UserName = context.User.Identity.Name;
                logEntry.UserId = context.User.Claims.FirstOrDefault(c =>
                    c.Type == "sub" || c.Type.EndsWith("nameidentifier"))?.Value;
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