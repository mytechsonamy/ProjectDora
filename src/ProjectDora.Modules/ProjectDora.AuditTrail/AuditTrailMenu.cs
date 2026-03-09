using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.AuditTrail;

public sealed class AuditTrailMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public AuditTrailMenu(IStringLocalizer<AuditTrailMenu> localizer)
    {
        S = localizer;
    }

    public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
    {
        if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Audit Trail"], S["Audit Trail"].PrefixPosition("8"), audit => audit
                .Permission(Permissions.ViewAuditTrail)
                .Add(S["Audit Log"], S["Audit Log"].PrefixPosition("1"), log => log
                    .Action("Index", "Admin", new { area = "OrchardCore.AuditTrail" })
                    .Permission(Permissions.ViewAuditTrail)
                    .LocalNav())
                .Add(S["Settings"], S["Settings"].PrefixPosition("2"), settings => settings
                    .Action("Index", "AuditTrailSettings", new { area = "OrchardCore.AuditTrail" })
                    .Permission(Permissions.ManageAuditSettings)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
