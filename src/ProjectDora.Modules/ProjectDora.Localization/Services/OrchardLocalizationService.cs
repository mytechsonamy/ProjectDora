using System.Globalization;
using OrchardCore.ContentLocalization;
using OrchardCore.ContentLocalization.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Entities;
using OrchardCore.Localization;
using OrchardCore.Localization.Models;
using OrchardCore.Settings;
using ProjectDora.Core.Abstractions;
using YesSql;

namespace ProjectDora.Localization.Services;

public sealed class OrchardLocalizationService : ProjectDora.Core.Abstractions.ILocalizationService
{
    private readonly OrchardCore.Localization.ILocalizationService _orchardLocalizationService;
    private readonly ISiteService _siteService;
    private readonly IContentManager _contentManager;
    private readonly IContentLocalizationManager _contentLocalizationManager;
    private readonly ISession _session;

    public OrchardLocalizationService(
        OrchardCore.Localization.ILocalizationService orchardLocalizationService,
        ISiteService siteService,
        IContentManager contentManager,
        IContentLocalizationManager contentLocalizationManager,
        ISession session)
    {
        _orchardLocalizationService = orchardLocalizationService;
        _siteService = siteService;
        _contentManager = contentManager;
        _contentLocalizationManager = contentLocalizationManager;
        _session = session;
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

    public async Task SetDefaultCultureAsync(string culture)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<LocalizationSettings>(settings =>
        {
            settings.DefaultCulture = culture;
        });
        await _siteService.UpdateSiteSettingsAsync(site);
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

    public async Task<TranslationStatusDto> GetTranslationStatusAsync(string contentItemId)
    {
        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);
        if (contentItem is null)
        {
            return new TranslationStatusDto(contentItemId, string.Empty, Array.Empty<CultureTranslationDto>());
        }

        var locPart = contentItem.As<LocalizationPart>();
        if (locPart is null || string.IsNullOrEmpty(locPart.LocalizationSet))
        {
            return new TranslationStatusDto(
                contentItemId,
                contentItem.ContentType,
                Array.Empty<CultureTranslationDto>());
        }

        var localizedItems = await _contentLocalizationManager.GetItemsForSetAsync(locPart.LocalizationSet);
        var supportedCultures = await _orchardLocalizationService.GetSupportedCulturesAsync();

        var localizedByCulture = localizedItems
            .Where(i => i.As<LocalizationPart>() is not null)
            .ToDictionary(
                i => i.As<LocalizationPart>()!.Culture ?? string.Empty,
                i => i);

        var cultureTranslations = supportedCultures
            .Select(culture =>
            {
                localizedByCulture.TryGetValue(culture, out var translated);
                return new CultureTranslationDto(
                    culture,
                    translated is null ? "Missing" : "Translated",
                    translated?.ContentItemId,
                    null);
            })
            .ToList();

        return new TranslationStatusDto(contentItemId, contentItem.ContentType, cultureTranslations);
    }

    public async Task<IReadOnlyList<TranslationStatusDto>> ListTranslationStatusAsync(string contentType)
    {
        var contentItems = await _session
            .Query<ContentItem, ContentItemIndex>(x => x.ContentType == contentType && x.Latest)
            .ListAsync();

        var result = new List<TranslationStatusDto>();
        foreach (var item in contentItems)
        {
            var status = await GetTranslationStatusAsync(item.ContentItemId);
            result.Add(status);
        }

        return result;
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
