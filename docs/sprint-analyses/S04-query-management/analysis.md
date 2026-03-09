# Sprint S04 — Query Management

## Kapsam (Scope)
- Spec items: 4.1.5.1, 4.1.5.2, 4.1.5.3, 4.1.5.4, 4.1.5.5, 4.1.5.6
- Stories: US-501, US-502, US-503, US-504, US-505, US-506, US-507, US-508
- Cross-references: 4.1.6.4 (query-level permissions per saved query), 4.1.11.6 (auto-API from SQL queries), 4.1.4.3 (template queries from design management), 4.1.7.5 (workflow activities access queries)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkish Text | Summary |
|------|-------------|---------|
| 4.1.5.1 | Sunulan urun, olusturulan icerik kayitlari uzerinde tam metin arama (full-text search) yapilabilmesine imkan saglayacak sekilde Apache Lucene teknolojisini kullanilmasi | Full-text search on content records using Apache Lucene technology |
| 4.1.5.2 | Dogrudan yonetim paneli uzerinden arama sorgulari duzenlenebilmesi, bu sorgularin test amaciyla calistirilabilmesi ve sorgu kapsaminda olusturulan indekslerin tekrar olusturulabilir yapida olmasi | Admin panel query editor with test execution and rebuildable indexes |
| 4.1.5.3 | Dagitik mimari kapsaminda ya da birden fazla farkli turdeki kurulum (instance) tarafindan uretilen indekslerin merkezi bir yapi uzerinde barindirilmasi ve analize tabi tutabilmesi icin Elasticsearch uzerinde arsivlenmesinin saglanmasi | Elasticsearch centralized index hosting for distributed/multi-instance architecture |
| 4.1.5.4 | Sunulan urun, o kurulumda kullanilan veri tabani uzerinde dogrudan calistirilabilecek sekilde SQL ve Lucene sorgular gelistirilebilir olmasi | Direct SQL and Lucene query development against the installation database |
| 4.1.5.5 | Olusturulan sorgularin yonetim paneli uzerinden dogrudan veya sorguya bagli olacak sekilde cesitli parametreler ile calistirilip test edilebilmesi | Query execution with parameters and testing from admin panel |
| 4.1.5.6 | Olusturulan bu sorgularin tasarim yonetimi kapsaminda urun tarafindan sunulacak kod editoru ve is akisi yonetimi kapsamindaki aktiviteler tarafindan cesitli kod ifadeleri ile dogrudan calistirilabilir olmasi | Queries executable from theme code editor and workflow activities |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S02 (Content Modeling) and S03 (Content Management) must be complete — content types and items must exist before queries can search them
- **Orchard Core**: Query management built on `OrchardCore.Queries`, `OrchardCore.Queries.Sql`, `OrchardCore.Lucene` modules
- **Search Engines**: Lucene.NET for dev/small deployments, Elasticsearch for production/distributed
- **Turkish Language**: TurkishAnalyzer required for correct stemming and lowercase rules (I vs i vs i)
- **SQL Safety**: Only SELECT queries allowed; INSERT/UPDATE/DELETE/DROP forbidden via SqlSafetyValidator
- **Tenant Isolation**: Every query must filter by tenant_id — enforced at service layer
- **Multi-tenant**: Each tenant has its own saved queries and indexes

### RBAC Gereksinimleri

| Permission | SuperAdmin | TenantAdmin | Editor | Author | Analyst | Viewer |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|
| QueryEngine.Manage | Y | Y | - | - | - | - |
| QueryEngine.Create | Y | Y | - | - | - | - |
| QueryEngine.Delete | Y | Y | - | - | - | - |
| QueryEngine.Execute | Y | Y | - | - | Y | - |

- `QueryEngine.Manage` — Full CRUD on saved queries, index management, rebuild indexes
- `QueryEngine.Create` — Create and edit saved queries
- `QueryEngine.Delete` — Delete saved queries
- `QueryEngine.Execute` — Execute saved queries and ad-hoc searches
- Per-query permissions (4.1.6.4): Each saved query can have its own permission controlling API execution

### Story Decomposition

| Story | Spec | Priority | Description |
|-------|------|----------|-------------|
| US-501 | 4.1.5.1 | P1 | Lucene full-text search with Turkish analyzer |
| US-502 | 4.1.5.2 | P1 | Query CRUD from admin panel |
| US-503 | 4.1.5.2 | P1 | Query test execution from admin panel |
| US-504 | 4.1.5.2 | P2 | Search index rebuild capability |
| US-505 | 4.1.5.3 | P2 | Elasticsearch integration for centralized indexing |
| US-506 | 4.1.5.4 | P1 | SQL query development with safety validation |
| US-507 | 4.1.5.5 | P1 | Parameterized query execution |
| US-508 | 4.1.5.6 | P2 | Query integration with Liquid templates and workflows |

### Priority Rationale

- **P0 (Security)**: SQL injection prevention is embedded in US-506 as a core constraint, not a separate story — every SQL query story must enforce parameterization and safety validation
- **P1 (Core CRUD)**: US-501 (Lucene search), US-502 (query CRUD), US-503 (test execution), US-506 (SQL queries), US-507 (parameterized execution) — these form the minimum viable query engine
- **P2 (Supporting)**: US-504 (index rebuild), US-505 (Elasticsearch), US-508 (template/workflow integration) — important but can be deferred without blocking other sprints

## Teknik Kararlar (Technical Decisions)

### D-001: IQueryService Abstraction Layer
- All query operations go through `IQueryService` and `ISavedQueryService` interfaces
- Isolates Orchard Core `OrchardCore.Queries` dependency
- Enables swap between Lucene/Elasticsearch/SQL backends transparently

