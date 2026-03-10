using OrchardCore.Security.Permissions;

namespace ProjectDora.Integration;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageApiClients = new(
        "Integration.ManageApiClients",
        "Create and delete API client credentials",
        isSecurityCritical: true);

    public static readonly Permission ViewApiClients = new(
        "Integration.ViewApiClients",
        "View registered API clients");

    public static readonly Permission ManageWebhooks = new(
        "Integration.ManageWebhooks",
        "Register, update, and delete webhooks");

    public static readonly Permission ViewWebhooks = new(
        "Integration.ViewWebhooks",
        "View registered webhooks and delivery history");

    public static readonly Permission AccessApi = new(
        "Integration.AccessApi",
        "Use the REST and GraphQL API endpoints");

    public static readonly Permission PublishQueryApi = new(
        "Integration.PublishQueryApi",
        "Publish saved queries as public API endpoints");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageApiClients,
        ViewApiClients,
        ManageWebhooks,
        ViewWebhooks,
        AccessApi,
        PublishQueryApi,
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
