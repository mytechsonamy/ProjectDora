# Sprint S10 — Integration

## Kapsam (Scope)
- Spec items: 4.1.11.1, 4.1.11.2, 4.1.11.3, 4.1.11.4, 4.1.11.5, 4.1.11.6, 4.1.11.7, 4.1.11.8
- Stories: US-1101, US-1102, US-1103, US-1104, US-1105, US-1106, US-1107, US-1108
- Cross-references: 4.1.5.1 (saved queries auto-exposed as API — S04), 4.1.3.1 (content delivery via headless — S03), 4.1.6.1 (API auth scoped to RBAC — S05), 4.1.10.7 (OpenID Connect foundation — S09), 4.1.7.3 (workflow triggers from API events — S06)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.11.1 | Platform uzerinde tanimlanan icerik tiplerine ait icerik ogelerinin REST API aracılıgıyla sorgulanabilmesi, olusturulabilmesi, guncellenmesi ve silinebilmesi | REST API for all content types — full CRUD operations (GET, POST, PUT, PATCH, DELETE) exposed as RESTful endpoints; each content type maps to a resource under `/api/v1/content/{contentType}`; OpenAPI 3.0 documented |
| 4.1.11.2 | Platform uzerinde tanimlanan icerik tiplerine ait icerik ogelerinin GraphQL API aracılıgıyla sorgulanabilmesi | GraphQL API for content delivery — Hot Chocolate server exposes all content types as queryable GraphQL types; supports field selection, filtering, pagination, and nested relations |
| 4.1.11.3 | Platformun basit bir Headless CMS olarak kullanılabilmesi; icerik ogelerinin JSON formatinda API uzerinden servis edilmesi | Headless CMS mode — content items served as structured JSON via both REST and GraphQL APIs; supports public (no auth) and authenticated endpoints configurable per content type |
| 4.1.11.4 | Platform API'lerinin OpenID Connect / JWT Bearer ile korunması; izinsiz erisimin engellenmesi | API authentication via OpenID Connect / JWT Bearer — all protected endpoints require a valid JWT issued by the platform's built-in OIDC provider (S09); anonymous access only for explicitly configured public endpoints |
| 4.1.11.5 | Kaydedilmis sorguların (Saved Queries) otomatik olarak API endpointlerine donusturulmesi | Auto-API generation from saved queries — each saved query (Lucene, Elasticsearch, SQL — S04) is automatically exposed as a GET endpoint under `/api/v1/queries/{queryName}/execute`; parameterized queries accept query string args |
| 4.1.11.6 | Platform API'lerinin versiyonlanmasi; geri uyumlulugunu bozan degisiklikler icin yeni API versiyonunun yayınlanması | API versioning strategy — REST endpoints support URL-based versioning (`/api/v1/`, `/api/v2/`); breaking changes always result in a new version; deprecated versions served for a minimum 6-month transition window with `Deprecation` header |
| 4.1.11.7 | Platform uzerinde tanimlanabilir webhook abonelikleri; belirli platform olaylarında (icerik yayinlama, is akisi tamamlanmasi vb.) HTTP POST iletimi | Webhook system — configurable webhook subscriptions that fire HTTP POST on platform events (content publish, workflow completion, user creation, query execution, etc.); supports retry with exponential backoff, payload signing (HMAC-SHA256), and delivery log |
| 4.1.11.8 | GraphQL sorgularinda derinlik ve karmasiklik sinirlamasi; API kotu kullanim onlemleri; rate limiting | API abuse prevention — GraphQL query depth limiting (max 10 levels), complexity limiting (max 500 points), persisted queries optional; REST rate limiting via token bucket (configurable per tenant and API key); DDoS defense headers |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S03 (Content Management) — content items must exist to be served via headless/REST/GraphQL endpoints
- **Dependency**: S04 (Query Management) — saved queries are the source for auto-API generation; `IQueryService` must be stable
- **Dependency**: S05 (User/Role/Permission) — API permissions map to RBAC roles; `IAuthService` enforces JWT claims against permission store
- **Dependency**: S09 (Infrastructure) — OpenID Connect token issuance and validation infrastructure (US-1004) must be complete before JWT-protected API endpoints can be activated; Redis cache available for rate limiting state
- **GraphQL**: Hot Chocolate v14 (ChilliCream) — schema-first or code-first; code-first chosen for dynamic content type registration; requires `HotChocolate.AspNetCore`, `HotChocolate.Data`
- **REST Versioning**: `Asp.Versioning.Mvc` (formerly `Microsoft.AspNetCore.Mvc.Versioning`) for URL-segment versioning
- **Webhook Delivery**: Background `IHostedService` with in-memory queue + PostgreSQL delivery log; `HttpClient` with `IHttpClientFactory` for outbound calls; HMAC-SHA256 payload signing
- **Rate Limiting**: ASP.NET Core 8 built-in `RateLimiter` middleware (`System.Threading.RateLimiting`) — token bucket per API key + IP; Redis-backed for multi-instance deployments
- **OpenAPI**: Swashbuckle (`Swashbuckle.AspNetCore`) for REST docs; Hot Chocolate's built-in GraphQL Voyager for schema exploration
- **Security**: All API inputs validated via FluentValidation pipeline; SQL auto-API queries run read-only; GraphQL mutation access requires explicit permission
- **Tenant Isolation**: Every API request scoped to the current tenant shell; no cross-tenant data access possible; `X-Tenant-Id` header or hostname routing determines shell
- **KVKK/GDPR**: API response payloads may include PII — content type fields classified as PII in data-governance.md must be masked or omitted in public (anonymous) responses
- **Performance**: GraphQL response caching via persisted queries and `@cacheControl` directives; REST responses cached in Redis using existing `ICacheService` (S09); ETag + `If-None-Match` headers for REST cache validation

### RBAC Gereksinimleri

| Permission | SuperAdmin | TenantAdmin | Editor | Author | Analyst | Denetci | Viewer | Anonymous |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Api.Access | Y | Y | Y | Y | Y | Y | Y | - |
| Api.AccessPublic | Y | Y | Y | Y | Y | Y | Y | Y |
| Api.Manage | Y | Y | - | - | - | - | - | - |
| Api.Query | Y | Y | Y | - | Y | Y | - | - |
| Api.ContentRead | Y | Y | Y | Y | Y | Y | Y | - |
| Api.ContentWrite | Y | Y | Y | Y | - | - | - | - |
| Api.GraphQL | Y | Y | Y | Y | Y | Y | Y | - |
| Webhooks.Manage | Y | Y | - | - | - | - | - | - |
| Webhooks.View | Y | Y | - | - | Y | Y | - | - |
| Webhooks.Subscribe | Y | Y | Y | - | - | - | - | - |
| ApiVersioning.ManageDeprecation | Y | Y | - | - | - | - | - | - |

- `Api.Access` — Authenticate and call any protected API endpoint using a valid JWT token
- `Api.AccessPublic` — Access publicly configured endpoints without authentication (headless content delivery for published items)
- `Api.Manage` — Register API keys, configure OIDC clients for API access, manage rate limit profiles per tenant
- `Api.Query` — Execute auto-generated query endpoints (`/api/v1/queries/{name}/execute`); requires QueryEngine.Execute on underlying query
- `Api.ContentRead` — Read content items via REST or GraphQL; subject to content-type-level RBAC from S05
- `Api.ContentWrite` — Create, update, delete content items via REST API; requires underlying ContentModeling permissions
- `Api.GraphQL` — Execute GraphQL queries and mutations; mutations additionally require content-type-level write permission
- `Webhooks.Manage` — Create, update, delete, and test webhook subscriptions for the tenant
- `Webhooks.View` — View webhook subscription list and delivery log (read-only)
- `Webhooks.Subscribe` — Register a webhook subscription (Editor can subscribe to content events for their automation)
- `ApiVersioning.ManageDeprecation` — Mark API versions as deprecated, set sunset date, configure transition window

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-1101 | 4.1.11.1, 4.1.11.3 | P1 | REST Content API — full CRUD endpoints for all content types with OpenAPI docs |
| US-1102 | 4.1.11.2, 4.1.11.3 | P1 | GraphQL API — Hot Chocolate server with dynamic content type schema, filtering, pagination |
| US-1103 | 4.1.11.4 | P0 | API authentication — JWT Bearer / OIDC enforcement, API key support, public endpoint configuration |
| US-1104 | 4.1.11.5 | P1 | Auto-API from saved queries — expose saved queries as REST endpoints with parameter mapping |
| US-1105 | 4.1.11.6 | P1 | API versioning — URL-segment versioning, deprecation headers, version lifecycle management |
| US-1106 | 4.1.11.7 | P1 | Webhook system — subscription management, event delivery, retry, HMAC-SHA256 signing, delivery log |
| US-1107 | 4.1.11.8 | P0 | API abuse prevention — GraphQL depth/complexity limiting, REST rate limiting, DDoS defense |
| US-1108 | 4.1.11.1, 4.1.11.2, 4.1.11.4 | P0 | Integration RBAC enforcement and tenant isolation — security audit of all API layers |

### Priority Rationale

