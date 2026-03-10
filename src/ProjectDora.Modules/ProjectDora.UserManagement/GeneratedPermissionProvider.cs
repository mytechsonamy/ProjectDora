using OrchardCore.Entities;
using OrchardCore.Security.Permissions;
using OrchardCore.Settings;

namespace ProjectDora.UserManagement.Services;

/// <summary>
/// Bridges dynamically generated content-type and query permissions into OC's authorization pipeline.
///
/// Problem: OrchardRoleService.GenerateContentTypePermissionsAsync() persists permission names to
/// ISiteService (GeneratedPermissionsData), but OC's IAuthorizationService only evaluates permissions
/// that IPermissionProvider.GetPermissionsAsync() returns. Without this provider, calls to
/// HasPermissionAsync("View_Article") always resolve to false — the persist step is a no-op.
///
/// Fix: This provider reads GeneratedPermissionsData at authorization time and feeds the generated
/// permissions into the OC chain. GetDefaultStereotypes() is intentionally empty — role assignment
/// is done explicitly via OrchardRoleService.AssignRolesToUserAsync(), not via stereotypes.
/// </summary>
public sealed class GeneratedPermissionProvider : IPermissionProvider
{
    private readonly ISiteService _siteService;

    public GeneratedPermissionProvider(ISiteService siteService)
    {
        _siteService = siteService;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var data = site.As<GeneratedPermissionsData>();

        if (data is null || data.PermissionNames.Count == 0)
        {
            return Enumerable.Empty<Permission>();
        }

        return data.PermissionNames.Select(static name =>
            new Permission(name, name) { Category = "Generated" });
    }

    /// <summary>
    /// No default stereotypes — generated permissions are assigned to roles explicitly,
    /// not inherited through role stereotypes.
    /// </summary>
    public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        => Enumerable.Empty<PermissionStereotype>();
}
