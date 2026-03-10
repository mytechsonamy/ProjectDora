using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.ThemeManagement;

namespace ProjectDora.Modules.Tests.ThemeManagement;

public class ThemeManagementMenuTests
{
    private readonly ThemeManagementMenu _menu;

    public ThemeManagementMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<ThemeManagementMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new ThemeManagementMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task ThemeManagement_Menu_AddsDesignTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Design");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task ThemeManagement_Menu_HasThemesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Design");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Themes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public async Task ThemeManagement_Menu_HasTemplatesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Design");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Templates");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task ThemeManagement_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("frontend", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
