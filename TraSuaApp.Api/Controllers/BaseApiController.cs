using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid UserId =>
        Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : Guid.Empty;

    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new
            {
                result.Message,
                result.EntityId,
                Data = result.Data,
                Before = result.BeforeData,
                After = result.AfterData
            });
        }

        return BadRequest(new { result.Message });
    }
}