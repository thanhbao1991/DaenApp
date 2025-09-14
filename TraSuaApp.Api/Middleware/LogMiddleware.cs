using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Api.Middleware
{
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

            // ❌ BỎ QUA nếu:
            //   • Phương thức GET
            //   • Bất kỳ đường dẫn nào dưới /api/logs
            //   • Bất kỳ đường dẫn nào dưới /api/auth (gồm login, logout, refresh…)
            if (HttpMethods.IsGet(request.Method)
                || context.Request.Path.StartsWithSegments("/api/logs", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/hub/entity", StringComparison.OrdinalIgnoreCase)


                )
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var logEntry = new LogDto
            {
                Id = Guid.NewGuid(),
                ThoiGian = DateTime.Now,
                Method = request.Method,
                Path = context.Request.Path,
                Ip = context.Connection.RemoteIpAddress?.ToString()
            };

            // — Ghi NGUYÊN REQUEST BODY —
            try
            {
                request.EnableBuffering();
                request.Body.Seek(0, SeekOrigin.Begin);
                using var reqReader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var reqBody = await reqReader.ReadToEndAsync();
                request.Body.Seek(0, SeekOrigin.Begin);

                logEntry.RequestBodyShort = reqBody;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đọc request body");
            }

            // — Ghi NGUYÊN RESPONSE BODY —
            var originalBody = context.Response.Body;
            await using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            string responseBody = "";
            try
            {
                await _next(context);
                logEntry.StatusCode = context.Response.StatusCode;

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                using var resReader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
                responseBody = await resReader.ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBody);

                logEntry.ResponseBodyShort = responseBody;

                // Gán EntityId tự động
                if (HttpMethods.IsPut(request.Method) || HttpMethods.IsDelete(request.Method))
                {
                    var lastSeg = context.Request.Path.Value?.Split('/').LastOrDefault();
                    if (Guid.TryParse(lastSeg, out var idFromPath))
                        logEntry.EntityId = idFromPath;
                }
                else if (HttpMethods.IsPost(request.Method))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        if (doc.RootElement.TryGetProperty("entityId", out var idProp)
                            && Guid.TryParse(idProp.ToString(), out var idFromResp))
                        {
                            logEntry.EntityId = idFromResp;
                        }
                    }
                    catch
                    {
                        // bỏ qua nếu không parse được
                    }
                }
            }
            catch
            {
                logEntry.StatusCode = 500;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logEntry.DurationMs = stopwatch.ElapsedMilliseconds;

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    logEntry.UserName = context.User.Identity.Name;
                    logEntry.UserId = context.User.Claims
                                          .FirstOrDefault(c => c.Type == "Id")
                                          ?.Value;
                }

                try
                {
                    await logService.CreateAsync(logEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LogMiddleware] Ghi log thất bại");
                }
            }
        }
    }
}
