using FluentAssertions;
using ProjectDora.ThemeManagement;

namespace ProjectDora.Modules.Tests.ThemeManagement;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Permissions_DefinesThreePermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Permissions_ManageThemesIsSecurityCritical()
    {
        Permissions.ManageThemes.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void ThemeManagement_Permissions_EditTemplatesIsNotSecurityCritical()
    {
        Permissions.EditTemplates.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Permissions_ViewThemesIsNotSecurityCritical()
    {
        Permissions.ViewThemes.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }
}
