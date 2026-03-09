# Spec Compliance Checklist

> Version: 1.0 | Last Updated: 2026-03-09
> Run this checklist at the start of every sprint and before every release.

## Purpose

Ensures all technology choices match the Teknik Şartname (Technical Specification) requirements. Prevents version drift (e.g., .NET 10 instead of .NET 8) and unauthorized technology substitutions.

## Checklist

| # | Spec Ref | Requirement | Status | Verification Method |
|---|----------|-------------|--------|---------------------|
| 1 | 4.1.10.3 | .NET 8 runtime | | See §1 |
| 2 | 4.1.10.5 | PostgreSQL (open-source DB) | | See §2 |
| 3 | 4.1.10.6 | SQLite support (small deployments) | | See §3 |
| 4 | 4.1.10.7 | Redis cache | | See §4 |
| 5 | 4.1.10.8 | Multi-tenancy | | See §5 |
| 6 | 4.1.10.22 | OpenID Connect | | See §6 |
| 7 | 4.1.5.1 | Apache Lucene (search) | | See §7 |
| 8 | 4.1.5.3 | Elasticsearch (search) | | See §8 |
| 9 | 4.1.11.2 | REST + GraphQL API | | See §9 |
| 10 | 4.1.12.1b-d | Docker + Dockerfile + docker-compose | | See §10 |
| 11 | 4.1.12.1e | PostgreSQL DB provider | | See §11 |
| 12 | 4.1.12.1m | S3-compatible object storage (MinIO) | | See §12 |
| 13 | 4.1.10.4 | Windows + Linux cross-platform | | See §13 |
| 14 | General | All components open-source (MIT, Apache 2.0, etc.) | | See §14 |

---

## Verification Details

### §1 — .NET 8 Runtime

**Files to check:** `*.csproj`, `Directory.Build.props`, `global.json`, `docker/Dockerfile`, `.github/workflows/ci.yml`

```bash
# All TargetFramework values must be net8.0
grep -r "TargetFramework" --include="*.csproj" src/ tests/ | grep -v "net8.0" && echo "FAIL: Non-net8.0 target found" || echo "PASS"

# Directory.Build.props
grep "TargetFramework" Directory.Build.props | grep -q "net8.0" && echo "PASS" || echo "FAIL: Directory.Build.props not targeting net8.0"

# global.json SDK version must be 8.0.x
grep '"version"' global.json | grep -q '"8\.0\.' && echo "PASS" || echo "FAIL: global.json SDK not 8.0.x"

# Dockerfile base image must be 8.0
grep "FROM.*mcr.microsoft.com/dotnet" docker/Dockerfile | grep -v "8.0" && echo "FAIL: Dockerfile not using .NET 8.0" || echo "PASS"

# CI workflow must use 8.0.x
grep "dotnet-version" .github/workflows/ci.yml | grep -q "8.0" && echo "PASS" || echo "FAIL: CI not using .NET 8.0.x"
```

**Expected:** All checks output PASS.

### §2 — PostgreSQL

**Files to check:** `docker/docker-compose.yml`, `docker/docker-compose.dev.yml`

```bash
# docker-compose must use postgres image
grep -q "postgres:" docker/docker-compose.yml && echo "PASS" || echo "FAIL: No PostgreSQL in docker-compose"
```

### §3 — SQLite Support

**Files to check:** `*.csproj` (NuGet references), application configuration

```bash
# OrchardCore supports SQLite out of the box — verify no explicit exclusion
# This check becomes active once code exists
grep -r "UseSqlite\|OrchardCore.*Sqlite\|Microsoft.EntityFrameworkCore.Sqlite" --include="*.csproj" --include="*.cs" src/ && echo "PASS: SQLite references found" || echo "INFO: No SQLite references yet (OK if pre-implementation)"
```

### §4 — Redis Cache

**Files to check:** `docker/docker-compose.yml`, `*.csproj`, configuration files

```bash
# Redis service in docker-compose
grep -q "redis:" docker/docker-compose.yml && echo "PASS" || echo "FAIL: No Redis in docker-compose"

# Redis NuGet package (once code exists)
grep -r "OrchardCore.Redis\|StackExchange.Redis\|Microsoft.Extensions.Caching.StackExchangeRedis" --include="*.csproj" src/ && echo "PASS: Redis package found" || echo "INFO: No Redis package yet (OK if pre-implementation)"
```

### §5 — Multi-tenancy

**Files to check:** Orchard Core tenant configuration, custom tenant isolation code

```bash
# Orchard Core provides multi-tenancy by default
# Check for tenant-related configurations
grep -r "OrchardCore.*Tenant\|ITenantService\|ShellSettings\|tenant" --include="*.cs" --include="*.json" src/ && echo "PASS: Tenant references found" || echo "INFO: No tenant code yet (OK if pre-implementation)"
```

### §6 — OpenID Connect

**Files to check:** `*.csproj`, Orchard Core module references

```bash
# OpenId module reference
grep -r "OrchardCore.OpenId" --include="*.csproj" --include="*.json" src/ && echo "PASS: OpenId module found" || echo "INFO: No OpenId reference yet (OK if pre-implementation)"
```

### §7 — Apache Lucene

**Files to check:** `*.csproj`, Orchard Core module references

```bash
# Lucene module reference
grep -r "OrchardCore.Search.Lucene\|Lucene.Net" --include="*.csproj" --include="*.json" src/ && echo "PASS: Lucene found" || echo "INFO: No Lucene reference yet (OK if pre-implementation)"
```

