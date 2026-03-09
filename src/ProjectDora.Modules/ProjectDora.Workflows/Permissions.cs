using OrchardCore.Security.Permissions;

namespace ProjectDora.Workflows;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageWorkflows = new(
        "Workflow.Manage",
        "Create, update, delete, and enable/disable workflow definitions",
        isSecurityCritical: true);

    public static readonly Permission ExecuteWorkflows = new(
        "Workflow.Execute",
        "Manually trigger workflow executions");

    public static readonly Permission ViewWorkflows = new(
        "Workflow.View",
        "View workflow definitions and execution history");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageWorkflows,
        ExecuteWorkflows,
        ViewWorkflows,
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
