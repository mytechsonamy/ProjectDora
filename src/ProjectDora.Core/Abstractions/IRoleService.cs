namespace ProjectDora.Core.Abstractions;

/// <summary>
/// Role and permission management — wraps Orchard Core's role management.
/// </summary>
public interface IRoleService
{
    Task<RoleDto> CreateAsync(CreateRoleCommand command);
    Task<RoleDto?> GetAsync(string roleName);
    Task<IReadOnlyList<RoleDto>> ListAsync();
    Task<RoleDto> UpdateAsync(string roleName, UpdateRoleCommand command);
    Task DeleteAsync(string roleName);
    Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames);
    Task RevokeRolesFromUserAsync(string userId, IEnumerable<string> roleNames);
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync();
    Task GenerateContentTypePermissionsAsync(string contentTypeName);
    Task GenerateQueryPermissionAsync(string queryName);
}

public record RoleDto(
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions);

public record CreateRoleCommand(
    string Name,
    string? Description = null,
    IReadOnlyList<string>? Permissions = null);

public record UpdateRoleCommand(
    string? Description = null,
    IReadOnlyList<string>? Permissions = null);

public record PermissionDto(
    string Name,
    string Description,
    string Category,
    bool IsSecurityCritical);

public record ContentTypePermissionDto(
    string ContentTypeName,
    IReadOnlyList<string> Actions);
