using System.Text.RegularExpressions;

namespace ProjectDora.QueryEngine.Services;

public static partial class SqlSafetyValidator
{
    private static readonly string[] ForbiddenKeywords = new[]
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE",
        "TRUNCATE", "GRANT", "REVOKE", "EXEC", "EXECUTE",
    };

    public static SqlValidationResult Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return SqlValidationResult.Fail("SQL query text cannot be empty.");
        }

        var normalized = sql.Trim();

        // Check for forbidden keywords first (word boundary match)
        foreach (var keyword in ForbiddenKeywords)
        {
            var pattern = $@"\b{keyword}\b";
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
            {
                return SqlValidationResult.Fail($"SQL contains forbidden keyword '{keyword}'.");
            }
        }

        // Must start with SELECT or WITH (for CTEs)
        if (!StartsWithSelect().IsMatch(normalized))
        {
            return SqlValidationResult.Fail("Only SELECT queries are allowed. Query must start with SELECT or WITH.");
        }

        return SqlValidationResult.Ok();
    }

    [GeneratedRegex(@"^\s*(SELECT|WITH)\s", RegexOptions.IgnoreCase)]
    private static partial Regex StartsWithSelect();
}

public sealed class SqlValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private SqlValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static SqlValidationResult Ok() => new(true, null);
    public static SqlValidationResult Fail(string error) => new(false, error);
}
