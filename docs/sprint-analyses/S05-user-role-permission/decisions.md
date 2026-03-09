# Sprint S05 — Decisions Log

## D-001: Leverage Orchard Core Users + Roles Modules
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.1 requires unlimited user and role creation. We need to decide whether to build a custom user/role system or extend Orchard Core's built-in modules.
- **Decision**: Use `OrchardCore.Users` and `OrchardCore.Roles` as the foundation, wrapped behind `IUserService`, `IRoleService`, and `IAuthService` abstraction interfaces. The abstraction layer isolates the rest of the platform from Orchard Core specifics, per ADR-001 (Modular Monolith).
- **Consequences**: Faster implementation; we inherit Orchard Core's YesSql-based user storage, password hashing (PBKDF2), and role management. The abstraction layer adds a thin overhead but enables future replacement. We must ensure Orchard Core's built-in limits (if any) are removed or configured to satisfy the "no upper limit" requirement. Custom `UserProfilePart` extends the User document with KOSGEB-specific fields (department, phone, title).
- **Alternatives Considered**: (a) Custom user/role store with EF Core -- rejected due to duplicating Orchard Core functionality. (b) Direct use of Orchard Core without abstraction -- rejected per ADR-001 requiring abstraction layer for all Orchard Core dependencies.
- **ADR**: ADR-001 (Modular Monolith, abstraction layer)

## D-002: Two-Level Permission Taxonomy
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.2 requires permissions at both platform level (admin panel access) and per-content-type level (copy, edit, delete, view). We need a structured permission naming convention that is extensible and queryable.
- **Decision**: Adopt a two-level permission taxonomy:
  - **Level 1 (Platform-wide)**: `{Module}.{Action}` -- e.g., `UserRolePermission.ManageUsers`, `AdminPanel.Access`, `ContentManagement.Publish`, `QueryEngine.Execute`
  - **Level 2 (Content-type-specific)**: `ContentType.{TypeName}.{Action}` -- e.g., `ContentType.DestekProgrami.Edit`, `ContentType.Duyuru.View`
  - **Level 3 (Query-specific)**: `Query.Execute.{QueryName}` -- e.g., `Query.Execute.KobiDestekRaporu`
  - Permission checks happen in MediatR pipeline behaviors via `[RequirePermission("PermissionName")]` attribute on command/query handlers.
- **Consequences**: Clear separation of concerns. Content-type and query permissions scale automatically via auto-generation (D-003, D-004). Role editor UI must display permissions grouped by level. Permission strings must be consistent and well-documented. No hierarchical inheritance between permissions -- flat model.
- **Alternatives Considered**: Hierarchical permission model (e.g., ManageContent implies EditContent) -- rejected for simplicity and explicit grant model. Each permission must be explicitly assigned.
- **ADR**: N/A (extends ADR-001 abstraction pattern)

## D-003: Auto-Generation of Content-Type Permissions via Domain Events
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.3 requires that when a new content type is created, the platform automatically generates a set of permissions for that type. This must happen without manual intervention.
- **Decision**: Subscribe to `ContentTypeCreated` domain event via MediatR `INotificationHandler`. When fired, generate four permissions: `ContentType.{TypeName}.View`, `ContentType.{TypeName}.Edit`, `ContentType.{TypeName}.Delete`, `ContentType.{TypeName}.Copy`. Register them in Orchard Core's permission store via a dynamic `IPermissionProvider`. On `ContentTypeDeleted`, remove the four permissions from all roles and deregister them.
- **Consequences**: No manual permission creation needed. The role editor dynamically shows new permissions. Permission generation is idempotent (safe to replay). The `IPermissionProvider` must be tenant-aware. Race condition mitigation: use distributed lock (Redis) when generating permissions in rapid succession.
- **ADR**: N/A

## D-004: Auto-Generation of Query Execution Permissions
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.4 requires that each saved query automatically gets a permission controlling API execution access. This integrates with S04 (Query Management) module.
- **Decision**: Subscribe to `QueryCreated` domain event. Generate permission `Query.Execute.{QueryName}`. This permission controls whether the query can be run via `POST /api/v1/queries/{queryId}/execute`. Queries without this permission granted to the caller's role will return 403. On `QueryDeleted`, remove the permission from all roles. Query names with spaces or Turkish characters are sanitized to PascalCase ASCII for the permission key.
- **Consequences**: Fine-grained query access control. Admins must explicitly grant query execution permissions per role. The permission is auto-created but not auto-assigned to any role -- explicit grant required. Name collision handled via unique suffix (`_2`, `_3`).
- **ADR**: N/A

## D-005: Module Boundary -- ProjectDora.Modules.UserManagement
- **Date**: 2026-03-09
- **Context**: User/role/permission management needs its own module within the modular monolith.
- **Decision**: Create `ProjectDora.Modules.UserManagement` module with the following boundaries:
  - **Owns**: User CRUD, Role CRUD, Permission assignment, Role-User assignment, Permission auto-generation, Menu visibility filtering
  - **Exposes**: `IUserService`, `IRoleService` (via `ProjectDora.Core` interfaces)
  - **Consumes**: `ContentTypeCreated` / `ContentTypeDeleted` events (from ContentModeling module), `QueryCreated` / `QueryDeleted` events (from QueryEngine module)
  - **Forbidden**: Direct database access from other modules -- all access via service interfaces. No other module may directly query the User or Role tables.
