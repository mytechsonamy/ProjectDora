# Sprint S05 — User, Role & Permission Management

## Kapsam (Scope)
- Spec items: 4.1.6.1, 4.1.6.2, 4.1.6.3, 4.1.6.4, 4.1.6.5
- Stories: US-601, US-602, US-603, US-604, US-605, US-606, US-607, US-608
- Cross-references: 4.1.1.1 (admin panel auth), 4.1.1.2 (role-based menus), 4.1.3 (content management CRUD), 4.1.5 (query permissions), 4.1.7 (workflow permissions), 4.1.10.8 (multi-tenancy), 4.1.10.22 (OpenID), 4.1.11.4 (API auth), 4.1.11.5 (role-based API access)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkish Text | English Summary |
|------|-------------|----------------|
| 4.1.6.1 | Urun yonetim paneli uzerinden bir ust limiti olmayacak sayida yeni kullanici ve rol tanimlanabilir yapida olmasi | Unlimited user and role creation via admin panel -- no upper limit on number of users or roles. The platform must allow the creation of new users and roles without any predefined ceiling. |
| 4.1.6.2 | Tanimlanacak roller kapsaminda hem urun yonetim paneli genelinde cesitli erisim kabiliyetlerine istinaden hem de olusturulan her bir ozel icerik turu bazinda detaylandirilabilir sekilde (kopyalama, duzenleme, silme, goruntuleme) farkli yetkilerin tanimlanabilmesi | Roles can have both platform-wide permissions (admin panel access, module features) and per-content-type granular permissions (copy, edit, delete, view). Two-level permission taxonomy. |
| 4.1.6.3 | Icerik turu bazinda tercih edilebilecek bu yetki setlerinin urun tarafindan otomatik olusturulmasi | Auto-generation of permission sets per content type -- when a new content type is created, the platform automatically creates corresponding CRUD permission entries (View, Edit, Delete, Copy). No manual permission creation needed. |
| 4.1.6.4 | Urun uzerinde olusturulan sorgularin API uzerinden calistirilip calistirilmama durumunu tayin amaciyla her bir sorgu ozelinde ayri bir yetkinin otomatik olarak olusmasi | Auto-generation of per-query API execution permissions -- each saved query gets its own permission controlling whether it can be executed via API. Permission is auto-created but not auto-assigned. |
| 4.1.6.5 | (Derived from 4.1.1.2) Yonetim paneli uzerinde olusturulan cesitli baglanti ve bilgi varliklari icin sadece belirli roldeki kullanicilar tarafindan kendilerine verilen yetkilendirme uyarinca kullanilabilecek ozel menulerin olusturulmasina imkan vermesi | Role-based menu visibility -- admin panel menu items and navigation links are shown/hidden based on the authenticated user's role permissions. Each menu item is associated with a required permission; users only see menus they have access to. |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S01 (Admin Panel) must be complete -- admin panel infrastructure is needed for user/role management screens
- **Dependency**: S02 (Content Modeling) must be complete -- content types are needed for per-type permission auto-generation (US-606)
- **Dependency**: S04 (Query Management) must be complete -- saved queries are needed for per-query permission auto-generation (US-607)
- **Orchard Core**: User/Role management via `OrchardCore.Users` and `OrchardCore.Roles` modules -- we extend with custom permission auto-generation and abstraction layer
- **Authentication**: OpenID Connect via `OrchardCore.OpenId` -- JWT bearer tokens (established in S01, spec 4.1.10.22)
- **Multi-tenant**: Users and roles are tenant-scoped -- `tenant_id` on every entity (spec 4.1.10.8). Same email can exist in different tenants.
- **No upper limit**: Spec 4.1.6.1 explicitly requires "bir ust limiti olmayacak sayida" (no upper limit on user/role count) -- no artificial caps, pagination mandatory
- **Permission granularity**: Two levels -- platform-wide (admin panel sections, module features) and content-type-level (CRUD per type)
- **KVKK/GDPR**: User PII (email, name) classified as personal data. Retention and encryption per data-governance.md.
- **Audit**: All user/role/permission changes must be audit-logged (spec 4.1.9.1) with actor, timestamp, IP, event type
- **Offline**: System must function without internet connection (spec 4.1.10.22) -- local OpenID token validation

### RBAC Gereksinimleri (RBAC Requirements Table)

