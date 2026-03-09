using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace ProjectDora.UserManagement;

public sealed class UserManagementMenu : INavigationProvider
{
    private readonly IStringLocalizer S;

    public UserManagementMenu(IStringLocalizer<UserManagementMenu> localizer)
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
            .Add(S["Users & Roles"], S["Users & Roles"].PrefixPosition("4"), ur => ur
                .Permission(Permissions.ViewUsers)
                .Add(S["Users"], S["Users"].PrefixPosition("1"), u => u
                    .Action("Index", "Admin", new { area = "OrchardCore.Users" })
                    .Permission(Permissions.ManageUsers)
                    .LocalNav())
                .Add(S["Roles"], S["Roles"].PrefixPosition("2"), r => r
                    .Action("Index", "Admin", new { area = "OrchardCore.Roles" })
                    .Permission(Permissions.ManageRoles)
                    .LocalNav()));

        return ValueTask.CompletedTask;
    }
}
