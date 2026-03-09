# ProjectDora Test Strategy

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Test Pyramid

```
                    /\
                   /  \        Performance (3%)
                  /    \       — Load, stress, benchmark
                 /------\
                /        \     Security (5%)
               /          \    — OWASP, RBAC bypass, injection
           /------------------\
          /                    \  E2E (10%)
         /                      \ — Full user flows, Playwright
        /------------------------\
       /                          \ Integration (20%)
      /                            \— Module boundaries, DB, API
     /------------------------------\
    /                                \ Unit (60%)
   /                                  \— Domain logic, handlers, validators
  /------------------------------------\
```

| Layer | Percentage | Scope | Execution Time |
|-------|-----------|-------|----------------|
| Unit | 60% | Pure logic, MediatR handlers, validators | < 5 min |
| Integration | 20% | Module boundaries, DB queries, Orchard Core services | < 15 min |
| E2E | 10% | Full user flows, API contracts, multi-step workflows | < 30 min |
| Security | 5% | OWASP Top 10, RBAC bypass, injection, auth flows | < 20 min |
| Performance | 5% | Load testing, stress, benchmarks | < 60 min |

## 2. Coverage Targets

| Scope | Target | Measurement |
|-------|--------|-------------|
| Core (ProjectDora.Core) | 80% | Line coverage via Coverlet |
| Modules (ProjectDora.Modules.*) | 70% | Line coverage via Coverlet |
| Overall Solution | 75% | Aggregated Coverlet + ReportGenerator |

**Coverage exclusions:** Auto-generated code, Orchard Core internals, Liquid templates, migration files, `Program.cs` startup.

## 3. Test Environments

| Environment | Purpose | Infrastructure | Data |
|-------------|---------|---------------|------|
| `dev` | Local development | `docker-compose.dev.yml` — PostgreSQL, Redis, MinIO, Elasticsearch | Seed via Orchard Recipe |
| `test` | CI pipeline | `docker-compose.test.yml` — Testcontainers (PostgreSQL, Redis) | Golden dataset fixtures |
| `staging` | Pre-production | `docker-compose.prod.yml` + test data overlay | Anonymized production-like |
| `prod` | Production | `docker-compose.prod.yml` | Live data |

### Test Data Strategy

- **Unit tests**: In-memory fakes, Moq mocks — no external dependencies
- **Integration tests**: Testcontainers (PostgreSQL, Redis, Elasticsearch) with golden dataset seed
- **E2E tests**: Full `docker-compose.test.yml` stack with Playwright
- **Data reset**: `WebApplicationFactory` + `IStartupFilter` seeds per test class; full reset via `docker compose down -v && docker compose up -d`

## 4. Tooling

| Tool | Purpose | NuGet Package |
|------|---------|--------------|
| xUnit | Test framework | `xunit` |
| Moq | Mocking | `Moq` |
| FluentAssertions | Assertion library | `FluentAssertions` |
| Testcontainers | Container-based integration tests | `Testcontainers` |
| Coverlet | Code coverage | `coverlet.collector` |
| ReportGenerator | Coverage reports | `dotnet-reportgenerator-globaltool` |
| Playwright | E2E browser testing | `Microsoft.Playwright` |
| k6 | Load testing (HTTP) | CLI tool |
| NBomber | Load testing (.NET native) | `NBomber` |
| BenchmarkDotNet | Micro-benchmarks | `BenchmarkDotNet` |
| Verify | Snapshot testing | `Verify.Xunit` |

## 5. Naming Convention

```
[Module]_[Feature]_[Scenario]_[ExpectedResult]
```

**Examples:**

```csharp
// Unit test
ContentModeling_CreateContentType_WithValidFields_ReturnsSuccess()
ContentModeling_CreateContentType_WithDuplicateName_ThrowsConflictException()

// Integration test
QueryEngine_ExecuteLuceneQuery_WithTurkishCharacters_ReturnsMatchingResults()
AuditTrail_LogContentChange_WithVersionDiff_PersistsToAuditSchema()

// E2E test
AdminPanel_LoginFlow_WithValidCredentials_RedirectsToDashboard()
Workflow_PublishContent_WithApprovalChain_SendsNotification()

```

