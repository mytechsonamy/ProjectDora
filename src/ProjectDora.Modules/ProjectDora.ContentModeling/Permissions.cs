using OrchardCore.Security.Permissions;

namespace ProjectDora.ContentModeling;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageContentTypes = new(
        "ContentModeling.Manage",
        "Manage content type definitions",
        isSecurityCritical: true);

    public static readonly Permission CreateContentTypes = new(
        "ContentModeling.Create",
        "Create new content type definitions");

    public static readonly Permission EditContentTypes = new(
        "ContentModeling.Edit",
        "Edit existing content type definitions");

    public static readonly Permission DeleteContentTypes = new(
        "ContentModeling.Delete",
        "Delete content type definitions",
        isSecurityCritical: true);

    public static readonly Permission ViewContentTypes = new(
        "ContentModeling.View",
        "View content type definitions");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageContentTypes,
        CreateContentTypes,
        EditContentTypes,
        DeleteContentTypes,
        ViewContentTypes,
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
                Permissions = new[] { ViewContentTypes },
            },
            new PermissionStereotype
            {
                Name = "Author",
                Permissions = new[] { ViewContentTypes },
            },
        };
    }
}
