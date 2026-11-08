using common_library;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace core_service.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.ErrorCode == ErrorCodes.Unauthorized)
        {
            return Unauthorized();
        }

        return BadRequest(result.ErrorCode);
    }

    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.ErrorCode == ErrorCodes.Unauthorized)
        {
            return Unauthorized();
        }

        return BadRequest(result.ErrorCode);
    }
}
