# Golden Test Dataset Specification

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Overview

This document defines the fixture data schema, categories, and seed mechanism for ProjectDora's golden test datasets. Golden datasets provide deterministic, realistic Turkish KOSGEB data for integration tests and regression testing.

**Storage:** `tests/TestData/` as JSON files in Orchard Core Recipe format.

**Loading:** `WebApplicationFactory` + `IStartupFilter` at test startup.

**Reset:** `docker compose down -v && docker compose up -d` for full reset.

## 2. Content Items

### 2.1 Content Types and Counts

| Content Type | Count | Description |
|-------------|-------|-------------|
| `Duyuru` (Announcement) | 50 | KOSGEB announcements, varying lengths |
| `DestekProgrami` (Support Program) | 50 | SME support programs with criteria |
| `KOBIBilgi` (SME Info) | 50 | SME information records |
| `SSS` (FAQ) | 30 | Frequently asked questions |
| `Etkinlik` (Event) | 20 | Events and seminars |

**Total: 200 content items**

### 2.2 Turkish Text Requirements

All text fields must include Turkish-specific characters to validate encoding:

- **Mandatory characters per content type**: ş, ç, ğ, ı, ö, ü, İ, Ş, Ç, Ğ, Ö, Ü
- **Edge cases in 10% of records**:
  - Very long titles (200+ characters)
  - Titles with numbers and special chars: `"2026 Yılı KOBİ Ar-Ge & İnovasyon Desteği (3. Dönem)"`
  - Empty optional fields (body = null)
  - Maximum length body (50,000 chars)
  - Mixed Turkish/English content

### 2.3 Content Item Schema (Recipe Format)

```json
{
  "name": "ProjectDora.GoldenDataset",
  "displayName": "Golden Dataset Seed",
  "description": "Test fixture data for ProjectDora",
  "steps": [
    {
      "name": "content",
      "data": [
        {
          "ContentItemId": "gd-duyuru-001",
          "ContentType": "Duyuru",
          "DisplayText": "KOBİ Teknoloji Geliştirme Desteği Başvuruları Başladı",
          "Published": true,
          "Latest": true,
          "TitlePart": {
            "Title": "KOBİ Teknoloji Geliştirme Desteği Başvuruları Başladı"
          },
          "BodyPart": {
            "Body": "Küçük ve orta büyüklükteki işletmelere yönelik teknoloji geliştirme destek programı kapsamında başvurular 1 Mart 2026 tarihinde başlamıştır. Şartları karşılayan KOBİ'ler, çevrimiçi başvuru sistemi üzerinden müracaatlarını yapabilecektir."
          },
          "CommonPart": {
            "CreatedUtc": "2026-01-15T09:00:00Z",
            "ModifiedUtc": "2026-01-15T09:00:00Z",
            "Owner": "editor-user-001"
          },
          "AuditTrailPart": {}
        }
      ]
    }
  ]
}
```

### 2.4 Content Status Distribution

| Status | Percentage | Count |
|--------|-----------|-------|
| Published | 60% | 120 |
| Draft | 25% | 50 |
| Archived | 10% | 20 |
| Scheduled | 5% | 10 |

## 3. Users and Roles

### 3.1 Role Definitions

| # | Role | Username | Permissions | Purpose |
|---|------|----------|-------------|---------|
| 1 | SuperAdmin | `superadmin` | All | Full system access |
| 2 | TenantAdmin | `tenantadmin-a` | Tenant management, content, users | Tenant-level admin |
| 3 | Editor | `editor-001` | Content CRUD, publish | Content management |
| 4 | Author | `author-001` | Content create, edit own | Content creation only |
| 5 | Analyst | `analyst-001` | Query execute, analytics, reports | Data analysis |
| 6 | Denetci (Auditor) | `denetci-001` | Audit read, reports | Audit trail review |
| 7 | SEOUzmani (SEO Specialist) | `seo-001` | Content read, SEO fields edit | SEO management |
| 8 | WorkflowAdmin | `wfadmin-001` | Workflow design, execute | Workflow management |
| 9 | Viewer | `viewer-001` | Content read only | Read-only access |
| 10 | Anonymous | (no login) | Public content only | Negative RBAC testing |

### 3.2 Special Test Users

| Username | Purpose |
|----------|---------|
| `no-permission-user` | Has authenticated role but zero permissions — negative RBAC tests |
| `multi-role-user` | Has Editor + Analyst roles — tests role combination |
| `disabled-user` | Account disabled — tests auth rejection |
| `expired-token-user` | For testing token expiry flows |

### 3.3 User Data Format

```json
{
  "name": "users",
  "data": [
    {
      "UserId": "user-editor-001",
      "UserName": "editor-001",
      "Email": "editor@kosgeb-test.gov.tr",
      "EmailConfirmed": true,
      "Enabled": true,
      "RoleNames": ["Editor"],
      "PasswordHash": "(bcrypt hash of 'Test1234!')"
    }
  ]
}
```

## 4. Tenants

| Tenant ID | Name | Purpose | Content Count |
|-----------|------|---------|---------------|
| `default` | Default Tenant | Primary test tenant | Full dataset |
| `test-tenant-a` | Test Tenant A | Multi-tenant isolation tests | 50 items |
| `empty-tenant` | Empty Tenant | Edge case: no data | 0 items |

