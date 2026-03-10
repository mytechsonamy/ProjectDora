using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.Workflows;

public sealed class WorkflowMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public WorkflowMenu(IStringLocalizer<WorkflowMenu> localizer)
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
            .Add(S["Workflows"], S["Workflows"].PrefixPosition("6"), wf => wf
                .Permission(Permissions.ViewWorkflows)
                .Add(S["Workflow Types"], S["Workflow Types"].PrefixPosition("1"), all => all
                    .Action("Index", "WorkflowType", new { area = "OrchardCore.Workflows" })
                    .Permission(Permissions.ViewWorkflows)
                    .LocalNav())
                .Add(S["Execution History"], S["Execution History"].PrefixPosition("2"), hist => hist
                    .Action("Index", "Workflow", new { area = "OrchardCore.Workflows" })
                    .Permission(Permissions.ViewWorkflows)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
