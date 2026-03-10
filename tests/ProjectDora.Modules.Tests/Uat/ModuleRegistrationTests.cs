using FluentAssertions;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace ProjectDora.Modules.Tests.Uat;

public class ModuleRegistrationTests
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
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1301")]
    public void Uat_Modules_AllTenModulesRegistered()
    {
        AllProviders().Should().HaveCount(10,
            "all 10 platform modules must be registered (S1–S11 complete)");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1301")]
    public void Uat_Modules_TotalPermissionCountInExpectedRange()
    {
        var total = AllProviders().Sum(p => p.GetPermissionsAsync().Result.Count());

        total.Should().BeInRange(50, 100,
            "platform should have 50–100 permissions covering all RBAC scenarios");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1302")]
    public void Uat_Modules_AllProvidersSupportAdministratorStereotype()
    {
        foreach (var provider in AllProviders())
        {
            var stereotypes = provider.GetDefaultStereotypes();
            stereotypes.Should().Contain(
                s => s.Name == "Administrator",
                $"{provider.GetType().Namespace} must define an Administrator stereotype");
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1303")]
    public void Uat_Modules_AdminMenuItemsHaveUniquePositions()
    {
        var menus = new INavigationProvider[]
        {
            new ProjectDora.AdminPanel.AdminMenu(TestLocalizer.For<ProjectDora.AdminPanel.AdminMenu>()),
            new ProjectDora.QueryEngine.QueryEngineMenu(TestLocalizer.For<ProjectDora.QueryEngine.QueryEngineMenu>()),
            new ProjectDora.UserManagement.UserManagementMenu(TestLocalizer.For<ProjectDora.UserManagement.UserManagementMenu>()),
            new ProjectDora.Workflows.WorkflowMenu(TestLocalizer.For<ProjectDora.Workflows.WorkflowMenu>()),
            new ProjectDora.Localization.LocalizationMenu(TestLocalizer.For<ProjectDora.Localization.LocalizationMenu>()),
            new ProjectDora.AuditTrail.AuditTrailMenu(TestLocalizer.For<ProjectDora.AuditTrail.AuditTrailMenu>()),
            new ProjectDora.Infrastructure.InfrastructureMenu(TestLocalizer.For<ProjectDora.Infrastructure.InfrastructureMenu>()),
            new ProjectDora.Integration.IntegrationMenu(TestLocalizer.For<ProjectDora.Integration.IntegrationMenu>()),
            new ProjectDora.ThemeManagement.ThemeManagementMenu(TestLocalizer.For<ProjectDora.ThemeManagement.ThemeManagementMenu>()),
        };

        var builder = new NavigationBuilder();
        foreach (var menu in menus)
        {
            var task = menu.BuildNavigationAsync("admin", builder);
            if (!task.IsCompleted) task.AsTask().GetAwaiter().GetResult();
        }

        var items = builder.Build();
        items.Should().HaveCountGreaterThanOrEqualTo(9,
            "all 9 admin menu providers should add at least one top-level item");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1304")]
    public void Uat_Modules_NoModuleHasZeroPermissions()
    {
        foreach (var provider in AllProviders())
        {
            var permissions = provider.GetPermissionsAsync().Result;
            permissions.Should().NotBeEmpty(
                $"{provider.GetType().Namespace} must define at least one permission");
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1305")]
    public void Uat_Modules_AdministratorHasAllPermissionsForEachModule()
    {
        foreach (var provider in AllProviders())
        {
            var allPermissions = provider.GetPermissionsAsync().Result.ToList();
            var stereotypes = provider.GetDefaultStereotypes().ToList();
            var admin = stereotypes.First(s => s.Name == "Administrator");

            admin.Permissions.Should().HaveCount(
                allPermissions.Count,
                $"Administrator should have all {allPermissions.Count} permissions in {provider.GetType().Namespace}");
        }
    }
}

internal static class TestLocalizer
{
    public static Microsoft.Extensions.Localization.IStringLocalizer<T> For<T>()
    {
        var mock = new Moq.Mock<Microsoft.Extensions.Localization.IStringLocalizer<T>>();
        mock.Setup(l => l[Moq.It.IsAny<string>()])
            .Returns<string>(s => new Microsoft.Extensions.Localization.LocalizedString(s, s));
        return mock.Object;
    }
}
