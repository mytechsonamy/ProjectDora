using FluentAssertions;
using ProjectDora.UserManagement;

namespace ProjectDora.Modules.Tests.UserManagement;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-604")]
    public void UserManagement_Permissions_DefinesFourPermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(4);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public void UserManagement_Permissions_ManageUsersIsSecurityCritical()
    {
        Permissions.ManageUsers.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-602")]
    public void UserManagement_Permissions_ManageRolesIsSecurityCritical()
    {
        Permissions.ManageRoles.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-603")]
    public void UserManagement_Permissions_AssignRolesIsSecurityCritical()
    {
        Permissions.AssignRoles.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-604")]
    public void UserManagement_Permissions_ViewUsersIsNotSecurityCritical()
    {
        Permissions.ViewUsers.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-604")]
    public void UserManagement_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(4);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-604")]
    public void UserManagement_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }
}
