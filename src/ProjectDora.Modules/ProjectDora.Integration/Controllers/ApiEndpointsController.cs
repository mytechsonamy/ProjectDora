using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;

namespace ProjectDora.Integration.Controllers;

/// <summary>
/// Admin controller for API client and published query endpoint management.
/// Authentication is enforced by [Authorize] + [Admin].
/// Authorization (permission-level) is enforced per-action via IAuthorizationService,
/// which evaluates OC's PermissionRequirement against the user's role claims.
/// [Authorize] alone only checks authentication — it does NOT check OC module permissions.
/// </summary>
[Admin]
[Authorize]
public class ApiEndpointsController : Controller
{
    private readonly IAuthorizationService _authorizationService;

    public ApiEndpointsController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewApiClients))
        {
            return Forbid();
        }

        return View();
    }
}