### D-002: Turkish Analyzer Configuration
- Lucene.NET: Use `TurkishAnalyzer` from `Lucene.Net.Analysis.Common`
- Elasticsearch: Custom analyzer with `turkish_stop` and `turkish_stemmer` filters
- Critical: Never use `LowerCaseFilter` — always `TurkishLowerCaseFilter` for correct I/i mapping

### D-003: SQL Safety Validator
- Whitelist approach: Only `SELECT` statements allowed
- Blacklist keywords: INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, GRANT, REVOKE, EXEC, EXECUTE
- Mandatory tenant_id injection into every SQL query WHERE clause
- 30-second query timeout on all SQL executions

### D-004: Search Index Strategy
- Development/small: Lucene.NET local indexes per tenant
- Production: Elasticsearch with tenant-prefixed index names
- Automatic fallback: ES down -> Lucene.NET (degraded mode)
- Index rebuild via `ISearchIndexService.ReindexAsync()`

### D-005: Caching Strategy
- Lucene query results: ReadThrough cache, 5min TTL, key pattern `query:lucene:{tenantId}:{queryHash}`
- SQL query results: No cache by default (data freshness), opt-in per saved query
- Cache invalidation: On content publish/update events via `ContentItemPublished` / `ContentItemUpdated` handlers

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-501-01 | Unit | US-501 | Lucene simple keyword search returns results |
| TC-501-02 | Unit | US-501 | Turkish stemming: "destekler" finds "destek" |
| TC-501-03 | Unit | US-501 | Turkish lowercase: "istanbul" finds "Istanbul" |
| TC-501-04 | Integration | US-501 | End-to-end Lucene search with indexed content |
| TC-501-05 | Security | US-501 | Unauthorized user cannot execute queries |
| TC-502-01 | Unit | US-502 | Create saved Lucene query |
| TC-502-02 | Unit | US-502 | Create saved SQL query |
| TC-502-03 | Unit | US-502 | Update saved query |
| TC-502-04 | Unit | US-502 | Delete saved query |
| TC-502-05 | Security | US-502 | Viewer cannot create queries |
| TC-503-01 | Integration | US-503 | Test-execute Lucene query from admin panel |
| TC-503-02 | Integration | US-503 | Test-execute SQL query from admin panel |
| TC-504-01 | Integration | US-504 | Rebuild Lucene index for content type |
| TC-504-02 | Integration | US-504 | Rebuild all indexes |
| TC-505-01 | Integration | US-505 | Elasticsearch index creation |
| TC-505-02 | Integration | US-505 | Elasticsearch query execution |
| TC-505-03 | Integration | US-505 | Fallback to Lucene when ES unavailable |
| TC-506-01 | Unit | US-506 | Valid SELECT query passes safety check |
| TC-506-02 | Unit | US-506 | DROP TABLE blocked by safety validator |
| TC-506-03 | Unit | US-506 | SQL injection attempt blocked |
| TC-506-04 | Security | US-506 | Tenant isolation enforced on SQL queries |
| TC-507-01 | Unit | US-507 | Parameterized query execution |
| TC-507-02 | Unit | US-507 | Missing required parameter returns error |
| TC-508-01 | Integration | US-508 | Query callable from Liquid template |
| TC-508-02 | Integration | US-508 | Query callable from workflow activity |

### Coverage Target
- Unit test coverage: >= 80% for QueryEngine module
- Integration test coverage: >= 60%
- Security tests: minimum 5 (SQL injection, RBAC, tenant isolation, parameter injection, query safety)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-501 complete
- [ ] US-502 complete
- [ ] US-503 complete
- [ ] US-504 complete
- [ ] US-505 complete
- [ ] US-506 complete
- [ ] US-507 complete
- [ ] US-508 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Creating and managing saved queries (Lucene, SQL, Elasticsearch)
- Query editor interface in admin panel
- Testing queries with parameters
- Understanding query types: full-text search vs SQL analytics
- Rebuilding search indexes when needed
- Turkish search behavior: stemming, special characters

### Teknik Dokumantasyon (Technical Documentation)
- `IQueryService` and `ISavedQueryService` interface contracts
- Turkish analyzer configuration for Lucene and Elasticsearch
- SQL safety validation rules and forbidden keywords
- Tenant isolation enforcement in query execution
- Search index management and rebuild procedures
- Elasticsearch cluster configuration and fallback behavior
- Caching strategy for query results
- MediatR command/query handlers for all query operations

### API Endpoints
- `GET /api/v1/queries` — List saved queries
- `POST /api/v1/queries` — Create a saved query
- `POST /api/v1/queries/{queryId}/execute` — Execute a saved query with parameters
- `GET /api/v1/queries/lucene` — Ad-hoc Lucene search (from US-501 DoR example)
- `POST /api/v1/search/reindex` — Trigger index rebuild
- `GET /api/v1/search/status` — Get search index status

### Configuration Parameters
- `QueryEngine:Lucene:AnalyzerName` — Default analyzer (turkish)
- `QueryEngine:Lucene:IndexPath` — Local index file storage path
- `QueryEngine:Elasticsearch:Url` — Elasticsearch cluster URL
- `QueryEngine:Elasticsearch:IndexPrefix` — Tenant prefix for ES indexes
- `QueryEngine:Sql:CommandTimeout` — SQL query timeout in seconds (default: 30)
- `QueryEngine:Sql:MaxResultRows` — Maximum rows returned (default: 10000)
- `QueryEngine:Cache:Enabled` — Enable/disable query result caching
- `QueryEngine:Cache:DefaultTtl` — Default cache TTL (default: 5m)
