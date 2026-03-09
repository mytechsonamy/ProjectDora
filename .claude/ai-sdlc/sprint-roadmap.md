# Sprint Roadmap

> Version: 1.0 | Last Updated: 2026-03-09
> Scope: 4.1.1 — 4.1.11 (Data Orchestration Platform)

## 1. Overview

| Metric | Value |
|--------|-------|
| Total sprints | ~14 |
| Sprint duration | 2 weeks |
| Total duration | ~28 weeks |
| Scope | Spec 4.1.1 — 4.1.11 |

## 2. Sprint Plan

### S0 — Project Setup (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | — |
| **Focus** | Project scaffold, Docker, CI, first test |
| **Stories** | Infrastructure setup (no user stories) |
| **Expected Output** | Solution skeleton, Docker Compose running, CI green, health check endpoint |
| **Dependencies** | None |
| **Risk** | Low |
| **Guide** | [sprint-zero-guide.md](sprint-zero-guide.md) |

### S1 — Admin Panel (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.1 |
| **Focus** | Admin panel, RBAC menus, media management |
| **Stories** | US-101 — US-1xx |
| **Expected Output** | Admin login, menu structure, media upload/download |
| **Dependencies** | S0 complete |
| **Risk** | Low |

### S2 — Content Modeling (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.2 |
| **Focus** | Dynamic content types, fields, rich text, aliases, galleries |
| **Stories** | US-201 — US-2xx |
| **Expected Output** | ContentType CRUD, field definitions, ContentPart registration |
| **Dependencies** | S1 (admin panel for UI) |
| **Risk** | Medium — core foundation, must be solid |

### S3 — Content Management (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.3 |
| **Focus** | Content CRUD, draft/publish, versioning, i18n, SEO URLs |
| **Stories** | US-301 — US-3xx |
| **Expected Output** | Content create/edit/delete, draft→published lifecycle, version history |
| **Dependencies** | S2 (content types must exist) |
| **Risk** | Medium — complex state machine (Draft/Published/Archived) |

### S4 — Query Management (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.5 |
| **Focus** | Lucene, Elasticsearch, SQL queries, parameterized queries |
| **Stories** | US-501 — US-5xx |
| **Expected Output** | SavedQuery CRUD, Lucene/ES execution, SQL read-only execution |
| **Dependencies** | S3 (content to query against) |
| **Risk** | High — SQL injection prevention critical |

### S5 — User/Role/Permission (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.6 |
| **Focus** | Unlimited users/roles, content-type-level RBAC |
| **Stories** | US-601 — US-6xx |
| **Expected Output** | User CRUD, role management, permission assignment, RBAC enforcement |
| **Dependencies** | S1 (admin panel), S2 (content-type-level perms) |
| **Risk** | High — security critical, must be thoroughly tested |

### S6 — Workflow Engine (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.7 |
| **Focus** | Drag-drop designer, event triggers, custom activities |
| **Stories** | US-701 — US-7xx |
| **Expected Output** | Workflow definitions, event-based triggers, activity execution |
| **Dependencies** | S3 (content events trigger workflows), S5 (permission-based activities) |
| **Risk** | Medium — Orchard Core Workflows module provides foundation |

### S7 — Multi-language (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.8 |
| **Focus** | CulturePicker, LocalizationPart, PO files |
| **Stories** | US-801 — US-8xx |
| **Expected Output** | Content localization, culture switching, PO file management |
| **Dependencies** | S3 (content to localize) |
| **Risk** | Low — Orchard Core has built-in localization support |

### S8 — Audit Logs (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.9 |
| **Focus** | Versioned diffs, retention, rollback |
| **Stories** | US-901 — US-9xx |
| **Expected Output** | Audit log recording, diff viewer, retention cleanup, hash chain integrity |
| **Dependencies** | S3 (content changes to audit), S5 (user actions to audit) |
| **Risk** | Medium — hash chain integrity and append-only enforcement |

