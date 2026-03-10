# Sprint S11 — Key Decisions

## D-001: Theme templates stored in DB, not filesystem
**Karar**: Customized templates persisted via Orchard Core `IFileStore` (DB-backed), not on disk.
**Neden**: Filesystem templates would be overwritten on each deployment. DB storage survives redeploys and enables per-tenant isolation.
**Etki**: `OrchardThemeService.SaveTemplateAsync()` writes to OC file store; `GetTemplateAsync()` reads from it.

## D-002: IExtensionManager.GetExtensions() — sync, not async
**Karar**: Use `_extensionManager.GetExtensions()` (synchronous), wrap result in `Task.FromResult()`.
**Neden**: OrchardCore 2.x removed `LoadExtensionsAsync()`. The sync method is the correct API.
**Etki**: All theme listing operations use sync extension discovery. No async deadlock risk since it's sync-wrapped.

## D-003: Monaco Editor via CDN (not bundled)
**Karar**: Monaco Editor loaded from CDN in admin Liquid views.
**Neden**: Monaco is large (~5MB); bundling it into the module would significantly increase build size and deploy time.
**Etki**: Admin users need internet access for the template editor. Acceptable for admin panel (operators, not public users).

## D-004: ManageThemes = IsSecurityCritical
**Karar**: `ManageThemes` permission marked `IsSecurityCritical = true`.
**Neden**: Theme templates can inject arbitrary HTML/JavaScript into every page. A compromised theme = full XSS attack surface. Treat as destructive/critical operation.
**Etki**: Destructive permission test in `PermissionSecurityTests.cs` covers `ThemeManagement.Permissions.ManageThemes`.
