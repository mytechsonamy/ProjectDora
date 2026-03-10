using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.Infrastructure;

public sealed class InfrastructureMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public InfrastructureMenu(IStringLocalizer<InfrastructureMenu> localizer)
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
            .Add(S["Infrastructure"], S["Infrastructure"].PrefixPosition("9"), infra => infra
                .Permission(Permissions.ViewTenants)
                .Add(S["Tenants"], S["Tenants"].PrefixPosition("1"), tenants => tenants
                    .Action("Index", "Admin", new { area = "OrchardCore.Tenants" })
                    .Permission(Permissions.ViewTenants)
                    .LocalNav())
                .Add(S["Recipes"], S["Recipes"].PrefixPosition("2"), recipes => recipes
                    .Action("Index", "Admin", new { area = "OrchardCore.Recipes" })
                    .Permission(Permissions.ImportRecipe)
                    .LocalNav())
                .Add(S["OpenID"], S["OpenID"].PrefixPosition("3"), oidc => oidc
                    .Action("Index", "Admin", new { area = "OrchardCore.OpenId" })
                    .Permission(Permissions.ManageOpenId)
                    .LocalNav())
                .Add(S["Sitemap"], S["Sitemap"].PrefixPosition("4"), sitemap => sitemap
                    .Action("Index", "Admin", new { area = "OrchardCore.Sitemaps" })
                    .Permission(Permissions.ManageSitemap)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
