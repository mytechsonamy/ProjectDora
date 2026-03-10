namespace ProjectDora.Core.Abstractions;

public interface IThemeService
{
    Task<IReadOnlyList<ThemeDto>> ListAvailableAsync();
    Task<ThemeDto?> GetActiveAsync();
    Task<ThemeDto> ActivateAsync(string themeId);
    Task<ThemeTemplateDto> GetTemplateAsync(string themeId, string templatePath);
    Task<ThemeTemplateDto> SaveTemplateAsync(string themeId, SaveTemplateCommand command);
    Task<IReadOnlyList<ThemeTemplateDto>> ListTemplatesAsync(string themeId);
    Task ResetTemplateAsync(string themeId, string templatePath);
}

public record ThemeDto(
    string ThemeId,
    string Name,
    string Description,
    string Version,
    string Author,
    bool IsActive,
    IReadOnlyList<string> AvailableTemplates);

public record ThemeTemplateDto(
    string ThemeId,
    string TemplatePath,
    string Content,
    bool IsCustomized,
    DateTime LastModifiedUtc);

public record SaveTemplateCommand(
    string TemplatePath,
    string Content);
