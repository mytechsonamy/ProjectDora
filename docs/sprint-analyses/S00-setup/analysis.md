# Sprint S00 — Project Setup & Infrastructure Bootstrap

## Kapsam (Scope)
- Spec items: (pre-spec) — foundation only
- Stories: US-001, US-002, US-003
- Cross-references: All subsequent sprints depend on S00 output

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements)

| # | Gereksinim | Aciklama |
|---|-----------|----------|
| S0-R1 | Solution structure | Modular monolith skeleton: ProjectDora.Web, ProjectDora.Core, ProjectDora.Modules/* |
| S0-R2 | Orchard Core bootstrap | OrchardCore.Application.Cms.Targets package, Startup.cs, appsettings.json |
| S0-R3 | Docker Compose | PostgreSQL 16, Redis, Elasticsearch, MinIO — all services locally runnable |
| S0-R4 | Database schemas | Init SQL for `orchard`, `audit`, `analytics` schemas |
| S0-R5 | CI pipeline | GitHub Actions: build + test on every push to main/feature/* |
| S0-R6 | Test framework | xUnit, FluentAssertions, Moq wired up; at least one health check test passing |
| S0-R7 | Abstraction layer stubs | IContentService, IQueryService, IWorkflowService, IAuthService interfaces in Core |
| S0-R8 | Sprint analysis structure | docs/sprint-analyses/ folder initialized with README.md template |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)
- No external sprint dependencies — this is the foundation
- .NET 10 SDK required (no .NET 8 installed on target machines)
- All packages must be open-source (MIT/Apache 2.0)
- OrchardCore 2.1.4 (latest stable as of project start)
- Docker Desktop on macOS (Apple Silicon) and Linux production server

## Teknik Kararlar (Technical Decisions)

| Karar | Tercih | ADR |
|-------|--------|-----|
| Architecture | Modular Monolith (single process, module isolation via interfaces) | ADR-001 |
| Framework | Orchard Core CMS 2.1.4 on .NET 10 | ADR-002 |
| Database | PostgreSQL 16 (primary), SQLite (small deployments) | ADR-003 |
| Test runner | xUnit + FluentAssertions (not MSTest or NUnit) | — |
| Directory.Build.props | TreatWarningsAsErrors=true, TargetFramework=net10.0 | — |

### Proje Yapisi (Project Structure Created)
```
ProjectDora/
├── src/
│   ├── ProjectDora.Web/               # Orchard Core host
│   ├── ProjectDora.Core/              # Abstractions, DTOs, shared models
│   └── ProjectDora.Modules/           # Custom OC modules (one per sprint)
├── tests/
│   ├── ProjectDora.Core.Tests/
│   └── ProjectDora.Modules.Tests/
├── docker/
│   └── docker-compose.yml
├── docs/
│   └── sprint-analyses/
├── Directory.Build.props
├── global.json                        # SDK: 10.0.100, rollForward: latestFeature
├── CLAUDE.md
└── .gitignore
```

### Docker Compose Services
```yaml
services:
  postgres:
    image: postgres:16           # No PGVector — standard PostgreSQL
  redis:
    image: redis:7-alpine
  elasticsearch:
    image: elasticsearch:8.11.0
  minio:
    image: minio/minio
```

### Init SQL Schemas
```sql
CREATE SCHEMA IF NOT EXISTS orchard;    -- Orchard Core YesSql content store
CREATE SCHEMA IF NOT EXISTS audit;      -- Custom EF Core audit trail
CREATE SCHEMA IF NOT EXISTS analytics;  -- Reporting & analytics data
-- Note: No 'ai' schema — AI modules out of scope
```

## Test Plani (Test Plan)
- US-001: Solution builds successfully (0 errors, 0 warnings)
- US-002: All Docker services start and health checks pass
- US-003: First xUnit test passes (health check endpoint returns 200)

## Sprint Sonucu (Sprint Outcome)
- **Tamamlanan**: All S0 stories — foundation complete
- **Test sayisi**: 3 baseline tests (solution build, Docker health, endpoint)
- **Sonraki sprint**: S01 (AdminPanel) can begin immediately

## Dokümantasyon Notlari
> Kullanim kilavuzu ve teknik dokumantasyon icin:
- Platform requires Docker Desktop for local development
- Use `dotnet test tests/ProjectDora.Modules.Tests/` to run module tests
- `Directory.Build.props` at root sets `TreatWarningsAsErrors=true` — zero warnings policy
- `global.json` pins SDK `10.0.100` with `latestFeature` rollForward
