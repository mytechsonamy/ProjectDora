using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.ContentModeling;

public class ContentItemDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_ContentItemDto_CreatesWithRequiredProperties()
    {
        // Act
        var dto = new ContentItemDto(
            "abc123",
            "DestekProgrami",
            "KOBİ Teknoloji Desteği",
            "Draft",
            1,
            "admin",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            "tr");

        // Assert
        dto.ContentItemId.Should().Be("abc123");
        dto.ContentType.Should().Be("DestekProgrami");
        dto.DisplayText.Should().Be("KOBİ Teknoloji Desteği");
        dto.Status.Should().Be("Draft");
        dto.Version.Should().Be(1);
        dto.Owner.Should().Be("admin");
        dto.PublishedUtc.Should().BeNull();
        dto.Culture.Should().Be("tr");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-302")]
    public void ContentManagement_Dto_ContentItemDto_PublishedStatusHasPublishedUtc()
    {
        // Arrange
        var publishedAt = DateTime.UtcNow;

        // Act
        var dto = new ContentItemDto(
            "abc123",
            "DestekProgrami",
            "KOBİ Teknoloji Desteği",
            "Published",
            2,
            "admin",
            DateTime.UtcNow,
            DateTime.UtcNow,
            publishedAt,
            null);

        // Assert
        dto.Status.Should().Be("Published");
        dto.PublishedUtc.Should().Be(publishedAt);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-303")]
    public void ContentManagement_Dto_ContentVersionDto_CreatesWithVersionInfo()
    {
        // Act
        var version = new ContentVersionDto(3, "Draft", "editor", DateTime.UtcNow);

        // Assert
        version.Version.Should().Be(3);
        version.Status.Should().Be("Draft");
        version.ModifiedBy.Should().Be("editor");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_CreateContentItemCommand_DefaultsToUnpublished()
    {
        // Act
        var command = new CreateContentItemCommand("Test Content");

        // Assert
        command.Published.Should().BeFalse();
        command.Culture.Should().BeNull();
        command.CloneFrom.Should().BeNull();
        command.Parts.Should().BeNull();
        command.Fields.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_CreateContentItemCommand_SupportsCloneFrom()
    {
        // Act
        var command = new CreateContentItemCommand("Cloned Content", CloneFrom: "source-id-123");

        // Assert
        command.CloneFrom.Should().Be("source-id-123");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_UpdateContentItemCommand_AllowsPartialUpdate()
    {
        // Act
        var command = new UpdateContentItemCommand(DisplayText: "Updated Title");

        // Assert
        command.DisplayText.Should().Be("Updated Title");
        command.Parts.Should().BeNull();
        command.Fields.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_ContentListQuery_HasSensibleDefaults()
    {
        // Act
        var query = new ContentListQuery();

        // Assert
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.SortOrder.Should().Be("desc");
        query.Status.Should().BeNull();
        query.Culture.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_PagedResult_CalculatesCorrectly()
    {
        // Act
        var items = new List<ContentItemDto>();
        var result = new PagedResult<ContentItemDto>(items, 50, 2, 10);

        // Assert
        result.TotalCount.Should().Be(50);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-301")]
    public void ContentManagement_Dto_ContentItemDto_TurkishCharactersPreserved()
    {
        // Act — Turkish special characters in displayText
        var dto = new ContentItemDto(
            "tr-content-1",
            "DestekProgrami",
            "Şırnak İlçesi Küçük Ölçekli Girişimci Desteği",
            "Draft",
            1,
            "admin",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            "tr");

        // Assert
        dto.DisplayText.Should().Contain("Şırnak");
        dto.DisplayText.Should().Contain("İlçesi");
        dto.DisplayText.Should().Contain("Küçük");
        dto.DisplayText.Should().Contain("Ölçekli");
        dto.DisplayText.Should().Contain("Girişimci");
    }
}
