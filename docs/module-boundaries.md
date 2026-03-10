# Module Boundary Specification

> Version: 1.0 | Status: Draft | Last Updated: 2026-03-09

## 1. Purpose

This document defines the public interfaces, allowed dependencies, and forbidden access patterns for every module in ProjectDora. AI agents **MUST** consult this document before generating any cross-module code.

**Rule**: Modules communicate ONLY via public interfaces. No direct DB access, no internal class references, no shared mutable state.

## 2. Module Dependency Graph

```
                          ┌──────────────────┐
                          │  ProjectDora.Core │ (shared kernel)
                          │  - Interfaces     │
                          │  - Domain Models  │
                          │  - Value Objects  │
                          └────────┬──────────┘
                                   │
              ┌────────────────────┤
              │                    │
    ┌─────────▼─────────┐  ┌──────▼──────────┐
    │  ProjectDora.Web   │  │ ProjectDora.     │
    │  (Host/Startup)    │  │ Modules.*        │
    └────────────────────┘  └─────────────────┘
                                   │
         ┌──────────┬──────────┬───┴────┬──────────┬──────────┐
         │          │          │        │          │          │
    ┌────▼───┐ ┌───▼────┐ ┌──▼──┐ ┌──▼───┐ ┌───▼───┐ ┌───▼────┐
    │Content │ │Content │ │Query│ │User/ │ │Work- │ │Audit  │
    │Modeling│ │Mgmt    │ │Eng. │ │Role  │ │flow  │ │Trail  │
    └────────┘ └────────┘ └─────┘ └──────┘ └──────┘ └────────┘
```

**Dependency direction**: Always downward toward Core. Never horizontal between modules except via Core interfaces.

## 3. Module Specifications

### 3.1 ProjectDora.Core (Shared Kernel)

| Attribute | Value |
|-----------|-------|
| **Owns** | Interfaces, domain models, value objects, events, DTOs |
| **Depends on** | Nothing (zero dependencies) |
| **Depended on by** | All modules |

**Public Interfaces:**

```csharp
// Content
IContentService          // CRUD, publish, version, localize content items
IContentTypeService      // Define and manage content types, fields, parts

// Query
IQueryService            // Execute Lucene, Elasticsearch, SQL queries
ISavedQueryService       // CRUD for saved/named queries

// Workflow
IWorkflowService         // Trigger, execute, manage workflow definitions

// Auth
IAuthService             // Authenticate, authorize, manage tokens
IUserService             // CRUD users
IRoleService             // CRUD roles, assign permissions

// Audit
IAuditService            // Log audit events, query audit trail

// Infrastructure
ITenantService           // Tenant provisioning and management
ICacheService            // Cache get/set/invalidate
ISearchIndexService      // Index management, reindex triggers
IStorageService          // File upload/download (MinIO abstraction)
```

**Shared DTOs** (defined in Core, used across modules):

```
ContentItemDto, ContentTypeDto, ContentFieldDto, ContentVersionDto
UserDto, RoleDto, PermissionDto
AuditLogDto, AuditDiffDto
QueryResultDto, PagedResultDto<T>
TenantDto
```

---

### 3.2 ProjectDora.Modules.ContentModeling

| Attribute | Value |
|-----------|-------|
| **Owns** | ContentType, ContentField, ContentPart definitions |
| **Schema** | `orchard` (YesSql) |
| **Implements** | `IContentTypeService` |
| **Depends on** | `ProjectDora.Core` |
| **May call** | `IAuditService`, `ISearchIndexService`, `ICacheService` |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| `IQueryService` | Query engine is a separate concern |
| `IWorkflowService` | Workflows react to events, not called directly |
| `IUserService` / `IRoleService` | Auth is separate module |
| `ContentItem` table directly | Content items managed by ContentManagement module |
| Any other module's internal classes | Module boundary violation |

**Domain Events Emitted:**
- `ContentTypeCreated`, `ContentTypeModified`, `ContentTypeDeleted`

---

### 3.3 ProjectDora.Modules.ContentManagement