### §8 — Elasticsearch

**Files to check:** `docker/docker-compose.yml`, `*.csproj`

```bash
# Elasticsearch in docker-compose
grep -q "elasticsearch:" docker/docker-compose.yml && echo "PASS" || echo "FAIL: No Elasticsearch in docker-compose"

# Elasticsearch NuGet package (once code exists)
grep -r "OrchardCore.Search.Elasticsearch\|Elasticsearch\|NEST" --include="*.csproj" src/ && echo "PASS: ES package found" || echo "INFO: No ES package yet (OK if pre-implementation)"
```

### §9 — REST + GraphQL API

**Files to check:** `*.csproj`, controller/endpoint files

```bash
# GraphQL (Hot Chocolate) reference
grep -r "HotChocolate\|OrchardCore.Apis.GraphQL" --include="*.csproj" src/ && echo "PASS: GraphQL found" || echo "INFO: No GraphQL reference yet (OK if pre-implementation)"

# REST controllers (once code exists)
grep -r "\[ApiController\]\|ControllerBase\|MapGet\|MapPost" --include="*.cs" src/ && echo "PASS: REST endpoints found" || echo "INFO: No REST endpoints yet (OK if pre-implementation)"
```

### §10 — Docker Infrastructure

**Files to check:** `docker/Dockerfile`, `docker/docker-compose.yml`

```bash
# Dockerfile exists and is valid
[ -f docker/Dockerfile ] && echo "PASS: Dockerfile exists" || echo "FAIL: No Dockerfile"

# docker-compose.yml exists
[ -f docker/docker-compose.yml ] && echo "PASS: docker-compose.yml exists" || echo "FAIL: No docker-compose.yml"

# docker-compose.dev.yml exists
[ -f docker/docker-compose.dev.yml ] && echo "PASS: docker-compose.dev.yml exists" || echo "FAIL: No docker-compose.dev.yml"
```

### §11 — PostgreSQL DB Provider

**Files to check:** `*.csproj`, connection string configuration

```bash
# Npgsql / PostgreSQL provider
grep -r "Npgsql\|UseNpgsql\|OrchardCore.*PostgreSQL" --include="*.csproj" --include="*.cs" --include="*.json" src/ && echo "PASS: PostgreSQL provider found" || echo "INFO: No PG provider yet (OK if pre-implementation)"
```

### §12 — S3 Object Storage (MinIO)

**Files to check:** `docker/docker-compose.yml`, `*.csproj`

```bash
# MinIO in docker-compose
grep -q "minio" docker/docker-compose.yml && echo "PASS" || echo "FAIL: No MinIO in docker-compose"

# S3/MinIO SDK (once code exists)
grep -r "AWSSDK.S3\|Minio\|Amazon.S3" --include="*.csproj" src/ && echo "PASS: S3 SDK found" || echo "INFO: No S3 SDK yet (OK if pre-implementation)"
```

### §13 — Windows + Linux Cross-Platform

**Files to check:** `*.csproj`, `Directory.Build.props`

```bash
# net8.0 (not net8.0-windows) ensures cross-platform
grep -r "TargetFramework.*net8.0-windows\|RuntimeIdentifier.*win" --include="*.csproj" src/ tests/ && echo "FAIL: Windows-specific target found" || echo "PASS: No platform-specific targets"
```

### §14 — Open-Source License Compliance

**Manual check + automated assistance:**

```bash
# List all NuGet packages (once code exists)
dotnet list src/ package --format json 2>/dev/null | head -50 || echo "INFO: No packages to check yet"

# Known compliant: OrchardCore (BSD), MediatR (Apache 2.0), FluentValidation (Apache 2.0),
# xUnit (Apache 2.0), Serilog (Apache 2.0), Hot Chocolate (MIT)
#
# Forbidden license types: GPL (without LGPL exception), AGPL, proprietary, SSPL
```

**Forbidden packages/licenses:**
- No GPL-only dependencies (LGPL with dynamic linking is acceptable)
- No AGPL dependencies
- No proprietary/commercial-only packages
- No SSPL (Server Side Public License) — e.g., MongoDB driver is fine (Apache 2.0), but MongoDB server >= 4.4 is SSPL

---

## Execution Instructions

### When to Run
1. **Sprint start** — Before any implementation begins
2. **Pre-release** — Before tagging a release
3. **After major dependency changes** — After adding new NuGet packages or Docker images

### How to Run
1. Execute each verification command in order
2. Record PASS/FAIL status in the checklist table above
3. Any FAIL must be resolved before sprint work begins
4. INFO items are acceptable for pre-implementation sprints

### Failure Response
- **FAIL on version**: Fix immediately. Update the offending file to match spec.
- **FAIL on missing infrastructure**: Add the missing component before sprint starts.
- **FAIL on license**: Remove the dependency and find an open-source alternative.

---

## Cross-References

- **Teknik Şartname**: `docs/Teknik_Şartname.pdf` — Source of truth for all requirements
- **Architecture Blueprint**: `docs/ProjectDora_Architecture_Blueprint.docx`
- **Governance**: [governance.md](governance.md) §5.7 — Architecture Guardian checks
- **Definition of Done**: [definition-of-done.md](definition-of-done.md) §2 — Sprint DoD includes spec compliance
