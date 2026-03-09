using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.AdminPanel;

public sealed class AdminMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public AdminMenu(IStringLocalizer<AdminMenu> localizer)
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
            .Add(S["Content"], content => content
                .Permission(Permissions.AccessAdminPanel)
                .Add(S["Content Items"], S["Content Items"].PrefixPosition("1"), ci => ci
                    .Action("List", "Admin", new { area = "OrchardCore.Contents" })
                    .Permission(Permissions.AccessAdminPanel)
                    .LocalNav())
                .Add(S["Content Types"], S["Content Types"].PrefixPosition("2"), ct => ct
                    .Action("List", "Admin", new { area = "OrchardCore.ContentTypes" })
                    .Permission(Permissions.AccessAdminPanel)
                    .LocalNav()))
            .Add(S["Media"], S["Media"].PrefixPosition("5"), media => media
                .Action("Index", "Admin", new { area = "OrchardCore.Media" })
                .Permission(Permissions.ManageMedia)
                .LocalNav());

        return ValueTask.CompletedTask;
    }
}
