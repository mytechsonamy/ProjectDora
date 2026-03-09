# Sprint S03 — Decisions Log

## D-001: Content Management Uses Same Module as Content Modeling
- **Date**: 2026-03-09
- **Context**: Content management (CRUD on content items) operates on content types defined in S02. The question is whether to create a separate module `ProjectDora.Modules.ContentManagement` or reuse `ProjectDora.Modules.ContentModeling`.
- **Decision**: Reuse `ProjectDora.Modules.ContentModeling` since content management and content modeling are tightly coupled in Orchard Core — both operate on the same `ContentItem` and `ContentType` entities via `IContentManager`. Separating them would create artificial boundaries and circular dependencies.
- **Consequences**: Single module handles both type definitions (S02) and item CRUD (S03). Permission namespaces remain distinct: `ContentModeling.*` for type operations, `ContentManagement.*` for item operations.
- **ADR**: N/A (implementation detail, not architecture-level)

## D-002: Draft-First Workflow as Default
- **Date**: 2026-03-09
- **Context**: Spec 4.1.3.2 requires a draft status before content is visible. Should new content default to Draft or allow direct-publish?
- **Decision**: All new content items default to Draft status. Publishing requires an explicit action with `ContentManagement.Publish` permission. The `CreateContentItemRequest.published` flag allows authorized users to create-and-publish in a single API call.
- **Consequences**: Authors can create content but cannot publish (requires Editor or higher). This enforces editorial review workflows. `VersionOptions.DraftRequired` is used for all create/update operations.
- **ADR**: N/A

## D-003: Immutable Versioning via Orchard Core Versionable Trait
- **Date**: 2026-03-09
- **Context**: Spec 4.1.3.6 requires that edits do not modify existing content but create new versions. How to implement?
- **Decision**: Use Orchard Core's built-in `Versionable()` content type trait. Every `UpdateAsync()` call creates a new `ContentItem` row with incremented `Number`. The `Latest` flag marks the newest version; `Published` flag marks the live version. Old versions are never deleted.
- **Consequences**: Storage grows with each edit. The `GetVersionsAsync()` method returns the full version chain. Rollback creates yet another new version (copy of target version data), maintaining the immutable history.
- **ADR**: N/A

## D-004: LocalizationPart for Multi-Language Content
- **Date**: 2026-03-09
- **Context**: Specs 4.1.3.4 and 4.1.3.5 require multi-language content variants that are relationally linked. How to implement?
- **Decision**: Use Orchard Core's `LocalizationPart` which assigns a `Culture` (BCP 47 code) and a `LocalizationSet` (shared GUID) to each content item. Creating a translation means cloning the original item, changing the culture, and keeping the same localization set ID.
- **Consequences**: Front-end language switcher can query by localization set + target culture to find translations. If a translation does not exist, the UI shows a disabled language option or falls back to the default culture. This approach is native to Orchard Core and requires minimal custom code.
- **ADR**: N/A

## D-005: AutoroutePart for SEO URLs with Turkish Transliteration
- **Date**: 2026-03-09
- **Context**: Specs 4.1.3.7 and 4.1.3.8 require pattern-based SEO URLs with per-language paths. How to handle Turkish characters in slugs?
- **Decision**: Use Orchard Core's `AutoroutePart` with Liquid slug patterns. Implement a custom slug transliteration filter that maps Turkish characters (s, c, g, i, o, u, I) to ASCII equivalents. Each language variant of a content item has its own `AutoroutePart.Path`, enabling per-culture URLs.
- **Consequences**: Turkish URLs are readable ASCII slugs. Custom paths can be set manually via `AllowCustomPath = true`. Duplicate slugs get a numeric suffix appended automatically. The slug is stored per content item version, so version rollback may change the URL.
- **ADR**: N/A

## D-006: Content Clone as Server-Side Deep Copy
- **Date**: 2026-03-09
- **Context**: Spec 4.1.3.1 mentions "icerik kopyasini olusturma" (content cloning). How to implement?
- **Decision**: Clone is implemented as: fetch latest version of source item, create a new content item of the same type, copy all parts and fields data (deep JSON clone), assign new `ContentItemId`, set status to Draft, set owner to current user. Clone does not copy version history or localization set membership.
- **Consequences**: Cloned item is independent from the source. No link between original and clone is maintained. Audit event `ContentItemCloned` records both source and target IDs.
- **ADR**: N/A
