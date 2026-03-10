using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Localization;

public class LocalizationServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void CultureDto_TurkishDefault_IsMarkedAsDefault()
    {
        var dto = new CultureDto("tr", "Türkçe", IsDefault: true, IsRightToLeft: false, DateTime.UtcNow);

        dto.Culture.Should().Be("tr");
        dto.IsDefault.Should().BeTrue();
        dto.IsRightToLeft.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void CultureDto_ArabicCulture_IsRtl()
    {
        var dto = new CultureDto("ar", "العربية", IsDefault: false, IsRightToLeft: true, DateTime.UtcNow);

        dto.Culture.Should().Be("ar");
        dto.IsRightToLeft.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void TranslationStatusDto_MixedCultures()
    {
        var translations = new[]
        {
            new CultureTranslationDto("tr", "Translated", "ci-001-tr", null),
            new CultureTranslationDto("en", "Missing", null, null),
        };

        var dto = new TranslationStatusDto("ci-001", "Article", translations);

        dto.ContentItemId.Should().Be("ci-001");
        dto.Cultures.Should().HaveCount(2);
        dto.Cultures.Should().Contain(t => t.Culture == "tr" && t.Status == "Translated");
        dto.Cultures.Should().Contain(t => t.Culture == "en" && t.Status == "Missing");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void CultureSettingsDto_SupportedCulturesPreserved()
    {
        var dto = new CultureSettingsDto("tr", new[] { "tr", "en", "ar" }, OmitDefaultCulturePrefix: true);

        dto.DefaultCulture.Should().Be("tr");
        dto.SupportedCultures.Should().HaveCount(3);
        dto.OmitDefaultCulturePrefix.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void DeactivateCulture_DefaultCulture_ThrowsInvalidOperation()
    {
        var defaultCulture = "tr";
        var cultureToDeactivate = "tr";

        var act = () =>
        {
            if (string.Equals(cultureToDeactivate, defaultCulture, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Cannot deactivate the default culture '{cultureToDeactivate}'.");
        };

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*default*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void CultureTranslationDto_MissingTranslation_NullContentItemId()
    {
        var dto = new CultureTranslationDto("en", "Missing", null, null);

        dto.Status.Should().Be("Missing");
        dto.TranslatedContentItemId.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-801")]
    public void TranslationStatusDto_EmptyTranslations_ReturnsEmpty()
    {
        var dto = new TranslationStatusDto("ci-001", "Article", Array.Empty<CultureTranslationDto>());

        dto.Cultures.Should().BeEmpty();
        dto.ContentType.Should().Be("Article");
    }
}
