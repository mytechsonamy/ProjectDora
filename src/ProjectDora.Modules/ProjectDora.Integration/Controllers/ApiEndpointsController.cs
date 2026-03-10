using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Admin;

namespace ProjectDora.Integration.Controllers;

[Admin]
[Authorize]
public class ApiEndpointsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
