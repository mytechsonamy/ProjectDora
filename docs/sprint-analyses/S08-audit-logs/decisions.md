# Sprint S08 — Decisions Log

## D-001: IAuditService Abstraction Over Orchard Core AuditTrail
- **Date**: 2026-03-09
- **Context**: Orchard Core provides the `OrchardCore.AuditTrail` module for audit event capture. We also use Audit.NET as a cross-cutting audit framework. We need to decide whether to use these directly or wrap them in an abstraction layer.
- **Decision**: Wrap all audit operations behind `IAuditService` interface defined in `ProjectDora.Core.Abstractions`. The Orchard Core + Audit.NET implementation lives in `ProjectDora.Modules.AuditTrail`. Key methods: `LogEventAsync()`, `GetEventsAsync()`, `GetEventByIdAsync()`, `GetContentHistoryAsync()`, `GetDiffAsync()`, `RollbackAsync()`, `ConfigureRetentionAsync()`, `PurgeAsync()`, `ExportAsync()`.
- **Consequences**: Additional adapter code, but consistent with the modular monolith abstraction pattern (ADR-001). All other modules reference `IAuditService` for audit logging — they never import Orchard Core or Audit.NET directly. Enables future swap of audit backend (e.g., to a dedicated audit service or event store) without module-level changes.
- **ADR**: ADR-001 (Modular Monolith)

## D-002: Separate audit Schema with EF Core
- **Date**: 2026-03-09
- **Context**: Audit records can grow rapidly and must not affect the performance of the main `orchard` schema. The architecture blueprint defines four schemas: `orchard`, `audit`, `ai`, `analytics`. Each schema owns its migrations and is accessed only by its owning service.
- **Decision**: Create dedicated `AuditDbContext` targeting the `audit` PostgreSQL schema. Tables: `AuditEvents` (main event log), `AuditRetentionPolicies` (per-tenant retention config), `AuditContentTypeScopes` (which content types are audited). EF Core migrations managed independently. No cross-schema JOINs — when the diff viewer needs content data, it calls `IContentService.GetAsync()` from the `orchard` schema via the abstraction layer.
- **Consequences**: Clean separation of concerns. Audit tables can be on separate tablespace or even separate database for large deployments. Independent backup/restore of audit data. Slightly more complex deployment (two migration sets), but this is already established by the four-schema architecture.
- **ADR**: N/A (follows existing schema separation pattern)

## D-003: Asynchronous Audit Write Pipeline
- **Date**: 2026-03-09
- **Context**: Audit event capture must not degrade content management performance. Every content operation (create, update, publish, delete) generates an audit event. Synchronous database writes would add latency to every content operation.
- **Decision**: Implement a two-phase write pipeline: (1) Orchard Core event handlers (`IContentHandler` implementations) capture audit event data synchronously and enqueue to a bounded `System.Threading.Channels.Channel<AuditEvent>` (capacity: 10,000). (2) A hosted background service (`AuditWriterBackgroundService`) reads from the channel and persists to the `audit` schema in batches. Fallback: if channel is full (back-pressure), write synchronously with a warning via Serilog. Channel capacity is configurable via `AuditTrail:ChannelCapacity`.
- **Consequences**: Near-zero latency impact on content operations. Risk of audit event loss on application crash (in-memory channel is volatile). Mitigated by: (a) bounded channel with back-pressure fallback, (b) Serilog file log as secondary audit trail for crash recovery, (c) batch size of 100 for frequent flushes. Acceptable trade-off per resilience-and-chaos-tests.md — audit is "eventual consistency" tier.
- **ADR**: N/A (infrastructure implementation detail)

## D-004: JSON Diff via JsonDiffPatch.NET
- **Date**: 2026-03-09
- **Context**: Spec 4.1.9.4 requires a change tracking tool that shows content differences between versions. Content items are stored as JSON documents in YesSql. We need a diff algorithm that works on JSON structures and handles Turkish UTF-8 content.
- **Decision**: Use JsonDiffPatch.NET (MIT license, NuGet: `JsonDiffPatch.Net`) for structural JSON diff. The diff output is a JSON patch document listing added, removed, and modified properties. The UI renders this as a side-by-side comparison: left=old version, right=new version, with color coding (green=added, red=removed, yellow=modified). For large content items (>512KB JSON), diff is computed asynchronously and cached for 10 minutes. Turkish UTF-8 content is handled natively by the library.
- **Consequences**: JsonDiffPatch.NET is well-maintained (MIT license, compatible with open-source requirement). Handles nested JSON structures (content parts, fields) naturally. For very large documents, diff computation may be expensive — the 512KB limit and caching mitigate this. Alternative considered: custom field-by-field comparison — rejected as too much custom code for the same result.
- **ADR**: N/A

