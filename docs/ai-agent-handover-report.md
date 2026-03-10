# ProjectDora AI Agent Handover Report

> Version: 2.0  
> Updated: 2026-03-10  
> Status: Revised after implementation wave

## 1. Executive Summary

ProjectDora is a modular Orchard Core-based enterprise backoffice platform. It combines content modeling, content management, query execution, user and role administration, workflows, localization, audit trail, infrastructure and tenant operations, integrations, and theme management under one host application.

Compared with the earlier assessment, the codebase has moved forward meaningfully. Several areas that were previously DTO-only or placeholder-level now execute real Orchard-backed behavior. The project should no longer be described as an internal alpha. The more accurate assessment now is:

**Current maturity: strong MVP / pre-beta**

This does not mean release-ready. It means the repository now has enough real implementation depth that the remaining work is concentrated in build health, dependency hygiene, several operational gaps, and product hardening rather than broad foundational scaffolding.

The current top-line conclusion for AI agents is:

1. Treat the product as real but not fully hardened.
2. Prioritize dependency and solution-level test health first.
3. Focus next on operational gaps, not broad re-architecture.
4. Use tests and module boundaries as the control plane for all further work.

## 2. What Changed Since The Previous Review

The earlier review flagged QueryEngine, UserManagement listing, Workflows execution visibility, Localization status handling, AuditTrail, Infrastructure tenancy, Integration, and ThemeManagement as incomplete or mostly skeletal.

That is no longer accurate in the same way.

The repository now shows real implementation progress in these areas:

- QueryEngine can execute ad hoc Lucene and SQL queries through Orchard query infrastructure.
- UserManagement now has real in-memory/queryable listing behavior over Orchard users.
- Workflows now support execution retrieval and execution listing.
- Localization now updates default culture settings and resolves translation status from content localization state.
- AuditTrail now persists audit events and reads them back through YesSql queries.
- Tenant operations now manipulate Orchard shell settings rather than returning DTO-only placeholders.
- Integration now persists API clients, webhooks, and published query endpoint settings via site settings and can actively test webhook delivery.
- ThemeManagement now reads the active theme and manages template customization records.

This implementation wave materially changes the product assessment.

## 3. Product Assessment

### 3.1 Product Strengths

- Clear enterprise CMS/backoffice product direction.
- Strong modular structure with a shared core abstraction layer.
- Broad business capability coverage across the main operational modules.
- Orchard Core remains a sensible platform choice for the target problem.
- Test coverage has expanded and module-level verification is now substantially better.
- The repository is now closer to product completion work than platform bootstrapping.

### 3.2 Product Weaknesses

- Solution-wide test health is still not clean.
- Dependency and package-audit friction is still the main engineering blocker.
- Some “real implementations” are still light or partial rather than production-deep.
- Certain operational behaviors still degrade quietly instead of failing explicitly.
- Some features now persist state but still need stronger lifecycle guarantees, telemetry, and negative-path handling.

### 3.3 Maturity Verdict

Current maturity: **Strong MVP / pre-beta**

Reasoning:

- Major feature areas now do real work.
- Module tests pass in significant volume.
- The remaining work is concentrated and actionable.
- Full release readiness is still blocked by dependency issues and a small number of functional hardening gaps.

## 4. Technology and Runtime Findings

### 4.1 .NET Version

The repository is still targeting **.NET 10 (`net10.0`)**.

Confirmed from code/build artifacts:

- `Directory.Build.props`
- `src/ProjectDora.Web/ProjectDora.Web.csproj`
- test project files
- `global.json`
- `docker/Dockerfile`

### 4.2 Runtime Stack

- ASP.NET Core host
- Orchard Core CMS integration
- Modular class-library solution structure
- xUnit, FluentAssertions, Moq
- Docker packaging path present

## 5. Current Delivery Status by Area

### 5.1 Host Application

Status: **Good baseline**

