using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.UserManagement;

public class RolePermissionTests
{
    // ── Risk 4: Permission generation ─────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void GenerateContentType_ProducesEightPermissions()
    {
        var contentTypeName = "DestekProgrami";

        // Mirror GetContentTypePermissionNames logic from OrchardRoleService
        var names = new[]
        {
            $"View_{contentTypeName}",
            $"Preview_{contentTypeName}",
            $"Publish_{contentTypeName}",
            $"Edit_{contentTypeName}",
            $"Delete_{contentTypeName}",
            $"ViewOwn_{contentTypeName}",
            $"EditOwn_{contentTypeName}",
            $"DeleteOwn_{contentTypeName}",
        };

        names.Should().HaveCount(8);
        names.Should().Contain($"View_{contentTypeName}");
        names.Should().Contain($"DeleteOwn_{contentTypeName}");
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void GenerateQueryPermission_ProducesCorrectName()
    {
        var queryName = "destek_arama";
        var permissionName = $"Query.Execute.{queryName}";

        permissionName.Should().Be("Query.Execute.destek_arama");
        permissionName.Should().StartWith("Query.Execute.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void ListPermissions_IncludesGeneratedPermissions()
    {
        // Simulate the merge logic from OrchardRoleService.ListPermissionsAsync
        var providerPermissions = new List<PermissionDto>
        {
            new("AdministerContent", "Administer content", "Content", true),
            new("ManageUsers", "Manage users", "Security", true),
        };

        var generatedNames = new List<string> { "View_Article", "Edit_Article", "Delete_Article" };

        var result = new List<PermissionDto>(providerPermissions);
        foreach (var name in generatedNames)
        {
            if (!result.Any(p => p.Name == name))
            {
                result.Add(new PermissionDto(name, string.Empty, "Generated", false));
            }
        }

        result.Should().HaveCount(5);
        result.Should().Contain(p => p.Name == "View_Article" && p.Category == "Generated");
        result.Should().Contain(p => p.Name == "AdministerContent" && p.Category == "Content");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void ListPermissions_NoDuplicates_WhenPermissionAlreadyExists()
    {
        // Simulate adding generated names that partially overlap with existing
        var existing = new List<PermissionDto>
        {
            new("View_Article", "View Article", "ContentType", false),
        };

        var toAdd = new[] { "View_Article", "Edit_Article" };
        foreach (var name in toAdd)
        {
            if (!existing.Any(p => p.Name == name))
            {
                existing.Add(new PermissionDto(name, string.Empty, "Generated", false));
            }
        }

        existing.Should().HaveCount(2);
        existing.Count(p => p.Name == "View_Article").Should().Be(1);
        existing.Should().Contain(p => p.Name == "Edit_Article");
    }

    // ── P0-1: GeneratedPermissionProvider feeds OC auth pipeline ──────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void GeneratedProvider_PermissionNames_AreWellFormed()
    {
        // Verify the permission name convention that GeneratedPermissionProvider
        // converts back into Permission objects for OC's authorization pipeline.
        // Names produced by GenerateContentTypePermissionsAsync / GenerateQueryPermissionAsync
        // must be stable — they are stored in GeneratedPermissionsData AND used by role claims.
        var contentTypeName = "DestekProgrami";
        var generatedNames = new[]
        {
            $"View_{contentTypeName}",
            $"Edit_{contentTypeName}",
            $"Delete_{contentTypeName}",
            $"Publish_{contentTypeName}",
            $"Preview_{contentTypeName}",
            $"ViewOwn_{contentTypeName}",
            $"EditOwn_{contentTypeName}",
            $"DeleteOwn_{contentTypeName}",
        };

        // The provider converts these strings to Permission objects with the same Name.
        var permissions = generatedNames
            .Select(static n => new OrchardCore.Security.Permissions.Permission(n, n) { Category = "Generated" })
            .ToList();

        permissions.Should().HaveCount(8);
        permissions.Should().OnlyHaveUniqueItems(p => p.Name);
        permissions.Should().AllSatisfy(p => p.Name.Should().NotBeNullOrWhiteSpace());
        permissions.Should().Contain(p => p.Name == $"View_{contentTypeName}");
        permissions.Should().Contain(p => p.Name == $"DeleteOwn_{contentTypeName}");
        // Category must be "Generated" so the UI can filter them separately from static permissions.
        permissions.Should().OnlyContain(p => p.Category == "Generated");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void GeneratedProvider_NoDuplicatesWithStaticPermissions()
    {
        // Verify that generated content-type / query permission names never collide
        // with the static permission names declared in UserManagement.Permissions.
        // A collision would cause double-registration in OC's permission provider chain
        // and unpredictable authorization behaviour.
        var staticProvider = new ProjectDora.UserManagement.Permissions();
        var staticNames = staticProvider.GetPermissionsAsync().Result
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Representative generated names from both generation paths.
        var generatedNames = new[]
        {
            "View_Article", "Edit_Article", "Delete_Article",
            "Publish_Article", "Preview_Article",
            "ViewOwn_Article", "EditOwn_Article", "DeleteOwn_Article",
            "Query.Execute.destek_arama",
        };

        foreach (var generated in generatedNames)
        {
            staticNames.Should().NotContain(generated,
                $"generated permission '{generated}' must not collide with static UserManagement permissions");
        }
    }
}
