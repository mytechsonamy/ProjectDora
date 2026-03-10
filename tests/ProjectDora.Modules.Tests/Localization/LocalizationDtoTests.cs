using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Localization;

public class LocalizationDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Dto_CultureDto_PreservesProperties()
    {
        var dto = new CultureDto(
            "tr",
            "Türkçe",
            IsDefault: true,
            IsRightToLeft: false,
            ActivatedUtc: DateTime.UtcNow);

        dto.Culture.Should().Be("tr");
        dto.DisplayName.Should().Be("Türkçe");
        dto.IsDefault.Should().BeTrue();
        dto.IsRightToLeft.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-807")]
    public void Localization_Dto_CultureDto_ArabicIsRightToLeft()
    {
        var dto = new CultureDto(
            "ar",
            "العربية",
            IsDefault: false,
            IsRightToLeft: true,
            ActivatedUtc: DateTime.UtcNow);

        dto.IsRightToLeft.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Dto_CultureSettingsDto_DefaultsToTurkish()
    {
        var supportedCultures = new[] { "tr", "en" };
        var settings = new CultureSettingsDto(
            DefaultCulture: "tr",
            SupportedCultures: supportedCultures,
            OmitDefaultCulturePrefix: true);

        settings.DefaultCulture.Should().Be("tr");
        settings.SupportedCultures.Should().HaveCount(2);
        settings.OmitDefaultCulturePrefix.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-808")]
    public void Localization_Dto_TranslationStatusDto_MissingStatus()
    {
        var cultures = new[]
        {
            new CultureTranslationDto("tr", "Published", "content-tr-001", "/tr/destek-programi"),
            new CultureTranslationDto("en", "Missing", null, null),
        };

        var status = new TranslationStatusDto(
            "content-001",
            "DestekProgrami",
            cultures);

        status.ContentItemId.Should().Be("content-001");
        status.Cultures.Should().HaveCount(2);
        status.Cultures.Should().Contain(c => c.Culture == "en" && c.Status == "Missing");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-808")]
    public void Localization_Dto_TranslationStatusDto_PublishedStatus()
    {
        var cultures = new[]
        {
            new CultureTranslationDto("tr", "Published", "content-tr-001", "/tr/kobi-destek"),
        };

        var status = new TranslationStatusDto("content-001", "DestekProgrami", cultures);

        status.Cultures.Should().ContainSingle(c => c.Status == "Published");
        status.Cultures[0].TranslatedSlug.Should().Contain("kobi");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-808")]
    public void Localization_Dto_TranslationStatusDto_DraftStatus()
    {
        var cultures = new[]
        {
            new CultureTranslationDto("tr", "Published", "content-tr-001", "/tr/duyuru"),
            new CultureTranslationDto("en", "Draft", "content-en-001", "/en/announcement"),
        };

        var status = new TranslationStatusDto("content-001", "Duyuru", cultures);

        status.Cultures.Should().Contain(c => c.Culture == "en" && c.Status == "Draft");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-804")]
    public void Localization_Dto_CultureTranslationDto_LocalizedSlugContainsCulturePrefix()
    {
        var dto = new CultureTranslationDto(
            "en",
            "Published",
            "content-en-001",
            "/en/sme-support-program");

        dto.TranslatedSlug.Should().StartWith("/en/");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Dto_ActivateCultureCommand_DefaultNotSetAsDefault()
    {
        var command = new ActivateCultureCommand("de");

        command.Culture.Should().Be("de");
        command.SetAsDefault.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void Localization_Dto_CultureSettingsDto_TurkishDisplayName()
    {
        var onlyCultures = new[] { "tr" };
        var settings = new CultureSettingsDto(
            "tr",
            onlyCultures,
            true);

        settings.DefaultCulture.Should().Be("tr");
        settings.SupportedCultures.Should().ContainSingle().Which.Should().Be("tr");
    }
}
