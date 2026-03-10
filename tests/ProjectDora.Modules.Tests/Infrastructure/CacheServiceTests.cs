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
}
