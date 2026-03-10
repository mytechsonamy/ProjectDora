using FluentAssertions;
using ProjectDora.Integration;

namespace ProjectDora.Modules.Tests.Integration;

public class PermissionsTests
{
    private readonly Permissions _permissions = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Permissions_DefinesSixPermissions()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().HaveCount(6);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Permissions_ManageApiClientsIsSecurityCritical()
    {
        Permissions.ManageApiClients.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1103")]
    public void Integration_Permissions_ViewWebhooksIsNotSecurityCritical()
    {
        Permissions.ViewWebhooks.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void Integration_Permissions_AccessApiIsNotSecurityCritical()
    {
        Permissions.AccessApi.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1104")]
    public void Integration_Permissions_PublishQueryApiIsNotSecurityCritical()
    {
        Permissions.PublishQueryApi.IsSecurityCritical.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Permissions_AdministratorHasAllPermissions()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        var admin = stereotypes.Should().ContainSingle(s => s.Name == "Administrator").Subject;
        admin.Permissions.Should().HaveCount(6);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Permissions_OnlyAdministratorStereotype()
    {
        var stereotypes = _permissions.GetDefaultStereotypes();

        stereotypes.Should().ContainSingle();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Permissions_AllPermissionNamesUseIntegrationPrefix()
    {
        var result = _permissions.GetPermissionsAsync().Result;

        result.Should().AllSatisfy(p => p.Name.Should().StartWith("Integration."));
    }
}
