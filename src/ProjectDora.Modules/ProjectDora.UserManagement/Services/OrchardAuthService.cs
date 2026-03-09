using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.UserManagement.Services;

public sealed class OrchardAuthService : IAuthService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrchardAuthService(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionName)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return false;
        }

        var permission = new Permission(permissionName);
        return await _authorizationService.AuthorizeAsync(user, permission);
    }

    public Task<string?> GetCurrentUserIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Task.FromResult(userId);
    }

    public Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId)
    {
        var permissions = _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToList() as IReadOnlyList<string> ?? Array.Empty<string>();

        return Task.FromResult(permissions);
    }
}
