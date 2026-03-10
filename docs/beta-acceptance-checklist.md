# Beta Acceptance Checklist — ProjectDora v1.0

**Version:** 1.0.0-beta
**Date:** 2026-03-10
**Owner:** Techsonamy Backend Team
**Client:** KOSGEB

---

## How to Use

Each item below must be verified before the release gate passes.
Mark status as: `[ ]` Not started | `[~]` In progress | `[x]` Done | `[!]` Blocked

---

## P0 — Build & Test Gate

| # | Criterion | Verification | Status |
|---|-----------|-------------|--------|
| P0-1 | `dotnet build ProjectDora.sln` completes with **0 errors, 0 warnings** | Run in CI | `[ ]` |
| P0-2 | `dotnet test ProjectDora.sln` reports **≥ 313 tests passing, 0 failures** | Run in CI | `[ ]` |
| P0-3 | All 10 modules have `[assembly: Module(...)]` manifest | `AllModules_HaveManifest_FeatureManagementVisible` test | `[ ]` |
| P0-4 | `GeneratedPermissionProvider` is registered and feeds OC auth chain | `GeneratedProvider_NoDuplicatesWithStaticPermissions` test | `[ ]` |
| P0-5 | `LuceneIndexRebuilderAdapter` is registered — `ReindexAsync()` does not throw | `ReindexAsync_LuceneRebuilderAvailable_ListsAndRebuildsAllIndexes` test | `[ ]` |

---

## P1 — Security & Authorization

| # | Criterion | Verification | Status |
|---|-----------|-------------|--------|
| S1-1 | `WebhooksController.Index` returns **403 Forbid** for users without `ViewWebhooks` permission | `WebhooksController_Index_WithoutPermission_ReturnsForbid` | `[ ]` |
| S1-2 | `ApiEndpointsController.Index` returns **403 Forbid** for users without `ViewApiClients` permission | `ApiEndpointsController_Index_WithoutPermission_ReturnsForbid` | `[ ]` |
| S1-3 | No two static permissions across all 10 modules share the same name | `AllModulePermissions_NoNamespaceCollisions_AcrossAllTenModules` | `[ ]` |
| S1-4 | Generated content-type permissions do not collide with static module permissions | `GeneratedPermissionNames_DoNotCollide_WithAnyStaticModulePermission` | `[ ]` |
| S1-5 | SQL injection patterns are rejected by `SqlSafetyValidator` | `SqlInjectionTests` suite | `[ ]` |
| S1-6 | All destructive operations are marked `isSecurityCritical: true` | `PermissionsTests` suite across all modules | `[ ]` |

---

## P2 — Functional Correctness

| # | Criterion | Verification | Status |
|---|-----------|-------------|--------|
| F2-1 | Workflow `TriggerAsync` returns numeric Workflow.Id when OC creates an execution record | `TriggerAsync_WhenOCReturnsExecutionContext_ReturnsRealDatabaseId` | `[ ]` |
| F2-2 | Workflow `TriggerAsync` returns `"trace:"` prefix (not a UUID) when no execution record created | `TriggerAsync_WhenNoExecutionContextReturned_ReturnsTraceMarker_NotFakeUuid` | `[ ]` |
| F2-3 | `GetExecutionAsync` with real numeric ID returns the workflow execution DTO | `TriggerAsync_EnabledWorkflow_ProducesQueryableExecutionId` | `[ ]` |
| F2-4 | Webhook delivery is logged after `TestWebhookAsync` call | `WebhookDeliveryLogEntry_SuccessfulDelivery_FieldsMapped` | `[ ]` |
| F2-5 | Webhook 5xx response triggers immediate 1x retry; `WasRetry=true` in log | `WebhookDeliveryLogEntry_RetryAttempt_WasRetryIsTrue` | `[ ]` |
| F2-6 | `GetDeliveryHistoryAsync` returns entries for a webhook | Manual: call `TestWebhookAsync`, then `GetDeliveryHistoryAsync` | `[ ]` |
| F2-7 | Delivery log capped at 10 entries per webhook | `IIntegrationService_MaxDeliveryLogEntries_IsTen` | `[ ]` |
| F2-8 | Cache `GetStatsAsync` with null Redis returns placeholder (no exception) | `GetStatsAsync_NullConnectionMultiplexer_ReturnsPlaceholderWithZeroHitMiss` | `[ ]` |
| F2-9 | Cache `GetStatsAsync` with Redis available returns parsed hit/miss ratio | `GetStatsAsync_WithRedisHitMiss_HitRatioIsCalculatedCorrectly` | `[ ]` |

---

## P3 — Infrastructure & Deployment

| # | Criterion | Verification | Status |
|---|-----------|-------------|--------|
| I3-1 | `docker-compose up -d` starts all services (PostgreSQL, Redis, Elasticsearch, MinIO) | Manual: check `docker ps` health | `[ ]` |
| I3-2 | OC setup wizard completes with `demo-setup` recipe | Manual: run setup, select demo-setup recipe | `[ ]` |
| I3-3 | Demo users (demo.editor, demo.analyst) created with correct role assignments | Manual: login with demo users | `[ ]` |
| I3-4 | `DestekProgrami` content type visible in Content Definition admin | Manual: navigate to Content → Content Definition | `[ ]` |
| I3-5 | Multi-tenant shell: create second tenant, verify isolation | Manual: Admin → Tenants → Add | `[ ]` |
| I3-6 | Redis cache is being used (not in-memory fallback) in production config | Manual: check `docker logs` for Redis connection | `[ ]` |

---

## P4 — Non-Functional

| # | Criterion | Verification | Status |
|---|-----------|-------------|--------|
| N4-1 | All dependencies have MIT/Apache 2.0 compatible licenses | `dotnet-license-cli` audit | `[ ]` |
| N4-2 | No `OrchardCore.*` package version drift (all pinned to 2.1.4) | `grep 2.1.4` across all `.csproj` files | `[ ]` |
| N4-3 | `TreatWarningsAsErrors=true` — CI build produces 0 warnings | CI build log | `[ ]` |
| N4-4 | Platform boots on both Windows and Linux | CI matrix: ubuntu-latest + windows-latest | `[ ]` |
| N4-5 | Serilog structured logs appear for all module startup operations | Manual: check log output on startup | `[ ]` |

---

## Release Gate Decision

| Condition | Decision |
|-----------|----------|
| All P0 + P1 + P2 checked | ✅ Release to KOSGEB staging |
| Any P0 or P1 blocked | 🚫 Do not release — fix required |
| P3/P4 items incomplete | ⚠️ Release with known limitations documented in release notes |

---

## Hotfix Protocol (Post-Release)

See `docs/release-management.md` §4 — Hotfix Workflow.

Critical severity (P0): patch within 24h, hotfix branch from `release/v1.0`, re-run full test suite.
High severity (P1): patch within 72h, same process.