- **Consequences**: Clean module boundary. Other modules check permissions via `IAuthService.HasPermissionAsync()` without knowing the implementation. The module registers its MediatR handlers, Orchard Core providers, and navigation providers at startup.
- **ADR**: ADR-001 (module boundaries per module-boundaries.md)

## D-006: No Artificial User/Role Limits + Pagination Strategy
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.1 states "bir ust limiti olmayacak sayida" (no upper limit on user/role count). Large KOSGEB deployments may have thousands of users.
- **Decision**: Ensure no hardcoded limits anywhere in the codebase. All list endpoints use pagination (`PagedResult<T>`) with configurable page sizes (default 20, max 100). Database indexes on `UserIndex` (username, email, enabled) and role name ensure performance at scale. Load testing target: 10,000 users, 100 roles, 500 auto-generated permissions per tenant.
- **Consequences**: Must implement efficient pagination and search. Admin panel user list must handle large datasets gracefully (server-side pagination, not client-side). Redis caching for frequently accessed permission checks (user permission set cached with 10m TTL, invalidated on role/permission change).
- **ADR**: N/A

## D-007: Tenant Isolation for User Management
- **Date**: 2026-03-09
- **Context**: Multi-tenant architecture (spec 4.1.10.8) requires strict user/role isolation between tenants. Security is critical -- cross-tenant data leak is a P0 vulnerability.
- **Decision**: All user and role operations are scoped to the current tenant (resolved from Orchard Core's `IShellSettings` which uses hostname or `X-Tenant-Id` header). SuperAdmin can manage users across tenants by switching tenant context. TenantAdmin is restricted to own tenant. YesSql's built-in tenant isolation (`ShellSettings.Name`) and custom query filters enforce isolation. Cross-tenant access returns 404 (not 403) to prevent information leakage.
- **Consequences**: User email uniqueness is per-tenant (same email can exist in different tenants). Role names are per-tenant. SuperAdmin operations require explicit tenant context parameter. Tenant isolation tests are mandatory for every endpoint. All audit events include tenant_id.
- **ADR**: N/A

## D-008: Role-Based Menu Visibility via INavigationProvider
- **Date**: 2026-03-09
- **Context**: Spec 4.1.6.5 (derived from 4.1.1.2) requires that admin panel menu items are visible only to users whose roles grant the associated permission. Menu visibility must be enforced server-side.
- **Decision**: Extend `INavigationProvider` to associate each admin menu item with a `RequiredPermission`. During menu tree construction, filter out items where `IAuthService.HasPermissionAsync(userId, menuItem.RequiredPermission)` returns false. The menu tree is built per-request and cached in Redis per user role combination (`menu:{tenantId}:{roleHash}`, TTL 10m, invalidated on role/permission change).
- **Consequences**: Menu tree varies by user's effective role set. Adding new modules/features automatically integrates with menu visibility if they register with a permission. Client-side must not cache the full menu tree independently -- always rely on the server-filtered response. Menu caching by role hash (not user ID) enables sharing cache entries among users with identical role sets.
- **Alternatives Considered**: Client-side filtering via permission set sent to frontend -- rejected because it leaks the full menu structure and permission set to the client, violating security-in-depth principle.
- **ADR**: N/A

## D-009: Password Policy and Account Security
- **Date**: 2026-03-09
- **Context**: User account security requires consistent password policies and session management for a government platform (KOSGEB).
- **Decision**: Password policy: minimum 8 characters, at least one uppercase, one lowercase, one digit. Optional: special character (configurable). Account lockout after 5 failed attempts for 15 minutes. Disabled user's existing JWT tokens are rejected via a token validation filter that checks `IsEnabled` flag on each request (not just at login). Password hashing via Orchard Core's built-in PBKDF2 with unique salt per user.
- **Consequences**: The token validation filter adds one cache lookup per request (Redis). Account lockout state stored in Redis with TTL. Failed login attempts are audit-logged. Password change forces re-authentication.
- **ADR**: N/A

## D-010: KOSGEB Default Roles Recipe
- **Date**: 2026-03-09
- **Context**: KOSGEB has domain-specific roles (Denetci, SEOUzmani, Analyst) that should be pre-configured on new tenant creation. Manual setup is error-prone.
- **Decision**: Create an Orchard Core Recipe (`kosgeb-default-roles.recipe.json`) that provisions the following default roles on tenant setup: SuperAdmin, TenantAdmin, Denetci, SEOUzmani, Analyst, Editor, Author, Viewer. Each role comes with a sensible default permission set. The recipe is idempotent and can be re-applied without duplicating roles.
- **Consequences**: New tenant provisioning is automated. Admins can customize roles after initial setup. Recipe format compatible with Orchard Core's deployment plan system (spec 4.1.10.15). Role names are in Turkish-friendly format (no special characters in role name key, display name supports Turkish).
- **ADR**: N/A
