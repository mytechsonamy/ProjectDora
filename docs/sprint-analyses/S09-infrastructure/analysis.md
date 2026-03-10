# Sprint S09 — Infrastructure

## Kapsam (Scope)
- Spec items: 4.1.10.1, 4.1.10.2, 4.1.10.3, 4.1.10.4, 4.1.10.5, 4.1.10.6, 4.1.10.7, 4.1.10.8, 4.1.10.9, 4.1.10.10, 4.1.10.11, 4.1.10.22
- Stories: US-1001, US-1002, US-1003, US-1004, US-1005, US-1006, US-1007
- Cross-references: 4.1.6.1 (user/role tenant scoping — S05), 4.1.9.1 (audit log tenant isolation — S08), 4.1.1.1 (admin panel auth bootstrap — S01), 4.1.11.4 (API auth via OpenID — S10), 4.1.12.1 (AI services offline capability)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.10.1 | Urunun birden fazla kurumun veya birimin bagımsız olarak kullanabilecegi cok kiracılı (multi-tenant) bir yapıda sunulabilmesi | Platform must support multi-tenant deployment — each tenant (e.g., separate KOSGEB department or partner institution) operates in full isolation with no data bleed between tenants |
| 4.1.10.2 | Her bir kiracı icin veritabanı, icerik ve kullanıcı verilerinin tamamen izole edilmis bir yapıda tutulması | Per-tenant data isolation at database schema level — tenant content, users, and settings stored separately; no cross-tenant direct SQL or shared document namespaces |
| 4.1.10.3 | Yuksek frekanslı veritabanı sorgularının onbellek katmanında tutularak platform performansının artırılması | Redis cache layer for high-frequency database queries — read-through caching for content listings, permission lookups, and query results to reduce PostgreSQL load |
| 4.1.10.4 | Onbellekteki verilerin belirli bir sure (TTL) ile sınırlandırılması ve guncel olmayan verilerin otomatik olarak temizlenmesi | Cache TTL management — configurable TTL per cache category; automatic eviction of stale entries using Redis LRU policy; explicit invalidation on write operations |
| 4.1.10.5 | Platformun baslangıc yapılandırmasının, icerik tiplerinin ve rol tanımlarının bir tarif (recipe) dosyası aracılıgıyla iceri aktarılabilmesi | Recipe-based initial configuration — JSON recipe files drive tenant setup including content type definitions, role assignments, initial content seeding, and settings configuration |
| 4.1.10.6 | Tarif dosyalarının yonetim panelinden yuklenebilmesi ve dısa aktarılabilmesi | Recipe import/export via admin panel — administrators can upload recipe files to configure tenants and export current configuration as a portable recipe for backup or replication |
| 4.1.10.7 | Platform uzerinde OpenID Connect protokolu ile entegre kimlik dogrulama altyapısının saglanması | OpenID Connect (OIDC) authentication infrastructure — JWT bearer tokens issued by Orchard Core's built-in OIDC provider; supports client credentials, authorization code, and resource owner flows |
| 4.1.10.8 | Cok kiracılı yapıda her kiracının kendi kullanıcı ve yetki konfigurasyonuna sahip olması | Tenant-scoped user and permission configuration — each tenant has its own user store, role definitions, and permission assignments; SuperAdmin operates across tenant boundaries |
| 4.1.10.9 | Platformun arama motoru altyapısının uretim ortamı icin Elasticsearch, gelistirme/kucuk ortamlar icin Lucene.NET ile calisabilmesi | Dual search engine support — Elasticsearch for production deployments, Lucene.NET for development and small deployments; abstracted via ISearchService interface to allow switching without code changes |
| 4.1.10.10 | Platform uzerindeki kamuya acık icerikler icin XML site haritası (sitemap) olusturulabilmesi ve guncel tutulabilmesi | XML sitemap generation — auto-generate and maintain sitemap.xml for publicly accessible content; configurable per content type and tenant; ping search engines on update |
| 4.1.10.11 | Platform altyapısının Windows ve Linux isletim sistemlerinde Docker konteynerlari ile calistırılabilir olması | Cross-platform deployment via Docker — platform must run on both Windows (Docker Desktop) and Linux (production server); Docker Compose orchestration for all services |
| 4.1.10.22 | (Derived from 4.1.6, 4.1.1) Platform kimlik dogrulama altyapısının cevrimdısı calısabilmesi ve yerel token dogrulamasını desteklemesi | Offline-capable authentication — OIDC token validation must work without internet access; all signing keys cached locally; no external identity provider dependency |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: All previous sprints (S01–S08) must be complete — infrastructure is a cross-cutting concern that wraps, isolates, and governs all prior modules
- **Dependency**: S05 (User/Role/Permission) — tenant-scoped user stores and permission tables must already be built before tenant isolation layer can be applied
- **Dependency**: S08 (Audit Logs) — audit schema's tenant-filtering patterns inform the infrastructure isolation model
- **Orchard Core**: Multi-tenancy via `OrchardCore.Tenants` + `ShellSettings`; each tenant gets its own `IShellScope` (isolated DI container, database connection, content)
- **Orchard Core**: Recipe system via `OrchardCore.Recipes`; `IRecipeStepHandler` for custom steps; `IRecipeManager` for import/export
- **Orchard Core**: OpenID module via `OrchardCore.OpenId`; `IOpenIdApplicationStore`, `IOpenIdScopeStore`, `IOpenIdTokenStore` for OIDC lifecycle management
- **Orchard Core**: Sitemap via `OrchardCore.Sitemaps`; `ISitemapBuilder`, `ISitemapCacheProvider`
- **Redis**: `StackExchange.Redis` + Orchard Core's `OrchardCore.Redis` module for distributed caching and cache-tag invalidation
- **Isolation Rule**: Tenant data isolation enforced at shell level — no shared YesSql session across tenants; PostgreSQL schema-per-tenant strategy preferred for production
- **Security**: Multi-tenant data leak is classified as P0 risk (see sprint-roadmap.md risk table) — requires dedicated isolation test suite
- **Offline**: All OIDC signing keys must be persisted locally (in PostgreSQL/YesSql) so token validation works without internet access
- **Cross-platform**: Paths, volumes, and process spawning must use .NET's `Path.Combine` and `RuntimeInformation` checks — no hardcoded `/` or `\` separators
- **KVKK/GDPR**: Tenant configuration may include PII (admin email, organization name) — classify per data-governance.md

### RBAC Gereksinimleri

| Permission | SuperAdmin | TenantAdmin | Denetci | Analyst | Editor | Author | Viewer | Anonymous |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Infrastructure.ManageTenants | Y | - | - | - | - | - | - | - |
| Infrastructure.ViewTenants | Y | - | - | - | - | - | - | - |
| Infrastructure.ManageCache | Y | Y | - | - | - | - | - | - |
| Infrastructure.PurgeCache | Y | - | - | - | - | - | - | - |
| Infrastructure.ImportRecipe | Y | Y | - | - | - | - | - | - |
| Infrastructure.ExportRecipe | Y | Y | - | - | - | - | - | - |
| Infrastructure.ManageOpenId | Y | Y | - | - | - | - | - | - |
| Infrastructure.ManageSitemap | Y | Y | - | Y | - | - | - | - |
| Infrastructure.ViewSitemap | Y | Y | Y | Y | Y | Y | Y | Y |
| Infrastructure.ManageSearchEngine | Y | Y | - | - | - | - | - | - |
| Infrastructure.ManageSettings | Y | Y | - | - | - | - | - | - |

- `Infrastructure.ManageTenants` — Create, suspend, and delete tenants (SuperAdmin only — cross-tenant destructive operation)
- `Infrastructure.ManageCache` — View cache statistics, configure TTLs, enable/disable cache per category
- `Infrastructure.PurgeCache` — Full cache flush (dangerous — may impact performance under load)
- `Infrastructure.ImportRecipe` — Upload and execute a recipe file to configure tenant
- `Infrastructure.ExportRecipe` — Export current tenant configuration as a downloadable recipe JSON
- `Infrastructure.ManageOpenId` — Configure OIDC clients, scopes, allowed flows, token lifetimes
- `Infrastructure.ManageSitemap` — Configure sitemap rules per content type, trigger regeneration
- `Infrastructure.ManageSearchEngine` — Switch search engine (Lucene ↔ Elasticsearch), manage index settings
- `Infrastructure.ManageSettings` — Modify tenant-level platform settings (app name, base URL, feature flags)

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-1001 | 4.1.10.1, 4.1.10.2, 4.1.10.8 | P0 | Multi-tenant provisioning and isolation — create, configure, and suspend tenants with full data isolation |
| US-1002 | 4.1.10.3, 4.1.10.4 | P1 | Redis cache layer — read-through caching for content listings, permissions, and query results with TTL management |
| US-1003 | 4.1.10.5, 4.1.10.6 | P1 | Recipe import and export — JSON recipe lifecycle for tenant configuration, content seeding, and backup/restore |
| US-1004 | 4.1.10.7, 4.1.10.22 | P0 | OpenID Connect authentication infrastructure — OIDC provider setup, client management, offline token validation |
| US-1005 | 4.1.10.10 | P2 | XML sitemap generation and maintenance — auto-generate sitemap.xml for public content, configurable per content type |
| US-1006 | 4.1.10.9 | P1 | Dual search engine support — Elasticsearch (production) and Lucene.NET (dev) switchable via configuration |
| US-1007 | 4.1.10.11 | P1 | Cross-platform Docker deployment hardening — Windows + Linux compatibility, health checks, graceful shutdown |

