using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.QueryEngine;

public class QueryDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Dto_SavedQueryDto_CreatesWithRequiredProperties()
    {
        var dto = new SavedQueryDto(
            "q-001",
            "destek_arama",
            "Lucene",
            "displayText:destek AND status:Published",
            "Destek programları arama sorgusu",
            false,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);

        dto.QueryId.Should().Be("q-001");
        dto.Name.Should().Be("destek_arama");
        dto.Type.Should().Be("Lucene");
        dto.QueryText.Should().Contain("destek");
        dto.IsApiExposed.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-507")]
    public void QueryEngine_Dto_QueryParameterDef_SupportsDefaults()
    {
        var param = new QueryParameterDef("durum", "string", Required: true, DefaultValue: "aktif");

        param.Name.Should().Be("durum");
        param.Type.Should().Be("string");
        param.Required.Should().BeTrue();
        param.DefaultValue.Should().Be("aktif");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Dto_CreateSavedQueryCommand_DefaultsToNotExposed()
    {
        var command = new CreateSavedQueryCommand("test_query", "Lucene", "destek");

        command.IsApiExposed.Should().BeFalse();
        command.Description.Should().BeNull();
        command.Parameters.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-501")]
    public void QueryEngine_Dto_LuceneQueryRequest_HasSensibleDefaults()
    {
        var request = new LuceneQueryRequest("KOBİ destek");

        request.QueryText.Should().Be("KOBİ destek");
        request.IndexName.Should().BeNull();
        request.ContentType.Should().BeNull();
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_Dto_SqlQueryRequest_DefaultTimeout30Seconds()
    {
        var request = new SqlQueryRequest("SELECT * FROM analytics.destek_programlari");

        request.CommandTimeout.Should().Be(30);
        request.Parameters.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-503")]
    public void QueryEngine_Dto_QueryResultDto_ReturnsEmptyResult()
    {
        var result = new QueryResultDto(
            Array.Empty<string>(),
            Array.Empty<IDictionary<string, object>>(),
            0,
            15,
            "Lucene");

        result.TotalCount.Should().Be(0);
        result.ExecutionTimeMs.Should().Be(15);
        result.QueryType.Should().Be("Lucene");
        result.Columns.Should().BeEmpty();
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-507")]
    public void QueryEngine_Dto_ExecuteQueryCommand_DefaultsPagination()
    {
        var command = new ExecuteQueryCommand();

        command.Page.Should().Be(1);
        command.PageSize.Should().Be(20);
        command.MaxResults.Should().BeNull();
        command.Parameters.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-501")]
    public void QueryEngine_Dto_LuceneQueryRequest_TurkishCharactersPreserved()
    {
        var request = new LuceneQueryRequest("Şırnak İlçesi Küçük Ölçekli Girişimci");

        request.QueryText.Should().Contain("Şırnak");
        request.QueryText.Should().Contain("İlçesi");
        request.QueryText.Should().Contain("Küçük");
        request.QueryText.Should().Contain("Ölçekli");
        request.QueryText.Should().Contain("Girişimci");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-502")]
    public void QueryEngine_Dto_SavedQueryDto_WithParameters()
    {
        var parameters = new List<QueryParameterDef>
        {
            new("il", "string", Required: true),
            new("min_butce", "int", Required: false, DefaultValue: 0),
        };

        var dto = new SavedQueryDto(
            "q-002",
            "kobi_rapor",
            "SQL",
            "SELECT * FROM analytics.destek WHERE il = @il AND butce >= @min_butce",
            "KOBİ destek raporu",
            true,
            parameters,
            DateTime.UtcNow,
            DateTime.UtcNow);

        dto.IsApiExposed.Should().BeTrue();
        dto.Parameters.Should().HaveCount(2);
        dto.Parameters![0].Name.Should().Be("il");
        dto.Parameters[1].DefaultValue.Should().Be(0);
    }
}
