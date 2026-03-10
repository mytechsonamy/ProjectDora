using FluentAssertions;
using OrchardCore.Security.Permissions;

namespace ProjectDora.Modules.Tests.Security;

public class PermissionSecurityTests
{
    private static IPermissionProvider[] AllProviders() => new IPermissionProvider[]
    {
        new ProjectDora.AdminPanel.Permissions(),
        new ProjectDora.AuditTrail.Permissions(),
        new ProjectDora.ContentModeling.Permissions(),
        new ProjectDora.Infrastructure.Permissions(),
        new ProjectDora.Integration.Permissions(),
        new ProjectDora.Localization.Permissions(),
        new ProjectDora.QueryEngine.Permissions(),
        new ProjectDora.ThemeManagement.Permissions(),
        new ProjectDora.UserManagement.Permissions(),
        new ProjectDora.Workflows.Permissions(),
    };

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1202")]
    public void Security_Permissions_AllModulesHaveAtLeastOneSecurityCriticalPermission()
    {
        var providers = new IPermissionProvider[]
        {
            new ProjectDora.AdminPanel.Permissions(),
            new ProjectDora.AuditTrail.Permissions(),
            new ProjectDora.Infrastructure.Permissions(),
            new ProjectDora.ThemeManagement.Permissions(),
            new ProjectDora.UserManagement.Permissions(),
            new ProjectDora.Integration.Permissions(),
        };

        foreach (var provider in providers)
        {
            var permissions = provider.GetPermissionsAsync().Result;
            permissions.Should().Contain(
                p => p.IsSecurityCritical,
                $"{provider.GetType().Namespace} should have at least one security-critical permission");
        }
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1202")]
    public void Security_Permissions_NoPermissionNameCollisions()
    {
        var allPermissions = AllProviders()
            .SelectMany(p => p.GetPermissionsAsync().Result)
            .Select(p => p.Name)
            .ToList();

        allPermissions.Should().OnlyHaveUniqueItems("permission names must be globally unique to avoid RBAC bypass");
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1202")]
    public void Security_Permissions_AllPermissionsHaveDescriptions()
    {
        var allPermissions = AllProviders()
            .SelectMany(p => p.GetPermissionsAsync().Result)
            .ToList();

        allPermissions.Should().AllSatisfy(p =>
            p.Description.Should().NotBeNullOrWhiteSpace(
                $"permission '{p.Name}' must have a description for audit trail readability"));
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1202")]
    public void Security_Permissions_DestructiveOperationsAreSecurityCritical()
    {
        var destructivePermissions = new[]
        {
            ProjectDora.AuditTrail.Permissions.Purge,
            ProjectDora.AuditTrail.Permissions.Rollback,
            ProjectDora.Infrastructure.Permissions.ManageTenants,
            ProjectDora.Infrastructure.Permissions.PurgeCache,
            ProjectDora.Infrastructure.Permissions.ManageOpenId,
            ProjectDora.Infrastructure.Permissions.ManageSettings,
            ProjectDora.Integration.Permissions.ManageApiClients,
            ProjectDora.ThemeManagement.Permissions.ManageThemes,
            ProjectDora.UserManagement.Permissions.ManageUsers,
        };

        foreach (var permission in destructivePermissions)
        {
            permission.IsSecurityCritical.Should().BeTrue(
                $"'{permission.Name}' is a destructive operation and must be security-critical");
        }
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1203")]
    public void Security_Permissions_TotalPermissionCount()
    {
        var totalCount = AllProviders().Sum(p => p.GetPermissionsAsync().Result.Count());

        totalCount.Should().BeGreaterThanOrEqualTo(50,
            "the platform should have at least 50 granular permissions for proper RBAC");
    }
}
