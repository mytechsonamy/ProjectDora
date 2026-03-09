# Migration Strategy

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Purpose

This document defines how database schema changes, content migrations, search reindexing, and data transformations are managed across ProjectDora's lifecycle.

## 2. Schema Migration

### 2.1 Migration Tools

| Schema | Tool | Migration Location |
|--------|------|-------------------|
| `orchard` | Orchard Core built-in (YesSql) | Module `Migrations.cs` files |
| `audit` | EF Core Migrations | `src/ProjectDora.Modules/AuditTrail/Migrations/` |
| `analytics` | EF Core Migrations | `src/ProjectDora.Modules/Analytics/Migrations/` |

### 2.2 Orchard Core Schema (YesSql)

Orchard Core uses YesSql document store. Schema changes happen via module `Migrations.cs`:

```csharp
public class Migrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<ContentItemIndex>(table => table
            .Column<string>("ContentType", col => col.WithLength(100))
            .Column<string>("DisplayText", col => col.WithLength(500))
            .Column<string>("Status", col => col.WithLength(20))
            .Column<DateTime>("CreatedUtc")
            .Column<string>("TenantId", col => col.WithLength(100))
        );
        return 1;
    }

    public int UpdateFrom1()
    {
        SchemaBuilder.AlterTable<ContentItemIndex>(table => table
            .AddColumn<string>("Culture", col => col.WithLength(10).WithDefault("tr"))
        );
        return 2;
    }
}
```

**Rules:**
- Never modify existing migration methods — always add `UpdateFromN()`
- Version numbers must be sequential (no gaps)
- Each migration runs once per tenant

### 2.3 EF Core Migrations (audit, analytics)

```bash
# Create migration
dotnet ef migrations add AddRetentionPolicyColumn \
  --project src/ProjectDora.Modules/ProjectDora.AuditTrail \
  --context AuditDbContext \
  --output-dir Migrations

# Apply migration
dotnet ef database update \
  --project src/ProjectDora.Modules/ProjectDora.AuditTrail \
  --context AuditDbContext

# Script for production (idempotent)
dotnet ef migrations script \
  --project src/ProjectDora.Modules/ProjectDora.AuditTrail \
  --context AuditDbContext \
  --idempotent \
  --output migrations/audit-migration.sql
```

**Rules:**
- Always generate idempotent SQL scripts for production deployment
- Review generated SQL before applying to production
- Backup database before applying migrations
- No data manipulation in schema migrations (use data migrations separately)

### 2.4 Migration Naming Convention

```
{YYYY}{MM}{DD}_{Description}

Examples:
20260315_AddRetentionPolicyColumn
20260320_AddCultureToContentIndex
20260401_AddTenantIdToAuditLogs
```

### 2.5 Zero-Downtime Migration Strategy

For production deployments, migrations must be backward-compatible:

| Phase | Allowed | Not Allowed |
|-------|---------|-------------|
| **Phase 1: Expand** | Add columns (nullable/default), add tables, add indexes | Drop columns, rename columns, change types |
| **Phase 2: Migrate** | Backfill data, update application code | — |
| **Phase 3: Contract** | Drop old columns, remove unused indexes | Must wait until Phase 2 deployed |

**Example: Renaming a column**

```
Phase 1: ALTER TABLE ADD new_column; -- Add new column
         UPDATE SET new_column = old_column; -- Backfill
Phase 2: Deploy code that reads/writes new_column (ignore old_column)
Phase 3: ALTER TABLE DROP old_column; -- Remove old column
```

## 3. Content Migration

### 3.1 Content Type Changes

When content type definitions change (fields added/removed):

```csharp
public class ContentMigration : IDataMigration
{
    private readonly IContentService _contentService;

    public async Task MigrateAsync()
    {
        // Get all items of type
        var items = await _contentService.GetAll("DestekProgrami");

        foreach (var item in items)
        {
            // Add new field with default value
            if (!item.Fields.ContainsKey("Kategori"))
            {
                item.Fields["Kategori"] = "Genel";
            }

            // Transform existing field
            if (item.Fields.TryGetValue("Tutar", out var tutar))
            {
                item.Fields["Tutar"] = ConvertToDecimal(tutar);
            }

            await _contentService.Update(item);
        }
    }
}
```

### 3.2 Content Migration Rules

