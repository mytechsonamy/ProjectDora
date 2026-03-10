using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.ThemeManagement;

public class ThemeDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Dto_ThemeDto_PreservesProperties()
    {
        var templates = new[] { "Layout.liquid", "Content-DestekProgrami.liquid" };

        var dto = new ThemeDto(
            "kosgeb-theme",
            "KOSGEB Kurumsal Tema",
            "Resmi KOSGEB kurumsal web sitesi teması",
            "1.0.0",
            "Techsonamy",
            IsActive: true,
            templates);

        dto.ThemeId.Should().Be("kosgeb-theme");
        dto.Name.Should().Contain("KOSGEB");
        dto.IsActive.Should().BeTrue();
        dto.AvailableTemplates.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Dto_ThemeDto_InactiveTheme()
    {
        var dto = new ThemeDto(
            "thebootstrapper",
            "TheBootstrapper",
            "Default Orchard Core theme",
            "2.1.4",
            "Orchard Core Team",
            IsActive: false,
            Array.Empty<string>());

        dto.IsActive.Should().BeFalse();
        dto.Author.Should().Contain("Orchard");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void ThemeManagement_Dto_ThemeTemplateDto_CustomizedTemplate()
    {
        var content = "{% layout 'Layout' %}\n<h1>{{ Model.ContentItem.DisplayText }}</h1>";
        var modified = DateTime.UtcNow;

        var dto = new ThemeTemplateDto(
            "kosgeb-theme",
            "Content-DestekProgrami.liquid",
            content,
            IsCustomized: true,
            modified);

        dto.ThemeId.Should().Be("kosgeb-theme");
        dto.TemplatePath.Should().Contain("DestekProgrami");
        dto.Content.Should().Contain("ContentItem");
        dto.IsCustomized.Should().BeTrue();
        dto.LastModifiedUtc.Should().Be(modified);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void ThemeManagement_Dto_ThemeTemplateDto_DefaultTemplate()
    {
        var dto = new ThemeTemplateDto(
            "kosgeb-theme",
            "Layout.liquid",
            "<!DOCTYPE html><html>{{ Model.Content }}</html>",
            IsCustomized: false,
            DateTime.UtcNow);

        dto.IsCustomized.Should().BeFalse();
        dto.Content.Should().Contain("html");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void ThemeManagement_Dto_SaveTemplateCommand_PreservesLiquidContent()
    {
        var liquidContent = """
            {% assign item = Model.ContentItem %}
            <article class="kobi-destek-programi">
                <h2>{{ item.DisplayText }}</h2>
                <p>{{ item.Content.DestekProgramiPart.Aciklama.Text }}</p>
            </article>
            """;

        var command = new SaveTemplateCommand(
            "Content-KobiDestekProgrami.liquid",
            liquidContent);

        command.TemplatePath.Should().Contain("KobiDestekProgrami");
        command.Content.Should().Contain("kobi-destek-programi");
        command.Content.Should().Contain("DestekProgramiPart");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void ThemeManagement_Dto_SaveTemplateCommand_TurkishTemplateName()
    {
        var command = new SaveTemplateCommand(
            "Widget-DuyuruListesi.liquid",
            "<ul>{% for item in Model.ContentItems %}<li>{{ item.DisplayText }}</li>{% endfor %}</ul>");

        command.TemplatePath.Should().Contain("Duyuru");
        command.Content.Should().Contain("ContentItems");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ThemeManagement_Dto_ThemeDto_TurkishDisplayNamePreserved()
    {
        var dto = new ThemeDto(
            "kosgeb-theme",
            "KOSGEB KOBİ Destek Portalı Teması",
            "Şırnak ve Güneydoğu illeri için özel tasarım",
            "2.0.0",
            "Techsonamy Ltd.",
            true,
            Array.Empty<string>());

        dto.Name.Should().Contain("KOBİ");
        dto.Description.Should().Contain("Şırnak");
    }
}
