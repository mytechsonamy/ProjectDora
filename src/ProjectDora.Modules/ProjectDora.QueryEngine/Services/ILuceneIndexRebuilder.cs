namespace ProjectDora.QueryEngine.Services;

/// <summary>
/// Abstracts Lucene index management for optional injection and testability.
/// Implement this interface using LuceneIndexingService when the Lucene module is enabled.
/// </summary>
public interface ILuceneIndexRebuilder
{
    /// <summary>Returns the names of all configured Lucene indexes.</summary>
    IEnumerable<string> ListIndexNames();

    /// <summary>Triggers a full rebuild of the specified Lucene index.</summary>
    Task RebuildAsync(string indexName);
}
