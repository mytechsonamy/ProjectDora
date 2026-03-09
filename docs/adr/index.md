# Architecture Decision Records — Index

> Last Updated: 2026-03-09

## Active ADRs

| ADR | Title | Status | Date | Summary |
|-----|-------|--------|------|---------|
| ADR-001 | Modular Monolith Architecture | **Accepted** | 2026-03-09 | Single .NET process with module boundaries via interfaces instead of microservices. Simpler deployment, shared transactions, Orchard Core compatibility. |
| ADR-002 | PostgreSQL as Primary Database | **Accepted** | 2026-03-09 | PostgreSQL for primary storage. SQLite as fallback for small/dev deployments. |
| ADR-003 | Elasticsearch for Production Search | **Accepted** | 2026-03-09 | Elasticsearch for production full-text search with Lucene.NET as fallback for development and small deployments. |
| ADR-004 | ~~Semantic Kernel for LLM Orchestration~~ | **Withdrawn** | 2026-03-09 | ~~Microsoft Semantic Kernel as the LLM orchestration layer.~~ Withdrawn — AI modules (4.1.12+) removed from scope. |
| ADR-005 | Orchard Core as CMS Foundation | **Accepted** | 2026-03-09 | Orchard Core CMS on .NET 8 as the foundation. Provides content modeling, theming, multi-tenancy, workflows out of the box. Abstraction layer isolates Orchard dependency. |
| ADR-006 | CQRS with MediatR | **Accepted** | 2026-03-09 | Command/Query Responsibility Segregation via MediatR library. Separates read and write paths, enables cross-cutting behaviors (validation, auth, audit logging) via pipeline behaviors. |
| ADR-007 | Three Database Schemas | **Accepted** | 2026-03-09 | Separate PostgreSQL schemas (orchard, audit, analytics) for domain isolation. No cross-schema direct SQL. Services communicate via Core interfaces. |
| ADR-008 | OpenID Connect Authentication | **Accepted** | 2026-03-09 | Orchard Core OpenId module for OAuth2/OIDC. JWT Bearer tokens for API auth. Short-lived access tokens (15 min) with refresh token rotation. |
| ADR-009 | ~~Local-Only AI Models~~ | **Withdrawn** | 2026-03-09 | ~~All AI/LLM models run locally via ONNX Runtime.~~ Withdrawn — AI modules (4.1.12+) removed from scope. |
| ADR-010 | Docker-Based Deployment | **Accepted** | 2026-03-09 | Docker Compose for all environments. Separate compose files for dev, test, and production. Testcontainers for integration tests. |

## ADR Template

When creating a new ADR, use this format:

```markdown
# ADR-{NNN}: {Title}

- **Status**: Proposed | Accepted | Deprecated | Superseded by ADR-{NNN}
- **Date**: YYYY-MM-DD
- **Deciders**: [Names/Roles]

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

### Positive
- ...

### Negative
- ...

### Neutral
- ...

## Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|-------------|
| ... | ... | ... | ... |
```

## Planned ADRs

| ADR | Title | Expected Sprint | Trigger |
|-----|-------|----------------|---------|
| ADR-011 | Caching Strategy (Redis patterns) | S03 | When implementing content caching |
| ADR-012 | Multi-Tenant Isolation Model | S09 | When implementing tenant provisioning |
| ADR-013 | ~~NL2SQL Safety Architecture~~ | — | ~~Withdrawn — AI modules removed from scope~~ |
| ADR-014 | ~~RAG Pipeline Design~~ | — | ~~Withdrawn — AI modules removed from scope~~ |
| ADR-015 | Audit Log Integrity (Hash Chain) | S08 | When implementing audit trail |

## Cross-References

- **Architecture Blueprint**: `docs/ProjectDora_Architecture_Blueprint.docx`
- **Module Boundaries**: [../module-boundaries.md](../module-boundaries.md) — modules informed by ADR-001
- **Domain Model**: [../domain-model.md](../domain-model.md) — data model informed by ADR-002, ADR-007
- **Test Strategy**: [../../.claude/testing/test-strategy.md](../../.claude/testing/test-strategy.md) — test tooling informed by ADR-010
