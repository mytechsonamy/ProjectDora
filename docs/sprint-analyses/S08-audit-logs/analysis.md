# Sprint S08 — Audit Logs

## Kapsam (Scope)
- Spec items: 4.1.9.1, 4.1.9.2, 4.1.9.3, 4.1.9.4, 4.1.9.5
- Stories: US-901, US-902, US-903, US-904, US-905, US-906, US-907
- Cross-references: 4.1.3.6 (immutable content versioning — S03), 4.1.6.2 (content-type-level RBAC — S05), 4.1.7.3 (workflow event triggers — S06), 4.1.10.8 (multi-tenant isolation — S01)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.9.1 | Urunun yonetimi paneli uzerinden her turden kullanici tarafindan yapilan tum icerik guncellemelerine iliskin yapilan guncellemenin zaman, kullanici adi, kullanici IP adresi, gerceklesen olay turu seklinde olaya bagli temel bilgilerin tutulabilmesine imkan veren bir ozellige sahip olmasi | Audit log for all content updates from admin panel — record timestamp, username, user IP address, event type for each change |
| 4.1.9.2 | Bu kapsamda tutulacak denetim kayitlarinin geriye donuk olacak sekilde belirli gun ile sinirlandirilip daha eski kayitlarin performans / alan kazanimi amaciyla silinebilmesinin saglanmasi | Retention policy — limit audit records by day count, purge old records for performance and storage optimization |
| 4.1.9.3 | Denetim kayitlarinin urun uzerinde yer alan sadece belirli turdeki icerikleri kapsayacak sekilde yonetim panelinden sinirlandirilabilmesinin saglanmasi | Filter audit records by content type — admin panel allows scoping audit logs to specific content types |
| 4.1.9.4 | Icerik yonetimi kapsaminda denetim kayitlari tutulmasi arzu edilen bir icerik turune ait iceriklerin farkli versiyonlari arasinda gerceklesen temel iceriksel farklarin bir degisiklik izleme araci ile kullaniciya sunulabilmesi | Versioned diff tool — display content differences between versions of an audited content type |
| 4.1.9.5 | Bir icerige ait eski bir versiyonun tekrar yuklenebilmesinin (o versiyona donulmesinin) saglanmasi | Rollback capability — restore a content item to a previous version |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S03 (Content Management) must be complete — content versioning (4.1.3.6) provides the version history that audit logs track and diff
- **Dependency**: S05 (User/Role/Permission) must be complete — audit events record authenticated user identity and role
- **Dependency**: S06 (Workflow Engine) — audit events can trigger workflow activities (e.g., notify on rollback)
- **Orchard Core**: Built on `OrchardCore.AuditTrail` module for event capture; extended with custom `AuditTrailPart`
- **Database**: Audit records stored in `audit` schema (separate from `orchard` schema) via EF Core — no cross-schema direct SQL
- **Audit.NET**: Used as the underlying audit framework; Orchard Core events feed into Audit.NET pipeline
- **Tenant Isolation**: Every audit record includes `tenantId`; queries always filter by tenant
- **KVKK/GDPR**: Audit records may contain PII (username, IP) — retention policy must comply with data-governance.md rules
- **Performance**: Audit writes must be asynchronous (fire-and-forget via background queue) to avoid blocking content operations

### RBAC Gereksinimleri

| Permission | SuperAdmin | TenantAdmin | Editor | Author | Analyst | Denetci | Viewer |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AuditTrail.View | Y | Y | - | - | - | Y | - |
| AuditTrail.ViewAll | Y | Y | - | - | - | - | - |
| AuditTrail.ManageSettings | Y | Y | - | - | - | - | - |
| AuditTrail.ViewDiff | Y | Y | - | - | - | Y | - |
| AuditTrail.Rollback | Y | Y | - | - | - | - | - |
| AuditTrail.Export | Y | Y | - | - | - | Y | - |
| AuditTrail.Purge | Y | - | - | - | - | - | - |

- `AuditTrail.View` — View audit log entries for content types the user has access to
- `AuditTrail.ViewAll` — View all audit log entries regardless of content type scope
- `AuditTrail.ManageSettings` — Configure retention policies, content type filters, audit event types
- `AuditTrail.ViewDiff` — View versioned diffs between content versions
- `AuditTrail.Rollback` — Restore content to a previous version from audit trail
- `AuditTrail.Export` — Export audit log entries as CSV/JSON
- `AuditTrail.Purge` — Manually purge audit records (SuperAdmin only — destructive operation)

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-901 | 4.1.9.1 | P1 | Audit event capture — log content changes with timestamp, user, IP, event type |
| US-902 | 4.1.9.1 | P1 | Audit log listing and filtering in admin panel |
| US-903 | 4.1.9.3 | P1 | Content type scoping — configure which content types are audited |
| US-904 | 4.1.9.4 | P1 | Versioned diff viewer — compare content versions side-by-side |
| US-905 | 4.1.9.5 | P1 | Rollback to previous version from audit trail |
| US-906 | 4.1.9.2 | P2 | Retention policy — automatic purge of old audit records |
| US-907 | 4.1.9.1 | P0 | RBAC enforcement and tenant isolation on audit operations (security) |

