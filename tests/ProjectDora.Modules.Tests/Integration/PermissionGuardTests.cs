using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProjectDora.Integration.Controllers;

namespace ProjectDora.Modules.Tests.Integration;

/// <summary>
/// Verifies that Integration controllers enforce OC module permissions via IAuthorizationService,
/// not just authentication ([Authorize] alone only checks identity, not OC permission claims).
///
/// Each test configures the IAuthorizationService mock to return either success or failure,
/// then asserts the controller action returns the correct IActionResult type.
/// This confirms that the permission check actually gates access — it is not cosmetic.
/// </summary>
public class PermissionGuardTests
{
    // ── WebhooksController ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task WebhooksController_Index_WithoutPermission_ReturnsForbid()
    {
        var authMock = BuildAuthMock(authorized: false);
        var controller = BuildController(new WebhooksController(authMock.Object));

        var result = await controller.Index();

        result.Should().BeOfType<ForbidResult>(
            "unauthenticated users with no ViewWebhooks permission must receive 403 Forbid, not a view");
        authMock.Verify(
            a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()),
            Times.Once,
            "authorization must be checked exactly once per request");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task WebhooksController_Index_WithPermission_ReturnsView()
    {
        var authMock = BuildAuthMock(authorized: true);
        var controller = BuildController(new WebhooksController(authMock.Object));

        var result = await controller.Index();

        result.Should().BeOfType<ViewResult>(
            "a user with ViewWebhooks permission must reach the view");
    }

    // ── ApiEndpointsController ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task ApiEndpointsController_Index_WithoutPermission_ReturnsForbid()
    {
        var authMock = BuildAuthMock(authorized: false);
        var controller = BuildController(new ApiEndpointsController(authMock.Object));

        var result = await controller.Index();

        result.Should().BeOfType<ForbidResult>(
            "users without ViewApiClients permission must receive 403 Forbid");
        authMock.Verify(
            a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task ApiEndpointsController_Index_WithPermission_ReturnsView()
    {
        var authMock = BuildAuthMock(authorized: true);
        var controller = BuildController(new ApiEndpointsController(authMock.Object));

        var result = await controller.Index();

        result.Should().BeOfType<ViewResult>(
            "a user with ViewApiClients permission must reach the view");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Mock<IAuthorizationService> BuildAuthMock(bool authorized)
    {
        var mock = new Mock<IAuthorizationService>();
        // Set up the base interface overload that OC's extension method delegates to:
        //   AuthorizeAsync(ClaimsPrincipal, object?, IEnumerable<IAuthorizationRequirement>)
        // OC's extension `AuthorizeAsync(user, Permission)` resolves to this via PermissionRequirement.
        mock.Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(authorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return mock;
    }

    private static T BuildController<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
            },
        };
        return controller;
    }
}
