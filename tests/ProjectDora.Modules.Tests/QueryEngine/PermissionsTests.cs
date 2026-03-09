using FluentAssertions;
using ProjectDora.QueryEngine;

namespace ProjectDora.Modules.Tests.QueryEngine;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Permissions_DefinesFourPermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(4);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Permissions_ManageIsSecurityCritical()
    {
        Permissions.ManageQueries.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Permissions_DeleteIsSecurityCritical()
    {
        Permissions.DeleteQueries.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Permissions_ExecuteIsNotSecurityCritical()
    {
        Permissions.ExecuteQueries.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(4);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-501")]
    public void QueryEngine_Permissions_AnalystHasOnlyExecute()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var analyst = stereotypes.Should().ContainSingle(s => s.Name == "Analyst").Subject;
        analyst.Permissions.Should().ContainSingle()
            .Which.Name.Should().Be("QueryEngine.Execute");
    }
}
