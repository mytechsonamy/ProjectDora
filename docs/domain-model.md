# ProjectDora Domain Model

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. System Context

```
┌──────────────────────────────────────────────────────────────────────┐
│                        ProjectDora Platform                          │
│                                                                      │
│  ┌─────────────┐  ┌────────────┐  ┌────────────┐                    │
│  │   CMS Core   │  │  Workflows  │  │  Queries   │                    │
│  └─────────────┘  └────────────┘  └────────────┘                    │
│                                                                      │
└──────────┬──────────────┬──────────────┬──────────────┬─────────────┘
           │              │              │              │
     ┌─────▼────┐  ┌─────▼────┐  ┌─────▼────┐  ┌─────▼────┐
     │PostgreSQL │  │  Redis   │  │Elastic/  │  │  MinIO   │
     │           │  │  Cache   │  │ Lucene   │  │ Storage  │
     └──────────┘  └──────────┘  └──────────┘  └──────────┘
```

### Actors

| Actor | Description | Access Level |
|-------|-------------|-------------|
| SuperAdmin | Platform-wide administration | Full access, all tenants |
| TenantAdmin | Tenant-level administration | Full access, own tenant |
| Editor | Content creation and publishing | Content CRUD + publish |
| Author | Content creation only | Content create + edit own |
| Analyst | Data analysis, reports | Query + analytics tools |
| Denetci (Auditor) | Audit trail review | Read-only audit + reports |
| SEOUzmani | SEO field management | Content read + SEO fields |
| WorkflowAdmin | Workflow design and management | Workflow CRUD + execute |
| Viewer | Read-only content access | Content read only |
| Anonymous | Public access | Public content only |
| External System | API consumer (headless CMS) | API key scoped |

## 2. Domain Model — Entity Relationship

### 2.1 ER Diagram (Text)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                  TENANT                                     │
│  tenant_id (PK), name, host, status, db_connection, created_utc            │
│                                                                             │
│  ┌──────────┐  ┌──────────────┐  ┌───────────────┐  ┌───────────────────┐  │
│  │   USER   │  │   ROLE       │  │ CONTENT_TYPE  │  │  WORKFLOW_DEF     │  │
│  │          │  │              │  │               │  │                   │  │
│  │ user_id  │  │ role_id      │  │ type_id       │  │ workflow_id       │  │
│  │ username │  │ name         │  │ name          │  │ name              │  │
│  │ email    │  │ permissions[]│  │ display_name  │  │ is_enabled        │  │
│  │ enabled  │  │              │  │ stereotype    │  │ start_activity_id │  │
│  │ tenant_id│  │ tenant_id    │  │ tenant_id     │  │ tenant_id         │  │
│  └────┬─────┘  └──────┬───────┘  └───────┬───────┘  └────────┬──────────┘  │
│       │               │                  │                    │             │
│       │  ┌────────────┐│   ┌─────────────┼────────────┐       │             │
│       │  │ USER_ROLE  ││   │             │            │       │             │
│       └──┤ user_id    ├┘   │    ┌────────▼──────┐     │  ┌────▼──────────┐  │
│          │ role_id    │    │    │  CONTENT_PART  │     │  │  WF_ACTIVITY  │  │
│          └────────────┘    │    │  part_id       │     │  │  activity_id  │  │
│                            │    │  name          │     │  │  name         │  │
│               ┌────────────▼──┐ │  type          │     │  │  properties{} │  │
│               │ CONTENT_FIELD │ └────────────────┘     │  └───────────────┘  │
│               │ field_id      │                        │                     │
│               │ name          │              ┌─────────▼──────────┐          │
│               │ type          │              │  WF_TRANSITION     │          │
│               │ settings{}    │              │  source_id         │          │
│               └───────────────┘              │  destination_id    │          │
│                            │                 │  condition         │          │
│                            │                 └────────────────────┘          │
│               ┌────────────▼──────────┐                                     │
│               │     CONTENT_ITEM      │                                     │
│               │  content_item_id (PK) │                                     │
│               │  content_type         │──────────────────┐                  │
│               │  display_text         │                  │                  │
│               │  status (Draft/Pub)   │     ┌────────────▼───────────┐      │
│               │  owner (FK→User)      │     │     CONTENT_VERSION    │      │
│               │  created_utc          │     │  version_id (PK)       │      │
│               │  modified_utc         │     │  content_item_id (FK)  │      │
│               │  published_utc        │     │  version_number        │      │
│               │  culture              │     │  data (JSON)           │      │
│               │  tenant_id            │     │  created_utc           │      │
│               └───────────┬───────────┘     │  created_by (FK→User) │      │
│                           │                 └────────────────────────┘      │
│                           │                                                 │
│               ┌───────────▼───────────┐                                     │
│               │      AUDIT_LOG        │                                     │
│               │  audit_id (PK)        │                                     │
│               │  entity_type          │                                     │
│               │  entity_id            │                                     │
│               │  action               │                                     │
│               │  user_id (FK→User)    │                                     │
│               │  timestamp            │                                     │
│               │  old_value (JSON)     │                                     │
│               │  new_value (JSON)     │                                     │
│               │  diff (JSON)          │                                     │
│               │  tenant_id            │                                     │
│               └───────────────────────┘                                     │
│                                                                             │
│  ┌──────────────────┐  ┌───────────────────┐                              │
│  │   SAVED_QUERY    │  │  SEARCH_INDEX     │                              │
│  │  query_id (PK)   │  │  index_name       │                              │
│  │  name            │  │  content_type     │                              │
│  │  type (Lucene/   │  │  record_count     │                              │
│  │   ES/SQL)        │  │  last_indexed_utc │                              │
│  │  query_text      │  │  tenant_id        │                              │
│  │  parameters{}    │  └───────────────────┘                              │
│  │  is_api_exposed  │                                                      │
│  │  tenant_id       │                                                      │
│  └──────────────────┘                                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Relationship Summary