| Permission | SuperAdmin | TenantAdmin | Denetci | SEOUzmani | Analyst | Editor | Author | Viewer | Anonymous |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| UserRolePermission.ManageUsers | Y | Y (own tenant) | - | - | - | - | - | - | - |
| UserRolePermission.ManageRoles | Y | Y (own tenant) | - | - | - | - | - | - | - |
| UserRolePermission.AssignRoles | Y | Y (own tenant) | - | - | - | - | - | - | - |
| UserRolePermission.ViewPermissions | Y | Y | Y | - | - | - | - | - | - |
| AdminPanel.Access | Y | Y | Y | Y | Y | Y | - | - | - |
| ContentManagement.Create | Y | Y | - | - | - | Y | Y | - | - |
| ContentManagement.Publish | Y | Y | - | - | - | Y | - | - | - |
| ContentManagement.EditOwn | Y | Y | - | - | - | Y | Y | - | - |
| ContentType.{Type}.View | Y | Y | Y | Y | Y | Y | Y | Y | - |
| ContentType.{Type}.Edit | Y | Y | - | - | - | Y | - | - | - |
| ContentType.{Type}.Delete | Y | Y | - | - | - | - | - | - | - |
| ContentType.{Type}.Copy | Y | Y | - | - | - | Y | - | - | - |
| Query.Execute.{QueryName} | Y | Y | - | - | Y | - | - | - | - |
| Menu.{MenuId}.Visible | Y | Y | configurable | configurable | configurable | configurable | - | - | - |

**KOSGEB-Specific Roles:**
- **SuperAdmin**: Platform-wide administrator across all tenants. Full access to everything.
- **TenantAdmin**: Tenant-level administrator. Full access within own tenant only.
- **Denetci (Auditor)**: Reads audit logs, reviews content changes, views permission assignments. Read-only, no mutation rights.
- **SEOUzmani (SEO Specialist)**: Manages SEO metadata, URL aliases, sitemap entries. Limited content editing for SEO fields only.
- **Analyst**: Executes queries, views reports, accesses analytics dashboards. No content editing.
- **Editor**: Creates, edits, publishes content. No user/role management.
- **Author**: Creates and edits own content only. Cannot publish.
- **Viewer**: Read-only access to published content. No admin panel access.
- **Anonymous**: Unauthenticated user. Access to public-facing content only.

### Story Decomposition

| Story | Spec | Priority | Description |
|-------|------|----------|-------------|
| US-601 | 4.1.6.1 | P0 | Create and manage users (unlimited, CRUD, profile) |
| US-602 | 4.1.6.1 | P0 | Create and manage roles (unlimited, CRUD, custom permissions) |
| US-603 | 4.1.6.1 | P1 | Assign/revoke roles to/from users |
| US-604 | 4.1.6.2 | P0 | Define platform-wide permissions on roles |
| US-605 | 4.1.6.2 | P1 | Define per-content-type granular permissions (copy, edit, delete, view) |
| US-606 | 4.1.6.3 | P1 | Auto-generate permission sets when content types are created/deleted |
| US-607 | 4.1.6.4 | P2 | Auto-generate per-query API execution permissions |
| US-608 | 4.1.6.5 | P1 | Role-based admin panel menu visibility |

### Priority Rationale

- **P0 (Must-have)**: US-601 (User CRUD), US-602 (Role CRUD), US-604 (Platform-wide permissions) -- these are the RBAC foundation every other module depends on
- **P1 (Should-have)**: US-603 (Role assignment), US-605 (Content-type permissions), US-606 (Auto-generate content-type permissions), US-608 (Menu visibility) -- critical for usable RBAC but can be partially deferred if needed
- **P2 (Nice-to-have)**: US-607 (Query permissions) -- depends on S04 (Query Management) and can follow in a subsequent sprint if needed

### Entity Mapping

| Domain Entity | Orchard Core Mapping | Custom Extension |
|--------------|---------------------|-----------------|
| User | `OrchardCore.Users.Models.User` | `UserProfilePart` with department, phone, title fields |
| Role | `OrchardCore.Roles.Models.Role` | Extended permission taxonomy, description field |
| Permission | `OrchardCore.Security.Permissions.Permission` | Auto-generated per content type and query via dynamic `IPermissionProvider` |
| UserRole (join) | Built-in M:N via YesSql `UserByRoleNameIndex` | Audit trail integration on every assignment/revocation |
| MenuItem | `OrchardCore.AdminMenu` | `RequiredPermission` field on each menu item for role-based visibility |

