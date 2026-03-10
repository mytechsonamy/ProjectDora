using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.Integration;

namespace ProjectDora.Modules.Tests.Integration;

public class IntegrationMenuTests
{
    private readonly IntegrationMenu _menu;

    public IntegrationMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<IntegrationMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new IntegrationMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task Integration_Menu_AddsIntegrationTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Integration");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task Integration_Menu_HasApiClientsSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Integration");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "API Clients");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1103")]
    public async Task Integration_Menu_HasWebhooksSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Integration");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Webhooks");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public async Task Integration_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("frontend", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
