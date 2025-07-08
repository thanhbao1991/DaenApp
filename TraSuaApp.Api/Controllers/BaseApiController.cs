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

    protected IActionResult FromResult(Result result, Guid? id = null)
    {
        if (result.IsSuccess)
            return Ok(id.HasValue
                ? new { result.Message, Id = id.Value }
                : new { result.Message });

        return BadRequest(new { result.Message });
    }

    protected IActionResult Result(bool success, string message, Guid? id = null)
    {
        if (success)
            return Ok(id.HasValue
                ? new { Message = message, Id = id.Value }
                : new { Message = message });

        return BadRequest(new { Message = message });
    }
}