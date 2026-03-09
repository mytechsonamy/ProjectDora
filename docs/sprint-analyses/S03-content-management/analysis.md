# Sprint S03 — Content Management

## Kapsam (Scope)
- Spec items: 4.1.3.1, 4.1.3.2, 4.1.3.3, 4.1.3.4, 4.1.3.5, 4.1.3.6, 4.1.3.7, 4.1.3.8
- Stories: US-301, US-302, US-303, US-304, US-305, US-306, US-307, US-308
- Cross-references: S04 (Theme Management) depends on published content; S05 (Query Management) queries content items; S07 (Audit Logs) tracks content changes; S08 (Multi-language) extends localization features started here

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.3.1 | Urun tarafindan hazir olarak sunulan veya yetkili kullanicilar tarafindan ozel olarak modellenen farkli tipteki icerikler icin ekleme, duzenleme, silme ve icerik kopyasini olusturma ozelliklerinin sunulmasi | CRUD operations (create, edit, delete, clone) for content items of any content type — both built-in and custom-modeled |
| 4.1.3.2 | Olusturulan iceriklerin web site on yuzunde gosteriminden once iceriklerin bulunabilecegi bir taslak icerik statusunun bulunmasi | Draft status must exist before content is visible on the front-end website |
| 4.1.3.3 | Yonetim panelinin tum icerikler icin yayina alma, yayindan kaldirma ve yayin oncesi onizleme yapma kabiliyetlerine sahip olmasi | Admin panel must support publish, unpublish, and pre-publish preview for all content |
| 4.1.3.4 | Olusturulan tum iceriklerin coklu dil destegi sayesinde urun uzerinde aktive edilen farkli diller icin olan versiyonlarinin olusturulabilmesi | Multi-language content versions — create language variants for all activated languages |
| 4.1.3.5 | Farkli diller icin olusturulan versiyonlarin bir iliskisel yaklasim ile birbirleri ile ilintili olmasi ve bu iliski yapisinin web sitesi on yuzunde bir kullanici deneyimi olarak sunulmasi (Orn; belirli bir icerige ait detay sayfayi ziyaret eden bir kullanicinin ayni sayfanin farkli dildeki versiyonuna erismek icin ilgili dil secici kisayoluna tikladigi zaman o dile ait ana sayfaya gitmeyip varsa dogrudan o belirli icerigin ilgili dildeki detay sayfasina erisimin saglanmasi) | Language variants must be relationally linked; the front-end language switcher must navigate directly to the same content's translation (not the home page of that language) |
| 4.1.3.6 | Icerik yonetimi kapsaminda yapilan her turden icerik degisikliginin icerigin kendisini aslinda degistirmeyip yeni bir versiyon olarak uretilmesinin ve eski versiyonlarin sistemde arsivlenmesinin saglanmasi | Immutable versioning — every content change creates a new version; old versions are archived in the system |
| 4.1.3.7 | Olusturulan iceriklerden, web sitesi uzerinde yayinlanmasi arzu edilenlere, belirli desene gore erisim adresi (URL) olusturulabilmesine ve tercihen bu URL'lerin ozgun bir sekilde degistirilebilmesine imkan vermesi | SEO-friendly URL generation based on patterns, with ability to customize URLs uniquely |
| 4.1.3.8 | Icerikler icin olusturulan erisim URL'lerinin farkli diller icin farkli olacak sekilde belirlenebilmesi | Content URLs can be different per language (localized slugs) |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)
- **Dependency**: S02 complete (content types must be defined before content items can be created)
- **Orchard Core**: Content management operations use `IContentManager` (wrapped by `IContentService` abstraction)
- **YesSql**: Content items stored as JSON documents, queried via `ContentItemIndex`
- **Versioning**: Orchard Core's built-in `Versionable()` trait on content types — each save creates a new version record
- **Draft/Publish**: Orchard Core's `Draftable()` trait — items start as Draft, explicit Publish action needed
- **Localization**: `LocalizationPart` links content items in a `LocalizationSet` — shared set ID across language variants
- **SEO URLs**: `AutoroutePart` generates slugs from Liquid patterns; supports per-culture paths
- **Multi-tenant**: All content items are tenant-scoped; `ContentItemIndex` includes tenant filter

### RBAC Gereksinimleri

