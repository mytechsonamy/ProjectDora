using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.Localization;

public sealed class LocalizationMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public LocalizationMenu(IStringLocalizer<LocalizationMenu> localizer)
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
            .Add(S["Localization"], S["Localization"].PrefixPosition("7"), loc => loc
                .Permission(Permissions.ViewTranslations)
                .Add(S["Cultures"], S["Cultures"].PrefixPosition("1"), cultures => cultures
                    .Action("Index", "Admin", new { area = "OrchardCore.Localization" })
                    .Permission(Permissions.ManageCultures)
                    .LocalNav())
                .Add(S["Translations"], S["Translations"].PrefixPosition("2"), trans => trans
                    .Action("Index", "Admin", new { area = "OrchardCore.ContentLocalization" })
                    .Permission(Permissions.ViewTranslations)
                    .LocalNav())
                .Add(S["PO Files"], S["PO Files"].PrefixPosition("3"), po => po
                    .Action("Index", "Admin", new { area = "OrchardCore.Localization" })
                    .Permission(Permissions.ManagePOFiles)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