**File naming:** `{Module}{Feature}Tests.cs` → e.g., `ContentModelingCreateTests.cs`

**Project naming:** `ProjectDora.{Module}.Tests` → e.g., `ProjectDora.Core.Tests`, `ProjectDora.Modules.Tests`

## 6. TDD Policy

### Mandatory TDD (Test-First)

Write failing tests before implementation for:

- **Abstraction layer interfaces** — `IContentService`, `IQueryService`, `IWorkflowService`, `IAuthService`
- **MediatR command/query handlers** — All CQRS handlers
- **FluentValidation validators** — All input validation rules
- **Domain models** — Value objects, entities with business rules
- **Audit event emission** — Every auditable action must have a test proving the event fires

### Post-Implementation Testing (Test-After)

Acceptable to write tests after implementation for:

- Orchard Core configuration (recipes, content type definitions)
- UI layout and Liquid template rendering
- Docker Compose orchestration
- Third-party library wrappers (MinIO, Elasticsearch client)
- Migration scripts

### TDD Workflow

```
1. Read DoR YAML → Extract acceptance_tests
2. Generate xUnit test stubs (all [Fact] / [Theory] methods, all failing)
3. Run tests → confirm RED (all fail)
4. Implement minimum code to pass
5. Run tests → confirm GREEN
6. Refactor → confirm GREEN
7. Check coverage → meets target?
8. Commit
```

## 7. Sprint Test Focus

| Sprint | Focus Area | Test Types | Key Deliverables |
|--------|-----------|------------|------------------|
| S0 | Project setup | Infrastructure | Test project scaffolding, CI pipeline, Testcontainers config |
| S1-S2 | Core abstractions, Content Modeling | Unit | IContentService tests, validator tests, content type CRUD |
| S3-S4 | Content Management, Query Engine | Unit + Integration | Draft/publish lifecycle, Lucene/ES query tests |
| S5-S6 | RBAC, Workflows | Unit + Integration | Permission matrix tests, workflow trigger/action tests |
| S7-S8 | Multi-language, Audit | Integration | Localization tests, audit diff/rollback tests |
| S9-S10 | Multi-tenant, API/Integration | Integration + E2E | Tenant isolation tests, REST/GraphQL contract tests |
| S11 | Theme Management + cross-cutting | Unit + Integration | Liquid template tests, cross-cutting polish |
| S12 | Security, Performance | Security + Performance | OWASP scan, RBAC bypass tests, load tests (k6/NBomber) |
| S13 | UAT, Final validation | E2E + Manual | User acceptance flows, full regression, coverage gap analysis |

## 8. CI/CD Integration

### Pipeline Stages

```
commit → build → unit-tests → integration-tests → coverage-check → security-scan → deploy-test → e2e-tests → performance-tests → deploy-staging
```

### Quality Gates (CI)

| Gate | Condition | Blocks |
|------|-----------|--------|
| Build | Zero errors, zero warnings (treat warnings as errors) | All subsequent stages |
| Unit Tests | 100% pass rate | Integration tests |
| Integration Tests | 100% pass rate | Coverage check |
| Coverage | >= target per scope (see Section 2) | Security scan |
| Security Scan | No Critical/High findings | Deploy to test |
| E2E Tests | 100% pass rate | Performance tests |
| AI Golden Dataset | >= 90% pass rate | Deploy to staging |

### Reports

- Coverage: HTML report via ReportGenerator, uploaded as CI artifact
- Test results: TRX format, parsed by CI dashboard
- Security: SARIF format for GitHub/GitLab integration
- Performance: k6 JSON summary with p95/p99 latency thresholds

## 9. Cross-References

- **DoR Template**: [definition-of-ready.md](../ai-sdlc/definition-of-ready.md) — acceptance tests feed test generation
- **Golden Dataset**: [golden-dataset.md](golden-dataset.md) — fixture data for integration and AI tests
- **Test Cases**: [test-cases.md](test-cases.md) — full test case registry mapped to spec items
- **Governance**: [governance.md](../ai-sdlc/governance.md) — test architect agent role and quality gates
- **Architecture**: `docs/ProjectDora_Architecture_Blueprint.docx` — module boundaries inform test scope
- **Spec**: `docs/Teknik_Şartname.pdf` — traceability from spec items to test cases