### Dependency Graph

```
US-601 (User CRUD)
  |
  +-- US-603 (Role Assignment) --> depends on US-601 + US-602
  |
  +-- US-608 (Menu Visibility) --> depends on US-604

US-602 (Role CRUD)
  |
  +-- US-604 (Platform Permissions) --> depends on US-602
  |     |
  |     +-- US-605 (Content-Type Permissions) --> depends on US-604
  |     |
  |     +-- US-606 (Auto-Generate CT Perms) --> depends on US-604 + S02
  |     |
  |     +-- US-607 (Auto-Generate Query Perms) --> depends on US-604 + S04
  |
  +-- US-603 (Role Assignment) --> depends on US-601 + US-602
```

## Teknik Kararlar (Technical Decisions)

See `decisions.md` for full decision log. Summary:

### D-001: Leverage Orchard Core Users + Roles Modules
- Use `OrchardCore.Users` and `OrchardCore.Roles` as the foundation, wrapped behind `IUserService`, `IRoleService`, and `IAuthService` abstraction interfaces per ADR-001

### D-002: Two-Level Permission Taxonomy
- Level 1 (Platform-wide): `{Module}.{Action}` -- e.g., `UserRolePermission.ManageUsers`, `AdminPanel.Access`
- Level 2 (Content-type-specific): `ContentType.{TypeName}.{Action}` -- e.g., `ContentType.DestekProgrami.Edit`
- Permission checks via MediatR pipeline behaviors using `[RequirePermission]` attribute

### D-003: Auto-Generation via Domain Events
- `ContentTypeCreated` event -> 4 permissions (View, Edit, Delete, Copy)
- `QueryCreated` event -> 1 permission (Query.Execute.{QueryName})
- `ContentTypeDeleted` / `QueryDeleted` -> cleanup orphaned permissions

### D-004: Module Structure
- Module: `ProjectDora.Modules.UserManagement`
- Implements: `IPermissionProvider`, `INavigationProvider`
- Consumes: `ContentTypeCreated`, `ContentTypeDeleted`, `QueryCreated`, `QueryDeleted` events

### D-005: No Artificial Limits + Pagination
- All list endpoints use `PagedResult<T>` -- server-side pagination mandatory
- Load testing target: 10,000 users, 100 roles, 500 permissions per tenant
- Redis caching for permission lookups with WriteThrough invalidation

### D-006: Tenant Isolation
- All operations scoped to current tenant via `IShellSettings`
- Email uniqueness is per-tenant (same email can exist in different tenants)
- SuperAdmin can operate across tenants; TenantAdmin restricted to own tenant

### D-007: Role-Based Menu Visibility
- Admin menu items have `RequiredPermission` metadata
- `INavigationProvider` filters menu tree based on current user's effective permissions
- Menu visibility is computed server-side -- no client-side filtering of hidden menus

### Abstraction Layer Interfaces

