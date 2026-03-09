using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using OrchardCore.Navigation;
using ProjectDora.QueryEngine;

namespace ProjectDora.Modules.Tests.QueryEngine;

public class QueryEngineMenuTests
{
    private readonly QueryEngineMenu _menu;

    public QueryEngineMenuTests()
    {
        var localizer = new Mock<IStringLocalizer<QueryEngineMenu>>();
        localizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns<string>(s => new LocalizedString(s, s));

        _menu = new QueryEngineMenu(localizer.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public async Task QueryEngine_Menu_AddsSearchAndQueriesTopLevel()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        items.Should().ContainSingle(i => i.Text != null && i.Text.Value == "Search & Queries");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public async Task QueryEngine_Menu_HasSavedQueriesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Search & Queries");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Saved Queries");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-501")]
    public async Task QueryEngine_Menu_HasLuceneIndexesSubMenu()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("admin", builder);

        var items = builder.Build();
        var parent = items.First(i => i.Text != null && i.Text.Value == "Search & Queries");
        parent.Items.Should().Contain(i => i.Text != null && i.Text.Value == "Lucene Indexes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public async Task QueryEngine_Menu_SkipsNonAdminNavigation()
    {
        var builder = new NavigationBuilder();

        await _menu.BuildNavigationAsync("main", builder);

        var items = builder.Build();
        items.Should().BeEmpty();
    }
}
