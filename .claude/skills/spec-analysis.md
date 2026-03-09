# Skill: Specification Analysis

> Target agents: BA, Architect

## 1. Purpose

Parse Turkish technical specification items (Teknik Şartname 4.1.x.x) into structured DoR YAML stories that AI agents can consume.

## 2. Input Sources

| Source | Path | Language | Use |
|--------|------|----------|-----|
| Technical Specification | `docs/Teknik_Şartname.pdf` | Turkish | Primary requirements |
| Architecture Blueprint | `docs/ProjectDora_Architecture_Blueprint.docx` | Turkish | Module structure, interfaces |
| Requirements Plan | `docs/ProjectDora_Gereksinim_ve_Gelistirme_Plani.docx` | Turkish | Sprint mapping, priorities |
| Domain Model | `docs/domain-model.md` | English | Entities, relationships, events |
| Module Boundaries | `docs/module-boundaries.md` | English | Interface contracts, forbidden deps |
| API Contract | `docs/api-contract.yaml` | English | Endpoint definitions |

## 3. Spec Item Decomposition Process

```
Spec Item (4.1.x.x)
    │
    ├─ 1. Identify actors (who)
    │     Map to RBAC roles: SuperAdmin, TenantAdmin, Editor, Author, Analyst, etc.
    │
    ├─ 2. Identify actions (what)
    │     Map to CRUD + domain operations: Create, Read, Update, Delete, Publish, etc.
    │
    ├─ 3. Identify entities (on what)
    │     Map to domain model: ContentItem, ContentType, User, Role, etc.
    │
    ├─ 4. Identify constraints
    │     ├─ RBAC: required permissions, denied roles
    │     ├─ Data: validation rules, field types, max lengths
    │     ├─ API: endpoint, methods, response codes
    │     └─ Business: multi-tenant, audit, localization
    │
    ├─ 5. Extract acceptance criteria (Given/When/Then)
    │     Minimum 3 per story
    │     Include: happy path, auth failure, validation failure
    │
    ├─ 6. Identify edge cases
    │     Minimum 2 per story
    │     Include: Turkish chars, max length, concurrent access, empty tenant
    │
    └─ 7. Map to tech stack
          ├─ Abstraction interface (IContentService, IQueryService, etc.)
          ├─ MediatR commands/queries
          ├─ Audit events
          └─ Localization keys
```

## 4. Spec Item → Story ID Mapping

| Spec Range | Module | Story ID Prefix | Sprint Range |
|-----------|--------|-----------------|-------------|
| 4.1.1.x | Admin Panel | US-1xx | S01-S02 |
| 4.1.2.x | Content Modeling | US-2xx | S02-S03 |
| 4.1.3.x | Content Management | US-3xx | S03-S04 |
| 4.1.4.x | Theme Management | US-4xx | S04 |
| 4.1.5.x | Query Management | US-5xx | S04-S05 |
| 4.1.6.x | User/Role/Permission | US-6xx | S05-S06 |
| 4.1.7.x | Workflow Engine | US-7xx | S06-S07 |
| 4.1.8.x | Multi-language | US-8xx | S07-S08 |
| 4.1.9.x | Audit Logs | US-9xx | S07-S08 |
| 4.1.10.x | Infrastructure | US-10xx | S09-S10 |
| 4.1.11.x | Integration (API) | US-11xx | S10 |

## 5. Priority Assignment Rules

| Condition | Priority | Example |
|-----------|----------|---------|
| Security (auth, RBAC, injection) | P0 | Permission check, tenant isolation |
| Core CRUD operations | P1 | Content create, user create |
| Supporting features | P2 | SEO URL, pagination, sorting |
| Cosmetic / nice-to-have | P3 | Tooltip, breadcrumb |

## 6. Turkish → English Translation Guide

When extracting from Turkish spec, use these term mappings:

| Turkish Term | English Equivalent | Domain Concept |
|-------------|-------------------|----------------|
| İçerik | Content | ContentItem |
| İçerik tipi | Content type | ContentType |
| Alan | Field | ContentField |
| Parça | Part | ContentPart |
| Yetki | Permission | Permission |
| Rol | Role | Role |
| Kullanıcı | User | User |
| İş akışı | Workflow | WorkflowDef |
| Sorgu | Query | SavedQuery |
| Denetim izi | Audit trail | AuditLog |
| Taslak | Draft | ContentStatus.Draft |
| Yayınla | Publish | ContentStatus.Published |
| Arşivle | Archive | ContentStatus.Archived |
| Sürüm | Version | ContentVersion |
| Kiracı | Tenant | Tenant |
| Destek programı | Support program | DestekProgrami (content type) |
| KOBİ | SME | Small/Medium Enterprise |
| Başvuru | Application | Application record |

## 7. Validation Rule Extraction

When spec says... → Generate this FluentValidation rule:

| Spec Phrase (TR) | Validation Rule |
|-----------------|-----------------|
| "zorunlu alan" (required field) | `.NotEmpty()` |
| "en fazla X karakter" (max X chars) | `.MaximumLength(X)` |
| "en az X karakter" (min X chars) | `.MinimumLength(X)` |
| "geçerli e-posta" (valid email) | `.EmailAddress()` |
| "sayısal değer" (numeric value) | `.Must(BeNumeric)` |
| "tarih formatı" (date format) | `.Must(BeValidDate)` |
| "benzersiz" (unique) | Check via service (not validator) |
| "X ile Y arası" (between X and Y) | `.InclusiveBetween(X, Y)` |

## 8. Output Validation

Before submitting DoR YAML, verify:

- [ ] YAML is valid (parseable)
- [ ] `story_id` follows naming convention
- [ ] `spec_refs` match valid spec items
- [ ] `module` matches module-boundaries.md module list
- [ ] `role` maps to a valid RBAC role
- [ ] `inputs` have realistic Turkish examples
- [ ] `acceptance_tests` count >= 3
- [ ] `edge_cases` count >= 2
- [ ] `constraints.rbac` references valid permissions
- [ ] `tech_notes.abstraction_interface` is a valid Core interface
- [ ] `tech_notes.audit_events` uses correct naming convention