```csharp
// IUserService -- User management abstraction
public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserCommand command, CancellationToken ct);
    Task<UserDto> GetUserAsync(string userId, CancellationToken ct);
    Task<PagedResult<UserDto>> ListUsersAsync(ListUsersQuery query, CancellationToken ct);
    Task<UserDto> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct);
    Task EnableUserAsync(string userId, CancellationToken ct);
    Task DisableUserAsync(string userId, CancellationToken ct);
    Task DeleteUserAsync(string userId, CancellationToken ct);
}

// IRoleService -- Role and permission management abstraction
public interface IRoleService
{
    Task<RoleDto> CreateRoleAsync(CreateRoleCommand command, CancellationToken ct);
    Task<RoleDto> GetRoleAsync(string roleId, CancellationToken ct);
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct);
    Task<RoleDto> UpdateRoleAsync(UpdateRoleCommand command, CancellationToken ct);
    Task DeleteRoleAsync(string roleId, CancellationToken ct);
    Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken ct);
    Task RevokeRolesFromUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken ct);
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct);
    Task<PermissionMatrixDto> GetContentTypePermissionMatrixAsync(string roleId, CancellationToken ct);
    Task GenerateContentTypePermissionsAsync(string contentTypeName, CancellationToken ct);
    Task RemoveContentTypePermissionsAsync(string contentTypeName, CancellationToken ct);
    Task GenerateQueryPermissionAsync(string queryName, CancellationToken ct);
    Task RemoveQueryPermissionAsync(string queryName, CancellationToken ct);
}

// IAuthService -- Authentication and authorization abstraction
public interface IAuthService
{
    Task<bool> HasPermissionAsync(string userId, string permissionName, CancellationToken ct);
    Task<string> GetCurrentUserIdAsync(CancellationToken ct);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct);
    Task<bool> IsUserEnabledAsync(string userId, CancellationToken ct);
}
```

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-601-01 | Unit | US-601 | Create user with valid data succeeds |
| TC-601-02 | Unit | US-601 | Create user with duplicate email returns 409 Conflict |
| TC-601-03 | Security | US-601 | Unauthorized user (Editor role) cannot create users |
| TC-601-04 | Unit | US-601 | Turkish special characters in displayName preserved (Seref, Gungor, Ozcaliskan) |
| TC-601-05 | Unit | US-601 | List users with server-side pagination |
| TC-601-06 | Unit | US-601 | Delete user performs soft-delete and emits audit event |
| TC-602-01 | Unit | US-602 | Create role with valid name and permissions succeeds |
| TC-602-02 | Unit | US-602 | Delete role cascades removal from all assigned users |
| TC-602-03 | Security | US-602 | Non-admin cannot create roles |
| TC-602-04 | Unit | US-602 | Reject duplicate role name within tenant |
| TC-602-05 | Unit | US-602 | Cannot delete built-in roles (SuperAdmin, TenantAdmin) |
| TC-603-01 | Unit | US-603 | Assign multiple roles to user succeeds |
| TC-603-02 | Unit | US-603 | Revoke role from user succeeds |
| TC-603-03 | Security | US-603 | Cannot assign SuperAdmin role without SuperAdmin permission |
| TC-603-04 | Unit | US-603 | Cannot remove own admin role (safety guard) |
| TC-604-01 | Unit | US-604 | Define platform-wide permission on role |
| TC-604-02 | Integration | US-604 | Permission check blocks unauthorized action end-to-end |
| TC-604-03 | Unit | US-604 | List all available platform permissions grouped by module |
| TC-604-04 | Unit | US-604 | Concurrent permission updates handled with optimistic concurrency |
| TC-605-01 | Unit | US-605 | Per-content-type permission (View, Edit, Delete, Copy) assigned to role |
| TC-605-02 | Integration | US-605 | User with type-specific permission can only access that type |
| TC-605-03 | Integration | US-605 | User can edit permitted type but cannot delete without Delete permission |
| TC-605-04 | Unit | US-605 | Permission matrix query returns all content types x actions grid |
| TC-606-01 | Unit | US-606 | Auto-generate 4 permissions on content type creation |
| TC-606-02 | Unit | US-606 | Auto-generated permissions appear in role editor immediately |
| TC-606-03 | Unit | US-606 | Permissions cleaned up on content type deletion |
| TC-606-04 | Unit | US-606 | Idempotent generation -- no duplicates on repeated event |
| TC-607-01 | Unit | US-607 | Auto-generate query execution permission on query creation |
| TC-607-02 | Integration | US-607 | Query execution blocked without per-query permission (403) |
| TC-607-03 | Unit | US-607 | Permission removed on query deletion |
| TC-608-01 | Unit | US-608 | Menu item hidden when user lacks required permission |
| TC-608-02 | Integration | US-608 | Admin navigation tree filtered by user's effective permissions |
| TC-608-03 | Unit | US-608 | SuperAdmin sees all menu items regardless |
| TC-608-04 | Security | US-608 | Disabled user blocked from login, existing tokens invalidated |
| TC-608-05 | Security | US-608 | Tenant isolation -- user in tenant A cannot see tenant B users |
| TC-608-06 | Security | US-608 | Privilege escalation prevention -- non-SuperAdmin cannot grant SuperAdmin |
| TC-608-07 | Security | US-608 | IDOR prevention -- cannot access user by ID across tenant boundary |

### Coverage Target
- Unit test coverage: >= 80% for UserManagement module
- Integration test coverage: >= 60%
- Security tests: 8 minimum (auth, RBAC, tenant isolation, IDOR, privilege escalation, role assignment, menu visibility, session invalidation)

