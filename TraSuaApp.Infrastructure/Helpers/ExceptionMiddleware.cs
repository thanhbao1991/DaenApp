using System.Net;
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
            var handledEx = DbExceptionHelper.Handle(ex);
            var response = new { Message = handledEx.Message };

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}