- **P0 (Security / Blocking)**: US-1103 (authentication enforcement) and US-1107 (abuse prevention) and US-1108 (RBAC + tenant isolation) — unauthenticated API endpoints or unthrottled GraphQL are immediate P0 risks; these must be in place before any content or query API is reachable from external clients.
- **P1 (Core Deliverables)**: US-1101 (REST content API), US-1102 (GraphQL), US-1104 (auto-query API), US-1105 (versioning), US-1106 (webhooks) — these constitute the complete integration surface defined in spec 4.1.11; all are required for the KOSGEB acceptance milestone.
- **Sequencing**: US-1103 must land before US-1101, US-1102, and US-1104 — all three expose data and must be protected from day one. US-1107 must land alongside US-1102 (GraphQL introspection and depth attacks are live immediately upon schema exposure).

### Entity and Component Mapping

| Domain Concept | Technology Component | ProjectDora Layer |
|---------------|---------------------|-------------------|
| REST content endpoint | `ApiController` + `Asp.Versioning` | `ContentApiController`, maps to `IContentService` |
| GraphQL schema | Hot Chocolate `ObjectType<T>` | `ContentQueryType`, `ContentMutationType` per registered content type |
| Headless content response | `ContentItemDto` (JSON) | Shaped by `IContentApiSerializer` from `ContentItem` |
| API authentication | Orchard Core OpenId `JwtBearerDefaults` | `ApiAuthenticationMiddleware`, `IApiKeyService` |
| Saved query endpoint | `QueryApiController` | Auto-registered from `ISavedQueryRepository` at startup |
| API version | `ApiVersion` attribute + URL segment | `v{major}` URL prefix, `ApiVersioningOptions` |
| Webhook subscription | `WebhookSubscription` EF entity | `IWebhookService`, PostgreSQL `integration` schema |
| Webhook delivery | `WebhookDeliveryLog` EF entity | `WebhookDispatchJob` background service |
| Rate limiter | `TokenBucketRateLimiter` | Redis-backed state via `ICacheService`, per API key + IP |
| GraphQL depth guard | Hot Chocolate `MaxExecutionDepth` rule | Configured in `AddGraphQL()` builder pipeline |
| OpenAPI docs | Swashbuckle + versioned doc groups | Generated at `/swagger/v1/swagger.json` per version |

### Dependency Graph

```
US-1103 (API Authentication — P0)
  |
  +-- US-1108 (RBAC Enforcement — P0) --> security wrapper around all API layers
  |     |
  |     +-- US-1107 (Abuse Prevention — P0) --> rate limiting + GraphQL guards co-deployed
  |
  +-- US-1101 (REST Content API — P1) --> protected by US-1103, versioned by US-1105
  |     |
  |     +-- US-1105 (API Versioning — P1) --> cross-cutting, applied to REST + GraphQL
  |
  +-- US-1102 (GraphQL API — P1) --> protected by US-1103 + depth guards from US-1107
  |
  +-- US-1104 (Auto-API from Queries — P1) --> protected by US-1103; sources from S04 QueryEngine
  |
  +-- US-1106 (Webhooks — P1) --> fires on events from US-1101 / US-1102 / WorkflowEngine (S06)
```

## Teknik Kararlar (Technical Decisions)

### D-001: IIntegrationService — Abstraction Interface

All integration layer management operations (API key lifecycle, webhook subscriptions, delivery dispatch, version management) flow through typed abstraction interfaces in `ProjectDora.Core.Abstractions`. This maintains ADR-001's Orchard Core isolation mandate; the HTTP layer (Hot Chocolate, MVC controllers) never calls Orchard Core internals directly.

```csharp
// IIntegrationService — top-level integration facade
public interface IIntegrationService
{
    // API key management
    Task<ApiKeyDto> CreateApiKeyAsync(CreateApiKeyCommand command, CancellationToken ct);
    Task<ApiKeyDto> GetApiKeyAsync(string keyId, CancellationToken ct);
    Task<IReadOnlyList<ApiKeyDto>> ListApiKeysAsync(string tenantId, CancellationToken ct);
    Task RevokeApiKeyAsync(string keyId, CancellationToken ct);

    // Public endpoint configuration
    Task<PublicEndpointConfigDto> GetPublicEndpointConfigAsync(string contentType, CancellationToken ct);
    Task UpdatePublicEndpointConfigAsync(UpdatePublicEndpointConfigCommand command, CancellationToken ct);

    // API version lifecycle
    Task<ApiVersionDto> GetVersionInfoAsync(string version, CancellationToken ct);
    Task DeprecateVersionAsync(DeprecateVersionCommand command, CancellationToken ct);
}

// IWebhookService — webhook subscription and delivery management
public interface IWebhookService
{
    Task<WebhookSubscriptionDto> CreateSubscriptionAsync(CreateWebhookSubscriptionCommand command, CancellationToken ct);
    Task<WebhookSubscriptionDto> GetSubscriptionAsync(string subscriptionId, CancellationToken ct);
    Task<IReadOnlyList<WebhookSubscriptionDto>> ListSubscriptionsAsync(string tenantId, CancellationToken ct);
    Task<WebhookSubscriptionDto> UpdateSubscriptionAsync(UpdateWebhookSubscriptionCommand command, CancellationToken ct);
    Task DeleteSubscriptionAsync(string subscriptionId, CancellationToken ct);
    Task<WebhookTestResultDto> TestSubscriptionAsync(string subscriptionId, CancellationToken ct);
    Task<PagedResult<WebhookDeliveryLogDto>> GetDeliveryLogAsync(string subscriptionId, int page, int pageSize, CancellationToken ct);

    // Dispatch (called internally by event handlers)
    Task DispatchAsync(string eventName, object payload, string tenantId, CancellationToken ct);
}

// IContentApiSerializer — shapes ContentItem into headless JSON output
public interface IContentApiSerializer
{
    Task<ContentItemApiDto> SerializeAsync(ContentItem item, SerializationContext context, CancellationToken ct);
    Task<IReadOnlyList<ContentItemApiDto>> SerializeManyAsync(IEnumerable<ContentItem> items, SerializationContext context, CancellationToken ct);
}

// IApiKeyService — API key hashing and validation
public interface IApiKeyService
{
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(string rawKey, CancellationToken ct);
    string GenerateKey();
    string HashKey(string rawKey); // SHA-256, stored in DB; raw key shown only once at creation
}
```

Module: `ProjectDora.Modules.Integration`
Implements: `IIntegrationService`, `IWebhookService`, `IContentApiSerializer`, `IApiKeyService`
Consumes: `IContentService` (S03), `IQueryService` (S04), `IAuthService` (S05), `ICacheService` (S09), `IOpenIdManagementService` (S09)

### D-002: REST vs GraphQL — Dual-Mode Strategy

Both REST and GraphQL are exposed simultaneously. They are not alternatives — they serve different consumer patterns:

| Dimension | REST (`/api/v1/content/`) | GraphQL (`/graphql`) |
|-----------|--------------------------|---------------------|
| Primary consumer | Server-to-server integrations, external partners | Internal KOSGEB dashboard, rich client apps |
| Schema discovery | OpenAPI / Swagger UI | GraphQL introspection / Voyager |
| Field selection | Fixed response shape (DTO) per endpoint | Client-driven field selection |
| Mutations | POST/PUT/PATCH/DELETE verbs | `Mutation` type |
| Caching | HTTP ETag + Redis | Persisted queries + `@cacheControl` |
| Rate limiting | Token bucket per API key | Complexity scoring per query |
| Versioning | URL segment (`/v1/`, `/v2/`) | Schema evolution (no version in URL) |

GraphQL schema evolution policy: additive changes only (new fields, new types) without a version bump. Removal or type changes require a deprecation annotation (`@deprecated(reason: "...")`) for minimum 3 sprints before removal.

### D-003: API Versioning Strategy

URL-segment versioning is chosen over header-based or query-string versioning for clarity and cacheability:

```
/api/v1/content/{contentType}      — current version
/api/v2/content/{contentType}      — next version (introduced only for breaking changes)
```

Implementation: `Asp.Versioning.Mvc` NuGet package. Each controller declares `[ApiVersion("1.0")]`. The `ApiVersioningOptions` sets `DefaultApiVersion = new ApiVersion(1, 0)` and `AssumeDefaultVersionWhenUnspecified = true`.

Breaking change definition — any of:
- Removing a response field
- Changing a field's type
- Renaming an endpoint path
- Changing authentication requirements
- Modifying pagination contract

Non-breaking (no version bump required):
- Adding new optional response fields
- Adding new optional query parameters
- Adding new endpoints
- Performance improvements

Deprecation lifecycle:
1. New version released → old version decorated with `[Deprecated]` attribute
2. Response includes `Deprecation: true` and `Sunset: {date}` HTTP headers
3. Minimum 6-month transition window before old version removal
4. Sunset date published in OpenAPI docs and release notes

### D-004: JWT Bearer Authentication and API Key Support

Two authentication schemes are supported, evaluated in order:

**Scheme 1 — JWT Bearer (primary)**
- Tokens issued by Orchard Core's built-in OIDC provider (US-1004 / S09)
- Validated offline using locally cached JWK set
- Claims: `sub` (user ID), `tenant` (tenant ID), `permissions` (comma-separated permission list)
- Access token lifetime: 1 hour (configurable per tenant)