| Attribute | Value |
|-----------|-------|
| **Owns** | ContentItem, ContentVersion, localization links |
| **Schema** | `orchard` (YesSql) |
| **Implements** | `IContentService` |
| **Depends on** | `ProjectDora.Core` |
| **May call** | `IContentTypeService` (validate type exists), `IAuditService`, `ICacheService`, `ISearchIndexService`, `IStorageService` |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| `IQueryService` | Queries are separate |
| `IWorkflowService` | Emits events; workflow engine reacts |
| `IRoleService` | Permission checks via `IAuthService` middleware |
| `ContentType` definition tables | Read via `IContentTypeService` |

**Domain Events Emitted:**
- `ContentItemCreated`, `ContentItemUpdated`, `ContentItemPublished`, `ContentItemDeleted`, `ContentItemVersionRolledBack`

---

### 3.4 ProjectDora.Modules.QueryEngine

| Attribute | Value |
|-----------|-------|
| **Owns** | SavedQuery, query execution logic, result formatting |
| **Schema** | `orchard` (queries), reads from `analytics` (SQL queries) |
| **Implements** | `IQueryService`, `ISavedQueryService` |
| **Depends on** | `ProjectDora.Core` |
| **May call** | `IAuditService`, `ICacheService` |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| `IContentService` / `IContentTypeService` | Content is separate; queries access content via search indexes |
| `IWorkflowService` | No workflow interaction |
| `IUserService` | Auth via middleware |
| Analytics interfaces | Uses analytics schema directly |
| Orchard Core `ContentManager` | Must use abstraction or search index |

**Special Rules:**
- SQL queries are **read-only** (SELECT only, enforced by parser)
- All SQL queries must include `tenant_id` filter (enforced by service)
- Query results are parameterized (no string interpolation)

**Domain Events Emitted:**
- `QueryExecuted`, `QueryCreated`, `QueryDeleted`

---

### 3.5 ProjectDora.Modules.UserRolePermission

| Attribute | Value |
|-----------|-------|
| **Owns** | User, Role, UserRole, Permission assignments |
| **Schema** | `orchard` (YesSql) |
| **Implements** | `IAuthService`, `IUserService`, `IRoleService` |
| **Depends on** | `ProjectDora.Core` |
| **May call** | `IAuditService`, `ITenantService` |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| `IContentService` | Content is separate |
| `IQueryService` | Queries are separate |
| `IWorkflowService` | Workflows are separate |

**Special Rules:**
- Password hashing: bcrypt only (no MD5, SHA)
- Token issuance: OpenID Connect via Orchard Core OpenId module
- Permission format: `Module.Action` (e.g., `ContentModeling.Create`)
- Tenant isolation: Users scoped to tenant; SuperAdmin spans tenants

**Domain Events Emitted:**
- `UserCreated`, `UserUpdated`, `UserDisabled`, `RoleAssigned`, `RoleRevoked`, `PermissionChanged`

---

### 3.6 ProjectDora.Modules.Workflows

| Attribute | Value |
|-----------|-------|
| **Owns** | WorkflowDef, WFActivity, WFTransition, WorkflowExecution |
| **Schema** | `orchard` (YesSql) |
| **Implements** | `IWorkflowService` |
| **Depends on** | `ProjectDora.Core` |
| **May call** | `IAuditService`, `IContentService` (for content actions in activities), `IUserService` (for notifications) |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| `IContentTypeService` | Content modeling is separate |
| `IQueryService` | Queries are separate |
| Database directly | Must use service interfaces for all data access |

**Special Rules:**
- Workflows are **event-driven**: they react to domain events, not polled
- Custom activities must implement `IActivity` interface from Core
- Timer triggers use background service, not polling

**Domain Events Emitted:**
- `WorkflowTriggered`, `WorkflowCompleted`, `WorkflowFaulted`

---

### 3.7 ProjectDora.Modules.AuditTrail

| Attribute | Value |
|-----------|-------|
| **Owns** | AuditLog, RetentionPolicy, audit diff computation |
| **Schema** | `audit` (EF Core — separate schema) |
| **Implements** | `IAuditService` |
| **Depends on** | `ProjectDora.Core` |
| **May call** | `ITenantService` (tenant context) |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| All other module interfaces | Audit is a passive consumer; it logs events but never triggers actions |
| `orchard` schema | Audit has its own schema |
| Content, Query, Workflow internals | Only receives events via IAuditService.Log() |

