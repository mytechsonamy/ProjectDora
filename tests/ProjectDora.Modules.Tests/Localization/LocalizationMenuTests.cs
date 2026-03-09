using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.Localization;

namespace ProjectDora.Modules.Tests.Localization;

public class LocalizationMenuTests
{
    private readonly LocalizationMenu _menu;

    public LocalizationMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<LocalizationMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new LocalizationMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public async Task Localization_Menu_AddsLocalizationTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Localization");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public async Task Localization_Menu_HasCulturesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Localization");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Cultures");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-805")]
    public async Task Localization_Menu_HasPOFilesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Localization");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "PO Files");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public async Task Localization_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("frontend", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
