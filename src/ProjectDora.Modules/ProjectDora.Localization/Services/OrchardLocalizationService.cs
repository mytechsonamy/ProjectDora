using System.Globalization;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Localization.Services;

public sealed class OrchardLocalizationService : ProjectDora.Core.Abstractions.ILocalizationService
{
    private readonly OrchardCore.Localization.ILocalizationService _orchardLocalizationService;

    public OrchardLocalizationService(
        OrchardCore.Localization.ILocalizationService orchardLocalizationService)
    {
        _orchardLocalizationService = orchardLocalizationService;
    }

    public async Task<IReadOnlyList<CultureDto>> GetSupportedCulturesAsync()
    {
        var defaultCulture = await _orchardLocalizationService.GetDefaultCultureAsync();
        var supportedCultures = await _orchardLocalizationService.GetSupportedCulturesAsync();

        return supportedCultures.Select(c => new CultureDto(
            c,
            TryGetDisplayName(c),
            string.Equals(c, defaultCulture, StringComparison.OrdinalIgnoreCase),
            IsRtl(c),
            DateTime.UtcNow)).ToList();
    }

    public async Task<CultureDto> ActivateCultureAsync(string culture)
    {
        var defaultCulture = await _orchardLocalizationService.GetDefaultCultureAsync();

        return new CultureDto(
            culture,
            TryGetDisplayName(culture),
            string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase),
            IsRtl(culture),
            DateTime.UtcNow);
    }

    public async Task DeactivateCultureAsync(string culture)
    {
        var defaultCulture = await _orchardLocalizationService.GetDefaultCultureAsync();

        if (string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot deactivate the default culture '{culture}'.");
        }
    }

    public Task SetDefaultCultureAsync(string culture)
    {
        return Task.CompletedTask;
    }

    public async Task<CultureSettingsDto> GetCultureSettingsAsync()
    {
        var defaultCulture = await _orchardLocalizationService.GetDefaultCultureAsync();
        var supportedCultures = await _orchardLocalizationService.GetSupportedCulturesAsync();

        return new CultureSettingsDto(
            defaultCulture,
            supportedCultures.ToList(),
            OmitDefaultCulturePrefix: true);
    }

    public Task<TranslationStatusDto> GetTranslationStatusAsync(string contentItemId)
    {
        var status = new TranslationStatusDto(
            contentItemId,
            string.Empty,
            Array.Empty<CultureTranslationDto>());

        return Task.FromResult(status);
    }

    public Task<IReadOnlyList<TranslationStatusDto>> ListTranslationStatusAsync(string contentType)
    {
        IReadOnlyList<TranslationStatusDto> result = Array.Empty<TranslationStatusDto>();
        return Task.FromResult(result);
    }

    private static string TryGetDisplayName(string culture)
    {
        try
        {
            return CultureInfo.GetCultureInfo(culture).DisplayName;
        }
        catch
        {
            return culture;
        }
    }

    private static bool IsRtl(string culture)
    {
        try
        {
            return new CultureInfo(culture).TextInfo.IsRightToLeft;
        }
        catch
        {
            return false;
        }
    }
}
