using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.UserManagement;

namespace ProjectDora.Modules.Tests.UserManagement;

public class UserManagementMenuTests
{
    private readonly UserManagementMenu _menu;

    public UserManagementMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<UserManagementMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new UserManagementMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task UserManagement_Menu_AddsUsersAndRolesTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Users & Roles");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task UserManagement_Menu_HasUsersSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Users & Roles");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Users");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-602")]
    public async Task UserManagement_Menu_HasRolesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Users & Roles");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Roles");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task UserManagement_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("main", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
