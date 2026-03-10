# Sprint S10 — Key Decisions

## D-001: REST + GraphQL dual API surface
**Karar**: Expose both REST (OpenAPI/Swagger) and GraphQL (Hot Chocolate) endpoints. REST for CRUD, GraphQL for flexible querying.
**Neden**: Spec 4.1.11 explicitly requires headless CMS with both REST and GraphQL. Hot Chocolate integrates well with Orchard Core's content system.
**Etki**: `IIntegrationService` covers API client management. Hot Chocolate schema auto-generated from registered content types via `IContentDefinitionManager`.

## D-002: API key authentication (not OAuth for machine clients)
**Karar**: Machine-to-machine API clients use API key bearer tokens managed by `ManageApiClients` permission. End-user OAuth via OIDC (S09).
**Neden**: Simpler than full OAuth client credentials for server-to-server integrations. API keys revokable via admin panel.
**Etki**: `ApiClientDto` has `ClientSecret` (hashed), `Scopes[]`, `IsActive`. `OrchardIntegrationService` generates and validates API keys.

## D-003: Published queries as API endpoints
**Karar**: Queries built in QueryEngine (S04) can be "published" as REST endpoints via Integration module. URL format: `GET /api/queries/{queryName}`.
**Neden**: Spec 4.1.11 requires auto-API generation from saved queries. Eliminates need to write custom controllers for common data access patterns.
**Etki**: `PublishQueryApiCommand` registers a query as a REST endpoint. Parameterized queries accept query string params.

## D-004: Webhook delivery with retry
**Karar**: Webhooks deliver via HTTP POST with 3 retry attempts (exponential backoff: 5s, 30s, 5min). Failed deliveries logged to audit trail.
**Neden**: Reliable event delivery is critical for integrations. Silent failures are worse than surfaced errors.
**Etki**: `WebhookDeliveryResultDto` tracks attempt count and last error. Retry handled by background `IHostedService`.
