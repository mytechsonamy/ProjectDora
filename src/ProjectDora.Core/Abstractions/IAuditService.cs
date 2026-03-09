namespace ProjectDora.Core.Abstractions;

public interface IAuditService
{
    Task LogAsync(CreateAuditEventCommand command);
    Task<AuditEventDto?> GetAsync(string auditEventId);
    Task<PagedResult<AuditEventDto>> ListAsync(ListAuditEventsQuery query);
    Task<IReadOnlyList<AuditEventDto>> GetContentHistoryAsync(string contentItemId);
    Task<ContentDiffDto> GetDiffAsync(string contentItemId, int fromVersion, int toVersion);
    Task RollbackAsync(string contentItemId, int targetVersion);
    Task<AuditSettingsDto> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateAuditSettingsCommand command);
    Task<long> PurgeAsync(int olderThanDays);
    Task<int> EnableContentTypeAuditAsync(string contentTypeName);
    Task<int> DisableContentTypeAuditAsync(string contentTypeName);
}

public record AuditEventDto(
    string AuditEventId,
    string TenantId,
    string EventType,
    string ContentType,
    string ContentItemId,
    int? ContentVersion,
    string UserName,
    string? UserIpAddress,
    string? OldValue,
    string? NewValue,
    DateTime OccurredUtc,
    IDictionary<string, object>? Metadata);

public record CreateAuditEventCommand(
    string EventType,
    string ContentType,
    string ContentItemId,
    int? ContentVersion,
    string UserName,
    string? UserIpAddress = null,
    string? OldValue = null,
    string? NewValue = null,
    IDictionary<string, object>? Metadata = null);

public record ListAuditEventsQuery(
    string? ContentType = null,
    string? ContentItemId = null,
    string? UserName = null,
    string? EventType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20);

public record ContentDiffDto(
    string ContentItemId,
    int FromVersion,
    int ToVersion,
    IReadOnlyList<FieldDiffEntry> Changes);

public record FieldDiffEntry(
    string FieldPath,
    string ChangeType,
    string? OldValue,
    string? NewValue);

public record AuditSettingsDto(
    bool IsEnabled,
    int RetentionDays,
    int MaxRecords,
    IReadOnlyList<string> AuditedContentTypes,
    string PurgeSchedule);

public record UpdateAuditSettingsCommand(
    bool? IsEnabled = null,
    int? RetentionDays = null,
    int? MaxRecords = null,
    IReadOnlyList<string>? AuditedContentTypes = null);
