using OrchardCore.Security.Permissions;

namespace ProjectDora.Localization;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageCultures = new(
        "Localization.ManageCultures",
        "Activate and deactivate supported cultures for the tenant",
        isSecurityCritical: true);

    public static readonly Permission CreateTranslation = new(
        "Localization.CreateTranslation",
        "Create new language variants of content items");

    public static readonly Permission EditTranslation = new(
        "Localization.EditTranslation",
        "Edit existing language variant content");

    public static readonly Permission DeleteTranslation = new(
        "Localization.DeleteTranslation",
        "Delete a language variant of a content item");

    public static readonly Permission ManagePOFiles = new(
        "Localization.ManagePOFiles",
        "Upload, edit, and manage PO translation files");

    public static readonly Permission ViewTranslations = new(
        "Localization.ViewTranslations",
        "View available translations and translation status");

    private readonly IEnumerable<Permission> _allPermissions = new[]
    {
        ManageCultures,
        CreateTranslation,
        EditTranslation,
        DeleteTranslation,
        ManagePOFiles,
        ViewTranslations,
    };

    public Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        return Task.FromResult(_allPermissions);
    }

    public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
    {
        return new[]
        {
            new PermissionStereotype
            {
                Name = "Administrator",
                Permissions = _allPermissions,
            },
        };
    }
}
