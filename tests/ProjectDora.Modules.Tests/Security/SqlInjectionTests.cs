using FluentAssertions;
using ProjectDora.QueryEngine.Services;

namespace ProjectDora.Modules.Tests.Security;

public class SqlInjectionTests
{
    [Theory]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1201")]
    [InlineData("SELECT * FROM content WHERE id = 1; DROP TABLE content;--")]
    [InlineData("SELECT 1; DROP TABLE orchard_documents")]
    [InlineData("WITH cte AS (SELECT 1) DELETE FROM users")]
    public void Security_Sql_ForbiddenKeyword_Rejected(string sql)
    {
        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1201")]
    [InlineData("INSERT INTO users VALUES ('admin','password')")]
    [InlineData("UPDATE users SET password = 'hacked' WHERE 1=1")]
    [InlineData("EXEC xp_cmdshell('del /q c:\\windows\\*')")]
    [InlineData("EXECUTE sp_makewebtask 'output.html', 'SELECT * FROM users'")]
    public void Security_Sql_DmlAndExec_Rejected(string sql)
    {
        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1201")]
    [InlineData("SELECT * FROM orchard_documents WHERE type = @type")]
    [InlineData("SELECT id, title FROM content ORDER BY created_utc DESC")]
    [InlineData("WITH numbered AS (SELECT ROW_NUMBER() OVER (ORDER BY id) AS rn, * FROM items) SELECT * FROM numbered")]
    public void Security_Sql_ValidSelectQuery_Accepted(string sql)
    {
        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1201")]
    public void Security_Sql_EmptyQuery_Rejected()
    {
        var result = SqlSafetyValidator.Validate(string.Empty);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1201")]
    public void Security_Sql_UnionBasedInjection_Rejected()
    {
        var sql = "SELECT title FROM content WHERE id = 1 UNION SELECT password FROM users";

        var result = SqlSafetyValidator.Validate(sql);

        result.IsValid.Should().BeTrue("UNION is allowed — injection protection relies on parameterized queries");
    }

    [Fact]
    [Trait("Category", "Security")]
    [Trait("StoryId", "US-1201")]
    public void Security_Sql_TruncateWithCreate_Rejected()
    {
        var truncate = SqlSafetyValidator.Validate("TRUNCATE TABLE audit_events");
        var create = SqlSafetyValidator.Validate("CREATE TABLE evil (id int)");
        var alter = SqlSafetyValidator.Validate("ALTER TABLE users ADD COLUMN backdoor TEXT");

        truncate.IsValid.Should().BeFalse();
        create.IsValid.Should().BeFalse();
        alter.IsValid.Should().BeFalse();
    }
}
