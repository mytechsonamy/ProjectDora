# Sprint S07 — Decisions Log

## D-001: ILocalizationService Abstraction Over Orchard Core Localization
- **Date**: 2026-03-09
- **Context**: Orchard Core provides `OrchardCore.Localization` and `OrchardCore.ContentLocalization` modules with built-in culture management, PO file loading, and content localization. We need to decide whether to use these directly or wrap them in an abstraction layer.
- **Decision**: Wrap all localization operations behind `ILocalizationService` and `ICultureService` interfaces defined in `ProjectDora.Core`. `ILocalizationService` handles content translation operations (create variant, list translations, get translation status). `ICultureService` handles tenant-level culture configuration (activate, deactivate, set default). The Orchard Core implementation lives in `ProjectDora.Modules.Localization`.
- **Consequences**: Additional adapter code, but consistent with the modular monolith abstraction pattern (ADR-001). Enables future swaps (e.g., if Orchard Core localization modules change in a major version). All other modules reference interfaces, not Orchard Core directly. Content Management module (S03) already uses `LocalizationPart` -- this sprint formalizes the abstraction.
- **ADR**: ADR-001 (Modular Monolith)

## D-002: Culture-Prefixed URL Routing Strategy
- **Date**: 2026-03-09
- **Context**: Spec 4.1.3.8 requires per-language URLs. We need to decide between (a) culture as URL prefix (`/tr/sayfa`, `/en/page`), (b) culture as subdomain (`tr.kosgeb.gov.tr`, `en.kosgeb.gov.tr`), or (c) culture as query parameter (`?culture=tr`).
- **Decision**: Use culture as URL prefix. Default culture (Turkish) prefix is optional -- `/destek-programlari` is equivalent to `/tr/destek-programlari`. Non-default cultures always include prefix: `/en/support-programs`. Implemented via ASP.NET Core `RequestLocalizationMiddleware` with `RouteDataRequestCultureProvider` for URL-based detection, plus `CookieRequestCultureProvider` for persistence across navigation. The culture detection order: (1) URL route data, (2) cookie, (3) Accept-Language header, (4) default culture.
- **Consequences**: SEO-friendly -- each language variant has a unique, crawlable URL. Requires `AutoroutePart` per-culture path support (already designed in S03 D-005). Optional prefix for default culture avoids breaking existing Turkish URLs. Subdomain approach rejected because it requires DNS configuration per language and complicates multi-tenant deployment.
- **ADR**: N/A

## D-003: PO File Storage and Fallback Strategy
- **Date**: 2026-03-09
- **Context**: Orchard Core uses GNU gettext PO files for UI string localization. We need to decide storage location, management approach, and fallback behavior.
- **Decision**: PO files stored at `App_Data/Localization/{tenantName}/{culture}.po` (tenant-isolated). Admin panel provides a PO editor UI with key-value grid for managing translations without direct file system access. PO file upload/download via API for bulk operations. Fallback chain: (1) Tenant-specific PO for exact culture (e.g., `tr-TR`), (2) Tenant-specific PO for parent culture (e.g., `tr`), (3) Module-bundled PO files (Orchard Core defaults), (4) Original string (msgid as-is). Turkish PO files ship with the platform as default; other languages start empty and are populated by translators.
- **Consequences**: Tenant isolation preserved -- each tenant can have different translations for the same UI strings. Admin PO editor reduces dependency on technical translators who know PO format. Bulk upload/download via API enables integration with external translation management systems (e.g., Crowdin, Transifex). File-based storage is simple but requires backup strategy for PO files in the `App_Data` directory.
- **ADR**: N/A