- Orchard host wiring is straightforward and serviceable.
- Health endpoints exist.
- No major reassessment needed here.

### 5.2 Core Abstractions

Status: **Good**

- Core remains the right contract layer for AI-agent-driven implementation.
- No evidence of major boundary collapse in the latest wave.

### 5.3 Content Modeling

Status: **Good**

- Still one of the stronger modules.
- Appears suitable as a stable dependency for related work.

### 5.4 Content Management

Status: **Moderate to good**

- Not the main concern in the latest reassessment.
- Should still receive targeted verification during UAT and audit-related follow-up work.

### 5.5 Query Engine

Status: **Improved but not fully hardened**

- Ad hoc Lucene and SQL execution now exist.
- Saved-query lifecycle remains present.
- SQL validation still guards execution.

Remaining issues:

- If the relevant Orchard query module is absent, execution returns an empty result instead of a strong failure.
- `ReindexAsync` is still a no-op.
- Tenant scoping and production-grade execution semantics still need hardening review.

### 5.6 User / Role / Auth

Status: **Improved**

- User listing is no longer stubbed.
- Basic lifecycle operations remain wired to Orchard identity.

Remaining issues:

- Listing strategy may still need stronger persistence/index-backed behavior for scale and correctness.
- Role and permission depth should still be re-verified, especially for tenant-sensitive paths.

### 5.7 Workflows

Status: **Improved**

- Execution retrieval and listing now exist.
- This is a material step up from the previous review.

Remaining issues:

- Execution DTO richness, traceability, and failure semantics still look relatively light.

### 5.8 Localization

Status: **Improved**

- Default culture mutation now performs real site settings updates.
- Translation status is now derived from content localization state.

Remaining issues:

- Lifecycle depth, completeness of translation metadata, and larger-scale UX/ops behavior still need validation.

### 5.9 Audit Trail

Status: **Improved substantially**

- Audit events are persisted and queried through YesSql.
- Settings are persisted.
- Purge, history lookup, and rollback delegation now exist.

Remaining issues:

- Diff behavior is still shallow and currently centered on a narrow set of fields.
- Production-grade audit completeness should still be validated before any compliance-sensitive claim.

### 5.10 Infrastructure / Tenant / Recipe / Cache

Status: **Improved, but uneven**

- Tenant lifecycle behavior now uses shell settings.
- This is materially better than DTO-only behavior.

Remaining issues:

- Tenant creation appears closer to settings registration than full tenant provisioning/setup.
- Recipe and cache flows still deserve a focused follow-up review.

### 5.11 Integration

Status: **Improved substantially**

- API clients, webhooks, and published query endpoint settings are now persisted through site settings.
- Webhook test execution performs a real HTTP call path.

Remaining issues:

- This is still closer to administrative integration management than a full production integration platform.
- Delivery history, retry semantics, and runtime publication depth should still be strengthened.

### 5.12 Theme Management

Status: **Improved**

- Active theme reading exists.
- Activation and template customization operations now perform real work against Orchard services.

Remaining issues:

- Theme lifecycle validation and operational safety should still be tested more deeply.

## 6. Engineering Health Findings

### 6.1 Build and Solution Test Health

Status: **Partially healthy, still blocked at solution level**

Observed current state:

- Module-level test execution is now healthy.
- `ProjectDora.Modules.Tests` passes with **296 passing tests**.
- Solution-wide `dotnet test ProjectDora.sln` still fails because `ProjectDora.Core.Tests` is blocked by package-audit and package-version issues.

Current blockers observed during verification:

- Package vulnerability warnings are treated as errors.
- Vulnerable packages still include:
  - `HtmlSanitizer` `8.2.871-beta`
  - `Microsoft.Identity.Abstractions` `7.1.0`
  - `Microsoft.Identity.Web` `3.3.1`
  - `MimeKit` `4.8.0`
  - `SixLabors.ImageSharp` `3.1.5`
