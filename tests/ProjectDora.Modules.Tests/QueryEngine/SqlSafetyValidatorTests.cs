using FluentAssertions;
using ProjectDora.QueryEngine.Services;

namespace ProjectDora.Modules.Tests.QueryEngine;

public class SqlSafetyValidatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_ValidSelectPasses()
    {
        var result = SqlSafetyValidator.Validate("SELECT program_adi, butce FROM analytics.destek_programlari WHERE durum = @durum");

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_WithCtePasses()
    {
        var sql = "WITH top_programs AS (SELECT program_adi, butce FROM analytics.destek_programlari) SELECT * FROM top_programs";

        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    [InlineData("DROP TABLE analytics.destek_programlari", "DROP")]
    [InlineData("DELETE FROM analytics.destek_programlari", "DELETE")]
    [InlineData("INSERT INTO analytics.destek_programlari VALUES (1)", "INSERT")]
    [InlineData("UPDATE analytics.destek_programlari SET butce = 0", "UPDATE")]
    [InlineData("TRUNCATE TABLE analytics.destek_programlari", "TRUNCATE")]
    [InlineData("ALTER TABLE analytics.destek_programlari ADD COLUMN x INT", "ALTER")]
    [InlineData("CREATE TABLE evil (id INT)", "CREATE")]
    [InlineData("GRANT ALL ON analytics.destek_programlari TO public", "GRANT")]
    [InlineData("REVOKE ALL ON analytics.destek_programlari FROM admin", "REVOKE")]
    public void QueryEngine_SqlSafety_ForbiddenKeywordBlocked(string sql, string expectedKeyword)
    {
        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain(expectedKeyword);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_CteWithDeleteBlocked()
    {
        var sql = "WITH del AS (DELETE FROM analytics.destek_programlari RETURNING *) SELECT * FROM del";

        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DELETE");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_EmptySqlFails()
    {
        var result = SqlSafetyValidator.Validate("");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_NonSelectStatementFails()
    {
        var result = SqlSafetyValidator.Validate("EXPLAIN SELECT * FROM analytics.destek_programlari");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SELECT");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_TurkishColumnNamesPass()
    {
        var sql = "SELECT program_adi, başlangıç_tarihi, bütçe FROM analytics.destek_programlari";

        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-506")]
    public void QueryEngine_SqlSafety_CaseInsensitiveForbiddenKeyword()
    {
        var result = SqlSafetyValidator.Validate("drop table analytics.destek_programlari");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DROP");
    }
}
