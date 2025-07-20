using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;
        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
            => _logger = logger;

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception in API");

            // Xử lý DbUpdateException do trùng key (SQL Server 2601/2627)
            if (context.Exception is DbUpdateException dbEx
                && dbEx.InnerException is SqlException sqlEx
                && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // Ví dụ: "Tên topping 'XXX' đã tồn tại."
                var msg = "Đối tượng đã tồn tại.";
                // Nếu bạn muốn tùy biến theo entity:
                // var entity = context.HttpContext.Request.Path.Value?.Split('/')[2];
                // msg = $"Tên {entity} đã tồn tại.";

                var result = Result<object>.Failure(msg);
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