**Scheme 2 — API Key (service-to-service)**
- Format: `Bearer pd_{tenantId}_{random40hexchars}` in `Authorization` header, OR `X-Api-Key` header
- Raw key shown once at creation; SHA-256 hash stored in `integration.ApiKeys` table
- API keys have configurable scopes (subset of permissions), expiry date, and rate limit profile
- API key requests bypass interactive OIDC flows — suitable for CI/CD pipelines and partner integrations

**Public endpoints** (anonymous access):
- Configured per content type via `UpdatePublicEndpointConfigCommand`
- Only `Published` content items are ever returned in anonymous responses
- PII fields (classified in data-governance.md) are always masked in anonymous responses
- Public endpoints still rate-limited (more aggressively than authenticated)

```csharp
// Authentication middleware registration order
builder.Services
    .AddAuthentication()
    .AddJwtBearer("Orchard", options => { /* OIDC JWK config */ })
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>("ApiKey", options => { });

// Policy: accept either scheme
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("Orchard", "ApiKey")
        .Build();
});
```

### D-005: Webhook System — Delivery Guarantees and Signing

Delivery model: **at-least-once** with idempotency key. Subscribers must handle duplicate delivery.

Delivery flow:
1. Platform event fires (e.g., `ContentItemPublished`)
2. `WebhookEventHandler` (implements `IOrchardEventHandler`) queries active subscriptions matching the event name and tenant
3. Each matching subscription queued to `WebhookDispatchJob` (in-memory `Channel<T>` for speed + PostgreSQL `integration.WebhookDeliveryQueue` for durability)
4. Background worker dequeues and POSTs to subscriber URL with 5s timeout
5. On success (2xx): log delivery as `Delivered`; on failure: exponential backoff retry (delays: 1m, 5m, 30m, 2h, 8h — max 5 attempts)
6. After 5 failed attempts: log as `Failed`; optionally emit `WebhookDeliveryFailed` platform event for alert workflows

HMAC-SHA256 signing:
```
X-ProjectDora-Signature: sha256={HMAC-SHA256(secret, requestBody)}
X-ProjectDora-Event: content.published
X-ProjectDora-Delivery: {deliveryId (UUID)}
X-ProjectDora-Timestamp: {Unix epoch seconds}
```
- Subscribers validate signature before processing
- `secret` is a per-subscription 32-byte random secret, stored encrypted in DB (AES-256, key from tenant configuration)
- Timestamp included to enable replay attack prevention (subscriber should reject payloads older than 5 minutes)

### D-006: GraphQL Depth, Complexity, and Introspection Controls

Hot Chocolate configuration:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMaxExecutionDepth(10)                      // max 10 nesting levels
    .AddQueryComplexity(options =>
    {
        options.MaximumAllowed = 500;              // 500 complexity points per query
        options.DefaultComplexity = 1;             // scalar field = 1 point
        options.DefaultChildComplexity = 1;        // object field adds 1 + child cost
        options.ApplyComplexityMultiplierOnFirst = true;  // paging multiplier
    })
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = IsDevelopment;
    });
```

Introspection: enabled in development; disabled in production by default; can be re-enabled for authenticated `Api.Manage` users only via a feature flag in tenant settings.

Persisted queries (optional per tenant): clients pre-register query strings by hash; server only executes registered queries. Reduces attack surface and enables schema evolution tracking.

### D-007: Auto-API Generation from Saved Queries

At application startup (and on saved query create/update/delete events), `QueryApiRegistrar` introspects all saved queries from `IQueryService` and registers corresponding ASP.NET Core routes dynamically.

Route pattern: `GET /api/v1/queries/{queryName}/execute`

- Parameters: query string key-value pairs mapped to saved query parameters; validated against parameter schema stored with the query
- Response: `PagedResult<JsonElement>` — raw JSON rows (Lucene/ES hits or SQL result set)
- SQL queries: always executed read-only (enforced at `IQueryService` level from S04); no DDL or DML
- Authentication: inherits from `Api.Query` permission + `QueryEngine.Execute` permission on the underlying saved query
- Rate limiting: 30 requests/minute per API key for query endpoints (more expensive than content reads)

Dynamic registration example:
```csharp
// Called at startup and on IQueryUpdatedEvent
public class QueryApiRegistrar : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var queries = await _queryService.ListSavedQueriesAsync(ct);
        foreach (var query in queries)
            _endpointRouteBuilder.MapGet(
                $"/api/v1/queries/{query.Name}/execute",
                ctx => ExecuteQueryEndpoint(ctx, query.Name));
    }
}
```

### D-008: Rate Limiting Strategy

Two layers of rate limiting, both tenant-scoped:

| Layer | Scope | Limit | Backend |
|-------|-------|-------|---------|
| IP-based (anonymous) | Per IP per tenant | 60 req/min | Redis token bucket |
| API key (authenticated) | Per API key per tenant | Configurable (default 600 req/min) | Redis token bucket |
| Query endpoints | Per API key per tenant | 30 req/min | Redis token bucket |
| GraphQL | Per API key per tenant | Complexity budget 5000 pts/min | Redis |

ASP.NET Core 8 `RateLimiter` middleware via `System.Threading.RateLimiting`. Redis-backed state ensures limits hold across multiple application instances in a multi-node deployment.

Rate limit response: HTTP 429 Too Many Requests with headers:
```
Retry-After: {seconds}
X-RateLimit-Limit: {limit}
X-RateLimit-Remaining: {remaining}
X-RateLimit-Reset: {Unix epoch}
```

### D-009: Database Schema for Integration Module

New schema `integration` in PostgreSQL (separate from `orchard` and `audit` schemas). Managed by EF Core `IntegrationDbContext`.

Tables:
- `integration.ApiKeys` — id, tenantId, name, keyHash, scopes, expiresAt, revokedAt, createdAt, createdBy, rateProfileId
- `integration.ApiRateProfiles` — id, tenantId, name, requestsPerMinute, queryRequestsPerMinute
- `integration.WebhookSubscriptions` — id, tenantId, targetUrl, eventNames (jsonb array), secretEncrypted, isActive, createdAt, createdBy
- `integration.WebhookDeliveryLog` — id, subscriptionId, deliveryId, eventName, statusCode, attempt, requestPayload, responseBody, deliveredAt, nextRetryAt, status
- `integration.PublicEndpointConfig` — id, tenantId, contentType, isPublic, maskedFields (jsonb array), updatedAt
- `integration.ApiVersions` — id, versionLabel, isDeprecated, sunsetDate, releaseNotes

See `docs/domain-model.md` for full ER diagram including `integration` schema tables.

## DoR YAML — User Stories

### US-1101: REST Content API

```yaml
story_id: "US-1101"
title: "Expose all content types as versioned REST CRUD endpoints with OpenAPI docs"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.1"
  - "4.1.11.3"
sprint: "S10"
priority: "P1"

role: "As an API consumer (external partner system)"
action: "I want to create, read, update, and delete content items via a REST API"
benefit: "So that external KOSGEB partner systems can integrate with the platform without needing direct database access or admin panel access"

inputs:
  - name: "contentType"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100), Matches('^[A-Za-z][A-Za-z0-9]*$')"
    example: "DestekProgrami"
  - name: "contentItemId"
    type: "string"
    required: false
    validation: "MaxLength(26)"
    example: "4te3x89qzfcbqhm65nmvx9sj2"
  - name: "body"
    type: "ContentItemApiDto (JSON)"
    required: true
    validation: "NotNull, ValidJson, ContentTypeMustExist"
    example: "{\"displayText\": \"KOBİ Teknoloji Desteği\", \"body\": {\"html\": \"...\"}}"
  - name: "page"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 10000)"
    example: 1
  - name: "pageSize"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 100)"
    example: 20

outputs:
  - name: "contentItem"
    type: "ContentItemApiDto"
    description: "Serialized content item as JSON; includes contentItemId, contentType, displayText, createdAt, modifiedAt, status, and all parts/fields as nested objects"
  - name: "items"
    type: "PagedResult<ContentItemApiDto>"
    description: "Paged list of content items for GET collection endpoints"

constraints:
  rbac:
    required_permissions:
      - "Api.ContentRead"
      - "Api.ContentWrite"
    denied_roles:
      - "Anonymous"
  data_model:
    content_type: "dynamic (any registered content type)"
    parts:
      - "TitlePart"
      - "BodyPart"
      - "CommonPart"
      - "AuditTrailPart"
    fields: []
  api:
    endpoint: "GET /api/v1/content/{contentType}"
    request_content_type: "application/json"
    response_codes:
      - 200: "Content items returned (GET collection)"
      - 201: "Content item created (POST)"
      - 204: "Content item deleted (DELETE)"
      - 400: "Validation error (invalid content type, missing required fields)"
      - 401: "No valid JWT or API key present"
      - 403: "Caller lacks Api.ContentRead or Api.ContentWrite permission"
      - 404: "Content type not found or content item not found"
      - 409: "Optimistic concurrency conflict (wrong version in If-Match header)"

