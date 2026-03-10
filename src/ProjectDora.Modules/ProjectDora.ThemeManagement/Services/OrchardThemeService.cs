using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Environment.Extensions;
using OrchardCore.Templates.Services;
using OrchardCore.Themes.Services;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.ThemeManagement.Services;

public sealed class OrchardThemeService : ProjectDora.Core.Abstractions.IThemeService
{
    private readonly IExtensionManager _extensionManager;
    private readonly ISiteThemeService _siteThemeService;
    private readonly TemplatesManager _templatesManager;

    public OrchardThemeService(
        IExtensionManager extensionManager,
        ISiteThemeService siteThemeService,
        TemplatesManager templatesManager)
    {
        _extensionManager = extensionManager;
        _siteThemeService = siteThemeService;
        _templatesManager = templatesManager;
    }

    public Task<IReadOnlyList<ThemeDto>> ListAvailableAsync()
    {
        var extensions = _extensionManager.GetExtensions();
        IReadOnlyList<ThemeDto> result = extensions
            .Where(e => e.IsTheme())
            .Select(e => new ThemeDto(
                e.Id,
                e.Manifest.Name,
                e.Manifest.Description,
                e.Manifest.Version,
                e.Manifest.Author,
                IsActive: false,
                Array.Empty<string>()))
            .ToList();
        return Task.FromResult(result);
    }

    public async Task<ThemeDto?> GetActiveAsync()
    {
        var theme = await _siteThemeService.GetSiteThemeAsync();
        if (theme is null)
        {
            return null;
        }

        return new ThemeDto(
            theme.Id,
            theme.Manifest.Name,
            theme.Manifest.Description,
            theme.Manifest.Version,
            theme.Manifest.Author,
            IsActive: true,
            Array.Empty<string>());
    }

    public async Task<ThemeDto> ActivateAsync(string themeId)
    {
        await _siteThemeService.SetSiteThemeAsync(themeId);

        var extension = _extensionManager.GetExtension(themeId);
        if (extension is null)
        {
            return new ThemeDto(
                themeId, themeId, string.Empty, "1.0.0", string.Empty,
                IsActive: true, Array.Empty<string>());
        }

        return new ThemeDto(
            extension.Id,
            extension.Manifest.Name,
            extension.Manifest.Description,
            extension.Manifest.Version,
            extension.Manifest.Author,
            IsActive: true,
            Array.Empty<string>());
    }

    public async Task<ThemeTemplateDto> GetTemplateAsync(string themeId, string templatePath)
    {
        var doc = await _templatesManager.GetTemplatesDocumentAsync();
        var key = $"{themeId}/{templatePath}";
        var isCustomized = doc.Templates.TryGetValue(key, out var template);

        return new ThemeTemplateDto(
            themeId,
            templatePath,
            template?.Content ?? string.Empty,
            IsCustomized: isCustomized,
            DateTime.UtcNow);
    }

    public async Task<ThemeTemplateDto> SaveTemplateAsync(string themeId, SaveTemplateCommand command)
    {
        var key = $"{themeId}/{command.TemplatePath}";
        var template = new OrchardCore.Templates.Models.Template
        {
            Content = command.Content,
        };

        await _templatesManager.UpdateTemplateAsync(key, template);

        return new ThemeTemplateDto(
            themeId,
            command.TemplatePath,
            command.Content,
            IsCustomized: true,
            DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<ThemeTemplateDto>> ListTemplatesAsync(string themeId)
    {
        var doc = await _templatesManager.GetTemplatesDocumentAsync();
        var prefix = $"{themeId}/";

        IReadOnlyList<ThemeTemplateDto> result = doc.Templates
            .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new ThemeTemplateDto(
                themeId,
                kvp.Key[prefix.Length..],
                kvp.Value.Content,
                IsCustomized: true,
                DateTime.UtcNow))
            .ToList();

        return result;
    }

    public async Task ResetTemplateAsync(string themeId, string templatePath)
    {
        var key = $"{themeId}/{templatePath}";
        await _templatesManager.RemoveTemplateAsync(key);
    }
}