### Priority Rationale

- **P0 (Security / System Foundation)**: US-1001 (multi-tenant isolation) and US-1004 (OpenID Connect) — tenant data leaks are an immediate P0 security risk; OIDC is the authentication backbone every module relies on. Both block all other infrastructure work.
- **P1 (Core Infrastructure)**: US-1002 (Redis cache), US-1003 (recipes), US-1006 (dual search), US-1007 (Docker hardening) — these enable production-grade operation and developer workflow; not security-critical but essential for acceptance criteria.
- **P2 (Supporting / SEO)**: US-1005 (sitemap) — valuable for production SEO but does not block any other story; can be partially deferred if time is tight.

### Entity and Component Mapping

| Domain Concept | Orchard Core Component | Custom Extension / ProjectDora Layer |
|---------------|----------------------|--------------------------------------|
| Tenant | `ShellSettings` + `IShellHost` + `IShellSettingsManager` | `TenantDto`, `ITenantService` abstraction |
| Tenant Shell | `IShellScope` (per-tenant DI scope) | Middleware to resolve `X-Tenant-Id` header |
| Cache Entry | `IDistributedCache` (Redis backend) | `ICacheService` with typed key helpers |
| Cache Tag | `ITagCache` (Orchard Core tag invalidation) | Tag-based invalidation on content/role changes |
| Recipe | `RecipeDescriptor` + `IRecipeStepHandler` | Custom steps: `RbacSetupStep`, `TenantSeedStep` |
| OIDC Client App | `OpenIdApplication` + `IOpenIdApplicationStore` | `OpenIdClientDto`, `IOpenIdManagementService` |
| OIDC Token | `OpenIdToken` + `IOpenIdTokenStore` | Offline validation via local JWK cache |
| Sitemap | `SitemapType` + `ISitemapBuilder` | `SitemapContentTypeSource` for dynamic types |
| Search Index | `ISearchQueryService` + Lucene/ES adapters | `ISearchService` abstraction (existing from S04) |

### Dependency Graph

```
US-1001 (Multi-Tenant Provisioning)
  |
  +-- US-1004 (OpenID Connect) --> tenant auth scope depends on US-1001 shell isolation
  |
  +-- US-1002 (Redis Cache) --> cache keys scoped by tenantId from US-1001
  |
  +-- US-1003 (Recipe Import/Export) --> recipes configure tenants provisioned in US-1001
  |     |
  |     +-- US-1006 (Dual Search) --> search index reset step needed in recipes
  |
  +-- US-1005 (Sitemap) --> sitemap scoped per tenant, depends on US-1001
  |
  +-- US-1007 (Docker Hardening) --> cross-cutting, no explicit code dependency
```

## Teknik Kararlar (Technical Decisions)

### D-001: IInfrastructureService — Abstraction Interface

All infrastructure management operations (tenant lifecycle, cache, recipe, OIDC client management) go through typed abstraction interfaces defined in `ProjectDora.Core.Abstractions`. This follows ADR-001's mandate to isolate Orchard Core dependencies.

```csharp
// ITenantService — Tenant lifecycle management abstraction
public interface ITenantService
{
    Task<TenantDto> CreateTenantAsync(CreateTenantCommand command, CancellationToken ct);
    Task<TenantDto> GetTenantAsync(string tenantName, CancellationToken ct);
    Task<IReadOnlyList<TenantDto>> ListTenantsAsync(CancellationToken ct);
    Task<TenantDto> UpdateTenantAsync(UpdateTenantCommand command, CancellationToken ct);
    Task SuspendTenantAsync(string tenantName, CancellationToken ct);
    Task ReactivateTenantAsync(string tenantName, CancellationToken ct);
    Task DeleteTenantAsync(string tenantName, CancellationToken ct);
    Task<TenantStateDto> GetTenantStateAsync(string tenantName, CancellationToken ct);
}

// ICacheService — Distributed cache management abstraction
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class;
    Task RemoveAsync(string key, CancellationToken ct);
    Task RemoveByTagAsync(string tag, CancellationToken ct);
    Task<CacheStatsDto> GetStatsAsync(CancellationToken ct);
    Task PurgeAllAsync(string tenantId, CancellationToken ct);
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct) where T : class;
}

// IRecipeService — Recipe import/export abstraction
public interface IRecipeService
{
    Task<RecipeExecutionResult> ImportRecipeAsync(Stream recipeStream, string tenantId, CancellationToken ct);
    Task<Stream> ExportRecipeAsync(string tenantId, ExportRecipeOptions options, CancellationToken ct);
    Task<IReadOnlyList<RecipeDescriptorDto>> ListAvailableRecipesAsync(CancellationToken ct);
    Task<RecipeExecutionResult> ExecuteRecipeByNameAsync(string recipeName, string tenantId, CancellationToken ct);
}

// IOpenIdManagementService — OIDC client and scope management abstraction
public interface IOpenIdManagementService
{
    Task<OpenIdClientDto> CreateClientAsync(CreateOpenIdClientCommand command, CancellationToken ct);
    Task<OpenIdClientDto> GetClientAsync(string clientId, CancellationToken ct);
    Task<IReadOnlyList<OpenIdClientDto>> ListClientsAsync(CancellationToken ct);
    Task<OpenIdClientDto> UpdateClientAsync(UpdateOpenIdClientCommand command, CancellationToken ct);
    Task DeleteClientAsync(string clientId, CancellationToken ct);
    Task<OpenIdScopeDto> CreateScopeAsync(CreateOpenIdScopeCommand command, CancellationToken ct);
    Task<IReadOnlyList<OpenIdScopeDto>> ListScopesAsync(CancellationToken ct);
    Task<JwkSetDto> GetLocalJwkSetAsync(CancellationToken ct);
    Task RotateSigningKeyAsync(CancellationToken ct);
}

// ISitemapService — Sitemap generation and management abstraction
public interface ISitemapService
{
    Task<SitemapDto> GetSitemapAsync(string tenantId, CancellationToken ct);
    Task<Stream> GenerateSitemapXmlAsync(string tenantId, CancellationToken ct);
    Task InvalidateSitemapCacheAsync(string tenantId, CancellationToken ct);
    Task<SitemapConfigDto> GetSitemapConfigAsync(string tenantId, CancellationToken ct);
    Task UpdateSitemapConfigAsync(UpdateSitemapConfigCommand command, CancellationToken ct);
    Task PingSearchEnginesAsync(string tenantId, CancellationToken ct);
}
```

Module: `ProjectDora.Modules.Infrastructure`
Implements: `ITenantService`, `ICacheService`, `IRecipeService`, `IOpenIdManagementService`, `ISitemapService`
Consumes: `OrchardCore.Tenants`, `OrchardCore.Redis`, `OrchardCore.Recipes`, `OrchardCore.OpenId`, `OrchardCore.Sitemaps`

### D-002: Multi-Tenant Isolation Strategy

Orchard Core supports two tenancy models: database-per-tenant and schema-per-tenant. For ProjectDora:

