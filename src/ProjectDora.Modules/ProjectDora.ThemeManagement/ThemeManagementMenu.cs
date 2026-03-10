using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.ThemeManagement;

public sealed class ThemeManagementMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public ThemeManagementMenu(IStringLocalizer<ThemeManagementMenu> localizer)
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
            .Add(S["Design"], S["Design"].PrefixPosition("5"), design => design
                .Permission(Permissions.ViewThemes)
                .Add(S["Themes"], S["Themes"].PrefixPosition("1"), themes => themes
                    .Action("Index", "Admin", new { area = "OrchardCore.Themes" })
                    .Permission(Permissions.ViewThemes)
                    .LocalNav())
                .Add(S["Templates"], S["Templates"].PrefixPosition("2"), templates => templates
                    .Action("Index", "Admin", new { area = "OrchardCore.Templates" })
                    .Permission(Permissions.EditTemplates)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
