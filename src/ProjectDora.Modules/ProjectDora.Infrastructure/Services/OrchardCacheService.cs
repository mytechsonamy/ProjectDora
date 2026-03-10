using OrchardCore.Entities;
using OrchardCore.Environment.Cache;
using OrchardCore.Settings;
using ProjectDora.Core.Abstractions;
using StackExchange.Redis;

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

    // Optional: present when OrchardCore.Redis is configured and IConnectionMultiplexer
    // is registered in DI (via OrchardCore.Redis module). Null when Redis is not configured
    // (e.g., SQLite/local dev deployments). Resolved via IServiceProvider to avoid DI
    // container throw when not registered.
    private readonly IConnectionMultiplexer? _redis;

    public OrchardCacheService(
        ISiteService siteService,
        ISignal signal,
        IServiceProvider services)
    {
        _siteService = siteService;
        _signal = signal;
        _redis = services.GetService(typeof(IConnectionMultiplexer)) as IConnectionMultiplexer;
    }

    public async Task<CacheStatsDto> GetStatsAsync()
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var settings = site.As<CacheSettingsData>() ?? new CacheSettingsData();
        var totalKeys = (long)settings.CategoryTtls.Count;

        if (_redis is null)
        {
            // Redis not configured — return placeholder stats.
            // HitCount/MissCount/MemoryUsedBytes require Redis INFO which is unavailable.
            return new CacheStatsDto(totalKeys, 0L, 0L, 0.0, 0L);
        }

        try
        {
            // Use the first available Redis endpoint.
            var endpoints = _redis.GetEndPoints();
            if (endpoints.Length == 0)
            {
                return new CacheStatsDto(totalKeys, 0L, 0L, 0.0, 0L);
            }

            var server = _redis.GetServer(endpoints[0]);

            // INFO returns IGrouping<string, KeyValuePair<string,string>>[]
            // where each group key is the section name (e.g. "stats", "memory").
            var infoGroups = await server.InfoAsync();

            var hits = ParseInfoLong(infoGroups, "keyspace_hits");
            var misses = ParseInfoLong(infoGroups, "keyspace_misses");
            var memory = ParseInfoLong(infoGroups, "used_memory");

            var total = hits + misses;
            var hitRatio = total > 0 ? (double)hits / total : 0.0;

            return new CacheStatsDto(totalKeys, hits, misses, hitRatio, memory);
        }
        catch
        {
            // Redis is registered but unreachable — degrade gracefully to placeholder.
            return new CacheStatsDto(totalKeys, 0L, 0L, 0.0, 0L);
        }
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

    // IServer.InfoAsync() returns IGrouping<string, KeyValuePair<string,string>>[]
    // where each IGrouping is a named section (e.g. "stats", "memory").
    private static long ParseInfoLong(
        System.Linq.IGrouping<string, KeyValuePair<string, string>>[] sections, string key)
    {
        foreach (var section in sections)
        {
            foreach (var kvp in section)
            {
                if (kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase) &&
                    long.TryParse(kvp.Value, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }
            }
        }

        return 0L;
    }
}
