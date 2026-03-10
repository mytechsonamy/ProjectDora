using OrchardCore.Entities;
using OrchardCore.Environment.Cache;
using OrchardCore.Settings;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Infrastructure.Services;

/// <summary>
/// Cache settings data persisted via ISiteService.
/// </summary>
internal sealed class CacheSettingsData
{
    public bool IsEnabled { get; set; } = true;
    public int DefaultTtlSeconds { get; set; } = 300;
    public Dictionary<string, int> CategoryTtls { get; set; } = new()
    {
        { "content", 300 },
        { "permissions", 600 },
        { "queries", 120 },
    };
}

public sealed class OrchardCacheService : ICacheService
{
    private readonly ISiteService _siteService;
    private readonly ISignal _signal;

    public OrchardCacheService(ISiteService siteService, ISignal signal)
    {
        _siteService = siteService;
        _signal = signal;
    }

    public Task<CacheStatsDto> GetStatsAsync()
    {
        // Detailed cache stats (hits/misses/memory) require Redis-specific APIs
        // (IConnectionMultiplexer INFO command) available when Redis module is loaded.
        // Returning structural placeholder — real counts require runtime Redis integration.
        var stats = new CacheStatsDto(0L, 0L, 0L, 0.0, 0L);
        return Task.FromResult(stats);
    }

    public async Task PurgeAsync(string? category = null)
    {
        // Signal cache invalidation — OC consumers subscribed to this token will clear their caches
        var signal = category is not null
            ? $"cache_purge_{category}"
            : "cache_purge_all";

        await _signal.SignalTokenAsync(signal);
    }

    public async Task<CacheSettingsDto> GetSettingsAsync()
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var data = site.As<CacheSettingsData>() ?? new CacheSettingsData();

        return new CacheSettingsDto(
            data.IsEnabled,
            data.DefaultTtlSeconds > 0 ? data.DefaultTtlSeconds : 300,
            data.CategoryTtls);
    }

    public async Task UpdateSettingsAsync(UpdateCacheSettingsCommand command)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<CacheSettingsData>(data =>
        {
            if (command.IsEnabled.HasValue)
            {
                data.IsEnabled = command.IsEnabled.Value;
            }

            if (command.DefaultTtlSeconds.HasValue)
            {
                data.DefaultTtlSeconds = command.DefaultTtlSeconds.Value;
            }

            if (command.CategoryTtls is not null)
            {
                data.CategoryTtls = new Dictionary<string, int>(command.CategoryTtls);
            }
        });

        await _siteService.UpdateSiteSettingsAsync(site);
    }
}