### Priority Rationale

- **P0 (Security)**: US-907 — RBAC and tenant isolation on all audit operations is a security prerequisite; audit records may expose sensitive change history
- **P1 (Core)**: US-901 (event capture), US-902 (listing), US-903 (content type scoping), US-904 (diff viewer), US-905 (rollback) — these form the minimum viable audit trail system as specified in 4.1.9
- **P2 (Supporting)**: US-906 (retention/purge) — important for production operations but not blocking core functionality

## Teknik Kararlar (Technical Decisions)

### D-001: IAuditService Abstraction Layer
- All audit operations go through `IAuditService` interface defined in `ProjectDora.Core.Abstractions`
- Isolates Orchard Core `OrchardCore.AuditTrail` and Audit.NET dependencies
- Implementation lives in `ProjectDora.Modules.AuditTrail`

### D-002: Separate Audit Schema with EF Core
- Audit records stored in `audit` schema in PostgreSQL — separate from `orchard` schema
- EF Core `AuditDbContext` manages audit tables: `AuditEvents`, `AuditRetentionPolicies`, `AuditContentTypeScopes`
- No cross-schema JOINs — content data fetched via `IContentService` when needed for diff display
- Migrations managed independently via `dotnet ef migrations` targeting AuditDbContext

### D-003: Asynchronous Audit Write Pipeline
- Content change events captured synchronously via Orchard Core event handlers (`ContentItemPublished`, `ContentItemUpdated`, etc.)
- Audit record persistence is asynchronous — events enqueued to an in-memory channel (`System.Threading.Channels`)
- Background service (`AuditWriterBackgroundTask`) dequeues and persists to `audit` schema
- Fallback: If channel is full, write synchronously with warning log

### D-004: JSON Diff Algorithm for Version Comparison
- Content items stored as JSON (YesSql document store) — diff operates on JSON representations
- Use JsonDiffPatch.NET library (MIT license) for structural diff
- Diff output: list of added, removed, and modified fields with old/new values
- UI renders diff as side-by-side comparison with color coding (green=added, red=removed, yellow=modified)

### D-005: Retention Policy Implementation
- Configurable per-tenant: `RetentionDays` (default: 365), `MaxRecords` (default: 1,000,000)
- Background job runs daily at configurable time (default: 02:00 UTC)
- Purge deletes records older than `RetentionDays` in batches of 1,000 (avoid table locks)
- Purge summary logged as an audit event itself (`AuditPurgeCompleted`)

### D-006: Rollback Creates New Version
- Rollback does NOT delete or overwrite existing versions
- Instead, rollback creates a new version with the content data from the target version
- New version is marked with `RollbackSource` metadata pointing to the source version number
- Rollback emits `ContentRolledBack` audit event with source and target version details

