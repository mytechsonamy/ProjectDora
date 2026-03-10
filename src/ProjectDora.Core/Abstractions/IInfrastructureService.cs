namespace ProjectDora.Core.Abstractions;

public interface ITenantService
{
    Task<TenantDto> CreateAsync(CreateTenantCommand command);
    Task<TenantDto?> GetAsync(string tenantName);
    Task<IReadOnlyList<TenantDto>> ListAsync();
    Task SuspendAsync(string tenantName);
    Task ResumeAsync(string tenantName);
    Task DeleteAsync(string tenantName);
}

public interface ICacheService
{
    Task<CacheStatsDto> GetStatsAsync();
    Task PurgeAsync(string? category = null);
    Task<CacheSettingsDto> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateCacheSettingsCommand command);
}

public interface IRecipeService
{
    Task<RecipeImportResultDto> ImportAsync(string recipeJson);
    Task<string> ExportAsync();
    Task<IReadOnlyList<RecipeSummaryDto>> ListAvailableAsync();
}

public record TenantDto(
    string TenantName,
    string DatabaseProvider,
    string ConnectionString,
    string State,
    string? RequestUrlPrefix,
    DateTime CreatedUtc,
    DateTime? SuspendedUtc);

public record CreateTenantCommand(
    string TenantName,
    string? RequestUrlPrefix = null,
    string DatabaseProvider = "Postgres",
    string? ConnectionString = null);

public record CacheStatsDto(
    long TotalKeys,
    long HitCount,
    long MissCount,
    double HitRatio,
    long MemoryUsedBytes);

public record CacheSettingsDto(
    bool IsEnabled,
    int DefaultTtlSeconds,
    IDictionary<string, int> CategoryTtls);

public record UpdateCacheSettingsCommand(
    bool? IsEnabled = null,
    int? DefaultTtlSeconds = null,
    IDictionary<string, int>? CategoryTtls = null);

public record RecipeImportResultDto(
    bool Success,
    string? Error,
    int StepsExecuted,
    TimeSpan Duration);

public record RecipeSummaryDto(
    string Name,
    string Description,
    string Version,
    bool IsBuiltIn);
