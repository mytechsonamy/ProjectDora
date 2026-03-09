using System.Text.Json.Nodes;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using ProjectDora.Core.Abstractions;
using YesSql;

namespace ProjectDora.ContentModeling.Services;

public sealed class OrchardContentService : IContentService
{
    private readonly IContentManager _contentManager;
    private readonly ISession _session;

    public OrchardContentService(IContentManager contentManager, ISession session)
    {
        _contentManager = contentManager;
        _session = session;
    }

    public async Task<ContentItemDto> CreateAsync(string contentType, CreateContentItemCommand command)
    {
        ContentItem item;

        if (!string.IsNullOrEmpty(command.CloneFrom))
        {
            var source = await _contentManager.GetAsync(command.CloneFrom, VersionOptions.Latest)
                ?? throw new KeyNotFoundException($"Source content item '{command.CloneFrom}' not found for cloning.");

            item = await _contentManager.CloneAsync(source);
        }
        else
        {
            item = await _contentManager.NewAsync(contentType);
            item.DisplayText = command.DisplayText;

            if (command.Published)
            {
                await _contentManager.CreateAsync(item, VersionOptions.Published);
            }
            else
            {
                await _contentManager.CreateAsync(item, VersionOptions.Draft);
            }
        }

        return MapToDto(item);
    }

    public async Task<ContentItemDto?> GetAsync(string contentItemId, int? version = null)
    {
        ContentItem? item;

        if (version.HasValue)
        {
            // Query specific version via YesSql index
            var allVersions = await _session
                .Query<ContentItem, ContentItemIndex>(x => x.ContentItemId == contentItemId)
                .ListAsync();

            item = allVersions
                .OrderBy(v => v.CreatedUtc)
                .Skip(version.Value - 1)
                .FirstOrDefault();
        }
        else
        {
            item = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        }

        return item is null ? null : MapToDto(item);
    }

    public async Task<PagedResult<ContentItemDto>> ListAsync(string contentType, ContentListQuery query)
    {
        var dbQuery = _session
            .Query<ContentItem, ContentItemIndex>(x => x.ContentType == contentType && x.Latest);

        if (query.Status == "Published")
        {
            dbQuery = _session
                .Query<ContentItem, ContentItemIndex>(x => x.ContentType == contentType && x.Published);
        }

        var totalCount = await dbQuery.CountAsync();

        var items = await dbQuery
            .OrderByDescending(x => x.CreatedUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ListAsync();

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<ContentItemDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    public async Task<ContentItemDto> UpdateAsync(string contentItemId, UpdateContentItemCommand command)
    {
        var item = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired)
            ?? throw new KeyNotFoundException($"Content item '{contentItemId}' not found.");

        if (!string.IsNullOrEmpty(command.DisplayText))
        {
            item.DisplayText = command.DisplayText;
        }

        await _contentManager.UpdateAsync(item);

        return MapToDto(item);
    }

    public async Task PublishAsync(string contentItemId)
    {
        var item = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest)
            ?? throw new KeyNotFoundException($"Content item '{contentItemId}' not found.");

        await _contentManager.PublishAsync(item);
    }

    public async Task UnpublishAsync(string contentItemId)
    {
        var item = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest)
            ?? throw new KeyNotFoundException($"Content item '{contentItemId}' not found.");

        await _contentManager.UnpublishAsync(item);
    }

    public async Task DeleteAsync(string contentItemId, bool hard = false)
    {
        var item = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest)
            ?? throw new KeyNotFoundException($"Content item '{contentItemId}' not found.");

        await _contentManager.RemoveAsync(item);
    }

    public async Task<ContentItemDto> CloneAsync(string contentItemId)
    {
        var source = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest)
            ?? throw new KeyNotFoundException($"Content item '{contentItemId}' not found.");

        var clone = await _contentManager.CloneAsync(source);

        return MapToDto(clone);
    }

    public async Task<ContentItemDto> RollbackAsync(string contentItemId, int targetVersion)
    {
        // Get all versions to find the target
        var allVersions = await _session
            .Query<ContentItem, ContentItemIndex>(x => x.ContentItemId == contentItemId)
            .ListAsync();

        var targetItem = allVersions
            .OrderBy(v => v.CreatedUtc)
            .Skip(targetVersion - 1)
            .FirstOrDefault()
            ?? throw new KeyNotFoundException($"Version {targetVersion} of content item '{contentItemId}' not found.");

        var current = await _contentManager.GetAsync(contentItemId, VersionOptions.DraftRequired)
            ?? throw new KeyNotFoundException($"Content item '{contentItemId}' not found.");

        // Copy display text and merge content from target version
        current.DisplayText = targetItem.DisplayText;
        current.Merge(targetItem);

        await _contentManager.UpdateAsync(current);

        return MapToDto(current);
    }

    public async Task<IReadOnlyList<ContentVersionDto>> GetVersionsAsync(string contentItemId)
    {
        var versions = await _session
            .Query<ContentItem, ContentItemIndex>(x => x.ContentItemId == contentItemId)
            .OrderByDescending(x => x.CreatedUtc)
            .ListAsync();

        var versionNumber = versions.Count();
        return versions.Select(v => new ContentVersionDto(
            versionNumber--,
            v.Published ? "Published" : (v.Latest ? "Draft" : "Archived"),
            v.Owner ?? string.Empty,
            v.ModifiedUtc ?? v.CreatedUtc ?? DateTime.UtcNow
        )).ToList();
    }

    private static ContentItemDto MapToDto(ContentItem item)
    {
        var status = item.Published ? "Published" : (item.Latest ? "Draft" : "Archived");

        // Count version from ContentItemVersionId (unique per version)
        var version = 1;

        return new ContentItemDto(
            item.ContentItemId,
            item.ContentType,
            item.DisplayText ?? string.Empty,
            status,
            version,
            item.Owner ?? string.Empty,
            item.CreatedUtc ?? DateTime.UtcNow,
            item.ModifiedUtc ?? DateTime.UtcNow,
            item.PublishedUtc,
            null);
    }
}