- There is still a downgrade conflict involving `Microsoft.Extensions.Caching.Memory`.
- `tests/ProjectDora.Core.Tests/ProjectDora.Core.Tests.csproj` still references `Microsoft.AspNetCore.Mvc.Testing` `8.0.13` while the solution targets `net10.0`.

Interpretation:

- This is no longer a “project cannot be trusted at all” situation.
- It is now a narrower but still important build/dependency problem.
- The engineering bottleneck has shifted from implementation breadth to dependency hygiene and final hardening.

### 6.2 Test Posture

Status: **Good and improving**

- Module test coverage appears materially better than before.
- The test suite now provides real confidence for a large portion of the modules.
- The missing piece is solution-wide clean execution, not absence of tests.

## 7. Release Readiness Assessment

### 7.1 Ready Now

- Architecture exploration
- Module boundary enforcement
- Feature-completion work inside most existing modules
- Internal demos
- Targeted module hardening
- UAT preparation work

### 7.2 Not Ready Now

- Full production release
- Compliance-sensitive audit guarantees
- Final release signoff
- Clean solution-level CI confidence

## 8. Revised Priority Action Plan

This is the updated execution order for AI agents after the implementation wave.

### P0: Solution Build and Dependency Stabilization

1. Fix `ProjectDora.Core.Tests` package compatibility and audit failures.
2. Resolve the `Microsoft.Extensions.Caching.Memory` downgrade.
3. Align test and host dependency versions with `net10.0`.
4. Get `dotnet test ProjectDora.sln` green.

Definition of done:

- Solution-wide restore and test pass.
- No blocking vulnerability-as-error failures remain in the normal developer workflow.

### P1: Operational Hardening of Newly Implemented Modules

1. Make QueryEngine fail explicitly when required Orchard runtime features are unavailable.
2. Implement `ReindexAsync`.
3. Deepen audit diff behavior beyond the current narrow field set.
4. Harden tenant lifecycle from shell-settings management toward reliable tenant provisioning semantics.
5. Strengthen webhook delivery tracking, persistence, and delivery-state handling.
6. Validate theme activation/template operations with stronger runtime tests.

Definition of done:

- Core operational flows behave predictably in both success and failure cases.
- Newly implemented modules no longer rely on “best effort” behavior in critical paths.

### P2: Security, Authorization, and Tenant Isolation Review

1. Re-validate SQL execution safety around all query entry points.
2. Verify tenant scoping in query, audit, integration, and identity flows.
3. Review authorization boundaries across admin operations.
4. Review secrets and sensitive configuration handling.

### P3: Product Completion and UX/Administration Refinement

1. Validate admin menus, permissions, and actual runtime actions end to end.
2. Improve validation and user-facing failure handling.
3. Close remaining consistency gaps between service behavior and admin expectations.

### P4: UAT, Smoke Testing, and Release Preparation

1. Expand end-to-end business scenarios.
2. Add seeded test/demo data flows.
3. Add smoke tests for startup, key journeys, and release packaging.
4. Prepare a release candidate checklist.

## 9. Revised AI-Agent Backlog

This backlog reflects the current state rather than the older “fill every skeleton module” assumption.

### 9.1 Foundation and Build

- Fix solution-level dependency conflicts.
- Upgrade or realign test package versions for `net10.0`.
- Clean the package-audit failure path.
- Add CI checks for restore, build, test, and package audit.

### 9.2 Query Engine

- Replace silent empty fallback behavior with explicit capability-aware handling.
- Implement `ReindexAsync`.
- Review SQL execution for tenant scoping and operational safety.
- Add runtime integration tests around missing-module and enabled-module behavior.

### 9.3 User / Role / Auth

- Verify correctness and scale behavior of user listing.
- Recheck role and permission lifecycle completeness.
- Add more negative-path and tenant-sensitive tests.

### 9.4 Workflows

- Improve execution metadata and traceability.
- Add deeper workflow-history and fault-path tests.

