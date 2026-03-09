using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.AuditTrail;

namespace ProjectDora.Modules.Tests.AuditTrail;

public class AuditTrailMenuTests
{
    private readonly AuditTrailMenu _menu;

    public AuditTrailMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<AuditTrailMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new AuditTrailMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-902")]
    public async Task AuditTrail_Menu_AddsAuditTrailTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Audit Trail");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-902")]
    public async Task AuditTrail_Menu_HasAuditLogSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Audit Trail");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Audit Log");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-906")]
    public async Task AuditTrail_Menu_HasSettingsSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Audit Trail");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Settings");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-902")]
    public async Task AuditTrail_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("frontend", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