- **Production (PostgreSQL)**: Schema-per-tenant — each tenant gets its own named PostgreSQL schema (e.g., `tenant_kosgeb`, `tenant_ankarasb`). The `orchard` schema houses the default/host tenant. YesSql is configured per-shell with the correct schema name.
- **Development/SQLite**: Single database with a tenant prefix in table names (Orchard Core's default SQLite tenancy behavior).
- **Shell lifecycle**: `IShellHost.GetOrCreateShellContextAsync(shellSettings)` creates an isolated DI scope per tenant. Requests are routed via `X-Tenant-Id` header (API) or hostname (web UI).
- **Strict rule**: Never pass a YesSql `ISession` across shell boundaries. Services that aggregate cross-tenant data (SuperAdmin views) must call tenant shells in sequence, never via a shared session.
- **Tenant provisioning sequence**: Create `ShellSettings` → Initialize database schema → Run setup recipe → Activate shell → Emit `TenantProvisioned` audit event.

### D-003: Redis Cache Layering and Key Conventions

Cache key structure: `{tenantId}:{module}:{resource}:{id}[:{param}]`

| Cache Category | Key Pattern | TTL | Invalidation Trigger |
|---------------|-------------|-----|---------------------|
| Content listing | `{tid}:content:{type}:list:{hash}` | 5m | `ContentItemPublished`, `ContentItemUnpublished`, `ContentItemDeleted` |
| Single content item | `{tid}:content:item:{contentItemId}` | 30m | `ContentItemUpdated`, `ContentItemDeleted` |
| Permission lookup | `{tid}:auth:perms:{userId}` | 15m | `RoleAssigned`, `RoleRevoked`, `PermissionChanged` |
| Query result | `{tid}:query:{queryName}:{paramHash}` | 5m | `QueryUpdated`, `ContentItemPublished` |
| Sitemap XML | `{tid}:sitemap:xml` | 1h | `ContentItemPublished`, `ContentItemUnpublished`, `SitemapConfigUpdated` |
| Tenant settings | `{tid}:settings:{category}` | 60m | `SettingsUpdated` |

Tag-based invalidation: `ITagCache.RemoveTagAsync(tag)` removes all keys tagged with a given cache tag. Each content type has a corresponding tag (e.g., `content-type:DestekProgrami`).

Redis eviction policy: `allkeys-lru` — when memory is full, least-recently-used keys are evicted regardless of TTL. Memory limit: 256MB (configurable via `redis.conf`).

### D-004: Recipe System and Custom Steps

Orchard Core recipes are JSON files executed as ordered steps. ProjectDora extends the recipe system with custom steps:

| Step Name | Handler Class | Purpose |
|-----------|--------------|---------|
| `TenantSeed` | `TenantSeedStepHandler` | Seed tenant-specific initial content from recipe |
| `RbacSetup` | `RbacSetupStepHandler` | Configure roles and permission assignments |
| `OpenIdSetup` | `OpenIdSetupStepHandler` | Register OIDC clients and scopes |
| `SearchIndexReset` | `SearchIndexResetStepHandler` | Drop and rebuild search index for tenant |
| `CacheWarmup` | `CacheWarmupStepHandler` | Pre-populate cache after tenant setup |

Import flow: Upload JSON → validate schema → `IRecipeManager.ExecuteRecipeAsync` → each step executed in order → `RecipeExecutionCompleted` audit event.

Export flow: `ExportRecipeOptions` specifies which steps to include (content types, roles, OIDC clients, settings) → assembled into `RecipeDescriptor` → serialized as downloadable JSON.

### D-005: OpenID Connect — Offline-First Design

Orchard Core's `OrchardCore.OpenId` module issues and validates JWTs internally using locally stored RSA keys. No external identity provider is required.

Key persistence: RSA signing keys stored in YesSql (per-tenant shell) under the key `OpenId:SigningKeys`. Keys are loaded at shell startup and cached in memory.

Offline validation: `IOpenIdValidationService` validates bearer tokens using cached JWKs — no network call required. Token validation fails fast if keys are missing from local store (returns 401, not a hang).

Token lifetimes (configurable per tenant):
- Access token: 1 hour (default)
- Refresh token: 30 days (default)
- Authorization code: 5 minutes (default)

Supported flows:
- `authorization_code` — Web UI login
- `client_credentials` — Service-to-service API calls
- `refresh_token` — Token renewal

Key rotation: `RotateSigningKeyAsync` generates a new RSA key pair, marks old key as secondary (still used for validation for 24h grace period), then removes old key.

### D-006: Dual Search Engine Abstraction

The existing `ISearchService` (introduced in S04 for query management) is extended to cover infrastructure-level index management:

```csharp
// Extended ISearchService (infrastructure additions)
public interface ISearchService
{
    // Existing (S04):
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct);

    // New (S09):
    Task<SearchEngineInfoDto> GetCurrentEngineInfoAsync(CancellationToken ct);
    Task ReindexAsync(string tenantId, string? contentType, CancellationToken ct);
    Task DeleteIndexAsync(string tenantId, CancellationToken ct);
    Task<IndexStatsDto> GetIndexStatsAsync(string tenantId, CancellationToken ct);
    Task SwitchEngineAsync(SearchEngineType engineType, CancellationToken ct);
}

public enum SearchEngineType { Lucene, Elasticsearch }
```

Configuration-driven switching: `appsettings.json` key `Search:Engine` accepts `"Lucene"` or `"Elasticsearch"`. The DI registration in `Startup.cs` reads this key and registers the correct adapter. Hot-switching (without restart) is supported for SuperAdmin via `SwitchEngineAsync` which triggers a full reindex.

Elasticsearch index naming: `projectdora-{tenantId}-content` (lowercase, per Elasticsearch convention).

### D-007: Cross-Platform Docker Hardening

- All file paths use `Path.Combine` — no hardcoded separators
- `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` guards for Windows-specific behavior (e.g., process spawning for recipe execution)
- Docker healthcheck endpoint: `GET /health/live` (liveness) and `GET /health/ready` (readiness — checks PostgreSQL, Redis, Elasticsearch connectivity)
- Graceful shutdown: `IHostApplicationLifetime.ApplicationStopping` token passed to all long-running services; background jobs honor cancellation
- Volume mounts use named volumes (not bind mounts) in production to avoid Windows/Linux path issues

See `docs/sprint-analyses/S09-infrastructure/decisions.md` for full decision details.

## DoR YAML — User Stories

### US-1001: Multi-Tenant Provisioning and Isolation

```yaml
story_id: "US-1001"
title: "Provision and isolate tenants with per-schema data separation"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.1"
  - "4.1.10.2"
  - "4.1.10.8"
sprint: "S09"
priority: "P0"

role: "As a SuperAdmin"
action: "I want to create, configure, and suspend tenants"
benefit: "So that each KOSGEB department or partner organization operates in complete data isolation on the same platform instance"

inputs:
  - name: "tenantName"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100), Matches('^[a-z][a-z0-9-]*$')"
    example: "kosgeb-ankara"
  - name: "displayName"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(200)"
    example: "KOSGEB Ankara Birimi"
  - name: "requestUrlHost"
    type: "string"
    required: false
    validation: "MaxLength(500), ValidHostname"
    example: "ankara.kosgeb.projectdora.gov.tr"
  - name: "databaseSchema"
    type: "string"
    required: false
    validation: "MaxLength(63), Matches('^[a-z][a-z0-9_]*$')"
    example: "tenant_ankara"
  - name: "setupRecipeName"
    type: "string"
    required: false
    validation: "MaxLength(100)"
    example: "KosgibDefaultSetup"

outputs:
  - name: "tenantId"
    type: "string"
    description: "Internal tenant identifier (same as tenantName for Orchard Core)"
  - name: "state"
    type: "string"
    description: "TenantState: Uninitialized | Initializing | Running | Disabled | Invalid"
  - name: "databaseSchema"
    type: "string"
    description: "Actual PostgreSQL schema assigned to this tenant"

constraints:
  rbac:
    required_permissions:
      - "Infrastructure.ManageTenants"
    denied_roles:
      - "Anonymous"
      - "TenantAdmin"
      - "Editor"
      - "Author"
      - "Viewer"
      - "Analyst"
      - "Denetci"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "POST /api/v1/tenants"
    request_content_type: "application/json"
    response_codes:
      - 201: "Tenant created and initialized"
      - 400: "Validation error (invalid name, hostname conflict)"
      - 403: "Caller lacks Infrastructure.ManageTenants permission"
      - 409: "Tenant name already exists"

acceptance_tests:
  - id: "AT-001"
    scenario: "Create tenant with valid parameters"
    given: "SuperAdmin is authenticated and no tenant named 'kosgeb-ankara' exists"
    when: "SuperAdmin submits CreateTenant with tenantName='kosgeb-ankara', displayName='KOSGEB Ankara'"
    then: "Tenant is created with state=Running, PostgreSQL schema 'tenant_ankara' is initialized, setup recipe executes, TenantProvisioned audit event is emitted"
  - id: "AT-002"
    scenario: "Tenant data isolation verified"
    given: "TenantA and TenantB both exist with separate content items"
    when: "Authenticated user in TenantA queries content listing"
    then: "Only TenantA content items are returned; TenantB items are never visible regardless of authentication"
  - id: "AT-003"
    scenario: "Suspend tenant blocks all access"
    given: "TenantA is in Running state with active sessions"
    when: "SuperAdmin suspends TenantA"
    then: "TenantA requests return 503 Service Unavailable; existing sessions invalidated; TenantSuspended audit event emitted"
  - id: "AT-004"
    scenario: "Reject duplicate tenant name"
    given: "Tenant 'kosgeb-ankara' already exists"
    when: "SuperAdmin attempts to create another tenant with tenantName='kosgeb-ankara'"
    then: "API returns 409 Conflict with descriptive error message"
  - id: "AT-005"
    scenario: "TenantAdmin cannot create tenants"
    given: "User has TenantAdmin role"
    when: "User attempts to call POST /api/v1/tenants"
    then: "API returns 403 Forbidden"

edge_cases:
  - id: "EC-001"
    scenario: "Tenant name with uppercase characters"
    input: "tenantName = 'KOSGEB-Ankara'"
    expected: "API returns 400 — tenantName must be lowercase alphanumeric with hyphens only"
  - id: "EC-002"
    scenario: "Database schema initialization fails mid-way"
    input: "PostgreSQL is temporarily unavailable during tenant provisioning"
    expected: "Tenant state set to Invalid; error logged; TenantProvisioningFailed audit event emitted; no partial schema left in database"
  - id: "EC-003"
    scenario: "Cross-tenant session reuse attempt"
    input: "JWT token issued for TenantA is used in a request to TenantB endpoint"
    expected: "Request rejected with 403 — token tenant claim does not match request tenant"

dependencies:
  stories: []
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure"]
  external: ["OrchardCore.Tenants", "PostgreSQL"]

tech_notes:
  abstraction_interface: "IAuthService"
  mediatr_commands:
    - "CreateTenantCommand"
    - "CreateTenantCommandHandler"
    - "SuspendTenantCommand"
    - "SuspendTenantCommandHandler"
    - "ReactivateTenantCommand"
    - "ReactivateTenantCommandHandler"
    - "DeleteTenantCommand"
    - "DeleteTenantCommandHandler"
  mediatr_queries:
    - "GetTenantQuery"
    - "GetTenantQueryHandler"
    - "ListTenantsQuery"
    - "ListTenantsQueryHandler"
  audit_events:
    - "TenantProvisioned"
    - "TenantSuspended"
    - "TenantReactivated"
    - "TenantDeleted"
    - "TenantProvisioningFailed"
  localization_keys:
    - "Infrastructure.Tenant.Created.Success"
    - "Infrastructure.Tenant.Suspended.Success"
    - "Infrastructure.Tenant.Validation.NameInvalid"
    - "Infrastructure.Tenant.Validation.NameDuplicate"
  caching:
    strategy: "WriteThrough"
    key_pattern: "host:tenants:list"
    ttl: "60m"
```

### US-1002: Redis Cache Layer

```yaml
story_id: "US-1002"
title: "Implement Redis read-through cache with TTL management and tag invalidation"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.3"
  - "4.1.10.4"
sprint: "S09"
priority: "P1"

role: "As a TenantAdmin"
action: "I want to configure and manage the Redis cache layer"
benefit: "So that high-frequency database queries are served from cache, reducing PostgreSQL load and improving response times for KOSGEB users"

inputs:
  - name: "tenantId"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100)"
    example: "kosgeb-ankara"
  - name: "category"
    type: "string"
    required: true
    validation: "NotEmpty, OneOf('content', 'permissions', 'queries', 'sitemap', 'settings')"
    example: "content"
  - name: "ttlSeconds"
    type: "int"
    required: false
    validation: "InclusiveBetween(30, 86400)"
    example: 300

outputs:
  - name: "stats"
    type: "CacheStatsDto"
    description: "Hit rate, miss rate, memory usage, key count per category"

constraints:
  rbac:
    required_permissions:
      - "Infrastructure.ManageCache"
    denied_roles:
      - "Anonymous"
      - "Editor"
      - "Author"
      - "Viewer"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "GET /api/v1/infrastructure/cache/stats"
    request_content_type: "application/json"
    response_codes:
      - 200: "Cache stats returned"
      - 403: "Caller lacks Infrastructure.ManageCache permission"

acceptance_tests:
  - id: "AT-001"
    scenario: "Content listing served from cache on second request"
    given: "Redis is running and content listing cache is enabled with TTL=5m"
    when: "Same content listing query is executed twice within 5 minutes"
    then: "First request hits database; second request is served from Redis cache; cache hit rate increments"
  - id: "AT-002"
    scenario: "Cache invalidated on content publish"
    given: "Content listing for 'DestekProgrami' is cached in Redis"
    when: "A new DestekProgrami content item is published"
    then: "Cache entries tagged 'content-type:DestekProgrami' are removed; next listing request re-queries database"
  - id: "AT-003"
    scenario: "TenantAdmin can view cache stats"
    given: "TenantAdmin is authenticated"
    when: "TenantAdmin calls GET /api/v1/infrastructure/cache/stats"
    then: "Cache statistics returned including hit rate, key count, memory usage for this tenant"
  - id: "AT-004"
    scenario: "Cache purge clears only tenant-scoped keys"
    given: "TenantA and TenantB both have cached data in Redis"
    when: "SuperAdmin purges cache for TenantA"
    then: "Only TenantA keys are removed; TenantB cache is untouched"

edge_cases:
  - id: "EC-001"
    scenario: "Redis connection failure during cache read"
    input: "Redis container is stopped while application is running"
    expected: "Cache miss is recorded; request falls through to database; error logged at Warning level; application continues to serve requests (degraded mode)"
  - id: "EC-002"
    scenario: "Cache stampede on TTL expiry"
    input: "100 concurrent requests arrive after cache TTL expires for a popular content listing"
    expected: "Only one database query executes; remaining requests wait and receive the result (GetOrSetAsync with distributed lock prevents stampede)"

dependencies:
  stories: ["US-1001"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure"]
  external: ["Redis 7", "StackExchange.Redis", "OrchardCore.Redis"]

tech_notes:
  abstraction_interface: "IQueryService"
  mediatr_commands:
    - "PurgeTenantCacheCommand"
    - "PurgeTenantCacheCommandHandler"
    - "UpdateCacheTtlCommand"
    - "UpdateCacheTtlCommandHandler"
  mediatr_queries:
    - "GetCacheStatsQuery"
    - "GetCacheStatsQueryHandler"
  audit_events:
    - "CachePurged"
    - "CacheConfigUpdated"
  localization_keys:
    - "Infrastructure.Cache.Purged.Success"
    - "Infrastructure.Cache.Config.Updated"
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

### US-1003: Recipe Import and Export

```yaml
story_id: "US-1003"
title: "Import and export tenant configuration as JSON recipes"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.5"
  - "4.1.10.6"
sprint: "S09"
priority: "P1"

role: "As a TenantAdmin"
action: "I want to import a recipe file to configure my tenant and export current configuration as a recipe"
benefit: "So that tenant setup is reproducible, deployable across environments, and recoverable from backup"

inputs:
  - name: "recipeFile"
    type: "File"
    required: true
    validation: "NotEmpty, MaxSize(10MB), ContentType('application/json')"
    example: "kosgeb-default-setup.json"
  - name: "tenantId"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100)"
    example: "kosgeb-ankara"

