using System.Diagnostics;
using System.Text;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;

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

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // ❌ BỎ QUA nếu: GET, /api/logs, /api/auth, /hub/entity
        if (HttpMethods.IsGet(request.Method)
            || context.Request.Path.StartsWithSegments("/api/logs", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/hub/entity", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        string requestBody = "";
        try
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            using var reqReader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reqReader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LogMiddleware] Không thể đọc request body");
        }

        var originalBody = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        string responseBody = "";
        int statusCode = 200;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var resReader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
            responseBody = await resReader.ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBody);
        }
        catch (Exception ex)
        {
            statusCode = 500;
            _logger.LogError(ex, "[LogMiddleware] Lỗi pipeline");
            throw;
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                var msg =
                    $"🟟 **API Log**\n" +
                    $"`[{request.Method}] {request.Path} -> {statusCode}`\n" +
                    $"IP: {context.Connection.RemoteIpAddress}\n" +
                    $"⏱ {stopwatch.ElapsedMilliseconds} ms\n" +
                    $"**Request**:\n```json\n{requestBody}\n```\n" +
                    $"**Response**:\n```json\n{responseBody}\n```";

                await DiscordService.SendAsync(DiscordEventType.Admin, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LogMiddleware] Gửi log Discord thất bại");
            }
        }
    }
}