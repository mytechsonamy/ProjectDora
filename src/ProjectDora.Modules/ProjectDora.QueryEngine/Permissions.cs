using OrchardCore.Security.Permissions;

namespace ProjectDora.QueryEngine;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageQueries = new(
        "QueryEngine.Manage",
        "Manage saved queries and search indexes",
        isSecurityCritical: true);

    public static readonly Permission CreateQueries = new(
        "QueryEngine.Create",
        "Create and edit saved queries");

    public static readonly Permission DeleteQueries = new(
        "QueryEngine.Delete",
        "Delete saved queries",
        isSecurityCritical: true);

    public static readonly Permission ExecuteQueries = new(
        "QueryEngine.Execute",
        "Execute saved queries and ad-hoc searches");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageQueries,
        CreateQueries,
        DeleteQueries,
        ExecuteQueries,
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
                Name = "Analyst",
                Permissions = new[] { ExecuteQueries },
            },
        };
    }
}