See `docs/sprint-analyses/S08-audit-logs/decisions.md` for full decision details.

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-901-01 | Unit | US-901 | Content publish emits AuditEventCreated with correct metadata |
| TC-901-02 | Unit | US-901 | Audit event records timestamp, username, IP address, event type |
| TC-901-03 | Unit | US-901 | Audit event records content type and content item ID |
| TC-901-04 | Integration | US-901 | End-to-end: edit content item -> audit record persisted in audit schema |
| TC-901-05 | Unit | US-901 | Async audit write does not block content operation |
| TC-902-01 | Integration | US-902 | List audit events with pagination |
| TC-902-02 | Integration | US-902 | Filter audit events by date range |
| TC-902-03 | Integration | US-902 | Filter audit events by username |
| TC-902-04 | Integration | US-902 | Filter audit events by event type |
| TC-902-05 | Integration | US-902 | Filter audit events by content item ID |
| TC-903-01 | Unit | US-903 | Enable auditing for specific content type |
| TC-903-02 | Unit | US-903 | Disable auditing for specific content type |
| TC-903-03 | Integration | US-903 | Disabled content type events are not recorded |
| TC-904-01 | Unit | US-904 | JSON diff between two versions returns correct changes |
| TC-904-02 | Unit | US-904 | Diff shows added, removed, and modified fields |
| TC-904-03 | Integration | US-904 | Diff viewer renders side-by-side comparison |
| TC-904-04 | Unit | US-904 | Diff handles Turkish content correctly (UTF-8) |
| TC-905-01 | Unit | US-905 | Rollback creates new version with source version data |
| TC-905-02 | Unit | US-905 | Rollback emits ContentRolledBack audit event |
| TC-905-03 | Integration | US-905 | Rollback preserves all content parts and fields |
| TC-905-04 | Unit | US-905 | Rollback to non-existent version returns 404 |
| TC-906-01 | Unit | US-906 | Retention policy purges records older than configured days |
| TC-906-02 | Unit | US-906 | Purge runs in batches to avoid table locks |
| TC-906-03 | Integration | US-906 | Purge background job executes on schedule |
| TC-906-04 | Unit | US-906 | Purge emits AuditPurgeCompleted event |
| TC-907-01 | Security | US-907 | Anonymous user cannot view audit logs |
| TC-907-02 | Security | US-907 | Viewer role cannot view audit logs |
| TC-907-03 | Security | US-907 | Denetci can view audit logs but cannot rollback |
| TC-907-04 | Security | US-907 | Editor cannot view audit logs |
| TC-907-05 | Security | US-907 | Tenant A cannot view Tenant B audit records |
| TC-907-06 | Security | US-907 | Only SuperAdmin can purge audit records |

### Coverage Target
- Unit test coverage: >= 80% for AuditTrail module
- Integration test coverage: >= 60%
- Security tests: minimum 6 (RBAC on view, viewAll, rollback, purge, export + tenant isolation)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-901 complete
- [ ] US-902 complete
- [ ] US-903 complete
- [ ] US-904 complete
- [ ] US-905 complete
- [ ] US-906 complete
- [ ] US-907 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Viewing audit logs: How to navigate the audit log listing, filter by date, user, event type, content type
- Content type scoping: How to enable/disable auditing for specific content types from settings
- Versioned diff: How to compare two versions of a content item side-by-side
- Rollback: How to restore a content item to a previous version, what happens to the current version
- Retention settings: How to configure audit record retention period (days)
- Export: How to export audit records as CSV or JSON for external analysis
- Roles: Which roles can access audit features (Denetci for read-only, TenantAdmin for full management)

### Teknik Dokumantasyon (Technical Documentation)
- `IAuditService` interface contract — all audit operations and their behavior
- `audit` schema design: `AuditEvents`, `AuditRetentionPolicies`, `AuditContentTypeScopes` tables
- Asynchronous audit write pipeline: event handler -> channel -> background writer
- JSON diff algorithm: JsonDiffPatch.NET usage for content version comparison
- Retention policy background job configuration and batch purge logic
- Rollback mechanism: new version creation with `RollbackSource` metadata
- Tenant isolation enforcement in audit queries
- MediatR command/query handlers for all audit operations
- Audit.NET integration and Orchard Core AuditTrail module configuration

### API Endpoints
- `GET /api/v1/audit` — List audit events (with filtering, pagination)
- `GET /api/v1/audit/{auditEventId}` — Get audit event details
- `GET /api/v1/audit/content/{contentItemId}` — Get audit history for a specific content item
- `GET /api/v1/audit/diff/{contentItemId}?fromVersion={n}&toVersion={m}` — Get version diff
- `POST /api/v1/audit/rollback/{contentItemId}` — Rollback content item to a specific version
- `GET /api/v1/audit/settings` — Get audit settings (retention, content type scopes)
- `PUT /api/v1/audit/settings` — Update audit settings
- `POST /api/v1/audit/export` — Export audit records (CSV/JSON)
- `DELETE /api/v1/audit/purge` — Manual purge of old records (SuperAdmin only)

### Configuration Parameters
- `AuditTrail:Enabled` — Enable/disable audit trail globally (default: true)
- `AuditTrail:AsyncWrite` — Enable asynchronous audit writes (default: true)
- `AuditTrail:ChannelCapacity` — In-memory channel buffer size (default: 10000)
- `AuditTrail:Retention:DefaultDays` — Default retention period in days (default: 365)
- `AuditTrail:Retention:MaxRecords` — Maximum audit records per tenant (default: 1000000)
- `AuditTrail:Retention:PurgeSchedule` — Cron expression for purge job (default: "0 2 * * *")
- `AuditTrail:Retention:PurgeBatchSize` — Records deleted per batch (default: 1000)
- `AuditTrail:Diff:MaxContentSizeKb` — Maximum content size for diff computation (default: 512)
- `AuditTrail:Export:MaxRecords` — Maximum records per export (default: 50000)
