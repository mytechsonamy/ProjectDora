using FluentAssertions;
using ProjectDora.AdminPanel;

namespace ProjectDora.Modules.Tests.AdminPanel;

public class PermissionsTests
{
    private readonly Permissions _sut = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-101")]
    public async Task AdminPanel_Permissions_GetPermissions_ReturnsAllDefinedPermissions()
    {
        // Act
        var permissions = await _sut.GetPermissionsAsync();

        // Assert
        permissions.Should().HaveCount(5);
        permissions.Should().Contain(p => p.Name == "AccessAdminPanel");
        permissions.Should().Contain(p => p.Name == "AdminPanel.ManageMenus");
        permissions.Should().Contain(p => p.Name == "Media.Upload");
        permissions.Should().Contain(p => p.Name == "Media.Delete");
        permissions.Should().Contain(p => p.Name == "Media.ManageFolders");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-101")]
    public void AdminPanel_Permissions_AccessAdminPanel_IsSecurityCritical()
    {
        // Assert
        Permissions.AccessAdminPanel.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-101")]
    public void AdminPanel_Permissions_DefaultStereotypes_AdministratorHasAllPermissions()
    {
        // Act
        var stereotypes = _sut.GetDefaultStereotypes();
        var admin = stereotypes.First(s => s.Name == "Administrator");

        // Assert
        admin.Permissions.Should().HaveCount(5);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-101")]
    public void AdminPanel_Permissions_DefaultStereotypes_EditorHasLimitedPermissions()
    {
        // Act
        var stereotypes = _sut.GetDefaultStereotypes();
        var editor = stereotypes.First(s => s.Name == "Editor");

        // Assert
        editor.Permissions.Should().Contain(Permissions.AccessAdminPanel);
        editor.Permissions.Should().Contain(Permissions.ManageMedia);
        editor.Permissions.Should().NotContain(Permissions.ManageAdminMenus);
        editor.Permissions.Should().NotContain(Permissions.DeleteMedia);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-102")]
    public void AdminPanel_Permissions_ManageMenus_IsNotSecurityCritical()
    {
        // Assert
        Permissions.ManageAdminMenus.IsSecurityCritical.Should().BeFalse();
    }
}
