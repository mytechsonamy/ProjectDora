using System.Text.Json.Nodes;
using FluentAssertions;
using Moq;
using OrchardCore.Queries;
using ProjectDora.Core.Abstractions;
using ProjectDora.QueryEngine.Services;

namespace ProjectDora.Modules.Tests.QueryEngine;

public class QueryServiceTests
{
    // ── Risk 2: Graceful failure when query module is not enabled ──────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-501")]
    public async Task ExecuteLuceneAsync_LuceneModuleNotEnabled_ThrowsMeaningfulException()
    {
        // IQueryManager.NewAsync(string, JsonNode?) — second param is JsonNode in OC 2.x
        var mockQueryManager = new Mock<IQueryManager>();
        mockQueryManager
            .Setup(m => m.NewAsync("Lucene", It.IsAny<JsonNode>()))
            .ReturnsAsync((Query?)null);

        var service = new OrchardQueryService(mockQueryManager.Object);

        var act = async () => await service.ExecuteLuceneAsync(new LuceneQueryRequest("test"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Lucene*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public async Task ExecuteSqlAsync_SqlModuleNotEnabled_ThrowsMeaningfulException()
    {
        var mockQueryManager = new Mock<IQueryManager>();
        mockQueryManager
            .Setup(m => m.NewAsync("Sql", It.IsAny<JsonNode>()))
            .ReturnsAsync((Query?)null);

        var service = new OrchardQueryService(mockQueryManager.Object);

        var act = async () => await service.ExecuteSqlAsync(new SqlQueryRequest("SELECT 1"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Sql*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-501")]
    public async Task ExecuteLuceneAsync_ExceptionMessage_ContainsModuleNotAvailable()
    {
        var mockQueryManager = new Mock<IQueryManager>();
        mockQueryManager
            .Setup(m => m.NewAsync(It.IsAny<string>(), It.IsAny<JsonNode>()))
            .ReturnsAsync((Query?)null);

        var service = new OrchardQueryService(mockQueryManager.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteLuceneAsync(new LuceneQueryRequest("destek")));

        ex.Message.Should().Contain("not available");
        ex.Message.Should().Contain("module");
    }

    // ── Risk 1: ReindexAsync ───────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-505")]
    public async Task ReindexAsync_NullLuceneRebuilder_ThrowsInvalidOperation()
    {
        var mockQueryManager = new Mock<IQueryManager>();
        var service = new OrchardQueryService(mockQueryManager.Object, luceneIndexRebuilder: null);

        var act = async () => await service.ReindexAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Lucene*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-505")]
    public async Task ReindexAsync_LuceneRebuilderAvailable_ListsAndRebuildsAllIndexes()
    {
        var mockQueryManager = new Mock<IQueryManager>();
        var mockRebuilder = new Mock<ILuceneIndexRebuilder>();
        mockRebuilder.Setup(m => m.ListIndexNames()).Returns(new[] { "search", "content" });
        mockRebuilder.Setup(m => m.RebuildAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = new OrchardQueryService(mockQueryManager.Object, mockRebuilder.Object);

        await service.ReindexAsync();

        mockRebuilder.Verify(m => m.ListIndexNames(), Times.Once);
        mockRebuilder.Verify(m => m.RebuildAsync("search"), Times.Once);
        mockRebuilder.Verify(m => m.RebuildAsync("content"), Times.Once);
    }

    // ── P0-2: LuceneIndexRebuilderAdapter contract ─────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-505")]
    public async Task ReindexAsync_RebuilderReportsEmptyIndexList_NoRebuildCalled()
    {
        // Adapter with zero indexes should complete without calling RebuildAsync.
        var mockQueryManager = new Mock<IQueryManager>();
        var mockRebuilder = new Mock<ILuceneIndexRebuilder>();
        mockRebuilder.Setup(m => m.ListIndexNames()).Returns(Array.Empty<string>());

        var service = new OrchardQueryService(mockQueryManager.Object, mockRebuilder.Object);

        await service.ReindexAsync();

        mockRebuilder.Verify(m => m.ListIndexNames(), Times.Once);
        mockRebuilder.Verify(m => m.RebuildAsync(It.IsAny<string>()), Times.Never);
    }
}
