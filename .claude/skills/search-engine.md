# Skill: Search & Query Engine

> Target agents: Developer, Test Architect

## 1. Search Architecture

```
User Query
    │
    ├── Lucene.NET (dev/small) ──→ Local index files
    │
    └── Elasticsearch (production) ──→ ES cluster
    │
    └── SQL (analytics) ──→ PostgreSQL analytics schema
```

**Fallback**: If Elasticsearch is down → automatic fallback to Lucene.NET (see resilience-and-chaos-tests.md).

## 2. Lucene.NET

### Index Management

```csharp
// Orchard Core manages Lucene indexes via ILuceneIndexManager
// Each content type can have its own index

// Index definition (via recipe or code)
{
  "name": "LuceneSettings",
  "LuceneSettings": {
    "SearchIndexes": {
      "content_duyuru": {
        "AnalyzerName": "turkish",
        "IndexLatest": false,
        "IndexedContentTypes": ["Duyuru"],
        "Culture": "tr"
      }
    }
  }
}
```

### Query Syntax

```
// Simple term
destek

// Field-specific
title:destek

// Boolean
destek AND program

// Wildcard
destek*

// Fuzzy
destk~2

// Range
createdUtc:[2026-01-01 TO 2026-03-31]

// Phrase
"destek programı"
```

### Turkish Analyzer

```csharp
// Orchard Core uses Lucene.Net.Analysis.Tr.TurkishAnalyzer
// Pipeline: StandardTokenizer → TurkishLowerCaseFilter → StopFilter → SnowballFilter

// This means:
// "Destekler" → "destek" (stemmed)
// "KOBİ'lerin" → "kobi" (lowercase + stemmed)
// "İSTANBUL" → "istanbul" (Turkish lowercase: İ→i, not I→i)
```

**Critical**: Turkish lowercase rules differ from English. `İ` → `i` (not `ı`). `I` → `ı` (not `i`). Always use `TurkishLowerCaseFilter`, never `LowerCaseFilter`.

## 3. Elasticsearch

### Index Mapping

```json
{
  "mappings": {
    "properties": {
      "contentItemId": { "type": "keyword" },
      "contentType": { "type": "keyword" },
      "displayText": {
        "type": "text",
        "analyzer": "turkish",
        "fields": {
          "keyword": { "type": "keyword" }
        }
      },
      "body": {
        "type": "text",
        "analyzer": "turkish"
      },
      "status": { "type": "keyword" },
      "createdUtc": { "type": "date" },
      "modifiedUtc": { "type": "date" },
      "owner": { "type": "keyword" },
      "tenantId": { "type": "keyword" },
      "culture": { "type": "keyword" }
    }
  },
  "settings": {
    "analysis": {
      "analyzer": {
        "turkish": {
          "type": "custom",
          "tokenizer": "standard",
          "filter": ["lowercase", "turkish_stop", "turkish_stemmer"]
        }
      },
      "filter": {
        "turkish_stop": {
          "type": "stop",
          "stopwords": "_turkish_"
        },
        "turkish_stemmer": {
          "type": "stemmer",
          "language": "turkish"
        }
      }
    }
  }
}
```

### Query DSL

```json
// Simple match
{
  "query": {
    "bool": {
      "must": [
        { "match": { "displayText": "destek programı" } }
      ],
      "filter": [
        { "term": { "tenantId": "default" } },
        { "term": { "status": "Published" } }
      ]
    }
  }
}

// Fuzzy search (typo tolerance)
{
  "query": {
    "bool": {
      "must": [
        { "match": { "displayText": { "query": "teknoloij", "fuzziness": "AUTO" } } }
      ],
      "filter": [
        { "term": { "tenantId": "default" } }
      ]
    }
  }
}

// Multi-field search
{
  "query": {
    "bool": {
      "must": [
        {
          "multi_match": {
            "query": "KOBİ destek",
            "fields": ["displayText^3", "body"],
            "type": "best_fields"
          }
        }
      ],
      "filter": [
        { "term": { "tenantId": "default" } },
        { "range": { "createdUtc": { "gte": "2026-01-01" } } }
      ]
    }
  },
  "sort": [{ "_score": "desc" }, { "createdUtc": "desc" }],
  "from": 0,
  "size": 20
}
```

