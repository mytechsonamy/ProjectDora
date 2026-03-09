using FluentAssertions;
using ProjectDora.Workflows;

namespace ProjectDora.Modules.Tests.Workflows;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Permissions_DefinesThreePermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Permissions_ManageIsSecurityCritical()
    {
        Permissions.ManageWorkflows.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-706")]
    public void Workflow_Permissions_ExecuteIsNotSecurityCritical()
    {
        Permissions.ExecuteWorkflows.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-708")]
    public void Workflow_Permissions_ViewIsNotSecurityCritical()
    {
        Permissions.ViewWorkflows.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }
}
