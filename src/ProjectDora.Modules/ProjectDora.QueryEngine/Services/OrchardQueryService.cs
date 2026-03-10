using System.Diagnostics;
using System.Globalization;
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

    public async Task<QueryResultDto> ExecuteLuceneAsync(LuceneQueryRequest request)
    {
        // Create a temporary Lucene query, execute it, then delete
        var tempName = string.Create(CultureInfo.InvariantCulture, $"__adhoc_lucene_{Guid.NewGuid():N}");
        var query = await _queryManager.NewAsync("Lucene", tempName);
        if (query is null)
        {
            // Lucene module not enabled — return empty result
            return new QueryResultDto(
                Array.Empty<string>(),
                Array.Empty<IDictionary<string, object>>(),
                0, 0, "Lucene");
        }

        // Set LuceneQuery-specific properties via reflection (available when Lucene module is loaded)
        var queryType = query.GetType();
        queryType.GetProperty("Index")?.SetValue(query, request.IndexName ?? "Search");
        queryType.GetProperty("Template")?.SetValue(query, request.QueryText);
        queryType.GetProperty("ReturnContentItems")?.SetValue(query, false);

        await _queryManager.SaveAsync(query);
        try
        {
            var sw = Stopwatch.StartNew();
            var parameters = new Dictionary<string, object>();
            if (request.ContentType is not null)
            {
                parameters["contenttype"] = request.ContentType;
            }

            var queryResult = await _queryManager.ExecuteQueryAsync(query, parameters);
            sw.Stop();

            return BuildResult(queryResult, request.Page, request.PageSize, sw.ElapsedMilliseconds, "Lucene");
        }
        finally
        {
            await _queryManager.DeleteQueryAsync(tempName);
        }
    }

    public async Task<QueryResultDto> ExecuteSqlAsync(SqlQueryRequest request)
    {
        var validation = SqlSafetyValidator.Validate(request.Sql);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        // Create a temporary SQL query, execute it, then delete
        var tempName = string.Create(CultureInfo.InvariantCulture, $"__adhoc_sql_{Guid.NewGuid():N}");
        var query = await _queryManager.NewAsync("Sql", tempName);
        if (query is null)
        {
            // SQL query module not enabled — return empty result
            return new QueryResultDto(
                Array.Empty<string>(),
                Array.Empty<IDictionary<string, object>>(),
                0, 0, "SQL");
        }

        // Set SqlQuery-specific properties via reflection (available when SQL query module is loaded)
        var queryType = query.GetType();
        queryType.GetProperty("Template")?.SetValue(query, request.Sql);

        await _queryManager.SaveAsync(query);
        try
        {
            var sw = Stopwatch.StartNew();
            var parameters = request.Parameters is not null
                ? new Dictionary<string, object>(request.Parameters)
                : new Dictionary<string, object>();

            var queryResult = await _queryManager.ExecuteQueryAsync(query, parameters);
            sw.Stop();

            return BuildResult(queryResult, 1, int.MaxValue, sw.ElapsedMilliseconds, "SQL");
        }
        finally
        {
            await _queryManager.DeleteQueryAsync(tempName);
        }
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
        // Requires OrchardCore.Search.Lucene runtime — index rebuild dispatched via event
        // ILuceneIndexManager.RebuildAsync() would be used here when the module is enabled
        return Task.CompletedTask;
    }

    private static QueryResultDto BuildResult(
        IQueryResults queryResult,
        int page,
        int pageSize,
        long elapsedMs,
        string queryType)
    {
        var rows = new List<IDictionary<string, object>>();
        var columns = new List<string>();

        foreach (var item in queryResult.Items)
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

        var paged = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new QueryResultDto(columns, paged, rows.Count, elapsedMs, queryType);
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