acceptance_tests:
  - id: "AT-001"
    scenario: "GET collection returns paged published content items"
    given: "User has Api.ContentRead permission and 5 published 'DestekProgrami' items exist for the tenant"
    when: "User calls GET /api/v1/content/DestekProgrami?page=1&pageSize=3"
    then: "Response 200 with 3 items, totalCount=5, correct pagination links, ETag header set"
  - id: "AT-002"
    scenario: "POST creates a new content item in draft state"
    given: "User has Api.ContentWrite permission and 'DestekProgrami' content type exists"
    when: "User POSTs valid JSON body to /api/v1/content/DestekProgrami"
    then: "Response 201 with contentItemId and status='Draft'; ContentItemCreated audit event emitted"
  - id: "AT-003"
    scenario: "PUT updates existing content item"
    given: "User has Api.ContentWrite permission and content item '4te3x89qzfcbqhm65nmvx9sj2' exists"
    when: "User PUTs updated JSON body with correct If-Match version header"
    then: "Response 200 with updated content item; ContentItemUpdated audit event emitted"
  - id: "AT-004"
    scenario: "Anonymous user cannot access protected content"
    given: "No Authorization header is present in the request"
    when: "Anonymous user calls GET /api/v1/content/DestekProgrami"
    then: "Response 401 Unauthorized; WWW-Authenticate header present"
  - id: "AT-005"
    scenario: "Cross-tenant content access is blocked"
    given: "User has valid JWT for TenantA; TenantB has content type 'DestekProgrami'"
    when: "User sends request with X-Tenant-Id: tenantB to GET /api/v1/content/DestekProgrami"
    then: "Response 403 — tenant claim in JWT does not match request tenant; no TenantB data returned"

edge_cases:
  - id: "EC-001"
    scenario: "Content type with Turkish characters in field values"
    input: "displayText = 'Şırnak İlçesi Küçük Ölçekli Girişimci Desteği 2026'"
    expected: "Content created and returned with Turkish characters preserved; UTF-8 encoded in JSON response"
  - id: "EC-002"
    scenario: "Optimistic concurrency conflict on concurrent update"
    input: "Two clients simultaneously PUT to the same content item ID with the same If-Match value"
    expected: "First PUT succeeds (200); second PUT returns 409 Conflict with message indicating version mismatch"
  - id: "EC-003"
    scenario: "Request for non-existent content type"
    input: "contentType = 'NonExistentType123'"
    expected: "Response 404 with error body indicating content type is not registered"

dependencies:
  stories: ["US-1103", "US-1105", "US-1108"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.ContentModeling"]
  external: ["Swashbuckle.AspNetCore", "Asp.Versioning.Mvc"]

tech_notes:
  abstraction_interface: "IContentService"
  mediatr_commands:
    - "CreateContentViaApiCommand"
    - "CreateContentViaApiCommandHandler"
    - "UpdateContentViaApiCommand"
    - "UpdateContentViaApiCommandHandler"
    - "DeleteContentViaApiCommand"
    - "DeleteContentViaApiCommandHandler"
  mediatr_queries:
    - "GetContentViaApiQuery"
    - "GetContentViaApiQueryHandler"
    - "ListContentViaApiQuery"
    - "ListContentViaApiQueryHandler"
  audit_events:
    - "ContentItemCreatedViaApi"
    - "ContentItemUpdatedViaApi"
    - "ContentItemDeletedViaApi"
  localization_keys:
    - "Integration.Content.Created.Success"
    - "Integration.Content.Updated.Success"
    - "Integration.Content.Deleted.Success"
    - "Integration.Content.NotFound"
    - "Integration.Content.Conflict"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:api:content:{contentType}:{contentItemId}"
    ttl: "5m"
```

### US-1102: GraphQL API

```yaml
story_id: "US-1102"
title: "Expose content types via Hot Chocolate GraphQL server with filtering and pagination"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.2"
  - "4.1.11.3"
sprint: "S10"
priority: "P1"

role: "As a frontend developer building the KOSGEB dashboard"
action: "I want to query content items via GraphQL with field selection, filtering, and pagination"
benefit: "So that the dashboard can fetch exactly the data it needs in a single request, minimizing over-fetching and reducing bandwidth"

inputs:
  - name: "query"
    type: "string (GraphQL document)"
    required: true
    validation: "NotEmpty, MaxLength(10000), ValidGraphQLSyntax, MaxDepth(10), MaxComplexity(500)"
    example: "{ destekProgramiCollection(first: 10, where: { status: { eq: PUBLISHED } }) { nodes { contentItemId displayText body { html } } } }"
  - name: "variables"
    type: "object (JSON)"
    required: false
    validation: "ValidJson"
    example: "{\"limit\": 10}"
  - name: "operationName"
    type: "string"
    required: false
    validation: "MaxLength(100)"
    example: "GetDestekProgramlari"

outputs:
  - name: "data"
    type: "object (GraphQL response)"
    description: "Requested fields for matching content items; shape determined by query"
  - name: "errors"
    type: "List<GraphQLError>"
    description: "Validation or execution errors; depth/complexity violations included here"
  - name: "extensions"
    type: "object"
    description: "Execution stats: complexity score, execution time (development only)"

constraints:
  rbac:
    required_permissions:
      - "Api.GraphQL"
    denied_roles:
      - "Anonymous"
  data_model:
    content_type: "dynamic (all registered content types)"
    parts:
      - "TitlePart"
      - "BodyPart"
      - "LocalizationPart"
    fields: []
  api:
    endpoint: "POST /graphql"
    request_content_type: "application/json"
    response_codes:
      - 200: "Query executed (note: GraphQL always returns 200; errors in response body)"
      - 400: "Malformed JSON or missing 'query' field"
      - 401: "No valid JWT or API key"
      - 429: "Rate limit or complexity budget exceeded"

acceptance_tests:
  - id: "AT-001"
    scenario: "Query content collection with field selection"
    given: "User has Api.GraphQL permission; 5 published 'DestekProgrami' items exist"
    when: "User sends GraphQL query requesting only contentItemId and displayText for first 3 items"
    then: "Response contains exactly 3 nodes with only contentItemId and displayText fields; no extra fields returned"
  - id: "AT-002"
    scenario: "Filter content by status"
    given: "3 Published and 2 Draft 'DestekProgrami' items exist; user has Editor role"
    when: "User queries with where: { status: { eq: PUBLISHED } }"
    then: "Exactly 3 items returned; Draft items are excluded"
  - id: "AT-003"
    scenario: "Query exceeding max depth is rejected"
    given: "User has Api.GraphQL permission"
    when: "User sends a GraphQL query with 11 levels of nesting"
    then: "Response 200 with errors array containing MaxDepthViolation; no data returned"
  - id: "AT-004"
    scenario: "Mutation to create content item"
    given: "User has Api.GraphQL and Api.ContentWrite permissions"
    when: "User sends GraphQL mutation createDestekProgrami with valid input"
    then: "Content item created; response includes new contentItemId; ContentItemCreatedViaApi audit event emitted"
  - id: "AT-005"
    scenario: "Introspection disabled in production"
    given: "Application running in production mode"
    when: "Anonymous or non-Api.Manage user sends __schema introspection query"
    then: "Response 200 with error: 'Introspection is not allowed'"

edge_cases:
  - id: "EC-001"
    scenario: "Query with complexity exactly at limit (500 points)"
    input: "A query calculated by the complexity visitor to score exactly 500 points"
    expected: "Query executes successfully; no complexity error"
  - id: "EC-002"
    scenario: "Query with complexity exceeding limit (501 points)"
    input: "A query calculated to score 501 points"
    expected: "Response 200 with complexity error in errors array; query not executed; complexity score included in error message"
  - id: "EC-003"
    scenario: "Turkish field name in GraphQL filter"
    input: "where clause filtering on a custom Turkish-named field stored as camelCase"
    expected: "Filter applied correctly; Turkish field value comparison is case-sensitive by default, case-insensitive when _icontains operator used"

dependencies:
  stories: ["US-1103", "US-1107", "US-1108"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.ContentModeling"]
  external: ["HotChocolate.AspNetCore", "HotChocolate.Data"]

tech_notes:
  abstraction_interface: "IContentService"
  mediatr_commands:
    - "GraphQLMutationCreateContentCommand"
    - "GraphQLMutationCreateContentCommandHandler"
  mediatr_queries:
    - "GraphQLContentCollectionQuery"
    - "GraphQLContentCollectionQueryHandler"
    - "GraphQLContentItemQuery"
    - "GraphQLContentItemQueryHandler"
  audit_events:
    - "ContentItemCreatedViaGraphQL"
    - "GraphQLQueryExecuted"
  localization_keys:
    - "Integration.GraphQL.ComplexityExceeded"
    - "Integration.GraphQL.DepthExceeded"
    - "Integration.GraphQL.IntrospectionDisabled"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:graphql:query:{queryHash}"
    ttl: "2m"
```

### US-1103: API Authentication

```yaml
story_id: "US-1103"
title: "Enforce JWT Bearer and API key authentication on all protected API endpoints"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.4"
sprint: "S10"
priority: "P0"

role: "As a TenantAdmin"
action: "I want to manage API keys and configure which content endpoints are publicly accessible"
benefit: "So that only authorized systems and users can access protected data, while public-facing content is accessible without credentials"

inputs:
  - name: "apiKeyName"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100)"
    example: "KOSGEB Partner Integration Key"
  - name: "scopes"
    type: "List<string>"
    required: true
    validation: "NotEmpty, Each(OneOf('Api.ContentRead','Api.ContentWrite','Api.Query','Api.GraphQL'))"
    example: "['Api.ContentRead', 'Api.Query']"
  - name: "expiresAt"
    type: "DateTime"
    required: false
    validation: "GreaterThan(UtcNow)"
    example: "2027-01-01T00:00:00Z"

outputs:
  - name: "apiKeyId"
    type: "string"
    description: "Unique identifier for the API key record"
  - name: "rawKey"
    type: "string"
    description: "Full API key string — shown ONLY once at creation; cannot be retrieved again"

constraints:
  rbac:
    required_permissions:
      - "Api.Manage"
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
    endpoint: "POST /api/v1/integration/api-keys"
    request_content_type: "application/json"
    response_codes:
      - 201: "API key created; raw key in response body"
      - 400: "Validation error (invalid scopes, past expiry)"
      - 403: "Caller lacks Api.Manage permission"
      - 401: "No valid JWT present (admin must authenticate first)"

acceptance_tests:
  - id: "AT-001"
    scenario: "Create API key and authenticate with it"
    given: "TenantAdmin is authenticated via JWT and calls POST /api/v1/integration/api-keys"
    when: "TenantAdmin creates a key with scopes=['Api.ContentRead'] and receives raw key"
    then: "Raw key returned in response; subsequent REST request using X-Api-Key header with raw key is accepted and returns 200"
  - id: "AT-002"
    scenario: "Revoked API key is rejected"
    given: "An API key exists and has been used successfully"
    when: "TenantAdmin revokes the key; partner system then uses the revoked key"
    then: "API returns 401; revocation takes effect within 60 seconds (cache TTL); ApiKeyRevoked audit event emitted"
  - id: "AT-003"
    scenario: "Expired API key is rejected"
    given: "An API key with expiresAt in the past exists"
    when: "Partner system presents the expired key"
    then: "API returns 401 with message 'API key has expired'"
  - id: "AT-004"
    scenario: "API key scope enforcement"
    given: "API key has only 'Api.ContentRead' scope"
    when: "Partner system uses the key to attempt POST /api/v1/content/DestekProgrami (write operation)"
    then: "API returns 403 — key lacks Api.ContentWrite scope"
  - id: "AT-005"
    scenario: "Public endpoint accessible without authentication"
    given: "Content type 'Duyuru' is configured as public via UpdatePublicEndpointConfigCommand"
    when: "Anonymous user calls GET /api/v1/content/Duyuru without Authorization header"
    then: "Response 200 with only Published 'Duyuru' items; PII fields masked; authentication not required"

edge_cases:
  - id: "EC-001"
    scenario: "Malformed Authorization header"
    input: "Authorization: Bearer not-a-real-jwt"
    expected: "Response 401 with WWW-Authenticate header; no internal error logged"
  - id: "EC-002"
    scenario: "JWT from wrong tenant presented"
    input: "Valid JWT issued for tenant 'kosgeb-ankara' used in request to tenant 'kosgeb-izmir'"
    expected: "Response 403; tenant claim mismatch detected; CrossTenantAccessAttempt audit event emitted"
  - id: "EC-003"
    scenario: "API key created with no expiry"
    input: "expiresAt = null (not provided)"
    expected: "Key created without expiry; system applies tenant-level maximum lifetime policy (default: 1 year) as a safety ceiling"

dependencies:
  stories: ["US-1004-S09"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.Infrastructure"]
  external: ["Microsoft.AspNetCore.Authentication.JwtBearer", "OrchardCore.OpenId"]

tech_notes:
  abstraction_interface: "IAuthService"
  mediatr_commands:
    - "CreateApiKeyCommand"
    - "CreateApiKeyCommandHandler"
    - "RevokeApiKeyCommand"
    - "RevokeApiKeyCommandHandler"
    - "UpdatePublicEndpointConfigCommand"
    - "UpdatePublicEndpointConfigCommandHandler"
  mediatr_queries:
    - "GetApiKeyQuery"
    - "GetApiKeyQueryHandler"
    - "ListApiKeysQuery"
    - "ListApiKeysQueryHandler"
  audit_events:
    - "ApiKeyCreated"
    - "ApiKeyRevoked"
    - "ApiKeyExpired"
    - "CrossTenantAccessAttempt"
    - "PublicEndpointConfigUpdated"
  localization_keys:
    - "Integration.ApiKey.Created.Success"
    - "Integration.ApiKey.Revoked.Success"
    - "Integration.ApiKey.Expired"
    - "Integration.ApiKey.InvalidScope"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:integration:apikey:{keyHash}"
    ttl: "60s"
```

### US-1104: Auto-API from Saved Queries

```yaml
story_id: "US-1104"
title: "Automatically expose saved queries as REST GET endpoints with parameter mapping"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.5"
sprint: "S10"
priority: "P1"

role: "As a TenantAdmin"
action: "I want saved queries to be automatically available as REST API endpoints"
benefit: "So that non-technical KOSGEB staff can consume search and data queries via simple HTTP calls without knowing query syntax or needing direct platform access"

inputs:
  - name: "queryName"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100), Matches('^[a-zA-Z0-9_-]+$'), QueryMustExist"
    example: "aktif-destek-programlari"
  - name: "parameters"
    type: "Dictionary<string, string> (query string)"
    required: false
    validation: "Keys must match parameter names defined in saved query; each value validated per parameter schema"
    example: "?il=Ankara&yil=2026"
  - name: "page"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 10000)"
    example: 1
  - name: "pageSize"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 100)"
    example: 20

