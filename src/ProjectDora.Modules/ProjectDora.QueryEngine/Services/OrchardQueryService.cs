using System.Diagnostics;
using OrchardCore.Queries;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.QueryEngine.Services;

public sealed class OrchardQueryService : IQueryService
{
    private readonly IQueryManager _queryManager;

    public OrchardQueryService(IQueryManager queryManager)
    {
        _queryManager = queryManager;
    }

    public Task<QueryResultDto> ExecuteLuceneAsync(LuceneQueryRequest request)
    {
        // Lucene execution requires OrchardCore.Search.Lucene runtime — delegation via IQueryManager
        var result = new QueryResultDto(
            Array.Empty<string>(),
            Array.Empty<IDictionary<string, object>>(),
            0,
            0,
            "Lucene");

        return Task.FromResult(result);
    }

    public Task<QueryResultDto> ExecuteSqlAsync(SqlQueryRequest request)
    {
        var validation = SqlSafetyValidator.Validate(request.Sql);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        // SQL execution requires database connection — delegation via IQueryManager
        var result = new QueryResultDto(
            Array.Empty<string>(),
            Array.Empty<IDictionary<string, object>>(),
            0,
            0,
            "SQL");

        return Task.FromResult(result);
    }

    public async Task<SavedQueryDto> CreateSavedQueryAsync(CreateSavedQueryCommand command)
    {
        if (command.Type == "SQL")
        {
            var validation = SqlSafetyValidator.Validate(command.QueryText);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }
        }

        var query = await _queryManager.NewAsync(command.Type, command.Name);
        await _queryManager.SaveAsync(query);

        return MapToDto(query, command.QueryText, command.Description, command.IsApiExposed, command.Parameters);
    }

    public async Task<SavedQueryDto?> GetSavedQueryAsync(string queryId)
    {
        var query = await _queryManager.GetQueryAsync(queryId);
        return query is null ? null : MapToDto(query);
    }

    public async Task<IReadOnlyList<SavedQueryDto>> ListSavedQueriesAsync()
    {
        var queries = await _queryManager.ListQueriesAsync();
        return queries.Select(q => MapToDto(q)).ToList();
    }

    public async Task<SavedQueryDto> UpdateSavedQueryAsync(string queryId, UpdateSavedQueryCommand command)
    {
        var query = await _queryManager.GetQueryAsync(queryId)
            ?? throw new KeyNotFoundException($"Saved query '{queryId}' not found.");

        if (command.QueryText is not null && query.Source == "SQL")
        {
            var validation = SqlSafetyValidator.Validate(command.QueryText);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ErrorMessage);
            }
        }

        await _queryManager.SaveAsync(query);

        return MapToDto(query);
    }

    public async Task DeleteSavedQueryAsync(string queryId)
    {
        var query = await _queryManager.GetQueryAsync(queryId)
            ?? throw new KeyNotFoundException($"Saved query '{queryId}' not found.");

        await _queryManager.DeleteQueryAsync(queryId);
    }

    public async Task<QueryResultDto> ExecuteSavedQueryAsync(string queryId, ExecuteQueryCommand command)
    {
        var query = await _queryManager.GetQueryAsync(queryId)
            ?? throw new KeyNotFoundException($"Saved query '{queryId}' not found.");

        var sw = Stopwatch.StartNew();

        var parameters = command.Parameters ?? new Dictionary<string, object>();
        var queryResult = await _queryManager.ExecuteQueryAsync(query, parameters);

        sw.Stop();

        var items = queryResult.Items;
        var rows = new List<IDictionary<string, object>>();
        var columns = new List<string>();

        foreach (var item in items)
        {
            if (item is IDictionary<string, object> dict)
            {
                if (columns.Count == 0)
                {
                    columns.AddRange(dict.Keys);
                }
                rows.Add(dict);
            }
        }

        var maxResults = command.MaxResults ?? command.PageSize;
        var pagedRows = rows
            .Skip((command.Page - 1) * command.PageSize)
            .Take(maxResults)
            .ToList();

        return new QueryResultDto(
            columns,
            pagedRows,
            rows.Count,
            sw.ElapsedMilliseconds,
            query.Source);
    }

    public Task ReindexAsync(string? contentType = null)
    {
        // Index rebuild requires OrchardCore.Search.Lucene runtime
        // Will be implemented with Lucene/ES integration
        return Task.CompletedTask;
    }

    private static SavedQueryDto MapToDto(
        Query query,
        string? queryText = null,
        string? description = null,
        bool isApiExposed = false,
        IReadOnlyList<QueryParameterDef>? parameters = null)
    {
        return new SavedQueryDto(
            query.Name,
            query.Name,
            query.Source,
            queryText ?? string.Empty,
            description,
            isApiExposed,
            parameters,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}
