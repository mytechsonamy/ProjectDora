# Sprint S12 — Key Decisions

## D-001: SqlSafetyValidator — whitelist approach (SELECT only)
**Karar**: SQL queries validated against a keyword whitelist (SELECT, WITH, UNION, ORDER BY, etc.) and a blacklist (DROP, DELETE, INSERT, UPDATE, TRUNCATE, CREATE, ALTER, EXEC, EXECUTE, XP_). Empty queries rejected.
**Neden**: QueryEngine (S04) allows user-defined SQL queries. Must prevent destructive operations and injection vectors. Whitelist approach is safer than blacklist-only.
**Etki**: `SqlSafetyValidator.Validate(sql)` is a static method. UNION is allowed (injection prevention relies on parameterized queries, not keyword blocking). Tested in `Security/SqlInjectionTests.cs`.

## D-002: Permission names globally unique — enforced by test
**Karar**: Automated test (`Security_Permissions_NoPermissionCollisions`) asserts all permission names across all 10 modules are globally unique.
**Neden**: Duplicate permission names cause silent RBAC bypass — one module's permission check passes for another module's resource.
**Etki**: Each module uses a namespace prefix convention: e.g., `AdminPanel.AccessAdminPanel`, `AuditTrail.ViewAuditLogs`. No collisions allowed.

## D-003: IsSecurityCritical flag for destructive permissions
**Karar**: 9 specific permissions tagged `IsSecurityCritical = true`: AuditTrail.Purge, AuditTrail.Rollback, Infrastructure.ManageTenants, Infrastructure.PurgeCache, Infrastructure.ManageOpenId, Infrastructure.ManageSettings, Integration.ManageApiClients, ThemeManagement.ManageThemes, UserManagement.ManageUsers.
**Neden**: These permissions can cause irreversible harm (data loss, full system access, XSS via themes). Must be treated differently in UI (extra confirmation) and auditing (always logged).
**Etki**: `Security_Permissions_DestructiveOperationsAreSecurityCritical` test enforces this list.

## D-004: TreatWarningsAsErrors covers Roslyn analyzers
**Karar**: Build policy enforces zero Roslyn analyzer warnings. CA1305 (IFormatProvider), CA1822 (static members), CA1826 (use indexer), CA1861 (inline arrays), CA2012 (ValueTask.GetResult) all treated as errors.
**Neden**: Prevents security-sensitive code smells (e.g., CA1305 prevents locale-dependent string comparisons; CA2012 prevents async deadlocks).
**Etki**: Every fix applied during development; no `#pragma warning disable` in production code.
