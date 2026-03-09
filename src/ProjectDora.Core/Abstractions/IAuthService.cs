namespace ProjectDora.Core.Abstractions;

/// <summary>
/// Authentication and authorization checks.
/// </summary>
public interface IAuthService
{
    Task<bool> HasPermissionAsync(string userId, string permissionName);
    Task<string?> GetCurrentUserIdAsync();
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId);
}