outputs:
  - name: "results"
    type: "PagedResult<JsonElement>"
    description: "Raw JSON rows from Lucene/ES hits or SQL result set; shape depends on the underlying query"
  - name: "queryName"
    type: "string"
    description: "Name of the executed saved query"
  - name: "executionMs"
    type: "int"
    description: "Query execution time in milliseconds"

constraints:
  rbac:
    required_permissions:
      - "Api.Query"
      - "QueryEngine.Execute"
    denied_roles:
      - "Anonymous"
      - "Viewer"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "GET /api/v1/queries/{queryName}/execute"
    request_content_type: "application/json"
    response_codes:
      - 200: "Query executed successfully; results returned"
      - 400: "Missing required parameter or parameter validation failed"
      - 401: "No valid JWT or API key"
      - 403: "Caller lacks Api.Query or QueryEngine.Execute permission"
      - 404: "Saved query '{queryName}' not found"
      - 429: "Query rate limit exceeded (30 req/min per API key)"

acceptance_tests:
  - id: "AT-001"
    scenario: "Execute parameterized saved query via API"
    given: "Saved Lucene query 'aktif-destek-programlari' exists with parameter 'il' (string)"
    when: "User calls GET /api/v1/queries/aktif-destek-programlari/execute?il=Ankara"
    then: "Query executes with il='Ankara' substituted; results returned as paged JSON; QueryExecuted audit event emitted"
  - id: "AT-002"
    scenario: "Endpoints are dynamically registered at startup"
    given: "Platform starts with 5 saved queries defined"
    when: "Application starts up"
    then: "5 route entries created at /api/v1/queries/{name}/execute; no manual registration required; routes visible in Swagger"
  - id: "AT-003"
    scenario: "New saved query creates new endpoint without restart"
    given: "Platform is running; no saved query named 'kucuk-isletmeler' exists"
    when: "TenantAdmin creates a new saved query named 'kucuk-isletmeler'"
    then: "GET /api/v1/queries/kucuk-isletmeler/execute becomes accessible within 30 seconds (no restart)"
  - id: "AT-004"
    scenario: "SQL saved query executes read-only"
    given: "Saved SQL query 'destek-ozet' contains a SELECT statement"
    when: "User executes the query endpoint"
    then: "Query runs in read-only mode; results returned; any attempt to run DDL/DML in the query body is blocked by IQueryService at the SQL layer"

edge_cases:
  - id: "EC-001"
    scenario: "Required query parameter missing"
    input: "GET /api/v1/queries/aktif-destek-programlari/execute (missing required 'il' parameter)"
    expected: "Response 400 with error listing missing parameter name and expected type"
  - id: "EC-002"
    scenario: "Deleted saved query endpoint becomes unavailable"
    input: "TenantAdmin deletes saved query 'aktif-destek-programlari' while route is registered"
    expected: "Route deregistered within 30 seconds; subsequent calls to the endpoint return 404"
  - id: "EC-003"
    scenario: "SQL injection attempt via query parameter"
    input: "GET /api/v1/queries/destek-ozet/execute?il=Ankara' OR '1'='1"
    expected: "Parameter sanitized/escaped by IQueryService parameterization; no SQL injection; results for il='Ankara'' OR ''1''=''1' (literal, no rows)"

