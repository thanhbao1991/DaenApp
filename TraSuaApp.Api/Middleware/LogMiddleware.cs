using System.Diagnostics;
using System.Text;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;

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
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            var logEntry = new Log
            {
                Id = Guid.NewGuid(),
                ThoiGian = DateTime.Now,
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                Ip = context.Connection.RemoteIpAddress?.ToString()
            };

            // Ghi lại Request Body nếu không phải GET
            if (request.Method != HttpMethods.Get)
            {
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

                    _logger.LogInformation($"[LogMiddleware] Method: {request.Method}, Body: {logEntry.RequestBodyShort}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể đọc request body");
                }
            }

            // Ghi lại response body
            var originalBody = context.Response.Body;
            await using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context); // Chạy tiếp pipeline

                logEntry.StatusCode = context.Response.StatusCode;

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBody); // Ghi ra lại cho client

                if (responseBody.StartsWith("["))
                {
                    try
                    {
                        var array = System.Text.Json.JsonDocument.Parse(responseBody).RootElement;
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
}