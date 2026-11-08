using core_service.Application.Interfaces;
using core_service.Application.ViewModels;
using core_service.Presentation.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace core_service.Presentation.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("signin-google")]
    public IActionResult SignInGoogle()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded) return BadRequest("Google authentication failed.");

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var fullName = result.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email)) return BadRequest("Email not provided by Google.");

        var authResult = await _authService.HandleGoogleLoginAsync(email, fullName ?? "User");

        if (authResult.Value.IsRegistered)
        {
            return Redirect($"http://localhost:3000/auth-success?token={authResult.Value.Token}");
        }
        else
        {
            return Redirect($"http://localhost:3000/complete-registration?regToken={authResult.Value.Token}");
        }
    }
      
    [HttpPost("complete-registration")]
    public async Task<ActionResult<string>> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
    {
        var result = await _authService.CompleteRegistrationAsync(request.RegToken, request.TaxId, request.DepartmentId);

        return HandleResult(result);
    }
}