| Permission | SuperAdmin | TenantAdmin | Editor | Author | Analyst | Denetci | SEO | Viewer |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| ContentManagement.Create | Y | Y | Y | Y | - | - | - | - |
| ContentManagement.EditOwn | Y | Y | Y | Y | - | - | - | - |
| ContentManagement.EditAll | Y | Y | Y | - | - | - | - | - |
| ContentManagement.Publish | Y | Y | Y | - | - | - | - | - |
| ContentManagement.Delete | Y | Y | - | - | - | - | - | - |
| ContentManagement.ViewDraft | Y | Y | Y | Y | - | - | - | - |
| ContentManagement.ViewPublished | Y | Y | Y | Y | Y | Y | Y | Y |

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-301 | 4.1.3.1 | P1 | Content item CRUD (create, read, update, delete, clone) |
| US-302 | 4.1.3.2, 4.1.3.3 | P1 | Draft/Publish/Unpublish lifecycle with preview |
| US-303 | 4.1.3.6 | P1 | Immutable content versioning with version history |
| US-304 | 4.1.3.4 | P2 | Multi-language content variant creation |
| US-305 | 4.1.3.5 | P2 | Linked language variants with front-end language switcher |
| US-306 | 4.1.3.7 | P2 | SEO-friendly URL generation (AutoroutePart) |
| US-307 | 4.1.3.8 | P2 | Per-language URL paths (localized slugs) |
| US-308 | 4.1.3.1 | P0 | RBAC enforcement on content operations (security) |

## Teknik Kararlar (Technical Decisions)

### D-001: Orchard Core ContentManager as Foundation
- All content CRUD goes through `IContentManager` internally, exposed via `IContentService` abstraction
- `CreateAsync()`, `UpdateAsync()`, `PublishAsync()`, `UnpublishAsync()`, `RemoveAsync()` map directly to Orchard Core API
- `VersionOptions.DraftRequired` enforced for all edits to ensure draft-first workflow
- Content item clone implemented as: Get latest version -> Create new item with same parts/fields data

### D-002: ContentItem State Machine
- States: **Draft** -> **Published** -> **Archived** (+ **Scheduled**)
- Transitions:
  - Create -> Draft (default)
  - Publish -> Published (requires `ContentManagement.Publish` permission)
  - Unpublish -> Draft (reverts published content)
  - Archive -> Archived (soft removal from public)
  - Restore -> Draft (from Archived)
  - Schedule -> Scheduled (auto-publishes at `ScheduledPublishUtc`)
- Orchard Core tracks `Published` and `Latest` boolean flags on `ContentItemIndex`

### D-003: Immutable Versioning (4.1.3.6)
- Orchard Core's `Versionable()` trait creates a new `ContentItem` row per version
- `ContentItem.Number` increments; `Latest=true` on newest, `Published=true` on live version
- Version history retrieved via `IContentService.GetVersionsAsync(contentItemId)`
- Old versions remain in YesSql — no physical deletion on edit

### D-004: Localization via LocalizationPart (4.1.3.4, 4.1.3.5)
- `LocalizationPart` assigns a `Culture` (BCP 47) and a `LocalizationSet` (shared ID across translations)
- Creating a translation: clone content item, assign new culture, same localization set
- Front-end language switcher queries by localization set + target culture
- If translation does not exist, graceful fallback (show link disabled or redirect to default culture)

### D-005: AutoroutePart for SEO URLs (4.1.3.7, 4.1.3.8)
- `AutoroutePart` with Liquid pattern: `{{ Model.ContentItem | display_text | slugify }}`
- Turkish character transliteration: s -> s, c -> c, g -> g, i -> i, o -> o, u -> u, I -> i
- Custom path override: `AllowCustomPath = true`
- Per-culture URLs: `AutoroutePart` path is stored per content item; each language variant has its own slug
- Example: TR: `/destek-programlari/kobi-teknoloji-destegi`, EN: `/support-programs/sme-technology-support`

### D-006: Preview Mechanism (4.1.3.3)
- Orchard Core provides built-in preview via `OrchardCore.ContentPreview` module
- Preview renders the draft version using the active theme's templates
- Preview URL: `/admin/preview/{contentItemId}` (admin-only, requires `ContentManagement.ViewDraft`)
- No public preview URLs — preview is admin panel only

