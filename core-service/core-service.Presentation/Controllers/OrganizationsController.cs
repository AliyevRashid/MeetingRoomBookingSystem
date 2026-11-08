using core_service.Application.Interfaces;
using core_service.Application.Models;
using core_service.Application.Users.Queries;
using core_service.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace core_service.Presentation.Controllers;

[Route("api/organizations")]
[ApiController]
public class OrganizationsController : ApiControllerBase
{
    private readonly IAuthService _authService;
    public OrganizationsController(IAuthService authService)
    {
        _authService = authService;
    }

    [Authorize(Policy = "RegisterOnly")]
    [HttpGet("{taxId}/departments")]
    public async Task<ActionResult<IEnumerable<DepartmentVM>>> GetDepartmentsByTaxIdAsync(string taxId)
    {
        var result = await _authService.GetDepartmentsByTaxIdAsync(taxId, CancellationToken.None);
       return HandleResult(result);
    }

    [Authorize(Policy = "AccessOnly")]
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserVM>>> FilterUsersByNameAsync([FromQuery] FilterUsersByNameQuery query, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
