# Sprint S00 — Key Decisions

## D-001: Target Framework = net10.0 (Not net8.0)
**Karar**: Use .NET 10 throughout, not .NET 8 as originally planned.
**Neden**: Only .NET 10.0.102 SDK installed on development machines. OrchardCore 2.1.4 packages are compatible with net10.0 (NuGet resolves lower TFM).
**Etki**: All `.csproj` files use `<TargetFramework>net10.0</TargetFramework>`, set via `Directory.Build.props`.

## D-002: TreatWarningsAsErrors = true
**Karar**: Zero-warning policy enforced at build level.
**Neden**: Prevents warning debt accumulation; forces clean code from the start.
**Etki**: CA1305, CA1822, CA1826, CA1861, CA2012 analyzer rules must all be satisfied. No `#pragma warning disable` without justification.

## D-003: No PGVector in Docker Compose
**Karar**: Use `postgres:16` (standard), NOT `pgvector/pgvector:pg16`.
**Neden**: AI modules (4.1.12+) removed from project scope. No embedding or vector search required.
**Etki**: Docker Compose uses standard PostgreSQL image. No `ai` schema created.

## D-004: OrchardCore 2.1.4 (not 3.x)
**Karar**: Pin to OrchardCore 2.1.4 for the entire project.
**Neden**: Latest stable release at project start. 3.x not yet stable.
**Etki**: Some OC APIs differ from documentation examples (e.g., `GetExtensions()` sync, no `LoadExtensionsAsync`; `ILocalizationService` limited mutate API).

## D-005: NuGetAuditMode = direct
**Karar**: Set `<NuGetAuditMode>direct</NuGetAuditMode>` in Web project.
**Neden**: OrchardCore 2.1.4 has transitive dependency downgrades that trigger NU1605/NU1902/NU1903 warnings. Audit only direct dependencies to suppress noise.
**Etki**: Pin `Microsoft.AspNetCore.Authentication.OpenIdConnect 9.0.0` explicitly to resolve transitive downgrade from Microsoft.Identity.Web.
