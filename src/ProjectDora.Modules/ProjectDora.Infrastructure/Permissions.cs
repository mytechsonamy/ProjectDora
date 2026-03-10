using OrchardCore.Security.Permissions;

namespace ProjectDora.Infrastructure;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageTenants = new(
        "Infrastructure.ManageTenants",
        "Create, suspend, and delete tenants",
        isSecurityCritical: true);

    public static readonly Permission ViewTenants = new(
        "Infrastructure.ViewTenants",
        "View tenant list and tenant details");

    public static readonly Permission ManageCache = new(
        "Infrastructure.ManageCache",
        "View cache statistics and configure TTL settings");

    public static readonly Permission PurgeCache = new(
        "Infrastructure.PurgeCache",
        "Flush all cache entries",
        isSecurityCritical: true);

    public static readonly Permission ImportRecipe = new(
        "Infrastructure.ImportRecipe",
        "Upload and execute recipe files to configure tenant");

    public static readonly Permission ExportRecipe = new(
        "Infrastructure.ExportRecipe",
        "Export current tenant configuration as a recipe file");

    public static readonly Permission ManageOpenId = new(
        "Infrastructure.ManageOpenId",
        "Configure OIDC clients, scopes, and token settings",
        isSecurityCritical: true);

    public static readonly Permission ManageSitemap = new(
        "Infrastructure.ManageSitemap",
        "Configure and regenerate XML sitemap rules");

    public static readonly Permission ViewSitemap = new(
        "Infrastructure.ViewSitemap",
        "View sitemap configuration and status");

    public static readonly Permission ManageSearchEngine = new(
        "Infrastructure.ManageSearchEngine",
        "Switch search engine and manage index settings");

    public static readonly Permission ManageSettings = new(
        "Infrastructure.ManageSettings",
        "Modify tenant-level platform settings",
        isSecurityCritical: true);

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageTenants,
        ViewTenants,
        ManageCache,
        PurgeCache,
        ImportRecipe,
        ExportRecipe,
        ManageOpenId,
        ManageSitemap,
        ViewSitemap,
        ManageSearchEngine,
        ManageSettings,
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
