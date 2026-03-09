using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.ContentModeling;

public class ContentTypeDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Dto_ContentTypeDto_CreatesWithRequiredProperties()
    {
        // Act
        var dto = new ContentTypeDto(
            "DestekProgrami",
            "Destek Programi",
            null,
            new List<ContentPartDto>(),
            new List<ContentFieldDto>());

        // Assert
        dto.Name.Should().Be("DestekProgrami");
        dto.DisplayName.Should().Be("Destek Programi");
        dto.Stereotype.Should().BeNull();
        dto.Parts.Should().BeEmpty();
        dto.Fields.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-202")]
    public void ContentModeling_Dto_ContentFieldDto_CreatesWithFieldType()
    {
        // Act
        var field = new ContentFieldDto("ProgramButcesi", "NumericField", true, null);

        // Assert
        field.Name.Should().Be("ProgramButcesi");
        field.FieldType.Should().Be("NumericField");
        field.Required.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-202")]
    public void ContentModeling_Dto_ContentFieldDto_SupportsSettings()
    {
        // Act
        var settings = new Dictionary<string, object> { { "scale", 2 }, { "minimum", 0 } };
        var field = new ContentFieldDto("Butce", "NumericField", true, settings);

        // Assert
        field.Settings.Should().ContainKey("scale");
        field.Settings!["scale"].Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Dto_CreateContentTypeRequest_CreatesWithPartsAndFields()
    {
        // Act
        var request = new CreateContentTypeRequest(
            "DestekProgrami",
            "Destek Programi",
            null,
            new List<string> { "TitlePart", "BodyPart" },
            new List<AddFieldRequest>
            {
                new("ProgramAdi", "TextField", Required: true),
                new("Butce", "NumericField"),
            });

        // Assert
        request.Name.Should().Be("DestekProgrami");
        request.Parts.Should().HaveCount(2);
        request.Fields.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-201")]
    public void ContentModeling_Dto_ContentPartDto_CreatesWithPosition()
    {
        // Act
        var part = new ContentPartDto("TitlePart", 1);

        // Assert
        part.Name.Should().Be("TitlePart");
        part.Position.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-202")]
    public void ContentModeling_Dto_AddFieldRequest_DefaultsRequiredToFalse()
    {
        // Act
        var request = new AddFieldRequest("TestField", "TextField");

        // Assert
        request.Required.Should().BeFalse();
        request.Settings.Should().BeNull();
    }
}