### 9.5 Localization

- Add richer translation-state tests and edge-case handling.
- Verify behavior against multiple localized content sets.

### 9.6 Audit Trail

- Extend diff fidelity.
- Add stronger rollback safety tests.
- Validate retention and purge behavior under realistic data volume.

### 9.7 Infrastructure

- Clarify whether tenant creation must also perform setup/provisioning.
- Harden tenant lifecycle flows accordingly.
- Review recipe and cache services in the same pass.

### 9.8 Integration

- Persist delivery results and delivery timestamps more completely.
- Add retry/error-handling strategy where required.
- Verify published-query API lifecycle against real runtime exposure needs.

### 9.9 Theme Management

- Add stronger tests around activation, active-theme reporting, and template reset semantics.
- Validate safety and correctness under concurrent or repeated updates.

### 9.10 QA and UAT

- Convert current module confidence into end-to-end product confidence.
- Add UAT coverage for content, identity, queries, workflows, localization, audit, tenancy, integration, and theme changes.

## 10. Recommended AI-Agent Work Packages

### Agent 1: Build and Dependency Stabilization Agent

Mission:

- Make the whole solution restore and test cleanly on `net10.0`.

Primary outputs:

- Dependency fixes
- Package-audit resolution strategy
- Green solution test run

### Agent 2: Query and Runtime Safety Agent

Mission:

- Harden QueryEngine from “implemented” to “operationally safe”.

Primary outputs:

- Reindex implementation
- Explicit failure semantics
- Tenant-aware execution review
- Query runtime tests

### Agent 3: Identity and Tenant Hardening Agent

Mission:

- Validate and harden user, role, auth, and tenant lifecycle behavior.

Primary outputs:

- Correctness and scale improvements
- Tenant lifecycle hardening
- Additional tests

### Agent 4: Audit and Workflow Hardening Agent

Mission:

- Deepen audit fidelity and workflow execution observability.

Primary outputs:

- Better diffs
- Better execution history semantics
- Hardening tests

### Agent 5: Integration and Theme Hardening Agent

Mission:

- Turn newly implemented integration and theme behaviors into production-safe features.

Primary outputs:

- Better delivery tracking
- Better lifecycle guarantees
- Runtime verification tests

### Agent 6: QA, UAT, and Release Agent

Mission:

- Translate module-level confidence into release-level confidence.

Primary outputs:

- UAT checklist
- Smoke tests
- Release-readiness checklist
- Risk register

## 11. Execution Rules For Future AI Agents

1. Start with solution-level build health before broad new feature work.
2. Treat implemented modules as real systems that need hardening, not wholesale rewrites.
3. Do not reintroduce placeholder logic where real behavior now exists.
4. Preserve module boundaries through `ProjectDora.Core`.
5. Pair functional changes with tests.
6. Be explicit about runtime assumptions involving Orchard modules and shell state.
7. Do not declare production readiness until solution-wide tests and dependency hygiene are clean.

## 12. Updated Done Criteria For The Whole Project

The project can be considered **beta-ready** only when all of the following are true:

- `dotnet test ProjectDora.sln` succeeds cleanly.
- No critical or high dependency vulnerabilities remain in the intended shipped set.
- Query runtime behavior is explicit and hardened.
- Audit behavior is sufficiently deep for trustworthy operational use.
- Tenant lifecycle behavior matches the intended product promise.
- Integration and theme operations are tested beyond happy-path persistence.
- End-to-end product journeys are covered by smoke/UAT scenarios.

## 13. Bottom Line

ProjectDora is no longer best described as an incomplete skeleton. It now has real implementation momentum and enough module depth to be taken seriously as a strong MVP. The strategic question has changed. The repository no longer primarily needs breadth; it needs stabilization, hardening, and release discipline.

That makes it an even better candidate for AI-agent execution than before, provided the next wave is tightly focused on solution health, operational correctness, and product-grade verification.
