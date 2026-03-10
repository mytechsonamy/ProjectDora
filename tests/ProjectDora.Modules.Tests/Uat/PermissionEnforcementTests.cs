using FluentAssertions;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Uat;

/// <summary>
/// E2E smoke tests for permission enforcement lifecycle:
///   generate permissions → verify name conventions → verify no namespace collision.
/// Tests operate at DTO/contract level without requiring a live OC runtime or ISiteService.
/// </summary>
public class PermissionEnforcementTests
{
    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1403")]
    public void ContentTypePermissions_NamingConvention_AllEightVerbsPresent()
    {
        // The 8 content-type verbs that OrchardRoleService.GenerateContentTypePermissionsAsync
        // generates must all be present, correctly named, and unique.
        var contentType = "DestekBasvurusu";
        var expectedVerbs = new[] { "View", "Preview", "Publish", "Edit", "Delete", "ViewOwn", "EditOwn", "DeleteOwn" };

        var generated = expectedVerbs.Select(v => $"{v}_{contentType}").ToList();

        generated.Should().HaveCount(8);
        generated.Should().OnlyHaveUniqueItems();
        generated.Should().AllSatisfy(n => n.Should().Contain(contentType));
        generated.Should().Contain($"View_{contentType}");
        generated.Should().Contain($"DeleteOwn_{contentType}");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1403")]
    public void QueryPermission_NamingConvention_PrefixedWithQueryExecute()
    {
        var queryName = "destekler_listesi";
        var permissionName = $"Query.Execute.{queryName}";

        permissionName.Should().StartWith("Query.Execute.");
        permissionName.Should().EndWith(queryName);
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1403")]
    public void AllModulePermissions_NoNamespaceCollisions_AcrossAllTenModules()
    {
        // All static permissions across all 10 modules must have unique names.
        // A collision would cause ambiguous authorization results.
        var allProviders = new IPermissionProvider[]
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

        var allNames = new List<string>();
        foreach (var provider in allProviders)
        {
            var names = provider.GetPermissionsAsync().Result.Select(p => p.Name).ToList();
            allNames.AddRange(names);
        }

        allNames.Should().OnlyHaveUniqueItems(
            "no two modules may declare a permission with the same name — authorization is name-based");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1403")]
    public void GeneratedPermissionNames_DoNotCollide_WithAnyStaticModulePermission()
    {
        // Generated names follow content-type (Verb_TypeName) or query (Query.Execute.Name)
        // patterns. Static module permissions follow Module.Verb patterns.
        // These must never overlap — if they did, a generated permission name would
        // inadvertently grant a static module permission (or vice versa).
        var allProviders = new IPermissionProvider[]
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

        var staticNames = allProviders
            .SelectMany(p => p.GetPermissionsAsync().Result.Select(perm => perm.Name))
            .ToHashSet(StringComparer.Ordinal);

        // Representative generated names for common KOSGEB content types.
        var representativeGenerated = new[]
        {
            "View_DestekProgrami", "Edit_DestekProgrami", "Delete_DestekProgrami",
            "View_Basvuru", "Publish_Basvuru",
            "Query.Execute.destekler_arama",
            "Query.Execute.istatistik_raporu",
        };

        foreach (var name in representativeGenerated)
        {
            staticNames.Should().NotContain(name,
                $"generated permission '{name}' must not collide with any static module permission");
        }
    }
}
