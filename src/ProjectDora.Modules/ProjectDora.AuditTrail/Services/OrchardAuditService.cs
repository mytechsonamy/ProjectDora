using System.Text.Json;
using OrchardCore.AuditTrail.Indexes;
using OrchardCore.AuditTrail.Models;
using OrchardCore.Entities;
using OrchardCore.Settings;
using ProjectDora.Core.Abstractions;
using YesSql;

namespace ProjectDora.AuditTrail.Services;

/// <summary>
/// Custom audit settings persisted via ISiteService.
/// </summary>
internal sealed class ProjectDoraAuditSettings
{
    public bool IsEnabled { get; set; } = true;
    public int RetentionDays { get; set; } = 365;
    public int MaxRecords { get; set; } = 1_000_000;
    public List<string> AuditedContentTypes { get; set; } = new();
    public string PurgeSchedule { get; set; } = "0 2 * * *";
}

/// <summary>
/// Custom event payload stored in AuditTrailEvent.Properties.
/// </summary>
internal sealed class AuditEventPayload
{
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

public sealed class OrchardAuditService : IAuditService
{
    private readonly ISession _session;
    private readonly ISiteService _siteService;
    private readonly IContentService _contentService;

    public OrchardAuditService(
        ISession session,
        ISiteService siteService,
        IContentService contentService)
    {
        _session = session;
        _siteService = siteService;
        _contentService = contentService;
    }

    public async Task LogAsync(CreateAuditEventCommand command)
    {
        var evt = new AuditTrailEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Category = command.ContentType,
            Name = command.EventType,
            CorrelationId = command.ContentItemId,
            UserName = command.UserName,
            NormalizedUserName = command.UserName?.ToUpperInvariant() ?? string.Empty,
            ClientIpAddress = command.UserIpAddress ?? string.Empty,
            CreatedUtc = DateTime.UtcNow,
        };

        if (command.OldValue is not null || command.NewValue is not null)
        {
            evt.Alter<AuditEventPayload>(p =>
            {
                p.OldValue = command.OldValue;
                p.NewValue = command.NewValue;
            });
        }

        await _session.SaveAsync(evt);
        await _session.SaveChangesAsync();
    }

    public async Task<AuditEventDto?> GetAsync(string auditEventId)
    {
        var evt = await _session
            .Query<AuditTrailEvent, AuditTrailEventIndex>(x => x.EventId == auditEventId)
            .FirstOrDefaultAsync();

        return evt is null ? null : MapToDto(evt);
    }

