# Sprint S11 — Theme Management

## Kapsam (Scope)
- Spec items: 4.1.4.1, 4.1.4.2, 4.1.4.3, 4.1.4.4, 4.1.4.5, 4.1.4.6
- Stories: US-401, US-402, US-403, US-404, US-405, US-406
- Cross-references: 4.1.1.1 (Admin Panel — theme activation UI in admin), 4.1.2 (Content Modeling — theme templates render content types), 4.1.10.8 (tenant-scoped theme configuration)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.4.1 | Platformun tema tabanlı bir yapıda sunulması ve yöneticilerin tema seçimi yapabilmesi | Theme-based platform presentation — administrators can select and activate themes from the admin panel; theme switch takes effect immediately without restart |
| 4.1.4.2 | Liquid şablon dili ile özelleştirilebilir tema yapısının desteklenmesi | Liquid template engine support — themes use Liquid (.liquid) template files for layout, content rendering, and UI components; Orchard Core's built-in Liquid support |
| 4.1.4.3 | Tema şablonlarının yönetim panelinden düzenlenebilmesi | In-browser template editing — admin panel provides a code editor (Monaco Editor) for editing Liquid template files directly via the web UI |
| 4.1.4.4 | Değiştirilmiş şablonların özgün sürüme sıfırlanabilmesi | Template reset to original — modified templates can be reset to the theme's original/default version with a single click |
| 4.1.4.5 | Tema şablonlarında kullanılmak üzere özelleştirilebilir CSS ve JavaScript dosyalarının desteklenmesi | Custom CSS/JS asset support — themes can include custom stylesheets and JavaScript files; assets bundled via Orchard Core's asset pipeline |
| 4.1.4.6 | Her kiracı için bağımsız tema seçimi ve özelleştirmesinin mümkün olması | Per-tenant theme isolation — each tenant can have its own active theme and independent template customizations; no theme configuration bleeds between tenants |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)
- **Dependency**: S01 (Admin Panel) — theme management UI lives in the admin panel
- **Dependency**: S09 (Infrastructure) — tenant isolation for per-tenant theme config
- **Constraint**: Liquid templates must be editable online without SSH/file system access
- **Constraint**: Theme files must not be overwritten on application restart (customizations persisted in DB)
- **Constraint**: Monaco Editor is large (CDN or bundled); must not slow admin panel load significantly

### RBAC Gereksinimleri (RBAC Requirements)

| Permission | Aciklama | Guvenlik Kriteri |
|-----------|----------|-----------------|
| ManageThemes | Activate/deactivate themes, manage templates | `IsSecurityCritical = true` |
| ViewThemeEditor | Read-only access to template editor | `IsSecurityCritical = false` |
| ResetTemplates | Reset customized templates to defaults | `IsSecurityCritical = false` |

## Teknik Kararlar (Technical Decisions)

| Karar | Tercih | Aciklama |
|-------|--------|----------|
| Theme discovery | `IExtensionManager.GetExtensions().Where(e.IsTheme())` | OC 2.x uses sync `GetExtensions()`, not async `LoadExtensionsAsync()` |
| Template storage | Orchard Core `IFileStore` (DB-backed for tenant isolation) | Not filesystem — ensures customizations survive deployments |
| Template editor | Monaco Editor (loaded via CDN in admin views) | VS Code-like editor in browser; supports Liquid syntax highlighting |
| Theme activation | `ISiteThemeService.SetSiteThemeAsync()` | Built-in OC service; theme switch = settings record update |

### IThemeService Interface (ProjectDora.Core.Abstractions)
```csharp
public interface IThemeService {
    Task<IReadOnlyList<ThemeDto>> ListAvailableAsync();
    Task<ThemeDto?> GetActiveAsync();
    Task<ThemeDto> ActivateAsync(string themeId);
    Task<ThemeTemplateDto> GetTemplateAsync(string themeId, string templatePath);
    Task<ThemeTemplateDto> SaveTemplateAsync(string themeId, SaveTemplateCommand command);
    Task<IReadOnlyList<ThemeTemplateDto>> ListTemplatesAsync(string themeId);
    Task ResetTemplateAsync(string themeId, string templatePath);
}
```

### OrchardThemeService Key Pattern
```csharp
// OC 2.x: use sync GetExtensions(), wrap in Task.FromResult
public Task<IReadOnlyList<ThemeDto>> ListAvailableAsync()
{
    var extensions = _extensionManager.GetExtensions();
    IReadOnlyList<ThemeDto> result = extensions
        .Where(e => e.IsTheme())
        .Select(e => new ThemeDto(...))
        .ToList();
    return Task.FromResult(result);
}
```

## Test Plani (Test Plan)

| Test ID | Kategori | Aciklama |
|---------|----------|----------|
| US-401-01 | Unit | `ManageThemes` permission is security-critical |
| US-401-02 | Unit | `ViewThemeEditor` and `ResetTemplates` are NOT security-critical |
| US-402-01 | Unit | `ListAvailableAsync()` returns non-empty list |
| US-402-02 | Unit | `GetActiveAsync()` returns theme with `IsActive = true` |
| US-403-01 | Unit | `GetTemplateAsync()` returns TemplatePath and Content |
| US-403-02 | Unit | `SaveTemplateAsync()` marks template as `IsCustomized = true` |
| US-404-01 | Unit | Administrator stereotype has all ThemeManagement permissions |
| US-404-02 | Unit | ThemeManagement menu item has unique position in admin nav |
| US-405-01 | Unit | `ThemeDto` record equality and immutability |
| US-406-01 | Unit | All 3 permissions defined (ManageThemes, ViewThemeEditor, ResetTemplates) |

## Sprint Sonucu (Sprint Outcome)
- **Tamamlanan**: ThemeManagement module skeleton — Permissions (3), AdminMenu, OrchardThemeService, all tests green
- **Test sayisi**: ~15 tests in `tests/ProjectDora.Modules.Tests/ThemeManagement/`
- **Sonraki**: S12 (Security hardening) can begin

## Dokümantasyon Notlari
> Kullanim kilavuzu ve teknik dokumantasyon icin:
- Theme activation: Admin → Theme Management → select theme → "Activate"
- Template editing: Admin → Theme Management → Templates → select file → Monaco Editor → Save
- Per-tenant: Each tenant has independent theme selection; SuperAdmin can set global default
- Template reset: Admin → Theme Management → Templates → select customized file → "Reset to Default"
