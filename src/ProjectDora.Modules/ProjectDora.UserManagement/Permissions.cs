using OrchardCore.Security.Permissions;

namespace ProjectDora.UserManagement;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageUsers = new(
        "UserRolePermission.ManageUsers",
        "Create, edit, enable/disable, and delete users",
        isSecurityCritical: true);

    public static readonly Permission ManageRoles = new(
        "UserRolePermission.ManageRoles",
        "Create, edit, and delete roles and permissions",
        isSecurityCritical: true);

    public static readonly Permission AssignRoles = new(
        "UserRolePermission.AssignRoles",
        "Assign and revoke roles to/from users",
        isSecurityCritical: true);

    public static readonly Permission ViewUsers = new(
        "UserRolePermission.ViewUsers",
        "View user list and profiles");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageUsers,
        ManageRoles,
        AssignRoles,
        ViewUsers,
    };

    public Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        return Task.FromResult(_allPermissions);
    }

    public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
    {
        return new[]
        {
            new PermissionStereotype
            {
                Name = "Administrator",
                Permissions = _allPermissions,
            },
        };
    }
}