outputs:
  - name: "executionId"
    type: "string"
    description: "Unique ID for tracking async recipe execution"
  - name: "status"
    type: "string"
    description: "Pending | Running | Completed | Failed"
  - name: "failedStep"
    type: "string"
    description: "Name of step that failed (if status=Failed)"

constraints:
  rbac:
    required_permissions:
      - "Infrastructure.ImportRecipe"
    denied_roles:
      - "Anonymous"
      - "Editor"
      - "Author"
      - "Viewer"
      - "Analyst"
      - "Denetci"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "POST /api/v1/infrastructure/recipes/import"
    request_content_type: "multipart/form-data"
    response_codes:
      - 202: "Recipe accepted for async execution"
      - 400: "Invalid JSON schema or missing required steps"
      - 403: "Caller lacks Infrastructure.ImportRecipe permission"
      - 422: "Recipe execution failed (step error details in body)"

acceptance_tests:
  - id: "AT-001"
    scenario: "Import valid recipe configures tenant"
    given: "TenantAdmin uploads a valid recipe JSON containing ContentDefinition and Roles steps"
    when: "POST /api/v1/infrastructure/recipes/import with valid file"
    then: "Recipe executes asynchronously; content types and roles defined in recipe are created in tenant; RecipeImported audit event emitted"
  - id: "AT-002"
    scenario: "Export recipe captures current configuration"
    given: "Tenant has 3 content types, 4 roles, and 2 OIDC clients configured"
    when: "TenantAdmin calls GET /api/v1/infrastructure/recipes/export"
    then: "Downloaded JSON recipe contains ContentDefinition step with all 3 types, Roles step with all 4 roles, OpenIdSetup step with 2 clients"
  - id: "AT-003"
    scenario: "Recipe execution failure rolls back safely"
    given: "Recipe file contains a step that references a non-existent content type for seeding"
    when: "Recipe is imported"
    then: "Failed step causes execution to stop; steps already executed are left as-is (idempotent design); RecipeExecutionFailed audit event emitted with failedStep name"
  - id: "AT-004"
    scenario: "Re-importing same recipe is idempotent"
    given: "Tenant already has content types defined in a recipe"
    when: "Same recipe is imported again"
    then: "No duplicate content types or roles created; execution completes with Completed status"