| Parent | Child | Cardinality | Notes |
|--------|-------|-------------|-------|
| Tenant | User | 1:N | Users belong to exactly one tenant |
| Tenant | Role | 1:N | Roles scoped to tenant |
| Tenant | ContentType | 1:N | Types scoped to tenant |
| Tenant | ContentItem | 1:N | All content tenant-isolated |
| Tenant | WorkflowDef | 1:N | Workflows tenant-scoped |
| Tenant | SavedQuery | 1:N | Queries tenant-scoped |
| User | Role | M:N | Via UserRole join table |
| User | ContentItem | 1:N | Owner relationship |
| User | AuditLog | 1:N | Actor of the action |
| ContentType | ContentField | 1:N | Fields define the type schema |
| ContentType | ContentPart | M:N | Parts are reusable across types |
| ContentType | ContentItem | 1:N | Items are instances of a type |
| ContentItem | ContentVersion | 1:N | Full version history |
| ContentItem | AuditLog | 1:N | All changes audited |
| ContentItem | ContentItem | 1:N | Localization link (culture variants) |
| WorkflowDef | WFActivity | 1:N | Activities in a workflow |
| WFActivity | WFTransition | 1:N | Transitions between activities |

## 3. Aggregate Roots

In DDD terms, these are the aggregate roots that AI agents must respect:

| Aggregate Root | Owned Entities | Invariants |
|---------------|----------------|------------|
| **Tenant** | TenantSettings | All child data isolated; tenant_id on every query |
| **User** | UserRoles, UserProfile | Email unique per tenant; password meets policy |
| **ContentType** | ContentFields, ContentParts | Name unique per tenant; field names unique per type |
| **ContentItem** | ContentVersions, AuditLogs | Always has at least v1; status transitions: Draft→Published→Archived |
| **WorkflowDef** | Activities, Transitions | Start activity must exist; no orphan transitions |
| **SavedQuery** | QueryParameters | SQL queries must be SELECT-only; parameterized |

### State Machine: ContentItem

```
         ┌──────────┐
         │          │
    ┌────▼────┐     │ (edit)
    │  Draft  ├─────┘
    └────┬────┘
         │ (publish)
    ┌────▼──────┐
    │ Published │◄──────┐
    └────┬──────┘       │ (republish)
         │ (archive)    │
    ┌────▼─────┐   ┌────┴───┐
    │ Archived ├──►│ Draft  │ (restore to draft)
    └──────────┘   └────────┘

   (schedule)
    Draft ──► Scheduled ──► Published (auto at date)
```

### State Machine: WorkflowExecution

```
    ┌─────────┐
    │ Idle    │
    └────┬────┘
         │ (trigger event)
    ┌────▼──────┐
    │ Running   │◄──────┐
    └────┬──┬───┘       │ (retry)
         │  │           │
    (ok) │  │ (error)   │
         │  │    ┌──────┴──┐
    ┌────▼┐ └───►│ Faulted │
    │Done │      └─────────┘
    └─────┘
```

## 4. Database Schema Mapping

### Schema: `orchard`

YesSql document store — managed by Orchard Core.

