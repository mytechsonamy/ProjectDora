using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.UserManagement;

public class UserDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public void UserManagement_Dto_UserDto_CreatesWithRequiredProperties()
    {
        var dto = new UserDto(
            "user-001",
            "mehmet.yilmaz",
            "mehmet@kosgeb.gov.tr",
            "Mehmet Yılmaz",
            true,
            new List<string> { "Editor", "Analyst" },
            DateTime.UtcNow,
            null);

        dto.UserId.Should().Be("user-001");
        dto.UserName.Should().Be("mehmet.yilmaz");
        dto.Email.Should().Be("mehmet@kosgeb.gov.tr");
        dto.DisplayName.Should().Be("Mehmet Yılmaz");
        dto.Enabled.Should().BeTrue();
        dto.Roles.Should().HaveCount(2);
        dto.LastLoginUtc.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public void UserManagement_Dto_UserDto_TurkishCharactersPreserved()
    {
        var dto = new UserDto(
            "user-002",
            "seref.ozcaliskan",
            "seref@kosgeb.gov.tr",
            "Şeref Güngör Özçalışkan",
            true,
            Array.Empty<string>(),
            DateTime.UtcNow,
            null);

        dto.DisplayName.Should().Contain("Şeref");
        dto.DisplayName.Should().Contain("Güngör");
        dto.DisplayName.Should().Contain("Özçalışkan");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public void UserManagement_Dto_CreateUserCommand_DefaultsToNoRoles()
    {
        var command = new CreateUserCommand("test", "test@test.com", "Password123!");

        command.Roles.Should().BeNull();
        command.DisplayName.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public void UserManagement_Dto_ListUsersQuery_HasSensibleDefaults()
    {
        var query = new ListUsersQuery();

        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.Role.Should().BeNull();
        query.Enabled.Should().BeNull();
        query.SearchTerm.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-602")]
    public void UserManagement_Dto_RoleDto_CreatesWithPermissions()
    {
        var dto = new RoleDto(
            "Destek Yöneticisi",
            "KOSGEB destek programlarını yöneten personel rolü",
            new List<string> { "ContentManagement.Create", "ContentType.DestekProgrami.Edit" });

        dto.Name.Should().Be("Destek Yöneticisi");
        dto.Description.Should().Contain("KOSGEB");
        dto.Permissions.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-602")]
    public void UserManagement_Dto_CreateRoleCommand_DefaultsToNoPermissions()
    {
        var command = new CreateRoleCommand("TestRole");

        command.Description.Should().BeNull();
        command.Permissions.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-604")]
    public void UserManagement_Dto_PermissionDto_CreatesCorrectly()
    {
        var dto = new PermissionDto(
            "UserRolePermission.ManageUsers",
            "Create, edit, enable/disable, and delete users",
            "Security",
            true);

        dto.Name.Should().Be("UserRolePermission.ManageUsers");
        dto.IsSecurityCritical.Should().BeTrue();
        dto.Category.Should().Be("Security");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-605")]
    public void UserManagement_Dto_ContentTypePermissionDto_CreatesWithActions()
    {
        var dto = new ContentTypePermissionDto(
            "DestekProgrami",
            new List<string> { "View", "Edit", "Delete", "Copy" });

        dto.ContentTypeName.Should().Be("DestekProgrami");
        dto.Actions.Should().HaveCount(4);
        dto.Actions.Should().Contain("View");
        dto.Actions.Should().Contain("Copy");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-608")]
    public void UserManagement_Dto_UserDto_DisabledUser()
    {
        var dto = new UserDto(
            "user-003",
            "disabled.user",
            "disabled@kosgeb.gov.tr",
            null,
            false,
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-30));

        dto.Enabled.Should().BeFalse();
        dto.DisplayName.Should().BeNull();
        dto.LastLoginUtc.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-602")]
    public void UserManagement_Dto_UpdateRoleCommand_AllowsPartialUpdate()
    {
        var command = new UpdateRoleCommand(Description: "Updated description");

        command.Description.Should().Be("Updated description");
        command.Permissions.Should().BeNull();
    }
}
