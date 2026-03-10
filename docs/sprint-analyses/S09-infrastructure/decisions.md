# Sprint S09 — Key Decisions

## D-001: Multi-tenant isolation via Orchard Core shell/tenant system
**Karar**: Use Orchard Core's built-in Shell Tenant system for multi-tenant isolation, not a custom tenant router.
**Neden**: OC 2.x ships a production-grade tenant shell system (separate DI containers, separate DB connections per tenant). Reimplementing would introduce risk and duplicate effort.
**Etki**: Tenant creation = provisioning a new OC Shell with its own PostgreSQL schema prefix. `ITenantService` wraps `IShellHost` and `IShellSettingsManager`.

## D-002: Redis TTL-based cache invalidation (no pub/sub)
**Karar**: Cache invalidation uses TTL expiry + explicit delete-on-write. No Redis pub/sub for distributed invalidation in this sprint.
**Neden**: Simpler implementation; current deployment is single-node. Pub/sub can be added in S12 hardening if needed.
**Etki**: `ICacheService` exposes `InvalidateAsync(key)` which calls `IDistributedCache.RemoveAsync()`.

## D-003: NuGetAuditMode=direct for OpenID packages
**Karar**: OrchardCore.OpenId NOT added to Integration module; only added to Web project host where actually needed.
**Neden**: Adding `OrchardCore.OpenId` to non-host modules triggers NU1605/NU1902/NU1903 due to transitive `Microsoft.Identity.Web` downgrade.
**Etki**: Web project explicitly pins `Microsoft.AspNetCore.Authentication.OpenIdConnect 9.0.0`. Stubs in module projects don't reference OC.OpenId.

## D-004: Elasticsearch optional (Lucene fallback)
**Karar**: Search is dual-mode: Elasticsearch in production, Lucene.NET in dev/small deployments. Configuration-driven via `appsettings.json`.
**Neden**: Spec 4.1.10.9 explicitly requires dual search engine support. Abstraction via `ISearchService` enables zero-code-change switch.
**Etki**: Docker Compose dev profile uses Elasticsearch. Lucene index stored in `/App_Data/Sites/{tenant}/Indexes/`.
