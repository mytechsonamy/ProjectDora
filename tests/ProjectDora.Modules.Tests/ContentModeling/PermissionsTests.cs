using FluentAssertions;
using ProjectDora.ContentModeling;

namespace ProjectDora.Modules.Tests.ContentModeling;

public class PermissionsTests
{
    private readonly Permissions _sut = new();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public async Task ContentModeling_Permissions_GetPermissions_ReturnsAllDefinedPermissions()
    {
        // Act
        var permissions = await _sut.GetPermissionsAsync();

        // Assert
        permissions.Should().HaveCount(5);
        permissions.Should().Contain(p => p.Name == "ContentModeling.Manage");
        permissions.Should().Contain(p => p.Name == "ContentModeling.Create");
        permissions.Should().Contain(p => p.Name == "ContentModeling.Edit");
        permissions.Should().Contain(p => p.Name == "ContentModeling.Delete");
        permissions.Should().Contain(p => p.Name == "ContentModeling.View");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Permissions_ManageContentTypes_IsSecurityCritical()
    {
        // Assert
        Permissions.ManageContentTypes.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Permissions_DeleteContentTypes_IsSecurityCritical()
    {
        // Assert
        Permissions.DeleteContentTypes.IsSecurityCritical.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Permissions_DefaultStereotypes_AdministratorHasAllPermissions()
    {
        // Act
        var stereotypes = _sut.GetDefaultStereotypes();
        var admin = stereotypes.First(s => s.Name == "Administrator");

        // Assert
        admin.Permissions.Should().HaveCount(5);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Permissions_DefaultStereotypes_EditorHasViewOnly()
    {
        // Act
        var stereotypes = _sut.GetDefaultStereotypes();
        var editor = stereotypes.First(s => s.Name == "Editor");

        // Assert
        editor.Permissions.Should().ContainSingle()
            .Which.Name.Should().Be("ContentModeling.View");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Permissions_DefaultStereotypes_AuthorHasViewOnly()
    {
        // Act
        var stereotypes = _sut.GetDefaultStereotypes();
        var author = stereotypes.First(s => s.Name == "Author");

        // Assert
        author.Permissions.Should().ContainSingle()
            .Which.Name.Should().Be("ContentModeling.View");
    }
}
