using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.AdminPanel;

namespace ProjectDora.Modules.Tests.AdminPanel;

public class AdminMenuTests
{
    private readonly AdminMenu _sut;

    public AdminMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<AdminMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _sut = new AdminMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-102")]
    public async Task AdminPanel_Navigation_BuildNavigation_SkipsNonAdminMenu()
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
    [Trait("StoryId", "US-102")]
    public async Task AdminPanel_Navigation_BuildNavigation_AddsContentMenuForAdmin()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("admin", builder);
        var items = builder.Build();

        // Assert
        items.Should().Contain(i => i.Text.Value == "Content");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-103")]
    public async Task AdminPanel_Navigation_BuildNavigation_AddsMediaMenu()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("admin", builder);
        var items = builder.Build();

        // Assert
        items.Should().Contain(i => i.Text.Value == "Media");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-102")]
    public async Task AdminPanel_Navigation_BuildNavigation_ContentHasSubMenuItems()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("admin", builder);
        var items = builder.Build();
        var contentMenu = items.First(i => i.Text.Value == "Content");

        // Assert
        contentMenu.Items.Should().Contain(i => i.Text.Value == "Content Items");
        contentMenu.Items.Should().Contain(i => i.Text.Value == "Content Types");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-102")]
    public async Task AdminPanel_Navigation_BuildNavigation_IsCaseInsensitive()
    {
        // Arrange
        var builder = new NavigationBuilder();

        // Act
        await _sut.BuildNavigationAsync("Admin", builder);
        var items = builder.Build();

        // Assert — should still build menu for "Admin" (uppercase A)
        items.Should().NotBeEmpty();
    }
}