## D-004: Per-Culture Search Index Strategy
- **Date**: 2026-03-09
- **Context**: S04 D-002 established TurkishAnalyzer as the default for all search indexes and deferred multi-language index strategy to this sprint. Different languages require different analyzers for correct stemming, tokenization, and stop words.
- **Decision**: Create separate Lucene indexes per activated culture. Each index uses the appropriate language-specific analyzer: Turkish -> `TurkishAnalyzer`, English -> `StandardAnalyzer`, German -> `GermanAnalyzer`, Arabic -> `ArabicAnalyzer`, etc. Index naming convention: `{tenantId}_{culture}_{contentType}` (e.g., `tenant1_tr_Duyuru`, `tenant1_en_Duyuru`). Elasticsearch follows the same pattern with culture-specific analyzer mappings. When a content item is published, it is indexed into the culture-specific index based on its `LocalizationPart.Culture`. Cross-culture search (searching all languages simultaneously) uses a multi-index query.
- **Consequences**: Index count multiplied by number of active cultures. Storage increase is manageable since most content will be in Turkish only. Correct stemming per language (critical -- S04 proved Turkish stemming fails with English analyzer). Index rebuild must iterate over all culture-specific indexes. Cross-culture search is more expensive but rarely needed.
- **ADR**: N/A

## D-005: Translation Status Tracking Model
- **Date**: 2026-03-09
- **Context**: Content editors need visibility into which content items have been translated and which are pending. The spec requires multi-language publishing; without status tracking, translation gaps would be invisible.
- **Decision**: Implement a `TranslationStatus` tracking model computed from `LocalizationPart` data. For each content item in the default culture, query its `LocalizationSet` to determine which activated cultures have variants. Status per culture: `Missing` (no variant exists), `Draft` (variant exists but unpublished), `Published` (variant published), `Outdated` (source content updated after translation was last published). A dashboard widget on the admin panel shows per-content-type translation coverage as a percentage matrix (content types x cultures). The translation status is computed on-demand (not persisted) to avoid sync issues, with Redis caching (5-minute TTL).
- **Consequences**: On-demand computation may be slow for tenants with thousands of content items. Mitigated by Redis caching and pagination. `Outdated` detection requires comparing `ModifiedUtc` timestamps between source and translation -- adds a join query. The dashboard provides actionable data for translation project management.
- **ADR**: N/A

## D-006: RTL Support as CSS Foundation Only
- **Date**: 2026-03-09
- **Context**: Spec 4.1.8 mentions RTL (right-to-left) support consideration. Arabic is the primary RTL language that might be relevant for KOSGEB (potential Arabic-speaking SME stakeholders in border regions).
- **Decision**: Implement foundational RTL support only: (1) Detect RTL cultures via `CultureInfo.TextInfo.IsRightToLeft`. (2) Add `dir="rtl"` attribute to HTML element when RTL culture is active. (3) Include a base RTL CSS stylesheet (`rtl.css`) that mirrors key layout properties using CSS logical properties (`margin-inline-start` instead of `margin-left`). (4) Use Liquid template conditional: `{% if Culture.IsRtl %}...{% endif %}`. Full RTL visual polish (pixel-perfect RTL layouts, RTL icon mirroring, complex form layouts) is deferred to a future sprint unless explicitly required by KOSGEB.
- **Consequences**: Minimal development effort for RTL foundation. Arabic or Hebrew languages can be activated and will have basic RTL layout support. Full RTL polishing requires dedicated design review. CSS logical properties are modern (supported by all target browsers) and provide automatic LTR/RTL switching.
- **ADR**: N/A

## D-007: CulturePicker Implementation Strategy
- **Date**: 2026-03-09
- **Context**: Spec 4.1.3.5 requires a front-end language switcher that navigates to the same content in the target language (not the homepage). Orchard Core provides `CulturePicker` as a widget/shape.
- **Decision**: Use Orchard Core's `CulturePickerShape` as the foundation. Customize the Liquid template to: (1) Show only cultures that have a published translation for the current content item. (2) For cultures without a translation, show the link as disabled with a tooltip "Ceviri bulunamadi" (Translation not found). (3) Persist selected culture in a cookie (`.AspNetCore.Culture`). (4) On click, redirect to the translated content's URL (not the homepage of that language) using `LocalizationSet` to find the target variant. (5) If the user is on a non-content page (e.g., search results), switch culture via cookie and reload the same page.
- **Consequences**: Requires querying `LocalizationSet` on every page render to determine available translations. Mitigated by caching translation availability per content item (WriteThrough cache, 10-minute TTL). The "disabled link" UX is more informative than hiding untranslated languages. Cookie persistence ensures culture selection is maintained across navigation.
- **ADR**: N/A
