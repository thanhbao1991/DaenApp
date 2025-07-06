using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TraSuaApp.Infrastructure.Helpers;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var handledEx = DbExceptionHelper.Handle(ex); // có thể là ex hoặc inner
            var response = new { Message = handledEx.Message };

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            // ✅ Cấu hình quan trọng để đảm bảo tiếng Việt không bị escape
            context.Response.ContentType = "application/json; charset=utf-8";

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            await context.Response.WriteAsync(json);
        }
    }
}