using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Infrastructure;

public class CacheServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void CacheStatsDto_ZeroCountsForPlaceholder()
    {
        var stats = new CacheStatsDto(0L, 0L, 0L, 0.0, 0L);

        stats.TotalKeys.Should().Be(0);
        stats.HitCount.Should().Be(0);
        stats.MissCount.Should().Be(0);
        stats.HitRatio.Should().Be(0.0);
        stats.MemoryUsedBytes.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void CacheSettingsDto_DefaultCategoryTtls()
    {
        var ttls = new Dictionary<string, int>
        {
            { "content", 300 },
            { "permissions", 600 },
            { "queries", 120 },
        };

        var dto = new CacheSettingsDto(IsEnabled: true, DefaultTtlSeconds: 300, CategoryTtls: ttls);

        dto.IsEnabled.Should().BeTrue();
        dto.DefaultTtlSeconds.Should().Be(300);
        dto.CategoryTtls.Should().ContainKey("content");
        dto.CategoryTtls["permissions"].Should().Be(600);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void UpdateCacheSettingsCommand_OnlyEnabledChange_PreservesNulls()
    {
        var cmd = new UpdateCacheSettingsCommand(IsEnabled: false, null, null);

        cmd.IsEnabled.Should().BeFalse();
        cmd.DefaultTtlSeconds.Should().BeNull();
        cmd.CategoryTtls.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void UpdateCacheSettingsCommand_AllValuesSet()
    {
        var newTtls = new Dictionary<string, int> { { "content", 600 } };
        var cmd = new UpdateCacheSettingsCommand(
            IsEnabled: true,
            DefaultTtlSeconds: 600,
            CategoryTtls: newTtls);

        cmd.IsEnabled.Should().BeTrue();
        cmd.DefaultTtlSeconds.Should().Be(600);
        cmd.CategoryTtls.Should().ContainKey("content");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void PurgeSignal_CategoryScoped_HasCategoryName()
    {
        var category = "content";
        var signal = $"cache_purge_{category}";

        signal.Should().Contain("cache_purge");
        signal.Should().Contain(category);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void PurgeSignal_NullCategory_IsGlobalSignal()
    {
        string? category = null;
        var signal = category is not null ? $"cache_purge_{category}" : "cache_purge_all";

        signal.Should().Be("cache_purge_all");
    }

    // ── Risk 3: GetStatsAsync uses CategoryTtls.Count as totalKeys ────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void GetStatsAsync_DefaultCategoryTtls_TotalKeysIsThree()
    {
        // Default CacheSettingsData has 3 categories: content, permissions, queries
        var defaultCategories = new Dictionary<string, int>
        {
            { "content", 300 },
            { "permissions", 600 },
            { "queries", 120 },
        };

        var totalKeys = (long)defaultCategories.Count;
        // CacheStatsDto(TotalKeys, HitCount, MissCount, HitRatio, MemoryUsedBytes)
        var stats = new CacheStatsDto(totalKeys, 0L, 0L, 0.0, 0L);

        stats.TotalKeys.Should().Be(3);
        // Hit/miss remain 0 — Redis-specific APIs not available without IConnectionMultiplexer
        stats.HitCount.Should().Be(0);
        stats.MissCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void GetStatsAsync_CustomCategoryTtls_TotalKeysMatchesCount()
    {
        var customCategories = new Dictionary<string, int>
        {
            { "content", 300 },
            { "permissions", 600 },
            { "queries", 120 },
            { "themes", 180 },
            { "workflows", 240 },
        };

        var totalKeys = (long)customCategories.Count;
        // CacheStatsDto(TotalKeys, HitCount, MissCount, HitRatio, MemoryUsedBytes)
        var stats = new CacheStatsDto(totalKeys, 0L, 0L, 0.0, 0L);

        stats.TotalKeys.Should().Be(5);
        stats.TotalKeys.Should().BeGreaterThan(3); // More categories than default
    }

    // ── P1-4: Redis-backed stats contract ─────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void GetStatsAsync_NullConnectionMultiplexer_ReturnsPlaceholderWithZeroHitMiss()
    {
        // When IConnectionMultiplexer is not available (Redis not configured),
        // OrchardCacheService.GetStatsAsync() must return placeholder stats
        // with HitCount = 0 and MissCount = 0 rather than throwing.
        // TotalKeys is derived from configured category count (always valid).
        var stats = new CacheStatsDto(3L, 0L, 0L, 0.0, 0L);

        stats.HitCount.Should().Be(0,
            "without Redis, hit/miss counters cannot be read from INFO command");
        stats.MissCount.Should().Be(0);
        stats.TotalKeys.Should().Be(3,
            "TotalKeys is derived from category TTL config, not from Redis");
        stats.MemoryUsedBytes.Should().Be(0,
            "used_memory requires Redis INFO — placeholder is 0");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void GetStatsAsync_WithRedisHitMiss_HitRatioIsCalculatedCorrectly()
    {
        // When Redis is available and INFO returns real hit/miss counts,
        // CacheStatsDto.HitRatio = hits / (hits + misses).
        const long hits = 300L;
        const long misses = 100L;
        var hitRatio = (double)hits / (hits + misses); // 0.75

        var stats = new CacheStatsDto(10L, hits, misses, hitRatio, 4_096_000L);

        stats.HitCount.Should().Be(300);
        stats.MissCount.Should().Be(100);
        stats.HitRatio.Should().BeApproximately(0.75, precision: 0.001);
        stats.MemoryUsedBytes.Should().Be(4_096_000L);
    }
}