**Isolation tests:**
- Query from `default` must NOT return `test-tenant-a` data
- Query from `test-tenant-a` must NOT return `default` data
- Query from `empty-tenant` must return empty results (not error)

## 5. Search Data

### 5.1 Elasticsearch/Lucene Index

| Index Name | Record Count | Source |
|-----------|-------------|--------|
| `content_duyuru` | 50 | Duyuru content items |
| `content_destekprogrami` | 50 | DestekProgrami items |
| `content_kobibilgi` | 50 | KOBIBilgi items |
| `content_sss` | 30 | SSS items |
| `content_etkinlik` | 20 | Etkinlik items |
| `content_all` | 200 | All content types combined |
| `search_extended` | 300+ | Extended records for pagination/relevance tests |

**Total: 500+ indexed records**

### 5.2 Search Test Cases

| Category | Example Query | Expected Results |
|----------|--------------|-----------------|
| Exact match | `"KOBİ Teknoloji Geliştirme"` | >= 5 results |
| Turkish stemming | `destekler` (finds `destek`, `destekleri`) | >= 10 results |
| Fuzzy match | `teknoloij` (typo) | >= 3 results |
| Multi-field | `title:destek AND body:başvuru` | >= 5 results |
| Range filter | `createdUtc:[2026-01 TO 2026-03]` | >= 20 results |
| Empty result | `"nonexistent-keyword-xyz"` | 0 results |
| Turkish case | `İSTANBUL` vs `istanbul` | Same results (case-insensitive) |
| Pagination | `*` with page size 10 | 20+ pages |

## 6. Workflow Definitions

### 6.1 Test Workflows

| # | Workflow | Trigger | Actions | Expected Outcome |
|---|---------|---------|---------|-----------------|
| 1 | Content Approval | Content submitted for review | Notify Editor → Wait for approval → Publish or Reject | Published or Draft with rejection note |
| 2 | New User Welcome | User account created | Send welcome email → Assign default role → Log audit | Email sent, role assigned, audit logged |
| 3 | Destek Başvuru | New DestekProgrami application | Validate fields → Check eligibility → Route to reviewer | Application routed or rejected with reason |
| 4 | Scheduled Publish | Timer (daily 09:00) | Query scheduled content → Publish if date reached → Notify | Content published, notification sent |
| 5 | Audit Alert | Audit event with severity=high | Evaluate rule → Send alert to Denetci → Log | Alert sent, logged |

### 6.2 Workflow Definition Format

```json
{
  "name": "ContentApproval",
  "displayName": "İçerik Onay Akışı",
  "isEnabled": true,
  "startActivity": {
    "activityId": "start-1",
    "name": "ContentPublishedEvent",
    "properties": {
      "ContentTypes": ["Duyuru", "DestekProgrami"]
    }
  },
  "activities": [
    {
      "activityId": "notify-1",
      "name": "NotifyContentOwnerTask",
      "properties": {
        "subject": "İçerik onay bekliyor",
        "body": "{{ContentItem.DisplayText}} içeriği onayınızı beklemektedir."
      }
    }
  ],
  "transitions": [
    { "sourceActivityId": "start-1", "destinationActivityId": "notify-1" }
  ]
}
```

## 7. Seed Mechanism

### 7.1 File Structure

```
tests/
└── TestData/
    ├── Recipes/
    │   ├── golden-content.recipe.json      # Content items (200)
    │   ├── golden-users.recipe.json        # Users and roles (14)
    │   ├── golden-tenants.recipe.json      # Tenant definitions (3)
    │   └── golden-workflows.recipe.json    # Workflow definitions (5)
    ├── Search/
    │   ├── search-index-seed.json          # Extended search records (500+)
    │   └── search-test-queries.json        # Expected search results
    └── README.md                           # Dataset documentation
```

### 7.2 Loading via WebApplicationFactory

```csharp
public class ProjectDoraWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace production DB with Testcontainers
            services.AddTestcontainersPostgres();
            services.AddTestcontainersRedis();

            // Seed golden dataset via Orchard Core Recipe
            services.AddTransient<IStartupFilter, GoldenDatasetStartupFilter>();
        });
    }
}

public class GoldenDatasetStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Load recipes from tests/TestData/Recipes/
            var recipeExecutor = app.ApplicationServices.GetRequiredService<IRecipeExecutor>();
            // Execute golden dataset recipes
            next(app);
        };
    }
}
```

### 7.3 Reset Strategy

```bash
# Full reset (integration tests)
docker compose -f docker-compose.test.yml down -v
docker compose -f docker-compose.test.yml up -d

# Per-test-class reset (via WebApplicationFactory)
# Each test class gets a fresh database via Testcontainers
# No shared state between test classes
```

## 8. Cross-References

- **Test Strategy**: [test-strategy.md](test-strategy.md) — environments, tooling
- **Test Cases**: [test-cases.md](test-cases.md) — test cases reference golden dataset fixtures for integration and E2E tests
- **DoR Template**: [definition-of-ready.md](../ai-sdlc/definition-of-ready.md) — story examples use golden dataset entities
- **Governance**: [governance.md](../ai-sdlc/governance.md) — QA Agent uses golden dataset for validation