    public async Task<PagedResult<AuditEventDto>> ListAsync(ListAuditEventsQuery query)
    {
        var dbQuery = _session.Query<AuditTrailEvent, AuditTrailEventIndex>();

        if (!string.IsNullOrEmpty(query.ContentType))
        {
            dbQuery = dbQuery.Where(x => x.Category == query.ContentType);
        }

        if (!string.IsNullOrEmpty(query.ContentItemId))
        {
            dbQuery = dbQuery.Where(x => x.CorrelationId == query.ContentItemId);
        }

        if (!string.IsNullOrEmpty(query.UserName))
        {
            var normalizedUser = query.UserName.ToUpperInvariant();
            dbQuery = dbQuery.Where(x => x.NormalizedUserName == normalizedUser);
        }

        if (!string.IsNullOrEmpty(query.EventType))
        {
            dbQuery = dbQuery.Where(x => x.Name == query.EventType);
        }

        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value;
            dbQuery = dbQuery.Where(x => x.CreatedUtc >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value;
            dbQuery = dbQuery.Where(x => x.CreatedUtc <= to);
        }

        var totalCount = await dbQuery.CountAsync();

        var events = await dbQuery
            .OrderByDescending(x => x.CreatedUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ListAsync();

        var dtos = events.Select(MapToDto).ToList();
        return new PagedResult<AuditEventDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<AuditEventDto>> GetContentHistoryAsync(string contentItemId)
    {
        var events = await _session
            .Query<AuditTrailEvent, AuditTrailEventIndex>(x => x.CorrelationId == contentItemId)
            .OrderByDescending(x => x.CreatedUtc)
            .ListAsync();

        return events.Select(MapToDto).ToList();
    }

    public async Task<ContentDiffDto> GetDiffAsync(string contentItemId, int fromVersion, int toVersion)
    {
        var fromItem = await _contentService.GetAsync(contentItemId, fromVersion);
        var toItem = await _contentService.GetAsync(contentItemId, toVersion);

        if (fromItem is null || toItem is null)
        {
            return new ContentDiffDto(contentItemId, fromVersion, toVersion, Array.Empty<FieldDiffEntry>());
        }

        var changes = new List<FieldDiffEntry>();

        if (fromItem.DisplayText != toItem.DisplayText)
        {
            changes.Add(new FieldDiffEntry("DisplayText", "Modified", fromItem.DisplayText, toItem.DisplayText));
        }

        if (fromItem.Status != toItem.Status)
        {
            changes.Add(new FieldDiffEntry(
                "Status",
                "Modified",
                fromItem.Status,
                toItem.Status));
        }

        return new ContentDiffDto(contentItemId, fromVersion, toVersion, changes);
    }

    public async Task RollbackAsync(string contentItemId, int targetVersion)
    {
        await _contentService.RollbackAsync(contentItemId, targetVersion);
    }

    public async Task<AuditSettingsDto> GetSettingsAsync()
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var data = site.As<ProjectDoraAuditSettings>();
        if (data is null)
        {
            return new AuditSettingsDto(true, 365, 1_000_000, Array.Empty<string>(), "0 2 * * *");
        }

        return new AuditSettingsDto(
            data.IsEnabled,
            data.RetentionDays,
            data.MaxRecords,
            data.AuditedContentTypes,
            data.PurgeSchedule);
    }

    public async Task UpdateSettingsAsync(UpdateAuditSettingsCommand command)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<ProjectDoraAuditSettings>(data =>
        {
            if (command.IsEnabled.HasValue)
            {
                data.IsEnabled = command.IsEnabled.Value;
            }

            if (command.RetentionDays.HasValue)
            {
                data.RetentionDays = command.RetentionDays.Value;
            }

            if (command.MaxRecords.HasValue)
            {
                data.MaxRecords = command.MaxRecords.Value;
            }

            if (command.AuditedContentTypes is not null)
            {
                data.AuditedContentTypes = command.AuditedContentTypes.ToList();
            }
        });

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    public async Task<long> PurgeAsync(int olderThanDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

        var eventsToDelete = await _session
            .Query<AuditTrailEvent, AuditTrailEventIndex>(x => x.CreatedUtc < cutoff)
            .ListAsync();

        var list = eventsToDelete.ToList();
        foreach (var evt in list)
        {
            _session.Delete(evt);
        }

        if (list.Count > 0)
        {
            await _session.SaveChangesAsync();
        }

        return list.Count;
    }

    public async Task<int> EnableContentTypeAuditAsync(string contentTypeName)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<ProjectDoraAuditSettings>(data =>
        {
            if (!data.AuditedContentTypes.Contains(contentTypeName, StringComparer.OrdinalIgnoreCase))
            {
                data.AuditedContentTypes.Add(contentTypeName);
            }
        });

        await _siteService.UpdateSiteSettingsAsync(site);

        var updated = site.As<ProjectDoraAuditSettings>();
        return updated?.AuditedContentTypes.Count ?? 0;
    }

    public async Task<int> DisableContentTypeAuditAsync(string contentTypeName)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<ProjectDoraAuditSettings>(data =>
        {
            data.AuditedContentTypes.RemoveAll(t =>
                string.Equals(t, contentTypeName, StringComparison.OrdinalIgnoreCase));
        });

        await _siteService.UpdateSiteSettingsAsync(site);

        var updated = site.As<ProjectDoraAuditSettings>();
        return updated?.AuditedContentTypes.Count ?? 0;
    }

    private static AuditEventDto MapToDto(AuditTrailEvent evt)
    {
        var payload = evt.As<AuditEventPayload>();

        return new AuditEventDto(
            evt.EventId,
            string.Empty,
            evt.Name,
            evt.Category,
            evt.CorrelationId,
            null,
            evt.UserName,
            string.IsNullOrEmpty(evt.ClientIpAddress) ? null : evt.ClientIpAddress,
            payload?.OldValue,
            payload?.NewValue,
            evt.CreatedUtc,
            null);
    }
}
