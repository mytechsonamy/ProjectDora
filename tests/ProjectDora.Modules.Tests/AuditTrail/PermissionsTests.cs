using FluentAssertions;
using ProjectDora.AuditTrail;

namespace ProjectDora.Modules.Tests.AuditTrail;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-907")]
    public void AuditTrail_Permissions_DefinesSevenPermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(7);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-907")]
    public void AuditTrail_Permissions_ManageSettingsIsSecurityCritical()
    {
        Permissions.ManageAuditSettings.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-907")]
    public void AuditTrail_Permissions_RollbackIsSecurityCritical()
    {
        Permissions.Rollback.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-907")]
    public void AuditTrail_Permissions_PurgeIsSecurityCritical()
    {
        Permissions.Purge.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-902")]
    public void AuditTrail_Permissions_ViewAuditTrailIsNotSecurityCritical()
    {
        Permissions.ViewAuditTrail.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-904")]
    public void AuditTrail_Permissions_ViewDiffIsNotSecurityCritical()
    {
        Permissions.ViewDiff.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-907")]
    public void AuditTrail_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(7);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-907")]
    public void AuditTrail_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }
}
