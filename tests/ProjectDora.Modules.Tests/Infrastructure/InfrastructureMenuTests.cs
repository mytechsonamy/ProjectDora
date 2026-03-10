using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.Infrastructure;

namespace ProjectDora.Modules.Tests.Infrastructure;

public class InfrastructureMenuTests
{
    private readonly InfrastructureMenu _menu;

    public InfrastructureMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<InfrastructureMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new InfrastructureMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task Infrastructure_Menu_AddsInfrastructureTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Infrastructure");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task Infrastructure_Menu_HasTenantsSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Infrastructure");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Tenants");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public async Task Infrastructure_Menu_HasRecipesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Infrastructure");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Recipes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task Infrastructure_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("frontend", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