### Pagination

```json
{
  "from": 0,     // offset (page - 1) * size
  "size": 20,    // page size (max 100)
  "track_total_hits": true
}
```

**Anti-pattern**: Never use `from` > 10000. For deep pagination, use `search_after`.

## 4. SQL Queries (Analytics)

### Parameterized Queries

```csharp
// ALWAYS parameterized — never string concatenation
public async Task<QueryResultDto> ExecuteAsync(
    string sql,
    Dictionary<string, object> parameters,
    string tenantId)
{
    // Inject tenant filter
    sql = InjectTenantFilter(sql, tenantId);

    await using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(sql, connection);
    foreach (var (key, value) in parameters)
    {
        command.Parameters.AddWithValue(key, value);
    }

    // Timeout
    command.CommandTimeout = 30;

    await using var reader = await command.ExecuteReaderAsync();
    return MapToQueryResult(reader);
}
```

### Tenant Filter Injection

```csharp
// Every SQL query MUST include tenant_id filter
private string InjectTenantFilter(string sql, string tenantId)
{
    // Parse SQL and add WHERE tenant_id = @tenantId
    // If WHERE exists: AND tenant_id = @tenantId
    // If no WHERE: WHERE tenant_id = @tenantId
}
```

### SQL Safety Validation

```csharp
public class SqlSafetyValidator
{
    private static readonly string[] ForbiddenKeywords = new[]
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE",
        "TRUNCATE", "GRANT", "REVOKE", "EXEC", "EXECUTE"
    };

    public bool IsSafe(string sql)
    {
        var normalized = sql.ToUpperInvariant();
        return !ForbiddenKeywords.Any(kw =>
            Regex.IsMatch(normalized, $@"\b{kw}\b"));
    }
}
```

## 5. ProjectDora Abstraction Layer

```csharp
public interface IQueryService
{
    Task<QueryResultDto> ExecuteLuceneAsync(string indexName, string query, QueryOptions options);
    Task<QueryResultDto> ExecuteElasticsearchAsync(string indexName, string queryJson, QueryOptions options);
    Task<QueryResultDto> ExecuteSqlAsync(string sql, Dictionary<string, object> parameters);
}

public interface ISavedQueryService
{
    Task<SavedQueryDto> CreateAsync(CreateQueryCommand command);
    Task<SavedQueryDto> GetAsync(string queryId);
    Task<IReadOnlyList<SavedQueryDto>> ListAsync();
    Task DeleteAsync(string queryId);
    Task<QueryResultDto> ExecuteAsync(string queryId, Dictionary<string, object> parameters);
}

public interface ISearchIndexService
{
    Task ReindexAsync(string? contentType = null, string? tenantId = null);
    Task<SearchIndexStatus> GetStatusAsync();
}

public record QueryOptions(
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    bool SortDescending = true);
```

## 6. Turkish Search Testing

### Must-Pass Test Cases

| Test | Input | Expected |
|------|-------|----------|
| Turkish lowercase İ | Search: `istanbul` | Finds: `İstanbul` |
| Turkish lowercase I | Search: `ışık` | Finds: `IŞIK` |
| Stemming | Search: `destekler` | Finds: `destek`, `destekleri`, `desteklerin` |
| Special chars | Search: `şırnak` | Finds: `Şırnak` |
| Fuzzy | Search: `teknoloij` | Finds: `teknoloji` (distance 1) |
| Phrase | Search: `"destek programı"` | Exact phrase match |
| No result | Search: `nonexistent-xyz` | Returns empty, no error |

## 7. Anti-Patterns

| Anti-Pattern | Correct |
|-------------|---------|
| `query.ToLower()` for Turkish text | Use `TurkishLowerCaseFilter` or `string.ToLower(new CultureInfo("tr-TR"))` |
| SQL string concatenation | Always parameterized queries |
| No tenant filter in query | Every query must filter by `tenantId` |
| Elasticsearch `from` > 10000 | Use `search_after` for deep pagination |
| Returning all fields from search | Use `_source` filtering for needed fields only |
| No query timeout | Set 30s timeout on all queries |
