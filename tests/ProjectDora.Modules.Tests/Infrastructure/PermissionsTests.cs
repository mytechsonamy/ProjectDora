using FluentAssertions;
using ProjectDora.Infrastructure;

namespace ProjectDora.Modules.Tests.Infrastructure;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Permissions_DefinesElevenPermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(11);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Permissions_ManageTenantsIsSecurityCritical()
    {
        Permissions.ManageTenants.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public void Infrastructure_Permissions_PurgeCacheIsSecurityCritical()
    {
        Permissions.PurgeCache.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1004")]
    public void Infrastructure_Permissions_ManageOpenIdIsSecurityCritical()
    {
        Permissions.ManageOpenId.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1007")]
    public void Infrastructure_Permissions_ManageSettingsIsSecurityCritical()
    {
        Permissions.ManageSettings.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Permissions_ViewTenantsIsNotSecurityCritical()
    {
        Permissions.ViewTenants.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1005")]
    public void Infrastructure_Permissions_ViewSitemapIsNotSecurityCritical()
    {
        Permissions.ViewSitemap.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(11);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void Infrastructure_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }
}
