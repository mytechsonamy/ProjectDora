using ProjectDora.Core.Abstractions;

namespace ProjectDora.AuditTrail.Services;

public sealed class OrchardAuditService : IAuditService
{
    private static readonly AuditSettingsDto DefaultSettings = new(
        IsEnabled: true,
        RetentionDays: 365,
        MaxRecords: 1_000_000,
        AuditedContentTypes: Array.Empty<string>(),
        PurgeSchedule: "0 2 * * *");

    public Task LogAsync(CreateAuditEventCommand command)
    {
        return Task.CompletedTask;
    }

    public Task<AuditEventDto?> GetAsync(string auditEventId)
    {
        return Task.FromResult<AuditEventDto?>(null);
    }

    public Task<PagedResult<AuditEventDto>> ListAsync(ListAuditEventsQuery query)
    {
        var result = new PagedResult<AuditEventDto>(
            Array.Empty<AuditEventDto>(),
            0,
            query.Page,
            query.PageSize);

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<AuditEventDto>> GetContentHistoryAsync(string contentItemId)
    {
        IReadOnlyList<AuditEventDto> result = Array.Empty<AuditEventDto>();
        return Task.FromResult(result);
    }

    public Task<ContentDiffDto> GetDiffAsync(string contentItemId, int fromVersion, int toVersion)
    {
        var diff = new ContentDiffDto(
            contentItemId,
            fromVersion,
            toVersion,
            Array.Empty<FieldDiffEntry>());

        return Task.FromResult(diff);
    }

    public Task RollbackAsync(string contentItemId, int targetVersion)
    {
        return Task.CompletedTask;
    }

    public Task<AuditSettingsDto> GetSettingsAsync()
    {
        return Task.FromResult(DefaultSettings);
    }

    public Task UpdateSettingsAsync(UpdateAuditSettingsCommand command)
    {
        return Task.CompletedTask;
    }

    public Task<long> PurgeAsync(int olderThanDays)
    {
        return Task.FromResult(0L);
    }

    public Task<int> EnableContentTypeAuditAsync(string contentTypeName)
    {
        return Task.FromResult(0);
    }

    public Task<int> DisableContentTypeAuditAsync(string contentTypeName)
    {
        return Task.FromResult(0);
    }
}
