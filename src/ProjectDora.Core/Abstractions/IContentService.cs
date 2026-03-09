namespace ProjectDora.Core.Abstractions;

/// <summary>
/// Content item CRUD and lifecycle operations — wraps Orchard Core's IContentManager.
/// </summary>
public interface IContentService
{
    Task<ContentItemDto> CreateAsync(string contentType, CreateContentItemCommand command);
    Task<ContentItemDto?> GetAsync(string contentItemId, int? version = null);
    Task<PagedResult<ContentItemDto>> ListAsync(string contentType, ContentListQuery query);
    Task<ContentItemDto> UpdateAsync(string contentItemId, UpdateContentItemCommand command);
    Task PublishAsync(string contentItemId);
    Task UnpublishAsync(string contentItemId);
    Task DeleteAsync(string contentItemId, bool hard = false);
    Task<ContentItemDto> CloneAsync(string contentItemId);
    Task<ContentItemDto> RollbackAsync(string contentItemId, int targetVersion);
    Task<IReadOnlyList<ContentVersionDto>> GetVersionsAsync(string contentItemId);
}

public record ContentItemDto(
    string ContentItemId,
    string ContentType,
    string DisplayText,
    string Status,
    int Version,
    string Owner,
    DateTime CreatedUtc,
    DateTime ModifiedUtc,
    DateTime? PublishedUtc,
    string? Culture);

public record ContentVersionDto(
    int Version,
    string Status,
    string ModifiedBy,
    DateTime ModifiedUtc);

public record CreateContentItemCommand(
    string DisplayText,
    bool Published = false,
    string? Culture = null,
    string? CloneFrom = null,
    IDictionary<string, object>? Parts = null,
    IDictionary<string, object>? Fields = null);

public record UpdateContentItemCommand(
    string? DisplayText = null,
    IDictionary<string, object>? Parts = null,
    IDictionary<string, object>? Fields = null);

public record ContentListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string SortOrder = "desc",
    string? Status = null,
    string? Culture = null);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
