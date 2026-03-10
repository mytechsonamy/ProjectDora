using OrchardCore.Search.Lucene;

namespace ProjectDora.QueryEngine.Services;

/// <summary>
/// Concrete ILuceneIndexRebuilder backed by OrchardCore's Lucene indexing services.
///
/// Problem: OrchardQueryService accepts ILuceneIndexRebuilder? via optional constructor injection,
/// but Startup.cs never registers a concrete implementation. At runtime _luceneIndexRebuilder is
/// always null and ReindexAsync() always throws "Lucene search module is not enabled."
///
/// Fix: Register this adapter in DI so the optional injection resolves to a real implementation.
/// LuceneIndexSettingsService lists configured index names; LuceneIndexingService performs the
/// rebuild. Both are registered as concrete types by OrchardCore.Search.Lucene when enabled.
///
/// Note: ListIndexNames() uses GetAwaiter().GetResult() because the ILuceneIndexRebuilder interface
/// is synchronous by design (used from within an already-async call chain in ReindexAsync).
/// This is safe in OC service layer context where no SynchronizationContext is captured.
/// </summary>
public sealed class LuceneIndexRebuilderAdapter : ILuceneIndexRebuilder
{
    private readonly LuceneIndexSettingsService _settingsService;
    private readonly LuceneIndexingService _indexingService;

    public LuceneIndexRebuilderAdapter(
        LuceneIndexSettingsService settingsService,
        LuceneIndexingService indexingService)
    {
        _settingsService = settingsService;
        _indexingService = indexingService;
    }

    public IEnumerable<string> ListIndexNames()
        => _settingsService.GetSettingsAsync()
            .GetAwaiter()
            .GetResult()
            .Select(static s => s.IndexName);

    public Task RebuildAsync(string indexName)
        => _indexingService.RebuildIndexAsync(indexName);
}
