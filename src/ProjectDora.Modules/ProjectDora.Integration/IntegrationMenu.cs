using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.Integration;

public sealed class IntegrationMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public IntegrationMenu(IStringLocalizer<IntegrationMenu> localizer)
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
            .Add(S["Integration"], S["Integration"].PrefixPosition("10"), integ => integ
                .Permission(Permissions.ViewApiClients)
                .Add(S["API Clients"], S["API Clients"].PrefixPosition("1"), clients => clients
                    .Action("Index", "Admin", new { area = "OrchardCore.OpenId" })
                    .Permission(Permissions.ViewApiClients)
                    .LocalNav())
                .Add(S["Webhooks"], S["Webhooks"].PrefixPosition("2"), webhooks => webhooks
                    .Action("Index", "Webhooks", new { area = "ProjectDora.Integration" })
                    .Permission(Permissions.ViewWebhooks)
                    .LocalNav())
                .Add(S["API Endpoints"], S["API Endpoints"].PrefixPosition("3"), endpoints => endpoints
                    .Action("Index", "ApiEndpoints", new { area = "ProjectDora.Integration" })
                    .Permission(Permissions.PublishQueryApi)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
