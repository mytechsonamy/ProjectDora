using OrchardCore.Security.Permissions;

namespace ProjectDora.AuditTrail;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ViewAuditTrail = new(
        "AuditTrail.View",
        "View audit log entries for accessible content types");

    public static readonly Permission ViewAllAuditTrail = new(
        "AuditTrail.ViewAll",
        "View all audit log entries regardless of content type");

    public static readonly Permission ManageAuditSettings = new(
        "AuditTrail.ManageSettings",
        "Configure audit retention policies and content type filters",
        isSecurityCritical: true);

    public static readonly Permission ViewDiff = new(
        "AuditTrail.ViewDiff",
        "View versioned diffs between content versions");

    public static readonly Permission Rollback = new(
        "AuditTrail.Rollback",
        "Restore content items to previous versions",
        isSecurityCritical: true);

    public static readonly Permission Export = new(
        "AuditTrail.Export",
        "Export audit log entries as CSV or JSON");

    public static readonly Permission Purge = new(
        "AuditTrail.Purge",
        "Manually purge audit records (destructive)",
        isSecurityCritical: true);

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ViewAuditTrail,
        ViewAllAuditTrail,
        ManageAuditSettings,
        ViewDiff,
        Rollback,
        Export,
        Purge,
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
