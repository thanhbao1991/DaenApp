using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Filters
{
    public class ApiExceptionFilter : IAsyncExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;
        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
            => _logger = logger;

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception in API");

            string webhookUrl =
                 "https://discord.com/api/webhooks/1385632148387533011/MmRNpkKCoslZwNO2F9uJd_ZCjiaSvXMKeIpQlDP7gpDBwk1HZt1g2nonmEUiOVITaK0H";
            string combined = context.Exception.ToString();

            using var client = new HttpClient();

            if (combined.Length < 1900)
            {
                var payload = new { content = combined };
                var json = JsonSerializer.Serialize(payload);
                await client.PostAsync(webhookUrl,
                    new StringContent(json, Encoding.UTF8, "application/json"));
            }
            else
            {
                var contentToSend = new MultipartFormDataContent();
                var bytes = Encoding.UTF8.GetBytes(combined);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                contentToSend.Add(fileContent, "file", "Error.txt");
                await client.PostAsync(webhookUrl, contentToSend);
            }

            // Xử lý DbUpdateException do trùng key (SQL Server 2601/2627)
            if (context.Exception is DbUpdateException dbEx
                && dbEx.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                var result = Result<object>.Failure("Đối tượng đã tồn tại.");
                context.Result = new BadRequestObjectResult(result);
                context.ExceptionHandled = true;
                return;
            }

            // Các exception khác
            var failureResult = Result<object>.Failure(context.Exception.Message);
            context.Result = new ObjectResult(failureResult)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            context.ExceptionHandled = true;
        }
    }
}