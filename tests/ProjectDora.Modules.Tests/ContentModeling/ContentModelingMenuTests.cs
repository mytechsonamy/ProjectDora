using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.ContentModeling;

namespace ProjectDora.Modules.Tests.ContentModeling;

public class ContentModelingMenuTests
{
    private readonly ContentModelingMenu _sut;

    public ContentModelingMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<ContentModelingMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _sut = new ContentModelingMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public async Task ContentModeling_Navigation_BuildNavigation_SkipsNonAdminMenu()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("main", builder);
        var items = builder.Build();

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public async Task ContentModeling_Navigation_BuildNavigation_AddsContentModelingMenu()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("admin", builder);
        var items = builder.Build();

        // Assert
        items.Should().Contain(i => i.Text.Value == "Content Modeling");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public async Task ContentModeling_Navigation_BuildNavigation_HasContentTypesSubMenu()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("admin", builder);
        var items = builder.Build();
        var modelingMenu = items.First(i => i.Text.Value == "Content Modeling");

        // Assert
        modelingMenu.Items.Should().Contain(i => i.Text.Value == "Content Types");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public async Task ContentModeling_Navigation_BuildNavigation_HasContentPartsSubMenu()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("admin", builder);
        var items = builder.Build();
        var modelingMenu = items.First(i => i.Text.Value == "Content Modeling");

        // Assert
        modelingMenu.Items.Should().Contain(i => i.Text.Value == "Content Parts");
    }
}
