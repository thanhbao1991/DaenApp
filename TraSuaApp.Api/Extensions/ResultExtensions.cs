using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
            {
                return new OkObjectResult(new
                {
                    result.Message,
                    result.EntityId,
                    Data = result.Data,
                    Before = result.BeforeData,
                    After = result.AfterData
                });
            }

            return new BadRequestObjectResult(new { result.Message });
        }
    }
}