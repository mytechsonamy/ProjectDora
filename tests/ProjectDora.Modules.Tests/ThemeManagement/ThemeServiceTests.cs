using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.ThemeManagement;

public class ThemeServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-401")]
    public void ThemeDto_AllFieldsPreserved()
    {
        var dto = new ThemeDto(
            "TheAgencyTheme",
            "The Agency Theme",
            "A modern responsive theme for KOSGEB",
            "2.1.0",
            "Orchard Core Team",
            IsActive: true,
            AvailableTemplates: new[] { "Layout/TheLayout", "Layout/Footer" });

        dto.ThemeId.Should().Be("TheAgencyTheme");
        dto.Name.Should().Be("The Agency Theme");
        dto.IsActive.Should().BeTrue();
        dto.AvailableTemplates.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-401")]
    public void ThemeTemplateDto_AllFieldsPreserved()
    {
        var dto = new ThemeTemplateDto(
            "TheAgencyTheme",
            "Layout/TheLayout",
            "<html>@RenderBody()</html>",
            IsCustomized: true,
            DateTime.UtcNow);

        dto.ThemeId.Should().Be("TheAgencyTheme");
        dto.TemplatePath.Should().Be("Layout/TheLayout");
        dto.IsCustomized.Should().BeTrue();
        dto.Content.Should().Contain("@RenderBody");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-401")]
    public void SaveTemplateCommand_ContentPreserved()
    {
        var cmd = new SaveTemplateCommand(
            "Layout/Header",
            "<header>KOSGEB</header>");

        cmd.TemplatePath.Should().Be("Layout/Header");
        cmd.Content.Should().Contain("KOSGEB");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-401")]
    public void ThemeTemplateDto_NotCustomized_EmptyContent()
    {
        var dto = new ThemeTemplateDto(
            "TheAgencyTheme",
            "Layout/Footer",
            string.Empty,
            IsCustomized: false,
            DateTime.UtcNow);

        dto.IsCustomized.Should().BeFalse();
        dto.Content.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-401")]
    public void ThemeDto_InactiveTheme_IsActiveFalse()
    {
        var dto = new ThemeDto(
            "OtherTheme",
            "Other Theme",
            string.Empty,
            "1.0.0",
            string.Empty,
            IsActive: false,
            AvailableTemplates: Array.Empty<string>());

        dto.IsActive.Should().BeFalse();
        dto.AvailableTemplates.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-401")]
    public void TemplateKey_Format_IsThemeIdSlashPath()
    {
        var themeId = "TheAgencyTheme";
        var path = "Layout/Header";
        var key = $"{themeId}/{path}";

        key.Should().Be("TheAgencyTheme/Layout/Header");
        key.Should().StartWith(themeId);
    }
}