### S9 — Infrastructure (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.10 |
| **Focus** | Multi-tenant, Redis caching, recipes/deployment plans, OpenID, sitemap |
| **Stories** | US-1001 — US-10xx |
| **Expected Output** | Tenant provisioning, cache layer, recipe import/export, OpenID config |
| **Dependencies** | All previous sprints (cross-cutting infrastructure) |
| **Risk** | High — multi-tenant isolation is critical |

### S10 — Integration (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.11 |
| **Focus** | REST API, GraphQL, headless CMS, auto-API from queries |
| **Stories** | US-1101 — US-11xx |
| **Expected Output** | REST endpoints, GraphQL schema, headless content delivery, query-to-API |
| **Dependencies** | S4 (queries), S3 (content), S5 (auth for API) |
| **Risk** | Medium — GraphQL depth/complexity limiting needed |

### S11 — Theme Management + Cross-Cutting (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | 4.1.4 + cross-cutting |
| **Focus** | Liquid templates, Monaco editor, cross-module polish |
| **Stories** | US-401 — US-4xx + bug fixes |
| **Expected Output** | Theme editor, template preview, Liquid rendering, UX polish |
| **Dependencies** | S3 (content to render), S7 (localized templates) |
| **Risk** | Low — primarily UI/rendering work |

### S12 — Security Hardening & Performance (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | Cross-cutting |
| **Focus** | OWASP scan, performance testing, chaos tests, security hardening |
| **Stories** | Non-functional requirements |
| **Expected Output** | Security scan report, performance baseline, chaos test results |
| **Dependencies** | All functional sprints complete |
| **Risk** | Medium — may uncover issues requiring rework |

### S13 — UAT & Release Prep (2 weeks)

| Field | Detail |
|-------|--------|
| **Spec** | — |
| **Focus** | User acceptance testing, stabilization, release preparation |
| **Stories** | Bug fixes, documentation, release checklist |
| **Expected Output** | UAT sign-off, release notes, deployment package, user manual |
| **Dependencies** | S12 complete |
| **Risk** | Low — stabilization only |

## 3. Sprint Dependencies Graph

```
S0 ──→ S1 ──→ S2 ──→ S3 ──→ S4
                │      │      │
                │      ├──→ S6 (workflows need content events)
                │      ├──→ S7 (localization needs content)
                │      └──→ S8 (audit needs content changes)
                │
                └──→ S5 (RBAC needs content types)
                      │
                      └──→ S9 (infrastructure needs all)
                            │
                            └──→ S10 (integration needs all)
                                  │
                                  └──→ S11 (themes need content + i18n)
                                        │
                                        └──→ S12 (hardening)
                                              │
                                              └──→ S13 (UAT)
```

## 4. Parallel Work Opportunities

| Sprint | Parallel Tracks |
|--------|----------------|
| S4 + S5 | Query + RBAC can run in parallel (different teams) |
| S6 + S7 + S8 | Workflow + i18n + Audit can overlap (independent modules) |
| S9 + S10 | Infrastructure + Integration can overlap if S3-S5 are done |

## 5. Risk Summary

| Risk | Sprints | Mitigation |
|------|---------|-----------|
| Content modeling foundation wrong | S2-S3 | Extra review, spike in S0 |
| SQL injection in query engine | S4 | Security-first development, SQL parser |
| RBAC bypass | S5 | Comprehensive security tests, penetration testing |
| Multi-tenant data leak | S9 | Global query filters, dedicated test suite |
| Performance under load | S12 | Early performance testing, load test in S10 |

## 6. Cross-References

- **Sprint Zero Guide**: [sprint-zero-guide.md](sprint-zero-guide.md) — S0 detailed setup
- **Definition of Ready**: [definition-of-ready.md](definition-of-ready.md) — story input criteria
- **Definition of Done**: [definition-of-done.md](definition-of-done.md) — story/sprint completion criteria
- **Sprint Analyses**: [../../docs/sprint-analyses/README.md](../../docs/sprint-analyses/README.md) — per-sprint reports
- **Test Strategy**: [../testing/test-strategy.md](../testing/test-strategy.md) — test targets per sprint
