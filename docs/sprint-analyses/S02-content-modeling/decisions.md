# Sprint S02 — Decisions Log

## D-001: Use Orchard Core ContentType System as Foundation
- **Date**: 2026-03-09
- **Context**: Spec 4.1.2.1 requires designing new content types with different field types. We can build a custom content modeling engine or leverage Orchard Core's built-in `IContentDefinitionManager`.
- **Decision**: Use Orchard Core's content definition system as the foundation, wrapped by a custom `IContentTypeService` abstraction in `ProjectDora.Core`. The abstraction exposes type/part/field CRUD without leaking Orchard Core types.
- **Consequences**: Rapid delivery of content modeling features. The abstraction layer allows future migration away from Orchard Core if needed. All MediatR commands/queries use the abstraction, never `IContentDefinitionManager` directly.
- **ADR**: Consistent with ADR-001 (Modular Monolith on Orchard Core)

## D-002: YesSql Document Store for Schema-Free Content (4.1.2.5)
- **Date**: 2026-03-09
- **Context**: Spec 4.1.2.5 explicitly requires that content structures be stored without database schema changes, using an intermediate technology (JSON, XML, etc.). Options: custom EF Core JSON columns, MongoDB, YesSql.
- **Decision**: Use YesSql (Orchard Core's built-in document store). Content items are stored as JSON documents in a single `Document` table. No schema migration is needed when content types change.
- **Consequences**: Fully satisfies spec requirement. Admin panel can display raw JSON via a document viewer. Trade-off: complex queries require index tables (addressed in D-003).
- **ADR**: N/A

## D-003: YesSql Index Tables for SQL Queryability (4.1.2.6)
- **Date**: 2026-03-09
- **Context**: Spec 4.1.2.6 requires that content be accessible via SQL queries, stored in typed table structures (text columns, numeric columns, boolean columns, etc.).
- **Decision**: Use YesSql `MapIndex` classes to create typed index tables. Orchard Core provides `ContentItemIndex`, `UserPickerFieldIndex`, etc. We create custom indexes per field type as needed (e.g., `TextFieldIndex`, `NumericFieldIndex`, `BooleanFieldIndex`).
- **Consequences**: Enables SQL queries against strongly-typed columns while maintaining JSON document storage. Index tables are automatically maintained by YesSql on content save.
- **ADR**: N/A

## D-004: Orchard Core Field Type Mapping
- **Date**: 2026-03-09
- **Context**: Spec 4.1.2.2 lists required field types: metin (text), sayi (number), dogru/yanlis (boolean), dosya/gorsel (file/image), zengin metin (rich text). We need to map these to Orchard Core field types.
- **Decision**: Map as follows: TextField (metin), NumericField (sayi), BooleanField (dogru/yanlis), MediaField (dosya/gorsel), HtmlField (zengin metin). Additionally support DateField, TimeField, LinkField, ContentPickerField for extended use cases.
- **Consequences**: All required field types are covered by Orchard Core's built-in fields. No custom field development needed for S02.
- **ADR**: N/A

## D-005: Rich Text Editor Selection (4.1.2.3)
- **Date**: 2026-03-09
- **Context**: Spec requires a rich text editor with standard features (bold, italic, etc.). Orchard Core supports multiple editors.
- **Decision**: Use Orchard Core's HtmlField with the default editor (Trumbowyg for lightweight, or configure TinyMCE for full-featured). Ensure toolbar includes: bold, italic, underline, headings (H1-H6), ordered/unordered lists, links, image insert, table support.
- **Consequences**: No custom editor development. Editor configuration stored in content type settings per HtmlField instance.
- **ADR**: N/A

## D-006: AliasPart for Content Aliases (4.1.2.8)
- **Date**: 2026-03-09
- **Context**: Spec requires content items to be accessible via alias names (e.g., "logo", "site-title", "address") for easy retrieval in templates and API calls.
- **Decision**: Use Orchard Core's `AliasPart`. Attach it to content types that need alias support. Aliases are unique per tenant. Programmatic lookup via `IContentAliasManager`.
- **Consequences**: Simple implementation. Aliases can be used in Liquid templates: `{% content_item_id alias: "logo" %}`. API access: `GET /api/v1/content/alias/{aliasValue}`.
- **ADR**: N/A

## D-007: BagPart for List/Container Pattern (4.1.2.10)
- **Date**: 2026-03-09
- **Context**: Spec requires content types that can contain lists of other content types (e.g., a "SliderBanner" containing multiple "SlideItem" items).
- **Decision**: Use Orchard Core's `BagPart` — a content part that acts as a container for child content items of specified types. Alternatively, `ListPart` can be used for simpler list scenarios.
- **Consequences**: Enables hierarchical content structures. Admin UI automatically provides add/remove/reorder for child items. Works with any content type combination.
- **ADR**: N/A

## D-008: SEO and Search Index Configuration (4.1.2.11, 4.1.2.13)
- **Date**: 2026-03-09
- **Context**: Spec requires SEO keyword/meta support for page-type content (4.1.2.11) and configurable full-text search indexing per field (4.1.2.13).
- **Decision**: Use Orchard Core's `SeoMetaPart` for SEO meta tags (keywords, description, robots). For search indexing, use `LuceneContentIndexSettings` to configure which fields are indexed per content type. This is managed via the admin panel's content type editor.
- **Consequences**: SEO and search indexing are configuration-driven, not code-driven. Content editors can see SEO fields when editing page-type content. Admins can toggle field indexing without code changes.
- **ADR**: N/A

## D-009: Localization Key/Value Management (4.1.2.12)
- **Date**: 2026-03-09
- **Context**: Spec requires creating and managing localization key/value pairs either per content type or site-wide, for foreign language support.
- **Decision**: Implement a custom `LocalizationSet` content type with key/value pairs stored as content fields. Use Orchard Core's `LocalizationPart` for content-level localization. For site-wide strings, use PO file management via admin panel or a custom key/value editor backed by `IStringLocalizer`.
- **Consequences**: Two localization levels supported: content-level (per item, per culture) and string-level (key/value pairs for UI strings). Admin panel provides management UI for both.
- **ADR**: N/A