| Table (YesSql) | Domain Entity | Notes |
|-----------------|--------------|-------|
| `Document` | ContentItem, ContentType, User, Role, WorkflowDef | JSON document storage |
| `ContentItemIndex` | ContentItem (indexed fields) | For fast queries |
| `UserIndex` | User (indexed fields) | Username, email lookup |
| `WorkflowIndex` | WorkflowDef (indexed fields) | Workflow queries |

### Schema: `audit`

EF Core managed — custom schema.

| Table | Domain Entity | Columns |
|-------|--------------|---------|
| `audit_logs` | AuditLog | audit_id, entity_type, entity_id, action, user_id, timestamp, old_value, new_value, diff, ip_address, tenant_id |
| `audit_retention_policies` | RetentionPolicy | policy_id, entity_type, retention_days, tenant_id |

### Schema: `analytics`

EF Core managed — denormalized for reporting.

| Table | Domain Entity | Columns |
|-------|--------------|---------|
| `destek_programlari` | DestekProgrami | id, program_adi, baslangic_tarihi, bitis_tarihi, butce, kategori, durum, tenant_id |
| `kobi_destekler` | KOBIDestek | id, kobi_adi, vergi_no, il, sektor, destek_programi_id, destek_miktari, basvuru_tarihi, onay_tarihi, durum, tenant_id |
| `kullanicilar` | KullaniciView | id, kullanici_adi, rol, son_giris, tenant_id |

## 5. Domain Events

Events emitted by aggregate roots, consumed by workflows and audit:

| Event | Source Aggregate | Payload | Consumers |
|-------|-----------------|---------|-----------|
| `TenantCreated` | Tenant | tenant_id, name | Audit, Schema Provisioner |
| `UserCreated` | User | user_id, username, email | Audit, Welcome Workflow |
| `UserDisabled` | User | user_id | Audit, Session Invalidator |
| `RoleAssigned` | User | user_id, role_id | Audit |
| `ContentTypeCreated` | ContentType | type_id, name, fields[] | Audit, Search Indexer |
| `ContentTypeModified` | ContentType | type_id, changes{} | Audit, Search Reindexer |
| `ContentItemCreated` | ContentItem | item_id, type, owner | Audit, Search Indexer, Workflow |
| `ContentItemUpdated` | ContentItem | item_id, version, diff | Audit, Search Indexer, Workflow |
| `ContentItemPublished` | ContentItem | item_id, version | Audit, Search Indexer, Workflow, Cache Invalidator |
| `ContentItemDeleted` | ContentItem | item_id, soft/hard | Audit, Search Indexer, Cache Invalidator |
| `ContentItemVersionRolledBack` | ContentItem | item_id, from_version, to_version | Audit |
| `WorkflowTriggered` | WorkflowDef | workflow_id, trigger_event, context | Audit, Workflow Engine |
| `WorkflowCompleted` | WorkflowDef | workflow_id, execution_id, result | Audit |
| `WorkflowFaulted` | WorkflowDef | workflow_id, execution_id, error | Audit, Alert |
| `QueryExecuted` | SavedQuery | query_id, user_id, result_count | Audit |

## 6. Value Objects

| Value Object | Used By | Properties | Invariants |
|-------------|---------|------------|------------|
| `TenantId` | All entities | string value | Non-empty, alphanumeric + hyphen |
| `ContentItemId` | ContentItem, Version, AuditLog | string value | Globally unique (GUID-based) |
| `Culture` | ContentItem, Localization | string value | Valid BCP 47 (tr, en, de, etc.) |
| `Permission` | Role | string value | Dot-separated: `Module.Action` |
| `Slug` | ContentItem (SEO URL) | string value | Lowercase, hyphenated, ASCII |
| `FieldType` | ContentField | enum | TextField, NumericField, DateField, BooleanField, MediaField, ContentPickerField |
| `ContentStatus` | ContentItem | enum | Draft, Published, Archived, Scheduled |
| `QueryType` | SavedQuery | enum | Lucene, Elasticsearch, SQL |
| `AuditAction` | AuditLog | enum | Created, Updated, Published, Deleted, RolledBack, Accessed |
| `DataSensitivity` | (classification) | enum | Public, Internal, Sensitive, PII |

## 7. Cross-References

- **Module Boundaries**: [module-boundaries.md](module-boundaries.md) — which modules own which aggregates
- **API Contract**: [api-contract.yaml](api-contract.yaml) — REST/GraphQL endpoints for each aggregate
- **Data Governance**: [data-governance.md](data-governance.md) — sensitivity classification per entity
- **Test Cases**: [../. claude/testing/test-cases.md](../.claude/testing/test-cases.md) — tests per domain entity
- **Golden Dataset**: [../.claude/testing/golden-dataset.md](../.claude/testing/golden-dataset.md) — fixture data per entity
