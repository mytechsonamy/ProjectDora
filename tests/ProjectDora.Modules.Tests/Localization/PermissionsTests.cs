using FluentAssertions;
using ProjectDora.Localization;

namespace ProjectDora.Modules.Tests.Localization;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Permissions_DefinesSixPermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(6);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Permissions_ManageCulturesIsSecurityCritical()
    {
        Permissions.ManageCultures.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-802")]
    public void Localization_Permissions_CreateTranslationIsNotSecurityCritical()
    {
        Permissions.CreateTranslation.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-805")]
    public void Localization_Permissions_ViewTranslationsIsNotSecurityCritical()
    {
        Permissions.ViewTranslations.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(6);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-808")]
    public void Localization_Permissions_AllPermissionNamesUseLocalizationPrefix()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().AllSatisfy(p => p.Name.Should().StartWith("Localization."));
    }
}
