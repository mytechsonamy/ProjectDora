# Sprint S04 — Decisions Log

## D-001: IQueryService Abstraction Over Orchard Core Queries
- **Date**: 2026-03-09
- **Context**: Orchard Core provides `OrchardCore.Queries` module with built-in Lucene and SQL query support. We need to decide whether to use it directly or wrap it in an abstraction layer.
- **Decision**: Wrap all query operations behind `IQueryService`, `ISavedQueryService`, and `ISearchIndexService` interfaces defined in `ProjectDora.Core`. The Orchard Core implementation lives in `ProjectDora.Modules.QueryEngine`.
- **Consequences**: Additional code for the adapter layer, but consistent with the modular monolith abstraction pattern (ADR-001). Enables future backend swaps (e.g., replacing Lucene with MeiliSearch) without module-level changes. All other modules reference interfaces, not Orchard Core directly.
- **ADR**: ADR-001 (Modular Monolith)

## D-002: Turkish Analyzer as Default for All Text Indexes
- **Date**: 2026-03-09
- **Context**: The platform's primary language is Turkish. Turkish has unique lowercase rules (I->i vs I->i) and stemming patterns that differ significantly from English. Standard analyzers will produce incorrect search results for Turkish text.
- **Decision**: Configure TurkishAnalyzer as the default analyzer for all Lucene indexes and Elasticsearch mappings. Use `TurkishLowerCaseFilter` exclusively — never the generic `LowerCaseFilter`. Index definitions must specify `"Culture": "tr"` and `"AnalyzerName": "turkish"`.
- **Consequences**: Non-Turkish content (if any in future) may need separate indexes with appropriate analyzers. Multi-language search would require per-culture index strategy (deferred to S07-S08 multi-language sprint). Turkish search quality will be significantly improved.
- **ADR**: N/A

## D-003: SQL Safety Validator — Whitelist + Blacklist Hybrid
- **Date**: 2026-03-09
- **Context**: Spec 4.1.5.4 requires SQL query development capability. This is a significant security surface — user-defined SQL could lead to data modification or exfiltration.
- **Decision**: Implement `SqlSafetyValidator` with: (1) Only SELECT statements allowed — query must start with SELECT after normalization. (2) Blacklist: INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, GRANT, REVOKE, EXEC, EXECUTE keywords blocked via word-boundary regex. (3) Mandatory tenant_id filter injection into every query. (4) Parameterized execution only — no string concatenation. (5) 30-second timeout. (6) Analytics schema only — no access to `orchard` or `audit` schemas.
- **Consequences**: Users cannot write arbitrary DDL/DML. The blacklist approach has known bypass risks (e.g., CTEs, subqueries), so we also restrict to `analytics` schema access only via database user permissions. Defense in depth.
- **ADR**: N/A (security implementation detail)

## D-004: Elasticsearch as Optional Production Component
- **Date**: 2026-03-09
- **Context**: Spec 4.1.5.3 requires Elasticsearch for centralized indexing in distributed deployments. However, not all deployments will be distributed — small/single-instance setups should work with Lucene.NET only.
- **Decision**: Make Elasticsearch optional. Lucene.NET is the default and always-available search engine. Elasticsearch activates when configured (`QueryEngine:Elasticsearch:Url` is set). Implement automatic fallback: if ES cluster is unreachable, degrade to Lucene.NET with a warning logged. Index sync: content changes are written to both Lucene and ES (when available) via domain event handlers.
- **Consequences**: Dual-write to both engines adds complexity. Need to handle eventual consistency between Lucene and ES indexes. Fallback behavior must be tested via chaos/resilience tests (ref: resilience-and-chaos-tests.md). Lucene indexes are always maintained regardless of ES availability.
- **ADR**: N/A

## D-005: Per-Query Permission Model
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.4 states that each saved query should have its own permission controlling whether it can be executed via API. This goes beyond the module-level `QueryEngine.Execute` permission.
- **Decision**: Each `SavedQuery` entity has an `isApiExposed` boolean flag. When a query is API-exposed, the system auto-generates a permission `QueryEngine.Execute.{QueryName}`. This permission must be assigned to roles that should be able to call the query via the REST API. Admin panel execution still requires only `QueryEngine.Execute`.
- **Consequences**: Permission proliferation risk if many queries are created. Mitigated by only generating permissions for API-exposed queries. The permission model stays flat (no hierarchical permission tree). Integration point with S05-S06 (User/Role/Permission sprint) for UI to manage per-query permissions.
- **ADR**: N/A

## D-006: Query Result Caching Strategy
- **Date**: 2026-03-09
- **Context**: Lucene and Elasticsearch queries can be expensive. SQL queries hit the analytics database directly. Need to balance freshness vs performance.
- **Decision**: Lucene query results are cached via Redis with ReadThrough strategy, 5-minute TTL, key pattern `query:lucene:{tenantId}:{queryHash}`. SQL queries are NOT cached by default (analytics data freshness is critical). Each saved query can opt-in to caching via a `cacheTtl` field. Cache is invalidated on `ContentItemPublished` and `ContentItemUpdated` events for affected content types.
- **Consequences**: Redis dependency for caching (already in tech stack). Cache stampede risk on popular queries — mitigate with cache-aside lock pattern. SQL opt-in caching needs admin panel UI for configuration (deferred to US-507 parameterized execution).
- **ADR**: N/A

## D-007: Query Integration Points (Liquid + Workflow)
- **Date**: 2026-03-09
- **Context**: Spec 4.1.5.6 requires queries to be callable from the theme template engine (Liquid) and workflow activities. This creates cross-module dependencies.
- **Decision**: Expose queries via: (1) Liquid filter: `{% assign results = Queries.ExecuteQuery "queryName" %}` — leverages Orchard Core's built-in Liquid query integration. (2) Workflow activity: `QueryActivity` custom activity that takes query name + parameters as inputs and outputs results to the workflow context. Both integration points use `ISavedQueryService.ExecuteAsync()` — same abstraction, same RBAC checks, same audit events.
- **Consequences**: Creates dependency from ThemeManagement and Workflow modules on QueryEngine interfaces. This is acceptable per module-boundaries.md — QueryEngine is a provider, others are consumers. The Liquid integration leverages Orchard Core's existing `QueryFilter` — minimal custom code.
- **ADR**: N/A
