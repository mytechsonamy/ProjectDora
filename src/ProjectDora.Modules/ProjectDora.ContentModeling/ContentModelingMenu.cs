using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.ContentModeling;

public sealed class ContentModelingMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public ContentModelingMenu(IStringLocalizer<ContentModelingMenu> localizer)
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
            .Add(S["Content Modeling"], S["Content Modeling"].PrefixPosition("2"), modeling => modeling
                .Permission(Permissions.ViewContentTypes)
                .Add(S["Content Types"], S["Content Types"].PrefixPosition("1"), ct => ct
                    .Action("List", "Admin", new { area = "OrchardCore.ContentTypes" })
                    .Permission(Permissions.ViewContentTypes)
                    .LocalNav())
                .Add(S["Content Parts"], S["Content Parts"].PrefixPosition("2"), cp => cp
                    .Action("ListParts", "Admin", new { area = "OrchardCore.ContentTypes" })
                    .Permission(Permissions.ManageContentTypes)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
