using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Infrastructure;

public class InfrastructureDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Dto_TenantDto_PreservesProperties()
    {
        var created = DateTime.UtcNow;

        var dto = new TenantDto(
            "kosgeb-ankara",
            "Postgres",
            "Host=localhost;Database=kosgeb_ankara",
            "Running",
            "ankara",
            created,
            null);

        dto.TenantName.Should().Be("kosgeb-ankara");
        dto.DatabaseProvider.Should().Be("Postgres");
        dto.State.Should().Be("Running");
        dto.RequestUrlPrefix.Should().Be("ankara");
        dto.SuspendedUtc.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Dto_TenantDto_SuspendedState()
    {
        var created = DateTime.UtcNow.AddDays(-30);
        var suspended = DateTime.UtcNow;

        var dto = new TenantDto(
            "kosgeb-izmir",
            "Postgres",
            string.Empty,
            "Disabled",
            "izmir",
            created,
            suspended);

        dto.State.Should().Be("Disabled");
        dto.SuspendedUtc.Should().NotBeNull();
        dto.SuspendedUtc.Should().Be(suspended);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Dto_CreateTenantCommand_DefaultsToPgProvider()
    {
        var command = new CreateTenantCommand("kosgeb-konya");

        command.TenantName.Should().Be("kosgeb-konya");
        command.DatabaseProvider.Should().Be("Postgres");
        command.RequestUrlPrefix.Should().BeNull();
        command.ConnectionString.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public void Infrastructure_Dto_CacheStatsDto_TrackHitRatio()
    {
        var stats = new CacheStatsDto(
            TotalKeys: 500,
            HitCount: 800,
            MissCount: 200,
            HitRatio: 0.80,
            MemoryUsedBytes: 1_048_576);

        stats.TotalKeys.Should().Be(500);
        stats.HitCount.Should().Be(800);
        stats.HitRatio.Should().BeApproximately(0.80, 0.01);
        stats.MemoryUsedBytes.Should().Be(1_048_576);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public void Infrastructure_Dto_CacheSettingsDto_DefaultTtls()
    {
        var categoryTtls = new Dictionary<string, int>
        {
            { "content", 300 },
            { "permissions", 600 },
        };

        var settings = new CacheSettingsDto(
            IsEnabled: true,
            DefaultTtlSeconds: 300,
            CategoryTtls: categoryTtls);

        settings.IsEnabled.Should().BeTrue();
        settings.DefaultTtlSeconds.Should().Be(300);
        settings.CategoryTtls.Should().ContainKey("content");
        settings.CategoryTtls["permissions"].Should().Be(600);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public void Infrastructure_Dto_UpdateCacheSettingsCommand_AllNullByDefault()
    {
        var command = new UpdateCacheSettingsCommand();

        command.IsEnabled.Should().BeNull();
        command.DefaultTtlSeconds.Should().BeNull();
        command.CategoryTtls.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void Infrastructure_Dto_RecipeImportResultDto_SuccessResult()
    {
        var result = new RecipeImportResultDto(
            Success: true,
            Error: null,
            StepsExecuted: 7,
            Duration: TimeSpan.FromMilliseconds(250));

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.StepsExecuted.Should().Be(7);
        result.Duration.TotalMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void Infrastructure_Dto_RecipeImportResultDto_FailureResult()
    {
        var result = new RecipeImportResultDto(
            Success: false,
            Error: "Invalid JSON in recipe file",
            StepsExecuted: 0,
            Duration: TimeSpan.Zero);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("JSON");
        result.StepsExecuted.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1003")]
    public void Infrastructure_Dto_RecipeSummaryDto_BuiltInRecipe()
    {
        var dto = new RecipeSummaryDto(
            "kosgeb-bootstrap",
            "KOSGEB platform initial configuration with content types, roles, and settings",
            "1.0.0",
            IsBuiltIn: true);

        dto.Name.Should().Be("kosgeb-bootstrap");
        dto.IsBuiltIn.Should().BeTrue();
        dto.Description.Should().Contain("KOSGEB");
    }
}
