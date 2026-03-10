# ProjectDora — Data Orchestration & Rational Analytics

KOSGEB Veri Orkestrasyon ve Rasyonel Analitik Platformu.

## Project Overview

A data orchestration platform for KOSGEB (Turkish SME development agency). The platform provides content management, workflow automation, query management, reporting, and data orchestration capabilities for managing SME support programs.

**Scope**: 4.1.1 — 4.1.11 (Data Orchestration Platform). AI modules (4.1.12+) are **out of scope**.

## Architecture

- **Pattern**: Modular Monolith (ADR-001) — single .NET process, module boundaries via interfaces
- **Core Framework**: Orchard Core CMS 2.1.4 on .NET 10
- **Abstraction Layer**: IContentService, IQueryService, IWorkflowService, IAuthService — isolates Orchard Core dependency
- **API Style**: REST + GraphQL (Hot Chocolate), Headless CMS support
- **Design**: Clean Architecture + CQRS/MediatR

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 (C#) |
| CMS | Orchard Core |
| Database | PostgreSQL (primary), SQLite (small deployments) |
| Cache | Redis |
| Search | Elasticsearch (production), Lucene.NET (dev/small) |
| Object Storage | MinIO (S3-compatible) |
| Containers | Docker, Docker Compose |
| Auth | OpenID Connect (Orchard Core OpenId module) |
| Logging | Serilog |
| Audit | Audit.NET + custom AuditTrailPart |
| Observability | OpenTelemetry, Grafana, Prometheus |

## Database Schemas

- `orchard` — Orchard Core content, users, settings (YesSql document store)
- `audit` — Audit trail logs (custom EF Core)
- `analytics` — Reporting and analytics data

**Rules**: No cross-schema direct SQL. Services share data via APIs. Each schema owns its migrations.

## Key Modules (Spec 4.1.x)

- 4.1.1 Admin Panel (RBAC menus, media management)
- 4.1.2 Content Modeling (dynamic types, fields, rich text, aliases, galleries)
- 4.1.3 Content Management (CRUD, draft/publish, versioning, i18n, SEO URLs)
- 4.1.4 Theme Management (Liquid templates, Monaco editor)
- 4.1.5 Query Management (Lucene, Elasticsearch, SQL, parameterized queries)
- 4.1.6 User/Role/Permission (unlimited users/roles, content-type-level RBAC)
- 4.1.7 Workflow Engine (drag-drop designer, event triggers, custom activities)
- 4.1.8 Multi-language (CulturePicker, LocalizationPart, PO files)
- 4.1.9 Audit Logs (versioned diffs, retention, rollback)
- 4.1.10 Infrastructure (multi-tenant, Redis, recipes/deployment plans, OpenID, sitemap)
- 4.1.11 Integration (REST, GraphQL, headless, auto-API from queries)

## Development Guidelines

- **Language**: Code in English, UI/UX supports Turkish (primary) + multi-language
- **Sprints**: 2-week cycles, ~14 sprints total (28 weeks)
- **Testing**: xUnit for unit tests, integration tests per module
- **Documentation**: ADR (Architecture Decision Records) for key decisions, Markdown-first
- **Branching**: Feature branches, PR-based workflow
- **Traceability**: Every user story traces to one or more spec items (4.1.x.x)

## Commands

```bash
# Start all services
docker-compose up -d

# Run tests
dotnet test

# Build
dotnet build

# Database migrations
dotnet ef database update
```

## Project Structure (Planned)

```
ProjectDora/
├── docs/                          # Specification and architecture documents
│   └── sprint-analyses/           # Sprint analysis reports and DoR YAML files
├── src/
│   ├── ProjectDora.Web/           # Main Orchard Core web application
│   ├── ProjectDora.Core/          # Shared domain models, interfaces, abstractions
│   └── ProjectDora.Modules/       # Custom Orchard Core modules (one per sprint)
│       ├── ProjectDora.AdminPanel/
│       ├── ProjectDora.ContentModeling/
│       ├── ProjectDora.AuditTrail/
│       ├── ProjectDora.Workflows/
│       ├── ProjectDora.QueryEngine/
│       ├── ProjectDora.UserManagement/
│       ├── ProjectDora.Localization/
│       ├── ProjectDora.Infrastructure/
│       ├── ProjectDora.Integration/
│       └── ProjectDora.ThemeManagement/
├── tests/
│   ├── ProjectDora.Core.Tests/
│   └── ProjectDora.Modules.Tests/
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.dev.yml
│   └── docker-compose.prod.yml
├── CLAUDE.md
└── .gitignore
```

## Key Documents

### Specification & Architecture
- `docs/Teknik_Şartname.pdf` — Official technical specification (Turkish)
- `docs/ProjectDora_Architecture_Blueprint.docx` — Architecture blueprint
- `docs/ProjectDora_Gereksinim_ve_Gelistirme_Plani.docx` — Requirements & dev plan

### Design & Governance
- `docs/domain-model.md` — ER diagram, aggregates, state machines, database schema mapping
- `docs/module-boundaries.md` — Module interfaces, dependency matrix, forbidden access rules
- `docs/api-contract.yaml` — OpenAPI 3.0 specification for all REST endpoints
- `docs/data-governance.md` — KVKK/GDPR classification, retention, encryption, PII rules
- `docs/threat-model.md` — OWASP/STRIDE threat catalog with mitigations
- `docs/resilience-and-chaos-tests.md` — Failure scenarios and expected degraded behaviors
- `docs/migration-strategy.md` — Schema migration, content migration, search reindex
- `docs/runbook.md` — DevOps operational procedures
- `docs/release-management.md` — Release process, hotfix workflow, rollback procedures
- `docs/adr/index.md` — Architecture Decision Records index (ADR-001 through ADR-010)
- `docs/sprint-analyses/README.md` — Sprint analysis format and folder structure

### AI-SDLC (Claude Code Context)
- `.claude/ai-sdlc/definition-of-ready.md` — YAML story format for AI agent consumption
- `.claude/ai-sdlc/definition-of-done.md` — DoD criteria for stories, sprints, and releases
- `.claude/ai-sdlc/governance.md` — Agent pipeline, quality gates, prompt templates, parallel teams
- `.claude/ai-sdlc/sprint-roadmap.md` — Sprint plan (~14 sprints), spec mapping, dependencies
- `.claude/ai-sdlc/sprint-zero-guide.md` — Sprint 0 setup: project scaffold, Docker, CI, first test
- `.claude/testing/test-strategy.md` — Test pyramid, coverage targets, TDD policy
- `.claude/testing/test-cases.md` — ~249 test cases across all modules
- `.claude/testing/golden-dataset.md` — Fixture data: content, users, tenants, search data, workflows

### Skill Library (Agent Domain Knowledge)
- `.claude/skills/orchard-core.md` — YesSql, ContentParts, Recipes, IContentManager API
- `.claude/skills/cqrs-mediatr.md` — Command/Query handlers, pipeline behaviors, validators
- `.claude/skills/spec-analysis.md` — Spec decomposition, Turkish→English mapping, DoR generation
- `.claude/skills/search-engine.md` — Lucene, Elasticsearch, Turkish analyzer, SQL safety
- `.claude/skills/rbac-security.md` — Permission model, role matrix, OWASP test patterns
- `.claude/skills/workflow-engine.md` — Triggers, activities, custom activity template
- `.claude/skills/test-generation.md` — DoR→xUnit mapping, assertion patterns, test categories
- `.claude/skills/devops-docker.md` — Docker Compose, Testcontainers, CI pipeline, Dockerfile

## Important Notes

- All components must be open-source (MIT, Apache 2.0, etc.)
- Multi-tenant isolation is critical — each tenant gets isolated schema or separate DB
- Platform must run on both Windows and Linux
- Source code will be delivered to KOSGEB with full IP rights
