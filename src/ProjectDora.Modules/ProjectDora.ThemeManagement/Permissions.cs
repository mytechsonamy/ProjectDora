using OrchardCore.Security.Permissions;

namespace ProjectDora.ThemeManagement;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageThemes = new(
        "Theme.Manage",
        "Activate themes and manage theme settings",
        isSecurityCritical: true);

    public static readonly Permission EditTemplates = new(
        "Theme.EditTemplates",
        "Edit Liquid template files in the Monaco editor");

    public static readonly Permission ViewThemes = new(
        "Theme.View",
        "View available themes and active theme details");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageThemes,
        EditTemplates,
        ViewThemes,
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
