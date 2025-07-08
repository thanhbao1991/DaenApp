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

    protected IActionResult Result(bool success, string message)
        => success ? Ok(new { Message = message }) : BadRequest(new { Message = message });

    protected IActionResult FromResult(Result result)
        => result.IsSuccess ? Ok(new { Message = result.Message }) : BadRequest(new { Message = result.Message });

    // ✅ Trả về kèm Id nếu có
    protected IActionResult Result(bool success, string message, Guid id)
        => success
            ? Ok(new { Message = message, Id = id })
            : BadRequest(new { Message = message });

    protected IActionResult FromResult(Result result, Guid id)
        => result.IsSuccess
            ? Ok(new { Message = result.Message, Id = id })
            : BadRequest(new { Message = result.Message });
}