### Security-Specific Tests
1. **RBAC Enforcement**: Every endpoint requires correct permission; denied roles return 403
2. **Tenant Isolation**: Cross-tenant access returns 404 (not 403, to avoid information leak)
3. **Privilege Escalation**: Non-SuperAdmin cannot assign SuperAdmin role
4. **IDOR**: Cannot access/modify users or roles by ID across tenant boundaries
5. **Session Invalidation**: Disabled user's existing JWT tokens are rejected
6. **Self-Protection**: Admin cannot disable own account or remove own admin role
7. **Menu Filtering**: Server-side menu filtering -- no client-side-only hiding
8. **Password Policy**: Minimum 8 chars, uppercase, lowercase, digit required

## Sprint Sonucu (Sprint Outcome)
- [ ] US-601 complete -- User CRUD (unlimited)
- [ ] US-602 complete -- Role CRUD (unlimited)
- [ ] US-603 complete -- Role assignment/revocation
- [ ] US-604 complete -- Platform-wide permissions
- [ ] US-605 complete -- Per-content-type permissions
- [ ] US-606 complete -- Auto-generate content type permissions
- [ ] US-607 complete -- Auto-generate query permissions
- [ ] US-608 complete -- Role-based menu visibility

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Kullanici yonetim ekrani: kullanici olusturma, duzenleme, etkinlestirme/devre disi birakma, silme
- Rol yonetim ekrani: rol olusturma, platform geneli ve icerik turu bazinda yetki atama
- Rol atama: bir kullaniciya bir veya birden fazla rol atama
- Yetki matrisi: rollerin hangi yetkilere sahip oldugunu gosteren gorsel tablo
- Otomatik yetki olusturma: yeni icerik turleri ve sorgular icin otomatik yetki olusturma aciklamasi
- Menu gorunurlugu: rollere gore admin panel menu ogelerinin nasil gosterilip gizlendigi
- KOSGEB roller: Denetci, SEOUzmani, Analyst rollerinin varsayilan yetkileri

### Teknik Dokumantasyon (Technical Documentation)
- `IUserService`, `IRoleService`, `IAuthService` abstraction interface contracts
- `IPermissionProvider` implementation for auto-generated dynamic permissions
- `INavigationProvider` implementation for role-based menu filtering
- MediatR command/query handlers for all CRUD operations
- Domain events: `UserCreated`, `UserUpdated`, `UserDeleted`, `UserEnabled`, `UserDisabled`, `RoleCreated`, `RoleUpdated`, `RoleDeleted`, `RoleAssigned`, `RoleRevoked`, `PermissionAutoGenerated`, `PermissionAutoRemoved`
- Permission naming convention: `{Module}.{Action}` and `ContentType.{TypeName}.{Action}` and `Query.Execute.{QueryName}`
- Tenant isolation: global query filter on `tenant_id` via `IShellSettings`
- Password policy configuration
- Redis caching strategy for permission lookups
- API endpoint summary

### API Endpoints
- `GET /api/v1/users` -- List users (paginated, filterable by role/status)
- `POST /api/v1/users` -- Create user
- `GET /api/v1/users/{userId}` -- Get user by ID
- `PUT /api/v1/users/{userId}` -- Update user (profile, enable/disable)
- `DELETE /api/v1/users/{userId}` -- Soft-delete user
- `PUT /api/v1/users/{userId}/roles` -- Assign roles to user (replace)
- `GET /api/v1/users/{userId}/roles` -- Get user's assigned roles
- `GET /api/v1/roles` -- List all roles in tenant
- `POST /api/v1/roles` -- Create role
- `GET /api/v1/roles/{roleId}` -- Get role with permissions
- `PUT /api/v1/roles/{roleId}` -- Update role (name, description, platform permissions)
- `DELETE /api/v1/roles/{roleId}` -- Delete role (cascade from users)
- `PUT /api/v1/roles/{roleId}/content-type-permissions` -- Update content-type-specific permissions
- `GET /api/v1/roles/{roleId}/content-type-permissions` -- Get content-type permission matrix for role
- `GET /api/v1/permissions` -- List all registered permissions (grouped by module)
- `GET /api/v1/admin/menu` -- Get filtered admin menu tree for current user
