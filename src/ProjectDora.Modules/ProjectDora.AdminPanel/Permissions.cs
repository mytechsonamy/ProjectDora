using OrchardCore.Security.Permissions;

namespace ProjectDora.AdminPanel;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission AccessAdminPanel = new(
        "AccessAdminPanel",
        "Access the administration panel",
        isSecurityCritical: true);

    public static readonly Permission ManageAdminMenus = new(
        "AdminPanel.ManageMenus",
        "Create, edit, and delete custom admin menus");

    public static readonly Permission ManageMedia = new(
        "Media.Upload",
        "Upload files to media library");

    public static readonly Permission DeleteMedia = new(
        "Media.Delete",
        "Delete files from media library");

    public static readonly Permission ManageMediaFolders = new(
        "Media.ManageFolders",
        "Create, rename, and delete media folders");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        AccessAdminPanel,
        ManageAdminMenus,
        ManageMedia,
        DeleteMedia,
        ManageMediaFolders,
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
            new PermissionStereotype
            {
                Name = "Editor",
                Permissions = new[] { AccessAdminPanel, ManageMedia },
            },
            new PermissionStereotype
            {
                Name = "Author",
                Permissions = new[] { AccessAdminPanel, ManageMedia },
            },
        };
    }
}