## D-005: Retention Policy with Background Purge Job
- **Date**: 2026-03-09
- **Context**: Spec 4.1.9.2 requires retention policies to limit audit records by day count and purge old records for performance. Audit tables can grow to millions of rows in production.
- **Decision**: Per-tenant configurable retention: `RetentionDays` (default: 365, min: 30, max: 3650) and `MaxRecords` (default: 1,000,000). A Quartz.NET-scheduled background job (`AuditPurgeJob`) runs daily (configurable via cron expression, default: `0 2 * * *`). Purge deletes in batches of 1,000 rows using `DELETE FROM audit.AuditEvents WHERE TenantId = @tid AND CreatedUtc < @cutoff LIMIT 1000` in a loop until no more matching rows. Each purge run emits an `AuditPurgeCompleted` event (itself an audit record) with count of purged records. Manual purge available via API (SuperAdmin only).
- **Consequences**: Batch deletion avoids long-running transactions and table locks. Daily schedule ensures predictable maintenance window. KVKK/GDPR compliance: retention period must be documented in data governance policy. Risk: if purge job fails, records accumulate — mitigated by alerting on purge job failures via OpenTelemetry metrics.
- **ADR**: N/A

## D-006: Rollback Creates New Version (Non-Destructive)
- **Date**: 2026-03-09
- **Context**: Spec 4.1.9.5 requires rollback capability — restoring a content item to a previous version. This must work with the immutable versioning model from S03 (4.1.3.6) which states that old versions are always preserved.
- **Decision**: Rollback is non-destructive. It creates a NEW content version with the data from the target (older) version. The new version gets the next sequential version number and is marked with `RollbackSource = targetVersionNumber` in metadata. The rollback emits a `ContentRolledBack` audit event recording: content item ID, source version number, new version number, user who performed the rollback. Rollback uses `IContentService.RollbackAsync(contentItemId, targetVersion)` which was defined in S03 but implemented here with full audit integration.
- **Consequences**: Complete audit trail preserved — every state is traceable. No data loss from rollback operations. The rolled-back version can itself be rolled back (undo the undo). Slight increase in version count and storage, but acceptable given the immutable versioning design. Users can always see "this version was created by rollback from version N" in the version history.
- **ADR**: N/A (consistent with S03 immutable versioning decision D-003)

## D-007: Audit Event Type Registry
- **Date**: 2026-03-09
- **Context**: Multiple modules emit audit events (content management, query engine, user management, workflows). We need a consistent event type taxonomy to enable meaningful filtering in the audit log.
- **Decision**: Define an `AuditEventType` enum/constants registry in `ProjectDora.Core`:
  - Content events: `ContentCreated`, `ContentUpdated`, `ContentPublished`, `ContentUnpublished`, `ContentDeleted`, `ContentRolledBack`, `ContentCloned`
  - User events: `UserLoggedIn`, `UserLoggedOut`, `UserCreated`, `UserUpdated`, `UserDeleted`, `RoleAssigned`, `RoleRevoked`
  - Settings events: `SettingsUpdated`, `RetentionPolicyUpdated`, `ContentTypeScopeUpdated`
  - System events: `AuditPurgeCompleted`, `AuditExported`
  - Each event type has a severity level: `Info`, `Warning`, `Critical`
- **Consequences**: Consistent taxonomy across all modules. New modules can register additional event types via `IAuditEventTypeProvider` interface. Filtering by event type in the admin panel is straightforward. Event types are stored as strings (not enum int values) for forward compatibility.
- **ADR**: N/A

## D-008: Audit Export Format
- **Date**: 2026-03-09
- **Context**: The Denetci (Auditor) role needs to export audit records for compliance review and external analysis. We need to support standard export formats.
- **Decision**: Support two export formats: CSV (for Excel/reporting tools) and JSON (for programmatic consumption). Export is paginated and streamed (not loaded entirely into memory). Maximum export size: 50,000 records per request (configurable). Export includes all audit event fields: timestamp, username, IP address, event type, content type, content item ID, summary. CSV uses UTF-8 with BOM for Turkish character support in Excel. Export emits an `AuditExported` audit event.
- **Consequences**: CSV+BOM ensures Turkish characters (i, s, c, g, o, u) display correctly in Excel. Streaming export supports large datasets without memory pressure. The 50K record limit prevents abuse and can be adjusted per deployment. JSON export enables integration with external SIEM/log analysis tools.
- **ADR**: N/A
