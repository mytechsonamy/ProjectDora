namespace ProjectDora.Core.Abstractions;

/// <summary>
/// Query execution and saved query management — wraps Orchard Core's query infrastructure.
/// </summary>
public interface IQueryService
{
    Task<QueryResultDto> ExecuteLuceneAsync(LuceneQueryRequest request);
    Task<QueryResultDto> ExecuteSqlAsync(SqlQueryRequest request);
    Task<SavedQueryDto> CreateSavedQueryAsync(CreateSavedQueryCommand command);
    Task<SavedQueryDto?> GetSavedQueryAsync(string queryId);
    Task<IReadOnlyList<SavedQueryDto>> ListSavedQueriesAsync();
    Task<SavedQueryDto> UpdateSavedQueryAsync(string queryId, UpdateSavedQueryCommand command);
    Task DeleteSavedQueryAsync(string queryId);
    Task<QueryResultDto> ExecuteSavedQueryAsync(string queryId, ExecuteQueryCommand command);
    Task ReindexAsync(string? contentType = null);
}

public record LuceneQueryRequest(
    string QueryText,
    string? IndexName = null,
    string? ContentType = null,
    int Page = 1,
    int PageSize = 20);

public record SqlQueryRequest(
    string Sql,
    IDictionary<string, object>? Parameters = null,
    int CommandTimeout = 30);

public record QueryResultDto(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IDictionary<string, object>> Rows,
    int TotalCount,
    long ExecutionTimeMs,
    string QueryType);

public record SavedQueryDto(
    string QueryId,
    string Name,
    string Type,
    string QueryText,
    string? Description,
    bool IsApiExposed,
    IReadOnlyList<QueryParameterDef>? Parameters,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public record QueryParameterDef(
    string Name,
    string Type,
    bool Required = false,
    object? DefaultValue = null);

public record CreateSavedQueryCommand(
    string Name,
    string Type,
    string QueryText,
    string? Description = null,
    bool IsApiExposed = false,
    IReadOnlyList<QueryParameterDef>? Parameters = null);

public record UpdateSavedQueryCommand(
    string? QueryText = null,
    string? Description = null,
    bool? IsApiExposed = null,
    IReadOnlyList<QueryParameterDef>? Parameters = null);

public record ExecuteQueryCommand(
    IDictionary<string, object>? Parameters = null,
    int Page = 1,
    int PageSize = 20,
    int? MaxResults = null);