**Special Rules:**
- **Write-append only**: Audit logs are never updated or deleted (except by retention policy)
- Diff computation happens at log time, not query time
- Retention cleanup is a background job, configurable per tenant

---

### 3.8 ProjectDora.Modules.Integration

| Attribute | Value |
|-----------|-------|
| **Owns** | REST controllers, GraphQL schema, API versioning, headless endpoints |
| **Schema** | None (stateless API layer) |
| **Implements** | HTTP endpoints for all public interfaces |
| **Depends on** | `ProjectDora.Core` |
| **May call** | All `I*Service` interfaces from Core (it's the API gateway) |

**Forbidden Dependencies:**

| Cannot Access | Reason |
|---------------|--------|
| Module internal classes | Must use only Core interfaces |
| Database directly | Must use service interfaces |
| Orchard Core `ContentManager` | Must use `IContentService` |

**Special Rules:**
- All endpoints require authentication except explicitly public ones
- API versioning: `/api/v1/` prefix
- Rate limiting applied at this layer
- Response format: JSON (REST), GraphQL (Hot Chocolate)
- Error format: RFC 7807 Problem Details

---

## 4. Dependency Matrix

`✅` = Allowed | `❌` = Forbidden | `—` = Self

| Module \ Can Call → | Core | ContentModeling | ContentMgmt | QueryEngine | UserRole | Workflow | Audit | Integration |
|---------------------|------|----------------|-------------|-------------|----------|----------|-------|-------------|
| **Core** | — | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **ContentModeling** | ✅ | — | ❌ | ❌ | ❌ | ❌ | ✅¹ | ❌ |
| **ContentMgmt** | ✅ | ✅² | — | ❌ | ❌ | ❌ | ✅¹ | ❌ |
| **QueryEngine** | ✅ | ❌ | ❌ | — | ❌ | ❌ | ✅¹ | ❌ |
| **UserRole** | ✅ | ❌ | ❌ | ❌ | — | ❌ | ✅¹ | ❌ |
| **Workflow** | ✅ | ❌ | ✅³ | ❌ | ✅⁴ | — | ✅¹ | ❌ |
| **Audit** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | — | ❌ |
| **Integration** | ✅ | ✅⁵ | ✅⁵ | ✅⁵ | ✅⁵ | ✅⁵ | ✅⁵ | — |

**Notes:**
1. ✅¹ Via `IAuditService` interface only
2. ✅² Via `IContentTypeService` interface only (validate type exists)
3. ✅³ Via `IContentService` interface only (workflow actions on content)
4. ✅⁴ Via `IUserService` interface only (notifications)
5. ✅⁵ Via Core interfaces only (API gateway pattern)

## 5. Enforcement

### Compile-Time

```xml
<!-- In each module .csproj, only reference ProjectDora.Core -->
<ProjectReference Include="..\ProjectDora.Core\ProjectDora.Core.csproj" />
<!-- NEVER reference other module projects directly -->
```

### Architecture Tests

```csharp
// ArchUnit-style test (NetArchTest or similar)
[Fact]
public void ContentModeling_ShouldNotDependOn_QueryEngine()
{
    var result = Types.InAssembly(typeof(ContentModelingModule).Assembly)
        .ShouldNot()
        .HaveDependencyOn("ProjectDora.Modules.QueryEngine")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

### Code Review Checklist

AI Reviewer Agent must check:
- [ ] Module only references `ProjectDora.Core`
- [ ] No `using ProjectDora.Modules.{OtherModule}` statements
- [ ] No direct DB context from another schema
- [ ] Cross-module data accessed only via interfaces

## 6. Cross-References

- **Domain Model**: [domain-model.md](domain-model.md) — entities owned by each module
- **API Contract**: [api-contract.yaml](api-contract.yaml) — endpoints map to Integration module
- **Test Cases**: [../.claude/testing/test-cases.md](../.claude/testing/test-cases.md) — module-scoped tests
- **Governance**: [../.claude/ai-sdlc/governance.md](../.claude/ai-sdlc/governance.md) — Reviewer Agent uses this