edge_cases:
  - id: "EC-001"
    scenario: "Recipe file exceeds 10MB size limit"
    input: "recipeFile = 15MB JSON file"
    expected: "API returns 400 with size validation error before any steps execute"
  - id: "EC-002"
    scenario: "Recipe with unknown step name"
    input: "Recipe JSON contains step name 'UnknownCustomStep'"
    expected: "Unknown step is skipped with a warning log entry; remaining steps execute; execution completes with a warning summary"

dependencies:
  stories: ["US-1001"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure"]
  external: ["OrchardCore.Recipes"]

tech_notes:
  abstraction_interface: "IWorkflowService"
  mediatr_commands:
    - "ImportRecipeCommand"
    - "ImportRecipeCommandHandler"
    - "ExportRecipeCommand"
    - "ExportRecipeCommandHandler"
  mediatr_queries:
    - "GetRecipeExecutionStatusQuery"
    - "GetRecipeExecutionStatusQueryHandler"
    - "ListAvailableRecipesQuery"
    - "ListAvailableRecipesQueryHandler"
  audit_events:
    - "RecipeImported"
    - "RecipeExported"
    - "RecipeExecutionFailed"
  localization_keys:
    - "Infrastructure.Recipe.Import.Success"
    - "Infrastructure.Recipe.Export.Success"
    - "Infrastructure.Recipe.Validation.FileTooLarge"
    - "Infrastructure.Recipe.Validation.InvalidSchema"
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

### US-1004: OpenID Connect Authentication Infrastructure

```yaml
story_id: "US-1004"
title: "Configure OIDC provider with offline-capable token validation and client management"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.7"
  - "4.1.10.22"
sprint: "S09"
priority: "P0"

role: "As a TenantAdmin"
action: "I want to configure OpenID Connect clients, scopes, and token settings"
benefit: "So that all platform users and integrating systems authenticate securely via standard OIDC, even without internet connectivity"

inputs:
  - name: "clientId"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(200), Matches('^[a-zA-Z0-9_-]+$')"
    example: "projectdora-web"
  - name: "displayName"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(300)"
    example: "ProjectDora Web Application"
  - name: "allowedFlows"
    type: "List<string>"
    required: true
    validation: "NotEmpty, Each(OneOf('authorization_code','client_credentials','refresh_token'))"
    example: ["authorization_code", "refresh_token"]
  - name: "redirectUris"
    type: "List<string>"
    required: false
    validation: "Each(ValidUri, MaxLength(2000))"
    example: ["https://projectdora.gov.tr/signin-oidc"]
  - name: "accessTokenLifetimeSeconds"
    type: "int"
    required: false
    validation: "InclusiveBetween(300, 86400)"
    example: 3600

outputs:
  - name: "clientId"
    type: "string"
    description: "Registered OIDC client identifier"
  - name: "clientSecret"
    type: "string"
    description: "Generated client secret (shown only once at creation)"
  - name: "discoveryEndpoint"
    type: "string"
    description: "OIDC discovery URL: /.well-known/openid-configuration"

constraints:
  rbac:
    required_permissions:
      - "Infrastructure.ManageOpenId"
    denied_roles:
      - "Anonymous"
      - "Editor"
      - "Author"
      - "Viewer"
      - "Analyst"
      - "Denetci"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "POST /api/v1/infrastructure/oidc/clients"
    request_content_type: "application/json"
    response_codes:
      - 201: "OIDC client registered"
      - 400: "Validation error"
      - 403: "Caller lacks Infrastructure.ManageOpenId permission"
      - 409: "Client ID already registered"

acceptance_tests:
  - id: "AT-001"
    scenario: "Register OIDC client and obtain token"
    given: "TenantAdmin registers a client with client_credentials flow"
    when: "External system calls POST /connect/token with client_id and client_secret"
    then: "Valid JWT access token returned with correct claims (sub, tenant_id, roles, exp)"
  - id: "AT-002"
    scenario: "Token validation works offline"
    given: "OIDC client is registered and token issued; network is disconnected"
    when: "API endpoint validates incoming bearer token"
    then: "Token validated successfully using locally cached JWK set; no network call made"
  - id: "AT-003"
    scenario: "Expired token rejected"
    given: "Access token with 1-hour lifetime has expired"
    when: "Expired token is used in API request"
    then: "API returns 401 Unauthorized with error 'token_expired'"
  - id: "AT-004"
    scenario: "Token from TenantA rejected for TenantB"
    given: "Valid token issued for TenantA"
    when: "Token is used in request to TenantB API endpoint"
    then: "Request rejected with 403 — tenant claim mismatch"
  - id: "AT-005"
    scenario: "OIDC discovery endpoint accessible"
    given: "OIDC provider is configured"
    when: "Client calls GET /.well-known/openid-configuration"
    then: "Discovery document returned with correct issuer, endpoints, and supported scopes"

edge_cases:
  - id: "EC-001"
    scenario: "Key rotation — tokens issued before rotation remain valid during grace period"
    input: "Token issued 30 minutes before key rotation; grace period is 24 hours"
    expected: "Token validated using secondary (old) key; new tokens issued with new key; after 24h old key removed"
  - id: "EC-002"
    scenario: "Client registration with duplicate clientId"
    input: "clientId = 'projectdora-web' which already exists"
    expected: "API returns 409 Conflict with message identifying the duplicate"

dependencies:
  stories: ["US-1001"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure"]
  external: ["OrchardCore.OpenId", "OpenIddict"]

tech_notes:
  abstraction_interface: "IAuthService"
  mediatr_commands:
    - "CreateOpenIdClientCommand"
    - "CreateOpenIdClientCommandHandler"
    - "UpdateOpenIdClientCommand"
    - "UpdateOpenIdClientCommandHandler"
    - "DeleteOpenIdClientCommand"
    - "DeleteOpenIdClientCommandHandler"
    - "RotateSigningKeyCommand"
    - "RotateSigningKeyCommandHandler"
  mediatr_queries:
    - "GetOpenIdClientQuery"
    - "GetOpenIdClientQueryHandler"
    - "ListOpenIdClientsQuery"
    - "ListOpenIdClientsQueryHandler"
    - "GetJwkSetQuery"
    - "GetJwkSetQueryHandler"
  audit_events:
    - "OpenIdClientCreated"
    - "OpenIdClientUpdated"
    - "OpenIdClientDeleted"
    - "SigningKeyRotated"
  localization_keys:
    - "Infrastructure.OpenId.Client.Created.Success"
    - "Infrastructure.OpenId.Client.Validation.DuplicateClientId"
    - "Infrastructure.OpenId.Key.Rotated.Success"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:oidc:jwks"
    ttl: "60m"
```

### US-1005: XML Sitemap Generation

```yaml
story_id: "US-1005"
title: "Auto-generate and maintain XML sitemap for public content"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.10"
sprint: "S09"
priority: "P2"

role: "As a TenantAdmin"
action: "I want to configure and regenerate the XML sitemap for publicly published content"
benefit: "So that search engines can discover and index KOSGEB public information efficiently"

inputs:
  - name: "tenantId"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100)"
    example: "kosgeb-ankara"
  - name: "contentTypeInclusions"
    type: "List<string>"
    required: false
    validation: "Each(MaxLength(100))"
    example: ["Duyuru", "DestekProgrami", "Haber"]
  - name: "changeFrequency"
    type: "string"
    required: false
    validation: "OneOf('always','hourly','daily','weekly','monthly','yearly','never')"
    example: "weekly"
  - name: "pingSearchEngines"
    type: "bool"
    required: false
    validation: ""
    example: true

outputs:
  - name: "sitemapUrl"
    type: "string"
    description: "Public URL of the generated sitemap.xml"
  - name: "urlCount"
    type: "int"
    description: "Number of URLs included in the sitemap"
  - name: "generatedAt"
    type: "DateTime"
    description: "Timestamp of the last generation"

constraints:
  rbac:
    required_permissions:
      - "Infrastructure.ManageSitemap"
    denied_roles:
      - "Anonymous"
      - "Author"
      - "Viewer"
  data_model:
    content_type: "N/A"
    parts:
      - "AutoroutePart"
    fields: []
  api:
    endpoint: "POST /api/v1/infrastructure/sitemap/regenerate"
    request_content_type: "application/json"
    response_codes:
      - 200: "Sitemap regenerated"
      - 403: "Caller lacks Infrastructure.ManageSitemap permission"

acceptance_tests:
  - id: "AT-001"
    scenario: "Sitemap generated with published content URLs"
    given: "10 published 'Duyuru' items and 5 published 'DestekProgrami' items exist with AutoroutePart slugs"
    when: "POST /api/v1/infrastructure/sitemap/regenerate"
    then: "sitemap.xml at /sitemap.xml contains 15 <url> entries with correct <loc> values and <lastmod> timestamps"
  - id: "AT-002"
    scenario: "Draft content excluded from sitemap"
    given: "3 draft 'Duyuru' items exist alongside 5 published items"
    when: "Sitemap is regenerated"
    then: "Sitemap contains only 5 published Duyuru URLs; draft content URLs are absent"
  - id: "AT-003"
    scenario: "Sitemap cache invalidated on content publish"
    given: "Sitemap is cached; a new Duyuru item is published"
    when: "Request arrives for /sitemap.xml"
    then: "Stale cached sitemap is invalidated; fresh sitemap including the new URL is generated and served"

edge_cases:
  - id: "EC-001"
    scenario: "Sitemap with over 50,000 URLs triggers sitemap index"
    input: "Tenant has 60,000 published content items with AutoroutePart"
    expected: "System generates a sitemap index file (sitemapindex.xml) with multiple sitemap files each containing <= 50,000 URLs (per XML Sitemap protocol limit)"
  - id: "EC-002"
    scenario: "AutoroutePart slug contains Turkish characters"
    input: "Published content with slug 'kosgeb-destek-programi-2026'"
    expected: "URL in sitemap is percent-encoded correctly; Turkish characters in display text are not in the slug (AutoroutePart slugify filter handles conversion)"

dependencies:
  stories: ["US-1001"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure"]
  external: ["OrchardCore.Sitemaps", "OrchardCore.Autoroute"]

tech_notes:
  abstraction_interface: "IContentService"
  mediatr_commands:
    - "RegenerateSitemapCommand"
    - "RegenerateSitemapCommandHandler"
    - "UpdateSitemapConfigCommand"
    - "UpdateSitemapConfigCommandHandler"
  mediatr_queries:
    - "GetSitemapQuery"
    - "GetSitemapQueryHandler"
  audit_events:
    - "SitemapRegenerated"
    - "SitemapConfigUpdated"
  localization_keys:
    - "Infrastructure.Sitemap.Regenerated.Success"
    - "Infrastructure.Sitemap.Config.Updated"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:sitemap:xml"
    ttl: "1h"
```

### US-1006: Dual Search Engine Support

```yaml
story_id: "US-1006"
title: "Support Elasticsearch (production) and Lucene.NET (dev) via switchable configuration"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.9"
sprint: "S09"
priority: "P1"

role: "As a SuperAdmin"
action: "I want to switch the search engine between Elasticsearch and Lucene.NET and manage index health"
benefit: "So that the platform scales with Elasticsearch in production while remaining developer-friendly with Lucene.NET locally"

inputs:
  - name: "engineType"
    type: "string"
    required: true
    validation: "NotEmpty, OneOf('Lucene','Elasticsearch')"
    example: "Elasticsearch"
  - name: "tenantId"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100)"
    example: "kosgeb-ankara"

outputs:
  - name: "previousEngine"
    type: "string"
    description: "Engine type before the switch"
  - name: "reindexJobId"
    type: "string"
    description: "Background job ID for the reindex operation triggered by the switch"
  - name: "indexStats"
    type: "IndexStatsDto"
    description: "Document count, index size, last indexed timestamp"

constraints:
  rbac:
    required_permissions:
      - "Infrastructure.ManageSearchEngine"
    denied_roles:
      - "Anonymous"
      - "TenantAdmin"
      - "Editor"
      - "Author"
      - "Viewer"
      - "Analyst"
      - "Denetci"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "POST /api/v1/infrastructure/search/switch"
    request_content_type: "application/json"
    response_codes:
      - 202: "Switch accepted; reindex job started"
      - 400: "Invalid engine type"
      - 403: "Caller lacks Infrastructure.ManageSearchEngine permission"

acceptance_tests:
  - id: "AT-001"
    scenario: "Switch from Lucene to Elasticsearch triggers reindex"
    given: "Platform is running with Lucene.NET and 500 indexed content items"
    when: "SuperAdmin calls POST /api/v1/infrastructure/search/switch with engineType='Elasticsearch'"
    then: "Background reindex job starts; content items are indexed into Elasticsearch; after completion, all searches use Elasticsearch; SearchEngineSwitched audit event emitted"
  - id: "AT-002"
    scenario: "Search results consistent after engine switch"
    given: "100 published DestekProgrami items; search for 'teknoloji' returns 15 results with Lucene"
    when: "Engine is switched to Elasticsearch and reindex completes"
    then: "Search for 'teknoloji' returns the same 15 results (Turkish analyzer applied in both engines)"
  - id: "AT-003"
    scenario: "Manual reindex rebuilds stale index"
    given: "5 content items were published while Elasticsearch was unreachable (missed events)"
    when: "SuperAdmin triggers POST /api/v1/infrastructure/search/reindex for the affected tenant"
    then: "All 5 missing items are added to the index; index document count matches published content item count"

edge_cases:
  - id: "EC-001"
    scenario: "Elasticsearch unavailable during switch"
    input: "engineType='Elasticsearch' but Elasticsearch service is down"
    expected: "Switch fails with 503 response; platform remains on Lucene; error logged; SearchEngineSwitchFailed audit event emitted"
  - id: "EC-002"
    scenario: "Reindex while new content is being published"
    input: "Reindex job running; 3 new content items published during reindex"
    expected: "Reindex completes without missing the 3 new items — events queued during reindex are applied after job completes"

dependencies:
  stories: ["US-1001", "US-1003"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure", "ProjectDora.Modules.QueryEngine"]
  external: ["Elasticsearch 8", "Lucene.NET 4.8"]

tech_notes:
  abstraction_interface: "IQueryService"
  mediatr_commands:
    - "SwitchSearchEngineCommand"
    - "SwitchSearchEngineCommandHandler"
    - "ReindexTenantCommand"
    - "ReindexTenantCommandHandler"
  mediatr_queries:
    - "GetIndexStatsQuery"
    - "GetIndexStatsQueryHandler"
    - "GetSearchEngineInfoQuery"
    - "GetSearchEngineInfoQueryHandler"
  audit_events:
    - "SearchEngineSwitched"
    - "SearchEngineSwitchFailed"
    - "SearchIndexRebuilt"
  localization_keys:
    - "Infrastructure.Search.Switch.Success"
    - "Infrastructure.Search.Switch.Failed"
    - "Infrastructure.Search.Reindex.Started"
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

### US-1007: Cross-Platform Docker Deployment Hardening

```yaml
story_id: "US-1007"
title: "Harden Docker deployment for Windows and Linux with health checks and graceful shutdown"
module: "ProjectDora.Modules.Infrastructure"
spec_refs:
  - "4.1.10.11"
sprint: "S09"
priority: "P1"

role: "As a DevOps Engineer"
action: "I want the platform to start, stop, and serve health check endpoints reliably on both Windows and Linux Docker hosts"
benefit: "So that KOSGEB's IT team can deploy and operate the platform on their existing infrastructure without OS-specific issues"

inputs:
  - name: "healthCheckEndpoint"
    type: "string"
    required: false
    validation: "MaxLength(200), ValidPath"
    example: "/health/ready"

outputs:
  - name: "livenessStatus"
    type: "string"
    description: "Healthy | Degraded | Unhealthy"
  - name: "readinessStatus"
    type: "string"
    description: "Healthy | Degraded | Unhealthy"
  - name: "dependencies"
    type: "List<HealthCheckComponentDto>"
    description: "PostgreSQL, Redis, Elasticsearch, MinIO status with latency"

constraints:
  rbac:
    required_permissions: []
    denied_roles: []
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "GET /health/live"
    request_content_type: "application/json"
    response_codes:
      - 200: "Liveness check passed"
      - 503: "Application is unhealthy"

acceptance_tests:
  - id: "AT-001"
    scenario: "Health check endpoints respond correctly"
    given: "All services (PostgreSQL, Redis, Elasticsearch, MinIO) are running"
    when: "GET /health/live and GET /health/ready are called"
    then: "/health/live returns 200 with status=Healthy; /health/ready returns 200 with all dependency statuses=Healthy"
  - id: "AT-002"
    scenario: "Readiness check degraded when Redis is unavailable"
    given: "Redis container is stopped; PostgreSQL and Elasticsearch are running"
    when: "GET /health/ready is called"
    then: "Response returns 503 with readiness=Degraded; Redis component shows Unhealthy; other components show Healthy"
  - id: "AT-003"
    scenario: "Graceful shutdown completes in-flight requests"
    given: "10 API requests are in progress"
    when: "SIGTERM is sent to the container"
    then: "Application stops accepting new requests; all 10 in-flight requests complete; application exits cleanly within 30 seconds"
  - id: "AT-004"
    scenario: "Platform starts successfully on Linux Docker host"
    given: "docker-compose.yml configured for Linux"
    when: "docker compose up -d runs on Ubuntu 24.04"
    then: "All services start within 60 seconds; /health/ready returns Healthy; admin panel accessible"

edge_cases:
  - id: "EC-001"
    scenario: "Volume mount path differences between Windows and Linux"
    input: "docker-compose.yml run on Windows Docker Desktop with WSL2 backend"
    expected: "Named volumes (not bind mounts) used for all data; no path separator issues; platform starts and functions correctly"
  - id: "EC-002"
    scenario: "PostgreSQL unavailable at startup"
    given: "Web container starts before PostgreSQL is ready"
    when: "Application attempts to initialize YesSql database connection"
    then: "Application retries with exponential backoff (max 5 attempts, 30s total); if PostgreSQL never becomes ready, container exits with error code and Docker restarts it"

dependencies:
  stories: []
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Infrastructure"]
  external: ["Docker", "Docker Compose", ".NET 8 health checks"]

tech_notes:
  abstraction_interface: "IAuthService"
  mediatr_commands: []
  mediatr_queries:
    - "GetSystemHealthQuery"
    - "GetSystemHealthQueryHandler"
  audit_events:
    - "SystemStarted"
    - "SystemShuttingDown"
  localization_keys: []
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-1001-01 | Unit | US-1001 | Create tenant with valid parameters returns Running state |
| TC-1001-02 | Unit | US-1001 | Create tenant with duplicate name returns 409 Conflict |
| TC-1001-03 | Unit | US-1001 | Tenant name with uppercase rejected with 400 validation error |
| TC-1001-04 | Integration | US-1001 | Tenant provisioning initializes separate PostgreSQL schema |
| TC-1001-05 | Integration | US-1001 | Suspend tenant returns 503 on all subsequent tenant requests |
| TC-1001-06 | Security | US-1001 | TenantAdmin cannot create new tenants — 403 returned |
| TC-1001-07 | Security | US-1001 | Tenant isolation: TenantA content query returns no TenantB items |
| TC-1001-08 | Security | US-1001 | Cross-tenant JWT token use returns 403 (tenant claim mismatch) |
| TC-1001-09 | Security | US-1001 | Database schema isolation: no YesSql session shared across shell boundaries |
| TC-1001-10 | Integration | US-1001 | TenantProvisioned audit event emitted after successful creation |
| TC-1001-11 | Unit | US-1001 | Provisioning failure sets tenant state to Invalid and emits TenantProvisioningFailed |
| TC-1002-01 | Unit | US-1002 | Second identical content listing query served from Redis cache |
| TC-1002-02 | Unit | US-1002 | Cache key follows pattern {tenantId}:content:{type}:list:{hash} |
| TC-1002-03 | Unit | US-1002 | Cache invalidated after ContentItemPublished event |
| TC-1002-04 | Unit | US-1002 | Cache invalidated after ContentItemDeleted event |
| TC-1002-05 | Unit | US-1002 | Permission lookup cache invalidated after RoleAssigned event |
| TC-1002-06 | Integration | US-1002 | TenantA cache purge does not affect TenantB cached entries |
| TC-1002-07 | Unit | US-1002 | Redis connection failure falls through to database without crashing |
| TC-1002-08 | Performance | US-1002 | Cache hit latency < 5ms; cache miss latency < 50ms under 100 concurrent requests |
| TC-1002-09 | Unit | US-1002 | GetOrSetAsync prevents cache stampede with distributed lock |
| TC-1003-01 | Unit | US-1003 | Valid recipe JSON imported successfully; content types created |
| TC-1003-02 | Unit | US-1003 | Recipe with unknown step name skips step and logs warning |
| TC-1003-03 | Unit | US-1003 | Recipe file exceeding 10MB rejected before execution |
| TC-1003-04 | Integration | US-1003 | Recipe import is idempotent — re-import does not create duplicates |
| TC-1003-05 | Integration | US-1003 | Export recipe captures content types, roles, and OIDC clients |
| TC-1003-06 | Unit | US-1003 | Failed step halts execution; RecipeExecutionFailed audit event emitted |
| TC-1003-07 | Integration | US-1003 | Custom RbacSetupStep handler creates roles and assigns permissions |
| TC-1003-08 | Security | US-1003 | Editor role cannot import recipes — 403 returned |
| TC-1004-01 | Unit | US-1004 | Register OIDC client with client_credentials flow returns client secret |
| TC-1004-02 | Integration | US-1004 | client_credentials token request returns valid JWT with correct claims |
| TC-1004-03 | Unit | US-1004 | Token validation uses locally cached JWK set — no network call |
| TC-1004-04 | Unit | US-1004 | Expired token rejected with 401 token_expired error |
| TC-1004-05 | Security | US-1004 | TenantA token rejected for TenantB API with 403 |
| TC-1004-06 | Security | US-1004 | Token with tampered signature rejected with 401 |
| TC-1004-07 | Unit | US-1004 | Key rotation: tokens issued pre-rotation valid during 24h grace period |
| TC-1004-08 | Unit | US-1004 | Key rotation: tokens issued pre-rotation rejected after grace period |
| TC-1004-09 | Integration | US-1004 | OIDC discovery endpoint returns correct issuer and endpoint URLs |
| TC-1004-10 | Unit | US-1004 | Duplicate clientId registration returns 409 Conflict |
| TC-1004-11 | Integration | US-1004 | Platform validates tokens without internet connectivity |
| TC-1005-01 | Unit | US-1005 | Sitemap includes only published content with AutoroutePart |
| TC-1005-02 | Unit | US-1005 | Draft content excluded from sitemap |
| TC-1005-03 | Unit | US-1005 | Sitemap cache invalidated on ContentItemPublished event |
| TC-1005-04 | Unit | US-1005 | Sitemap over 50,000 URLs generates sitemap index |
| TC-1005-05 | Integration | US-1005 | GET /sitemap.xml returns valid XML Sitemap 0.9 document |
| TC-1005-06 | Unit | US-1005 | Turkish character slugs percent-encoded correctly in sitemap URLs |
| TC-1006-01 | Unit | US-1006 | Switch from Lucene to Elasticsearch triggers background reindex job |
| TC-1006-02 | Integration | US-1006 | Search results consistent between Lucene and Elasticsearch for Turkish text |
| TC-1006-03 | Unit | US-1006 | Switch fails gracefully when Elasticsearch is unavailable |
| TC-1006-04 | Integration | US-1006 | Manual reindex rebuilds index matching published content count |
| TC-1006-05 | Unit | US-1006 | Content published during reindex is not missed after job completes |
| TC-1006-06 | Security | US-1006 | TenantAdmin cannot switch search engine — 403 returned |
| TC-1007-01 | Integration | US-1007 | GET /health/live returns 200 when all dependencies healthy |
| TC-1007-02 | Integration | US-1007 | GET /health/ready returns 503 when Redis is unavailable |
| TC-1007-03 | Integration | US-1007 | SIGTERM triggers graceful shutdown; in-flight requests complete |
| TC-1007-04 | Integration | US-1007 | Platform starts on Linux Docker host within 60 seconds |
| TC-1007-05 | Unit | US-1007 | PostgreSQL startup retry uses exponential backoff (max 5 attempts) |
| TC-1007-06 | Unit | US-1007 | All file paths use Path.Combine — no hardcoded OS separators |

### Dedicated Security Test Suite — Multi-Tenant Isolation

Given the P0 risk classification of multi-tenant data leaks (sprint-roadmap.md), a dedicated isolation test class is required:

| Test ID | Scenario | Expected |
|---------|----------|----------|
| TC-ISO-001 | YesSql session not shared across shell boundaries | ISession from TenantA shell cannot query TenantB documents |
| TC-ISO-002 | Direct PostgreSQL query respects schema boundary | SQL query issued in TenantA context uses TenantA schema only |
| TC-ISO-003 | Redis keys for TenantA not readable in TenantB context | Key `tenantB:content:item:*` returns no results when queried from TenantA's ICacheService |
| TC-ISO-004 | Audit events from TenantA not visible in TenantB audit log | IAuditService.ListEventsAsync in TenantB returns empty set for TenantA events |
| TC-ISO-005 | User accounts not shared across tenants | Same username can exist independently in TenantA and TenantB |
| TC-ISO-006 | Recipe execution in TenantA does not affect TenantB configuration | ContentTypes created by TenantA recipe are absent from TenantB |

### Coverage Target
- Unit test coverage: >= 80% for Infrastructure module
- Integration test coverage: >= 60%
- Security tests: minimum 14 (6 RBAC + 6 tenant isolation + 2 OIDC token security)
- Performance tests: 1 (TC-1002-08 cache latency baseline)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-1001 complete — Multi-tenant provisioning and isolation
- [ ] US-1002 complete — Redis cache layer
- [ ] US-1003 complete — Recipe import and export
- [ ] US-1004 complete — OpenID Connect authentication infrastructure
- [ ] US-1005 complete — XML sitemap generation
- [ ] US-1006 complete — Dual search engine support
- [ ] US-1007 complete — Cross-platform Docker deployment hardening

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Tenant yonetimi: Yeni tenant olusturma, yapılandırma, askıya alma ve yeniden etkinlestirme adımları
- Onbellek yonetimi: Onbellek istatistiklerini goruntuleme, kategori bazında TTL yapılandırma, manuel temizleme
- Tarif (Recipe) sistemi: Tarif dosyası formatı, import etme adımları, dısa aktarma ve yedekleme senaryoları
- OpenID Connect: OIDC istemcisi kaydetme, desteklenen akıslar, token suresi yapılandırması, anahtar donusumu
- Site haritası (Sitemap): Hangi icerik turlerinin dahil edildigi, yeniden olusturma, arama motoru bildirimi
- Arama motoru: Lucene.NET ile Elasticsearch arasında gecis, endeks sıfırlama, indeks istatistikleri
- Saglik kontrolleri: /health/live ve /health/ready endpoint'leri, izleme entegrasyonu

### Teknik Dokumantasyon (Technical Documentation)
- `ITenantService` interface contract — tenant lifecycle operations
- `ICacheService` interface contract — cache key conventions, TTL categories, tag invalidation
- `IRecipeService` interface contract — import/export flow, custom step handler registration
- `IOpenIdManagementService` interface contract — OIDC client and scope management
- `ISitemapService` interface contract — sitemap generation and configuration
- Multi-tenant isolation model: shell-per-tenant, schema-per-tenant, YesSql session boundaries
- Cache key naming convention: `{tenantId}:{module}:{resource}:{id}[:{param}]`
- Redis tag-based invalidation: how `ITagCache` is wired to domain events
- OIDC offline validation: JWK caching, key rotation grace period, signing key persistence in YesSql
- Custom recipe steps: `TenantSeedStepHandler`, `RbacSetupStepHandler`, `OpenIdSetupStepHandler`
- Search engine abstraction: `ISearchService` implementation switching via `Search:Engine` config key
- Docker health check implementation: ASP.NET Core `IHealthCheck` registrations, liveness vs. readiness
- PostgreSQL startup retry policy: Polly retry with exponential backoff
- MediatR command/query handlers for all infrastructure operations
- Domain events: `TenantProvisioned`, `TenantSuspended`, `TenantReactivated`, `TenantDeleted`, `CachePurged`, `RecipeImported`, `RecipeExported`, `OpenIdClientCreated`, `SigningKeyRotated`, `SitemapRegenerated`, `SearchEngineSwitched`, `SearchIndexRebuilt`

### API Endpoints
- `POST /api/v1/tenants` — Create tenant
- `GET /api/v1/tenants` — List all tenants (SuperAdmin only)
- `GET /api/v1/tenants/{tenantName}` — Get tenant details
- `PUT /api/v1/tenants/{tenantName}` — Update tenant settings
- `POST /api/v1/tenants/{tenantName}/suspend` — Suspend tenant
- `POST /api/v1/tenants/{tenantName}/reactivate` — Reactivate suspended tenant
- `DELETE /api/v1/tenants/{tenantName}` — Delete tenant (SuperAdmin only — destructive)
- `GET /api/v1/infrastructure/cache/stats` — Get cache statistics for current tenant
- `PUT /api/v1/infrastructure/cache/config` — Update cache TTL configuration
- `POST /api/v1/infrastructure/cache/purge` — Purge all cache for current tenant
- `POST /api/v1/infrastructure/recipes/import` — Import recipe (multipart/form-data)
- `GET /api/v1/infrastructure/recipes/export` — Export current tenant configuration as recipe
- `GET /api/v1/infrastructure/recipes` — List available built-in recipes
- `GET /api/v1/infrastructure/recipes/{executionId}/status` — Get async recipe execution status
- `POST /api/v1/infrastructure/oidc/clients` — Register OIDC client
- `GET /api/v1/infrastructure/oidc/clients` — List OIDC clients
- `GET /api/v1/infrastructure/oidc/clients/{clientId}` — Get OIDC client
- `PUT /api/v1/infrastructure/oidc/clients/{clientId}` — Update OIDC client
- `DELETE /api/v1/infrastructure/oidc/clients/{clientId}` — Delete OIDC client
- `POST /api/v1/infrastructure/oidc/keys/rotate` — Rotate signing keys
- `GET /api/v1/infrastructure/oidc/jwks` — Get public JWK set
- `GET /.well-known/openid-configuration` — OIDC discovery document
- `POST /connect/token` — Token endpoint (OIDC standard)
- `POST /api/v1/infrastructure/sitemap/regenerate` — Trigger sitemap regeneration
- `GET /api/v1/infrastructure/sitemap/config` — Get sitemap configuration
- `PUT /api/v1/infrastructure/sitemap/config` — Update sitemap configuration
- `GET /sitemap.xml` — Public sitemap (anonymous access)
- `POST /api/v1/infrastructure/search/switch` — Switch search engine
- `POST /api/v1/infrastructure/search/reindex` — Trigger full reindex
- `GET /api/v1/infrastructure/search/stats` — Get index statistics
- `GET /health/live` — Liveness health check (unauthenticated)
- `GET /health/ready` — Readiness health check with dependency statuses (unauthenticated)

### Configuration Parameters
- `MultiTenancy:DefaultSchema` — Default PostgreSQL schema for host tenant (default: `"orchard"`)
- `MultiTenancy:TenantSchemaPrefix` — Prefix for tenant schemas (default: `"tenant_"`)
- `MultiTenancy:TenantResolutionHeader` — HTTP header for tenant resolution in API mode (default: `"X-Tenant-Id"`)
- `Redis:ConnectionString` — Redis server connection string (default: `"redis:6379"`)
- `Redis:MaxMemoryMb` — Redis max memory limit in MB (default: `256`)
- `Redis:DefaultTtlSeconds` — Default TTL for cache entries without explicit TTL (default: `300`)
- `Redis:ContentListTtlSeconds` — TTL for content listing cache (default: `300`)
- `Redis:PermissionTtlSeconds` — TTL for permission lookup cache (default: `900`)
- `Redis:SitemapTtlSeconds` — TTL for sitemap XML cache (default: `3600`)
- `OpenId:AccessTokenLifetimeSeconds` — Default access token lifetime (default: `3600`)
- `OpenId:RefreshTokenLifetimeDays` — Default refresh token lifetime (default: `30`)
- `OpenId:KeyRotationGracePeriodHours` — Old key validity after rotation (default: `24`)
- `Sitemap:MaxUrlsPerFile` — Maximum URLs per sitemap file before splitting (default: `50000`)
- `Sitemap:DefaultChangeFrequency` — Default change frequency for content URLs (default: `"weekly"`)
- `Search:Engine` — Active search engine: `"Lucene"` or `"Elasticsearch"` (default: `"Lucene"`)
- `Search:ElasticsearchUrl` — Elasticsearch base URL (default: `"http://elasticsearch:9200"`)
- `Search:IndexNamePrefix` — Prefix for Elasticsearch index names (default: `"projectdora-"`)
- `HealthChecks:StartupRetryCount` — PostgreSQL startup retry attempts (default: `5`)
- `HealthChecks:StartupRetryDelaySeconds` — Initial retry delay for startup (default: `2`, exponential backoff)
- `HealthChecks:GracefulShutdownTimeoutSeconds` — Max time to wait for in-flight requests on shutdown (default: `30`)
