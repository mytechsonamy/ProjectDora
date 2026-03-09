# Sprint S01 — Decisions Log

## D-001: Use Orchard Core Built-in Admin Panel
- **Date**: 2026-03-09
- **Context**: Spec 4.1.1.1 requires an authorized admin panel. We can build from scratch or leverage Orchard Core's built-in admin module.
- **Decision**: Use `OrchardCore.Admin` as the foundation, extend with custom `INavigationProvider` and `IPermissionProvider` implementations.
- **Consequences**: Faster delivery, consistent with Orchard Core ecosystem, but tied to Orchard Core's admin UI patterns (Razor/Liquid). Abstraction layer isolates this dependency.
- **ADR**: Consistent with ADR-001 (Modular Monolith on Orchard Core)

## D-002: MinIO as Media Storage Backend
- **Date**: 2026-03-09
- **Context**: Spec 4.1.1.3 requires file/media management. Options: local filesystem, MinIO (S3-compatible), Azure Blob.
- **Decision**: Use MinIO via `OrchardCore.Media.AmazonS3` provider. MinIO is already in Docker Compose from S0.
- **Consequences**: S3-compatible API, works offline, open-source (spec requirement). Media URLs served via Orchard Core's media middleware.
- **ADR**: N/A

## D-003: Admin Panel Module as Separate Orchard Core Module
- **Date**: 2026-03-09
- **Context**: Admin panel customizations (menus, permissions) need a home.
- **Decision**: Create `ProjectDora.Modules.AdminPanel` as a dedicated Orchard Core module with `Startup.cs`, `Manifest.cs`, navigation providers, and permission providers.
- **Consequences**: Clean separation of concerns. Each subsequent module (Content, Query, etc.) registers its own navigation items via the same `INavigationProvider` pattern.
- **ADR**: N/A
