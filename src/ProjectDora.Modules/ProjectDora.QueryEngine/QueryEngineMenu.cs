using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.QueryEngine;

public sealed class QueryEngineMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public QueryEngineMenu(IStringLocalizer<QueryEngineMenu> localizer)
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
            .Add(S["Search & Queries"], S["Search & Queries"].PrefixPosition("5"), queries => queries
                .Permission(Permissions.ExecuteQueries)
                .Add(S["Saved Queries"], S["Saved Queries"].PrefixPosition("1"), sq => sq
                    .Action("Index", "Admin", new { area = "OrchardCore.Queries" })
                    .Permission(Permissions.ManageQueries)
                    .LocalNav())
                .Add(S["Lucene Indexes"], S["Lucene Indexes"].PrefixPosition("2"), li => li
                    .Action("Index", "Admin", new { area = "OrchardCore.Search.Lucene" })
                    .Permission(Permissions.ManageQueries)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