- Always create a new content version (never modify in-place)
- Log migration as audit event (`ContentMigrated`)
- Run migration per tenant
- Dry-run first: count affected items, preview changes
- Provide rollback capability (restore to previous version)

### 3.3 Orchard Core Recipe Migration

For bulk content changes, use Orchard Core Recipe format:

```json
{
  "name": "Migration_20260401",
  "steps": [
    {
      "name": "content",
      "data": [
        {
          "ContentItemId": "existing-item-id",
          "ContentType": "DestekProgrami",
          "TitlePart": { "Title": "Updated Title" },
          "CustomField": { "NewField": "value" }
        }
      ]
    }
  ]
}
```

## 4. Search Reindexing

### 4.1 When to Reindex

| Trigger | Scope | Method |
|---------|-------|--------|
| Content type schema change | Affected content type index | Partial reindex |
| Elasticsearch mapping change | All indexes | Full reindex |
| Lucene analyzer change | Affected index | Full rebuild |
| Data migration completed | Affected content types | Partial reindex |
| Elasticsearch recovery (after downtime) | All indexes | Full reindex |
| New tenant provisioned | Tenant indexes | Initial index |

### 4.2 Reindex Process

```
1. Create new index with updated mapping: content_duyuru_v2
2. Bulk index all documents from DB to new index
3. Swap alias: content_duyuru → content_duyuru_v2 (atomic)
4. Delete old index: content_duyuru_v1
```

### 4.3 Reindex API

```
POST /api/internal/search/reindex
{
  "scope": "full" | "content_type" | "tenant",
  "contentType": "DestekProgrami",  // if scope=content_type
  "tenantId": "default",            // if scope=tenant
  "async": true                     // background job
}
```

### 4.4 Estimated Reindex Times

| Dataset Size | Method | Estimated Time |
|-------------|--------|---------------|
| < 1,000 items | Inline (sync) | < 10s |
| 1,000-10,000 | Background job | 1-5 min |
| 10,000-100,000 | Bulk API, batched | 5-30 min |
| > 100,000 | Parallel bulk, new index + alias swap | 30-120 min |

## 5. Multi-Tenant Migration

### 5.1 Per-Tenant Migration

All migrations run per tenant. Order:

```
1. Upgrade schema (shared across tenants if using shared DB)
2. For each active tenant:
   a. Run content migration
   b. Reindex search
   c. Verify health check
3. For each inactive tenant:
   a. Mark as "pending migration"
   b. Run migration on next activation
```

### 5.2 New Tenant Provisioning

```
1. Create tenant record
2. Provision database schema (or separate DB)
3. Apply all migrations up to current version
4. Execute seed recipe (default content types, roles, settings)
5. Create tenant admin user
6. Initialize search indexes (empty)
7. Verify health check
```

## 6. Rollback Strategy

| Migration Type | Rollback Method |
|----------------|----------------|
| Schema (add column) | Deploy code that ignores new column; drop column later |
| Schema (drop column) | **Not reversible** — must restore from backup |
| Content migration | Restore content items to previous version (via audit trail) |
| Search reindex | Swap alias back to old index |
**Rule**: Always take a database backup before any production migration.

```bash
# Pre-migration backup
pg_dump -h localhost -U projectdora -d projectdora -F c -f backup_$(date +%Y%m%d_%H%M%S).dump

# Restore if needed
pg_restore -h localhost -U projectdora -d projectdora -c backup_20260401_120000.dump
```

## 7. Migration Checklist

Before every production migration:

- [ ] Migration tested in staging environment
- [ ] Database backup taken
- [ ] Rollback plan documented
- [ ] Estimated downtime communicated (if any)
- [ ] Idempotent SQL script generated (EF Core)
- [ ] Content migration dry-run completed (item count, preview)
- [ ] Search reindex tested
- [ ] Health checks pass after migration
- [ ] Audit events for migration logged

## 8. Cross-References

- **Domain Model**: [domain-model.md](domain-model.md) — schema definitions
- **Module Boundaries**: [module-boundaries.md](module-boundaries.md) — each module owns its migrations
- **Runbook**: [runbook.md](runbook.md) — operational migration procedures
- **Golden Dataset**: [../.claude/testing/golden-dataset.md](../.claude/testing/golden-dataset.md) — seed data format