dependencies:
  stories: ["US-1103", "US-0501-S04"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.QueryEngine"]
  external: ["Microsoft.AspNetCore.Routing"]

tech_notes:
  abstraction_interface: "IQueryService"
  mediatr_commands: []
  mediatr_queries:
    - "ExecuteSavedQueryViaApiQuery"
    - "ExecuteSavedQueryViaApiQueryHandler"
  audit_events:
    - "QueryExecutedViaApi"
  localization_keys:
    - "Integration.Query.Executed.Success"
    - "Integration.Query.NotFound"
    - "Integration.Query.ParameterMissing"
    - "Integration.Query.RateLimitExceeded"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:api:query:{queryName}:{paramHash}"
    ttl: "5m"
```

### US-1105: API Versioning

```yaml
story_id: "US-1105"
title: "Implement URL-segment API versioning with deprecation lifecycle management"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.6"
sprint: "S10"
priority: "P1"

role: "As a TenantAdmin"
action: "I want to manage the API version lifecycle, mark old versions as deprecated, and set sunset dates"
benefit: "So that external integrators have a stable, predictable API surface with sufficient notice before breaking changes are introduced"

inputs:
  - name: "version"
    type: "string"
    required: true
    validation: "NotEmpty, Matches('^v[0-9]+$')"
    example: "v1"
  - name: "sunsetDate"
    type: "DateTime"
    required: true
    validation: "GreaterThan(UtcNow.AddMonths(6))"
    example: "2027-01-01T00:00:00Z"
  - name: "deprecationReason"
    type: "string"
    required: false
    validation: "MaxLength(500)"
    example: "v2 introduces breaking changes to content response shape; migrate to /api/v2/ before sunset date"

outputs:
  - name: "versionInfo"
    type: "ApiVersionDto"
    description: "Current version state: label, isDeprecated, sunsetDate, releaseNotes URL"

constraints:
  rbac:
    required_permissions:
      - "ApiVersioning.ManageDeprecation"
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
    endpoint: "POST /api/v1/integration/versions/{version}/deprecate"
    request_content_type: "application/json"
    response_codes:
      - 200: "Version marked as deprecated; sunset date set"
      - 400: "Validation error (sunset date less than 6 months, invalid version format)"
      - 403: "Caller lacks ApiVersioning.ManageDeprecation permission"
      - 404: "Version not found"

acceptance_tests:
  - id: "AT-001"
    scenario: "Deprecated version includes Deprecation and Sunset headers"
    given: "API version 'v1' is marked as deprecated with sunsetDate='2027-06-01'"
    when: "External client calls GET /api/v1/content/DestekProgrami"
    then: "Response includes headers: Deprecation: true, Sunset: Mon, 01 Jun 2027 00:00:00 GMT, Link: </api/v2/content/DestekProgrami>; rel='successor-version'"
  - id: "AT-002"
    scenario: "Active version has no deprecation headers"
    given: "API version 'v2' is the current active version (not deprecated)"
    when: "Client calls GET /api/v2/content/DestekProgrami"
    then: "Response does NOT include Deprecation or Sunset headers"
  - id: "AT-003"
    scenario: "Sunset date must be at least 6 months in the future"
    given: "TenantAdmin attempts to deprecate v1 with sunsetDate 3 months from now"
    when: "POST /api/v1/integration/versions/v1/deprecate with sunsetDate=UtcNow.AddMonths(3)"
    then: "Response 400 with error: minimum sunset window is 6 months"

edge_cases:
  - id: "EC-001"
    scenario: "Request to already-deprecated version"
    input: "Version is already marked deprecated; TenantAdmin attempts to deprecate it again with a new sunset date"
    expected: "Deprecation record updated with new sunset date; response 200; ApiVersionDeprecationUpdated audit event emitted"
  - id: "EC-002"
    scenario: "Request to unknown version segment"
    input: "GET /api/v99/content/DestekProgrami (version 99 does not exist)"
    expected: "Response 400 with error: 'API version 99.0 is not supported'; list of supported versions included in error body"

dependencies:
  stories: ["US-1101", "US-1102"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration"]
  external: ["Asp.Versioning.Mvc"]

tech_notes:
  abstraction_interface: "IIntegrationService"
  mediatr_commands:
    - "DeprecateApiVersionCommand"
    - "DeprecateApiVersionCommandHandler"
  mediatr_queries:
    - "GetApiVersionInfoQuery"
    - "GetApiVersionInfoQueryHandler"
  audit_events:
    - "ApiVersionDeprecated"
    - "ApiVersionDeprecationUpdated"
  localization_keys:
    - "Integration.ApiVersion.Deprecated.Success"
    - "Integration.ApiVersion.SunsetTooSoon"
    - "Integration.ApiVersion.NotFound"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:integration:apiversion:{version}"
    ttl: "60m"
```

### US-1106: Webhook System

```yaml
story_id: "US-1106"
title: "Webhook subscription management with signed delivery, retry, and delivery log"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.7"
sprint: "S10"
priority: "P1"

role: "As a TenantAdmin"
action: "I want to configure webhook subscriptions that fire on platform events"
benefit: "So that external KOSGEB partner systems and internal automation tools are notified in real time when content is published, workflows complete, or other significant events occur"

inputs:
  - name: "targetUrl"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(2000), ValidHttpsUrl"
    example: "https://partner.kosgeb.gov.tr/hooks/platform-events"
  - name: "eventNames"
    type: "List<string>"
    required: true
    validation: "NotEmpty, Each(MaxLength(100), ValidEventName)"
    example: "['content.published', 'workflow.completed', 'user.created']"
  - name: "description"
    type: "string"
    required: false
    validation: "MaxLength(500)"
    example: "Notify partner portal when a new DestekProgrami is published"

outputs:
  - name: "subscriptionId"
    type: "string"
    description: "Unique identifier for the webhook subscription"
  - name: "secret"
    type: "string"
    description: "HMAC-SHA256 signing secret — shown ONLY once; subscriber must store it securely"

constraints:
  rbac:
    required_permissions:
      - "Webhooks.Manage"
    denied_roles:
      - "Anonymous"
      - "Author"
      - "Viewer"
      - "Analyst"
      - "Denetci"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "POST /api/v1/integration/webhooks"
    request_content_type: "application/json"
    response_codes:
      - 201: "Subscription created; secret returned"
      - 400: "Validation error (invalid URL, unknown event name)"
      - 403: "Caller lacks Webhooks.Manage permission"
      - 409: "Duplicate subscription (same URL + same event set already active)"

acceptance_tests:
  - id: "AT-001"
    scenario: "Webhook fires on content publish"
    given: "Active webhook subscription for 'content.published' targeting an HTTPS test endpoint"
    when: "Editor publishes a 'DestekProgrami' content item via REST API"
    then: "Within 5 seconds, POST is made to targetUrl with JSON payload containing contentItemId, contentType, eventName, tenantId, timestamp; X-ProjectDora-Signature header is present and HMAC-SHA256 valid"
  - id: "AT-002"
    scenario: "Delivery is retried on transient failure"
    given: "Webhook subscription exists; target URL returns 503 on first attempt"
    when: "Platform event fires and delivery fails with 503"
    then: "Delivery retried after 1 minute (attempt 2); delivery log updated with attempt count; if attempt 2 succeeds, status set to Delivered"
  - id: "AT-003"
    scenario: "After 5 failed attempts, delivery marked as Failed"
    given: "Webhook target URL consistently returns 500 across all retry attempts"
    when: "All 5 delivery attempts are exhausted"
    then: "Delivery log entry status set to Failed; WebhookDeliveryFailed platform event emitted; no further retries; subscription remains active for future events"
  - id: "AT-004"
    scenario: "Test delivery validates connectivity before activation"
    given: "TenantAdmin creates a webhook subscription"
    when: "TenantAdmin calls POST /api/v1/integration/webhooks/{id}/test"
    then: "Platform sends a test POST to targetUrl with eventName='webhook.test'; response from target (status code, latency) returned to caller"
  - id: "AT-005"
    scenario: "Delivery log shows full history"
    given: "10 deliveries have been attempted for a subscription (mix of success and retry)"
    when: "TenantAdmin calls GET /api/v1/integration/webhooks/{id}/deliveries?page=1&pageSize=10"
    then: "Paged delivery log returned with deliveryId, eventName, attempt, statusCode, deliveredAt, and responseBodyPreview for each entry"

edge_cases:
  - id: "EC-001"
    scenario: "Webhook target URL times out (> 5 seconds)"
    input: "Target URL is reachable but takes 6 seconds to respond"
    expected: "Delivery marked as failed (timeout); 5-second timeout strictly enforced; HttpClient.Timeout = 5s; retry scheduled as per backoff policy"
  - id: "EC-002"
    scenario: "Webhook payload for deleted content item"
    input: "Content item is created then immediately deleted before webhook delivery thread processes the event"
    expected: "Webhook fires with the original payload (event snapshot at time of trigger); payload does not attempt live fetch of content item"
  - id: "EC-003"
    scenario: "Subscribe to unknown event name"
    input: "eventNames = ['nonexistent.event.type']"
    expected: "Response 400 with error listing valid event names; subscription not created"

dependencies:
  stories: ["US-1101", "US-1103"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.Workflows"]
  external: ["PostgreSQL (integration schema)", "System.Net.Http.HttpClient"]

tech_notes:
  abstraction_interface: "IIntegrationService"
  mediatr_commands:
    - "CreateWebhookSubscriptionCommand"
    - "CreateWebhookSubscriptionCommandHandler"
    - "UpdateWebhookSubscriptionCommand"
    - "UpdateWebhookSubscriptionCommandHandler"
    - "DeleteWebhookSubscriptionCommand"
    - "DeleteWebhookSubscriptionCommandHandler"
    - "TestWebhookSubscriptionCommand"
    - "TestWebhookSubscriptionCommandHandler"
  mediatr_queries:
    - "ListWebhookSubscriptionsQuery"
    - "ListWebhookSubscriptionsQueryHandler"
    - "GetWebhookDeliveryLogQuery"
    - "GetWebhookDeliveryLogQueryHandler"
  audit_events:
    - "WebhookSubscriptionCreated"
    - "WebhookSubscriptionDeleted"
    - "WebhookDelivered"
    - "WebhookDeliveryFailed"
    - "WebhookSecretRotated"
  localization_keys:
    - "Integration.Webhook.Created.Success"
    - "Integration.Webhook.Deleted.Success"
    - "Integration.Webhook.Test.Sent"
    - "Integration.Webhook.InvalidEventName"
    - "Integration.Webhook.InvalidUrl"
  caching:
    strategy: "ReadThrough"
    key_pattern: "{tenantId}:integration:webhooks:active:{eventName}"
    ttl: "5m"
```

### US-1107: API Abuse Prevention

```yaml
story_id: "US-1107"
title: "GraphQL depth/complexity limiting and REST rate limiting to prevent API abuse"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.8"
sprint: "S10"
priority: "P0"

role: "As a TenantAdmin"
action: "I want configurable rate limits and GraphQL complexity guards protecting all API endpoints"
benefit: "So that the platform remains available under high load and malicious clients cannot exhaust resources via deeply nested GraphQL queries or request flooding"

inputs:
  - name: "rateLimitProfileName"
    type: "string"
    required: true
    validation: "NotEmpty, MaxLength(100)"
    example: "PartnerDefault"
  - name: "requestsPerMinute"
    type: "int"
    required: true
    validation: "InclusiveBetween(1, 10000)"
    example: 600
  - name: "queryRequestsPerMinute"
    type: "int"
    required: true
    validation: "InclusiveBetween(1, 1000)"
    example: 30
  - name: "graphqlMaxDepth"
    type: "int"
    required: false
    validation: "InclusiveBetween(1, 15)"
    example: 10
  - name: "graphqlMaxComplexity"
    type: "int"
    required: false
    validation: "InclusiveBetween(50, 2000)"
    example: 500

outputs:
  - name: "profile"
    type: "ApiRateProfileDto"
    description: "Created or updated rate limit profile with all settings"

constraints:
  rbac:
    required_permissions:
      - "Api.Manage"
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
    endpoint: "POST /api/v1/integration/rate-profiles"
    request_content_type: "application/json"
    response_codes:
      - 201: "Rate limit profile created"
      - 400: "Validation error"
      - 403: "Caller lacks Api.Manage permission"

acceptance_tests:
  - id: "AT-001"
    scenario: "Rate limit enforced: 429 after exceeding threshold"
    given: "Anonymous IP rate limit is 60 req/min; no API key in use"
    when: "Anonymous client sends 61 requests within 60 seconds"
    then: "First 60 requests receive 200; 61st request receives 429 Too Many Requests with Retry-After header"
  - id: "AT-002"
    scenario: "GraphQL query rejected for exceeding max depth"
    given: "Max depth is configured at 10"
    when: "Client sends GraphQL query with 11 levels of nesting"
    then: "Response 200 with errors array containing 'Max allowed execution depth exceeded'; no data field present in response"
  - id: "AT-003"
    scenario: "GraphQL query rejected for exceeding max complexity"
    given: "Max complexity is 500 points"
    when: "Client sends a query with calculated complexity of 501 points"
    then: "Response 200 with errors array containing 'Query complexity 501 exceeds maximum allowed 500'; query not executed"
  - id: "AT-004"
    scenario: "Authenticated API key has higher rate limit than anonymous"
    given: "Anonymous limit: 60 req/min; API key profile: 600 req/min"
    when: "Authenticated client with API key sends 200 requests in 60 seconds"
    then: "All 200 requests succeed; rate limit not triggered for authenticated client"
  - id: "AT-005"
    scenario: "Rate limit state is tenant-scoped"
    given: "TenantA anonymous limit reached (60 req/min)"
    when: "Anonymous client sends request to TenantB"
    then: "Request succeeds — TenantA and TenantB rate limit counters are independent"

edge_cases:
  - id: "EC-001"
    scenario: "Redis unavailable — rate limiting degrades gracefully"
    input: "Redis connection lost while rate limiter middleware is active"
    expected: "Rate limiting middleware falls back to in-memory token bucket (best-effort, not cluster-shared); requests are not blocked; warning logged; circuit breaker opens after 3 consecutive Redis errors"
  - id: "EC-002"
    scenario: "GraphQL mutation complexity scoring"
    input: "GraphQL mutation with 3 nested relation writes"
    expected: "Complexity scorer counts mutations at 10 points each (not 1); complexity guardrail is more sensitive for write operations"

dependencies:
  stories: ["US-1102", "US-1103"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.Infrastructure"]
  external: ["System.Threading.RateLimiting", "HotChocolate.AspNetCore", "StackExchange.Redis"]

tech_notes:
  abstraction_interface: "IIntegrationService"
  mediatr_commands:
    - "CreateRateProfileCommand"
    - "CreateRateProfileCommandHandler"
    - "UpdateRateProfileCommand"
    - "UpdateRateProfileCommandHandler"
  mediatr_queries:
    - "GetRateProfileQuery"
    - "GetRateProfileQueryHandler"
  audit_events:
    - "RateLimitExceeded"
    - "GraphQLDepthViolation"
    - "GraphQLComplexityViolation"
    - "RateProfileCreated"
    - "RateProfileUpdated"
  localization_keys:
    - "Integration.RateLimit.Exceeded"
    - "Integration.GraphQL.DepthExceeded"
    - "Integration.GraphQL.ComplexityExceeded"
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

### US-1108: Integration RBAC Enforcement and Tenant Isolation

```yaml
story_id: "US-1108"
title: "Security audit: enforce RBAC and tenant isolation across all integration API layers"
module: "ProjectDora.Modules.Integration"
spec_refs:
  - "4.1.11.1"
  - "4.1.11.2"
  - "4.1.11.4"
sprint: "S10"
priority: "P0"

role: "As a Security Auditor"
action: "I want all integration API endpoints to enforce RBAC permissions and tenant boundaries"
benefit: "So that no user or system can access, modify, or receive data belonging to another tenant or a permission scope they have not been granted"

inputs:
  - name: "targetEndpoint"
    type: "string"
    required: true
    validation: "NotEmpty"
    example: "/api/v1/content/DestekProgrami"
  - name: "callerRole"
    type: "string"
    required: true
    validation: "NotEmpty, OneOf('Anonymous','Viewer','Author','Editor','Analyst','Denetci','TenantAdmin','SuperAdmin')"
    example: "Viewer"

outputs:
  - name: "accessGranted"
    type: "bool"
    description: "Whether the caller role is allowed to access the endpoint"
  - name: "httpStatusCode"
    type: "int"
    description: "Expected HTTP status code (200, 401, 403)"

constraints:
  rbac:
    required_permissions:
      - "Api.Access"
    denied_roles:
      - "Anonymous"
  data_model:
    content_type: "N/A"
    parts: []
    fields: []
  api:
    endpoint: "N/A (this story covers security test scaffolding, not a new endpoint)"
    request_content_type: "application/json"
    response_codes:
      - 200: "Access granted"
      - 401: "Unauthenticated"
      - 403: "Insufficient permission"

acceptance_tests:
  - id: "AT-001"
    scenario: "RBAC matrix verified: all roles vs all integration endpoints"
    given: "A test matrix of all 8 roles against all 20+ integration endpoints is defined"
    when: "Security test suite runs against all endpoint/role combinations"
    then: "Every combination matches the RBAC matrix in the sprint analysis; zero deviations"
  - id: "AT-002"
    scenario: "Tenant isolation: no cross-tenant data access via REST"
    given: "TenantA JWT used in all REST requests; TenantB has identical content type and content"
    when: "All REST content endpoints are called with TenantA JWT but X-Tenant-Id: tenantB header"
    then: "All requests return 403; zero TenantB records returned; CrossTenantAccessAttempt audit events emitted"
  - id: "AT-003"
    scenario: "Tenant isolation: no cross-tenant data access via GraphQL"
    given: "TenantA JWT; GraphQL query for all content types"
    when: "GraphQL query sent with TenantA JWT but request routes to TenantB shell"
    then: "Query returns 403 via middleware before schema execution; TenantB data never reaches resolver"
  - id: "AT-004"
    scenario: "Webhook delivery does not expose cross-tenant secrets"
    given: "TenantA and TenantB both have active webhooks for 'content.published'"
    when: "TenantA publishes content"
    then: "Only TenantA webhook subscriptions receive delivery; TenantB subscriptions receive no delivery; TenantA secret is not present in TenantB delivery payloads"
  - id: "AT-005"
    scenario: "API key scope restriction enforced for GraphQL mutations"
    given: "API key with only 'Api.ContentRead' scope"
    when: "Client uses the key to send a GraphQL mutation"
    then: "Response 200 with error: 'Unauthorized: mutation requires Api.ContentWrite scope'; mutation not executed"

edge_cases:
  - id: "EC-001"
    scenario: "IDOR attempt: access content item from another tenant by guessing ID"
    input: "Valid TenantA JWT; content item ID belonging to TenantB in the request path"
    expected: "Response 404 (not 403) — TenantA cannot confirm TenantB's content item IDs exist; information leakage prevented"
  - id: "EC-002"
    scenario: "Webhook endpoint URL points to internal platform address"
    input: "targetUrl = 'http://localhost:5000/api/v1/admin/tenants' (SSRF attempt)"
    expected: "Validation rejects URL at creation time: private IP ranges and localhost are blocked; response 400 with 'SSRF protection: internal URLs are not permitted'"

dependencies:
  stories: ["US-1101", "US-1102", "US-1103", "US-1104", "US-1106"]
  modules: ["ProjectDora.Core", "ProjectDora.Modules.Integration", "ProjectDora.Modules.Infrastructure"]
  external: []

tech_notes:
  abstraction_interface: "IAuthService"
  mediatr_commands: []
  mediatr_queries:
    - "IntegrationRbacMatrixQuery"
    - "IntegrationRbacMatrixQueryHandler"
  audit_events:
    - "CrossTenantAccessAttempt"
    - "IntegrationSecurityViolation"
  localization_keys:
    - "Integration.Security.CrossTenantDenied"
    - "Integration.Security.SsrfBlocked"
    - "Integration.Security.InsufficientScope"
  caching:
    strategy: "None"
    key_pattern: ""
    ttl: ""
```

## Test Gereksinimleri (Test Requirements)

### Unit Tests

| Test Class | Scenarios | Notes |
|-----------|-----------|-------|
| `ContentApiControllerTests` | GET collection paging, POST create, PUT update, DELETE, ETag handling | Mock `IContentService`, `IAuthService`; verify response shape and status codes |
| `GraphQLSchemaTests` | Dynamic type registration per content type, field selection, filtering | Use Hot Chocolate test client; verify schema shape after content type registration |
| `GraphQLDepthComplexityTests` | Depth limit (10), complexity limit (500), mutation complexity multiplier | Parameterized tests with queries at N-1, N, N+1 boundary values |
| `ApiKeyServiceTests` | Key generation, SHA-256 hashing, scope validation, expiry enforcement | No external deps; pure unit tests |
| `ApiKeyAuthHandlerTests` | Valid key accepted, revoked key rejected, wrong tenant key rejected | Mock `IApiKeyService`, `ICacheService` |
| `JwtBearerAuthTests` | Valid JWT accepted, expired JWT rejected, wrong tenant claim rejected | Use test JWK set; offline validation only |
| `WebhookServiceTests` | Subscription CRUD, HMAC-SHA256 signature generation, SSRF URL validation | Mock HTTP outbound calls |
| `WebhookDispatchJobTests` | Successful delivery, 503 retry scheduling, 5-attempt exhaustion, timeout handling | Mock `HttpClient`; verify retry interval calculations |
| `QueryApiRegistrarTests` | Routes registered at startup, dynamic route on query create, route removed on query delete | Use `WebApplicationFactory`; verify route table |
| `RateLimiterTests` | Token bucket refill, 429 on exhaustion, per-tenant isolation, Redis fallback | Mock `ICacheService`; time-advancing token bucket |
| `ApiVersioningTests` | v1 headers present, deprecated version adds Deprecation/Sunset headers, unknown version 400 | `WebApplicationFactory` integration tests |
| `PublicEndpointConfigTests` | Anonymous access to public content type, PII masking in anonymous response | Mock `IContentApiSerializer` |

### Security Tests

| Test Class | Scenarios | OWASP Category |
|-----------|-----------|---------------|
| `CrossTenantIsolationTests` | JWT from TenantA rejected for TenantB REST endpoints; GraphQL tenant guard; Webhook cross-tenant delivery isolation | A01 Broken Access Control |
| `IdorPreventionTests` | GET /api/v1/content/{type}/{id} returns 404 (not 403) for cross-tenant IDs | A01 Broken Access Control |
| `SsrfWebhookTests` | Webhook targetUrl validation blocks localhost, 127.x, 10.x, 192.168.x, 169.254.x | A10 Server-Side Request Forgery |
| `SqlInjectionQueryApiTests` | Parameterized query API with injection strings in all parameters | A03 Injection |
| `GraphQLInjectionTests` | GraphQL input fields with SQL/script injection strings | A03 Injection |
| `AuthBypassTests` | All 20+ endpoints called without token (expect 401); all endpoints called with wrong-scope token (expect 403) | A07 Identification & Authentication Failures |
| `ApiKeySecurityTests` | Raw key not stored in DB; key hash only; revoked key within TTL window | A02 Cryptographic Failures |
| `WebhookSignatureTests` | Subscriber receives valid HMAC-SHA256; tampered payload fails verification; replay with old timestamp fails | A08 Software & Data Integrity Failures |
| `RateLimitBypassTests` | IP rotation attempt, X-Forwarded-For spoofing | A05 Security Misconfiguration |
| `GraphQLIntrospectionProductionTests` | Introspection blocked in production for non-Api.Manage users | A05 Security Misconfiguration |

### Integration Tests (Testcontainers)

| Test Class | Scope |
|-----------|-------|
| `RestApiIntegrationTests` | Full stack: REST CRUD with PostgreSQL + Redis; verify ETag round-trip, cache hit/miss |
| `GraphQLIntegrationTests` | Full stack: Hot Chocolate + real content types; filter, paginate, mutate |
| `WebhookDeliveryIntegrationTests` | Full stack: event fires → dispatch job → mock HTTPS receiver → delivery log |
| `AutoApiQueryIntegrationTests` | Full stack: create saved query → endpoint registered → call endpoint → results returned |
| `ApiVersioningIntegrationTests` | Full stack: v1 and v2 endpoints co-exist; deprecation headers verified |
| `MultiTenantApiIsolationTests` | Full stack: two tenant shells in same process; verify zero data bleed via REST and GraphQL |

## Kabul Kriterleri (Acceptance Criteria Summary)

### Spec Traceability

| Spec Item | US | DoR Status |
|-----------|-----|-----------|
| 4.1.11.1 REST API | US-1101, US-1108 | Ready |
| 4.1.11.2 GraphQL API | US-1102, US-1108 | Ready |
| 4.1.11.3 Headless CMS | US-1101, US-1102 | Ready |
| 4.1.11.4 API Authentication | US-1103, US-1108 | Ready |
| 4.1.11.5 Auto-API from Queries | US-1104 | Ready |
| 4.1.11.6 API Versioning | US-1105 | Ready |
| 4.1.11.7 Webhooks | US-1106 | Ready |
| 4.1.11.8 Abuse Prevention | US-1107 | Ready |

### Definition of Done Gates

- [ ] All 8 user stories pass DoR checklist (10/10 criteria each)
- [ ] Unit test coverage >= 80% for `ProjectDora.Modules.Integration`
- [ ] All security test classes pass (zero deviations from RBAC matrix)
- [ ] GraphQL depth (10) and complexity (500) limits enforced and tested at boundary values
- [ ] REST rate limiting (429) confirmed via integration test
- [ ] Webhook HMAC-SHA256 signature verified end-to-end by integration test
- [ ] Cross-tenant isolation tests pass (REST + GraphQL + webhooks)
- [ ] SSRF protection on webhook URL confirmed (localhost + RFC-1918 ranges blocked)
- [ ] SQL injection prevented in auto-query API (parameterized execution confirmed)
- [ ] API versioning headers (`Deprecation`, `Sunset`) verified for deprecated versions
- [ ] OpenAPI docs (`/swagger/v1/swagger.json`) generated and valid
- [ ] GraphQL schema available at `/graphql` with Voyager explorer (dev only)
- [ ] `integration` PostgreSQL schema migrations run clean on fresh database
- [ ] Auto-API query registration confirmed without application restart
- [ ] All stories traced to spec 4.1.11.x in code comments and PR descriptions

## Bagimlılık ve Risk Ozeti (Dependency & Risk Summary)

### Critical Dependencies from Prior Sprints

| Dependency | Sprint | Risk if Missing |
|-----------|--------|----------------|
| `IContentService` (CRUD + serialization) | S03 | REST and GraphQL cannot serve content — blocks US-1101, US-1102 |
| `IQueryService` (saved query listing + execution) | S04 | Auto-API generation impossible — blocks US-1104 |
| `IAuthService` (permission evaluation) | S05 | RBAC enforcement on all API endpoints — blocks US-1103, US-1108 |
| OpenID Connect OIDC token validation | S09 (US-1004) | JWT authentication fails at middleware — blocks all protected endpoints |
| `ICacheService` (Redis) | S09 (US-1002) | Rate limiting fallback to in-memory only; ETag caching degraded |

### Sprint Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Hot Chocolate dynamic schema complexity (content types change at runtime) | Medium | High | Use code-first type providers with `ITypeModule`; cache schema until content type changes |
| Webhook SSRF — partner URL pointing to internal services | Low | Critical | Strict URL validation at creation time; block RFC-1918 + loopback ranges; outbound `HttpClient` via separate named client with no internal routing |
| GraphQL N+1 queries for nested content relations | High | Medium | DataLoader pattern in all resolvers; `GreenDonut` batching; integration tests with query plan inspection |
| Redis unavailability causing rate limit bypass | Low | Medium | In-memory fallback token bucket; alert on Redis circuit breaker opening |
| API key raw value exposure in logs | Medium | High | Middleware strips Authorization header from structured logs; log only key ID prefix (first 8 chars); audit log stores key ID only |
| Auto-query route registration race condition (route registered before query validation) | Low | Low | Route registration checks query `IsActive` flag; inactive/invalid queries never registered |

See `docs/sprint-analyses/S10-integration/decisions.md` for full ADR-level decision rationale.
