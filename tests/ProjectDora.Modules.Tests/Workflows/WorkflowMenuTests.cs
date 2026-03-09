using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.Workflows;

namespace ProjectDora.Modules.Tests.Workflows;

public class WorkflowMenuTests
{
    private readonly WorkflowMenu _menu;

    public WorkflowMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<WorkflowMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new WorkflowMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public async Task Workflow_Menu_AddsWorkflowsTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Workflows");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public async Task Workflow_Menu_HasAllWorkflowsSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Workflows");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "All Workflows");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-708")]
    public async Task Workflow_Menu_HasExecutionHistorySubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Workflows");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Execution History");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public async Task Workflow_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("frontend", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