### Abstraction Layer
- Primary interface: `IContentService`
- Methods used:
  - `CreateAsync(contentType, command)` -> US-301
  - `GetAsync(contentItemId, version?)` -> US-301, US-303
  - `ListAsync(contentType, query)` -> US-301
  - `UpdateAsync(contentItemId, command)` -> US-301
  - `PublishAsync(contentItemId)` -> US-302
  - `UnpublishAsync(contentItemId)` -> US-302
  - `DeleteAsync(contentItemId, hard?)` -> US-301
  - `RollbackAsync(contentItemId, targetVersion)` -> US-303
  - `GetVersionsAsync(contentItemId)` -> US-303

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-301-01 | Unit | US-301 | Create content item with valid data |
| TC-301-02 | Unit | US-301 | Update content item (own) |
| TC-301-03 | Unit | US-301 | Delete content item (soft delete) |
| TC-301-04 | Unit | US-301 | Clone content item creates new item with same data |
| TC-301-05 | Integration | US-301 | Content item persisted and retrievable via API |
| TC-301-06 | Unit | US-301 | List content items with pagination and filtering |
| TC-302-01 | Unit | US-302 | Create content item defaults to Draft status |
| TC-302-02 | Unit | US-302 | Publish transitions Draft to Published |
| TC-302-03 | Unit | US-302 | Unpublish transitions Published to Draft |
| TC-302-04 | Integration | US-302 | Preview renders draft content |
| TC-302-05 | Unit | US-302 | Scheduled publish sets status to Scheduled |
| TC-303-01 | Unit | US-303 | Edit creates new version, does not modify existing |
| TC-303-02 | Unit | US-303 | Version history returns all versions ordered |
| TC-303-03 | Unit | US-303 | Rollback creates new version from target version data |
| TC-303-04 | Integration | US-303 | Old versions remain accessible by version number |
| TC-304-01 | Unit | US-304 | Create language variant with new culture |
| TC-304-02 | Unit | US-304 | Language variant shares localization set ID |
| TC-304-03 | Integration | US-304 | List available translations for a content item |
| TC-305-01 | Integration | US-305 | Language switcher links to correct translation |
| TC-305-02 | Unit | US-305 | Missing translation returns graceful fallback |
| TC-306-01 | Unit | US-306 | AutoroutePart generates slug from display text |
| TC-306-02 | Unit | US-306 | Turkish characters transliterated in slug |
| TC-306-03 | Unit | US-306 | Custom URL path override works |
| TC-306-04 | Unit | US-306 | Duplicate slug gets suffix appended |
| TC-307-01 | Unit | US-307 | Different URL per language variant |
| TC-307-02 | Integration | US-307 | Localized URL resolves to correct content |
| TC-308-01 | Security | US-308 | Anonymous user cannot create content |
| TC-308-02 | Security | US-308 | Viewer role cannot create content |
| TC-308-03 | Security | US-308 | Author cannot edit others' content |
| TC-308-04 | Security | US-308 | Author cannot publish content |
| TC-308-05 | Security | US-308 | Editor cannot delete content |
| TC-308-06 | Security | US-308 | Tenant A cannot access Tenant B content |

### Coverage Target
- Unit test coverage: >= 80% for ContentManagement operations in ContentModeling module
- Integration test coverage: >= 60%
- Security tests: minimum 6 (RBAC on create, editOwn, editAll, publish, delete + tenant isolation)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-301 complete
- [ ] US-302 complete
- [ ] US-303 complete
- [ ] US-304 complete
- [ ] US-305 complete
- [ ] US-306 complete
- [ ] US-307 complete
- [ ] US-308 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Content item CRUD: How to create, edit, delete, and clone content items from the admin panel
- Draft vs Published: Explanation of the draft-first workflow, how to preview before publishing
- Publishing: How to publish, unpublish, and schedule future publication
- Versioning: How to view version history, compare versions, and rollback to a previous version
- Multi-language: How to create translations for content, how the language switcher works on the front-end
- SEO URLs: How URL patterns work, how to customize a content item's URL, how localized URLs differ per language

### Teknik Dokumantasyon (Technical Documentation)
- `IContentService` abstraction interface — all content CRUD methods and their Orchard Core mapping
- ContentItem state machine: Draft -> Published -> Archived (+ Scheduled) with transition rules
- `VersionOptions` usage: `Draft`, `Published`, `Latest`, `Number(n)` — when to use each
- `LocalizationPart` configuration: Culture assignment, LocalizationSet linking
- `AutoroutePart` pattern configuration: Liquid slug templates, Turkish transliteration rules
- `PublishLaterPart` for scheduled publishing: `ScheduledPublishUtc` field, background task
- Content preview: `OrchardCore.ContentPreview` module configuration
- MediatR command/query pipeline for content operations

### API Endpoints
- `POST /api/v1/content/{contentType}` — Create content item
- `GET /api/v1/content/{contentType}` — List content items (with filtering, pagination, sorting)
- `GET /api/v1/content/{contentType}/{contentItemId}` — Get content item (optionally by version)
- `PUT /api/v1/content/{contentType}/{contentItemId}` — Update content item
- `DELETE /api/v1/content/{contentType}/{contentItemId}` — Delete content item (soft/hard)
- `POST /api/v1/content/{contentType}/{contentItemId}/publish` — Publish (optionally scheduled)
- `POST /api/v1/content/{contentType}/{contentItemId}/unpublish` — Unpublish
- `GET /api/v1/content/{contentType}/{contentItemId}/versions` — List version history
- `POST /api/v1/content/{contentType}/{contentItemId}/rollback` — Rollback to specific version
