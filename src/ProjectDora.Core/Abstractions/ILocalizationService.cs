namespace ProjectDora.Core.Abstractions;

public interface ILocalizationService
{
    Task<IReadOnlyList<CultureDto>> GetSupportedCulturesAsync();
    Task<CultureDto> ActivateCultureAsync(string culture);
    Task DeactivateCultureAsync(string culture);
    Task SetDefaultCultureAsync(string culture);
    Task<CultureSettingsDto> GetCultureSettingsAsync();

    Task<TranslationStatusDto> GetTranslationStatusAsync(string contentItemId);
    Task<IReadOnlyList<TranslationStatusDto>> ListTranslationStatusAsync(string contentType);
}

public interface ICultureService
{
    string GetCurrentCulture();
    bool IsRightToLeft(string culture);
    string GetCultureDisplayName(string culture);
}

public record CultureDto(
    string Culture,
    string DisplayName,
    bool IsDefault,
    bool IsRightToLeft,
    DateTime ActivatedUtc);

public record CultureSettingsDto(
    string DefaultCulture,
    IReadOnlyList<string> SupportedCultures,
    bool OmitDefaultCulturePrefix);

public record TranslationStatusDto(
    string ContentItemId,
    string ContentType,
    IReadOnlyList<CultureTranslationDto> Cultures);

public record CultureTranslationDto(
    string Culture,
    string Status,
    string? TranslatedContentItemId,
    string? TranslatedSlug);

public record ActivateCultureCommand(string Culture, bool SetAsDefault = false);
