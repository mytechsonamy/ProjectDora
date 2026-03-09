# Sprint S07 — Multi-language Support

## Kapsam (Scope)
- Spec items: 4.1.8.1, 4.1.3.4, 4.1.3.5, 4.1.3.8, 4.1.2.12
- Stories: US-801, US-802, US-803, US-804, US-805, US-806, US-807, US-808
- Cross-references: S03 (Content Management) established LocalizationPart and AutoroutePart foundations; S04 D-002 (Turkish Analyzer) deferred multi-language search indexing to this sprint; 4.1.10.14 (multi-tenant feature toggles for language activation); 4.1.4.3 (Liquid template localization); 4.1.12.3e/4.1.12.4l (AI chatbot Turkish language support depends on localization infrastructure)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.8.1 | Urunun sunulacak icerikler icin ayni anda birden fazla dilde yayin yapmaya imkan verir durumda olmasi ve hangi dillerin kullanilmak istendigi tercihinin dogrudan yonetim paneli uzerinden gerceklestirilabilmesi | Platform must support simultaneous publishing in multiple languages; language activation/deactivation managed via admin panel |
| 4.1.3.4 | Olusturulan tum iceriklerin coklu dil destegi sayesinde urun uzerinde aktive edilen farkli diller icin olan versiyonlarinin olusturulabilmesi | All content items must support creation of language variants for every activated language |
| 4.1.3.5 | Farkli diller icin olusturulan versiyonlarin bir iliskisel yaklasim ile birbirleri ile ilintili olmasi ve bu iliski yapisinin web sitesi on yuzunde bir kullanici deneyimi olarak sunulmasi | Language variants must be relationally linked; front-end language switcher navigates to the exact translated content page (not the homepage of that language) |
| 4.1.3.8 | Icerikler icin olusturulan erisim URL'lerinin farkli diller icin farkli olacak sekilde belirlenebilmesi | Content URLs must be different per language (localized slugs) |
| 4.1.2.12 | Olusturulan ozel icerik turu ozelinde veya tum site genelinde yabanci dil destegi kapsaminda kullanilmak uzere anahtar/deger (key/value) ikililerinin olusturulabilmesinin ve yonetilebilmesinin saglanmasi | Key/value pairs for foreign language support, manageable per content type or site-wide |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S02 (Content Modeling) and S03 (Content Management) must be complete -- `LocalizationPart`, `AutoroutePart`, and content versioning must exist
- **Orchard Core**: Multi-language is built on `OrchardCore.Localization`, `OrchardCore.ContentLocalization`, `OrchardCore.Localization.ContentLanguageHeader` modules
- **PO Files**: Orchard Core uses GNU gettext `.po` files for UI string localization via `IStringLocalizer<T>` and `IHtmlLocalizer<T>`
- **CulturePicker**: `OrchardCore.Localization.CulturePicker` provides the front-end language selector widget
- **Primary Language**: Turkish (`tr`) is the default culture; all other languages are secondary
- **RTL Consideration**: Arabic (`ar`) is the only potential RTL language; Orchard Core supports `dir="rtl"` via `CultureInfo`; this sprint adds foundational RTL CSS support but full RTL is deferred unless explicitly required
- **Multi-tenant**: Each tenant can activate a different set of supported languages
- **Search Indexes**: Per-culture Lucene/Elasticsearch indexes needed for correct stemming (deferred from S04 D-002)
- **URL Routing**: Culture-prefixed URL routing (`/tr/destek-programlari`, `/en/support-programs`) via `RequestLocalizationMiddleware`

### RBAC Gereksinimleri

| Permission | SuperAdmin | TenantAdmin | Editor | Author | Analyst | Denetci | SEO | Viewer |
|-----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Localization.ManageCultures | Y | Y | - | - | - | - | - | - |
| Localization.CreateTranslation | Y | Y | Y | Y | - | - | - | - |
| Localization.EditTranslation | Y | Y | Y | Y | - | - | - | - |
| Localization.DeleteTranslation | Y | Y | - | - | - | - | - | - |
| Localization.ManagePOFiles | Y | Y | - | - | - | - | - | - |
| Localization.ViewTranslations | Y | Y | Y | Y | Y | Y | Y | Y |

- `Localization.ManageCultures` -- Activate/deactivate supported languages for the tenant
- `Localization.CreateTranslation` -- Create new language variants of content items
- `Localization.EditTranslation` -- Edit existing language variant content
- `Localization.DeleteTranslation` -- Delete a language variant (does not affect other variants)
- `Localization.ManagePOFiles` -- Upload, edit, and manage PO translation files
- `Localization.ViewTranslations` -- View available translations and translation status

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-801 | 4.1.8.1 | P0 | Supported culture configuration (activate/deactivate languages via admin panel) |
| US-802 | 4.1.3.4 | P1 | Content localization with LocalizationPart (create language variants) |
| US-803 | 4.1.3.5 | P1 | CulturePicker widget and front-end language switcher |
| US-804 | 4.1.3.8 | P1 | Localized URL routing with culture-prefixed paths |
| US-805 | 4.1.2.12 | P1 | PO file management for UI string localization |
| US-806 | 4.1.8.1, 4.1.2.12 | P2 | Admin panel UI localization (Turkish default, extensible) |
| US-807 | 4.1.8.1 | P2 | RTL layout support foundation |
| US-808 | 4.1.8.1 | P1 | ILocalizationService abstraction and translation status tracking |

### Priority Rationale

- **P0 (Foundation)**: US-801 (culture configuration) -- all other stories depend on knowing which languages are activated
- **P1 (Core CRUD)**: US-802 (content localization), US-803 (CulturePicker), US-804 (localized URLs), US-805 (PO files), US-808 (abstraction + status) -- minimum viable multi-language
- **P2 (Supporting)**: US-806 (admin UI localization), US-807 (RTL) -- important but can be deferred

### Domain Model Entities

| Entity | Schema | Description |
|--------|--------|-------------|
| `SiteCultureSettings` | `orchard` (YesSql) | Tenant-level culture configuration: default culture, supported cultures |
| `ContentItem + LocalizationPart` | `orchard` (YesSql) | Content item with culture and localization set assignment |
| `POTranslationEntry` | `orchard` (YesSql) | PO file entry with msgid/msgstr per culture |
| `TranslationStatus` | `orchard` (YesSql) | Tracks translation coverage per content item and culture |

## Teknik Kararlar (Technical Decisions)

### D-001: ILocalizationService Abstraction Layer
- All localization operations go through `ILocalizationService` and `ICultureService` interfaces defined in `ProjectDora.Core`
- Wraps Orchard Core's `ILocalizationService`, `ISiteService` (for culture settings), and `IContentLocalizationManager`
- Enables unit testing and future backend swaps
- See `decisions.md` D-001 for full rationale

### D-002: Culture-Prefixed URL Strategy
- URLs follow the pattern `/{culture}/{slug}` -- e.g., `/tr/destek-programlari`, `/en/support-programs`
- Default culture (Turkish) can optionally omit the prefix: `/destek-programlari` = `/tr/destek-programlari`
- Implemented via ASP.NET Core `RequestLocalizationMiddleware` with `RouteDataRequestCultureProvider`
- See `decisions.md` D-002

### D-003: PO File Storage and Management
- PO files stored in tenant-specific paths: `App_Data/Localization/{tenantName}/{culture}.po`
- Admin panel provides a PO editor UI for managing translations without file system access
- Fallback chain: PO file for current culture -> PO file for culture parent (e.g., `tr-TR` -> `tr`) -> default string
- See `decisions.md` D-003

### D-004: Per-Culture Search Index Strategy
- Each activated culture gets its own Lucene index with the appropriate analyzer
- Turkish: `TurkishAnalyzer`, English: `StandardAnalyzer`, German: `GermanAnalyzer`, etc.
- Elasticsearch: culture-specific index names `{tenantId}_{culture}_content`
- See `decisions.md` D-004

### D-005: Translation Status Tracking
- Each content item tracks translation coverage: which cultures have been translated, which are pending
- Translation status computed from LocalizationSet membership: if a culture is activated but no variant exists, status = "Missing"
- Dashboard widget shows per-content-type translation coverage percentage
- See `decisions.md` D-005

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-801-01 | Unit | US-801 | Activate a new supported culture |
| TC-801-02 | Unit | US-801 | Deactivate a supported culture |
| TC-801-03 | Unit | US-801 | Set default culture for tenant |
| TC-801-04 | Security | US-801 | Viewer cannot manage cultures |
| TC-801-05 | Unit | US-801 | Cannot deactivate the default culture |
| TC-802-01 | Unit | US-802 | Create Turkish content with LocalizationPart |
| TC-802-02 | Unit | US-802 | Create English translation variant sharing LocalizationSet |
| TC-802-03 | Unit | US-802 | Reject duplicate translation for same culture |
| TC-802-04 | Integration | US-802 | List all translations for a content item |
| TC-802-05 | Security | US-802 | Viewer cannot create translations |
| TC-803-01 | Integration | US-803 | CulturePicker renders activated cultures |
| TC-803-02 | Integration | US-803 | Language switcher navigates to translated content (not homepage) |
| TC-803-03 | Unit | US-803 | CulturePicker hides cultures with no content translation |
| TC-803-04 | Integration | US-803 | Cookie persists selected culture across navigation |
| TC-804-01 | Unit | US-804 | Turkish content gets `/tr/` prefixed URL |
| TC-804-02 | Unit | US-804 | English content gets `/en/` prefixed URL |
| TC-804-03 | Unit | US-804 | Default culture URL prefix is optional |
| TC-804-04 | Integration | US-804 | Localized URL resolves to correct culture variant |
| TC-804-05 | Unit | US-804 | Turkish slug transliteration: "KOBİ Destek Programı" -> "kobi-destek-programi" |
| TC-805-01 | Unit | US-805 | Load PO file for Turkish culture |
| TC-805-02 | Unit | US-805 | IStringLocalizer returns translated string from PO file |
| TC-805-03 | Unit | US-805 | Fallback to default string when PO entry missing |
| TC-805-04 | Integration | US-805 | Upload PO file via admin panel |
| TC-805-05 | Unit | US-805 | PO pluralization rules for Turkish |
| TC-806-01 | Integration | US-806 | Admin panel renders in Turkish when culture=tr |
| TC-806-02 | Integration | US-806 | Admin panel renders in English when culture=en |
| TC-807-01 | Unit | US-807 | RTL CSS class applied when culture is Arabic |
| TC-807-02 | Unit | US-807 | LTR layout preserved for Turkish and English |
| TC-808-01 | Unit | US-808 | Translation status = "Missing" when no variant exists |
| TC-808-02 | Unit | US-808 | Translation status = "Draft" when variant is draft |
| TC-808-03 | Unit | US-808 | Translation status = "Published" when variant is published |
| TC-808-04 | Integration | US-808 | Translation coverage percentage calculated correctly |
| TC-S07-SEC-01 | Security | All | Tenant A cannot access Tenant B culture settings |
| TC-S07-SEC-02 | Security | All | Anonymous user cannot manage PO files |
| TC-S07-SEC-03 | Security | All | Author cannot delete translations |

### Coverage Target
- Unit test coverage: >= 80% for Localization module
- Integration test coverage: >= 60%
- Security tests: minimum 6 (RBAC on culture management, translation CRUD, PO management + tenant isolation)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-801 complete
- [ ] US-802 complete
- [ ] US-803 complete
- [ ] US-804 complete
- [ ] US-805 complete
- [ ] US-806 complete
- [ ] US-807 complete
- [ ] US-808 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Activating and deactivating supported languages from admin panel Settings > Localization
- Creating content translations: selecting a content item, clicking "Add Translation", choosing target language
- Using the CulturePicker widget on the front-end to switch between languages
- Understanding translation status indicators (Missing, Draft, Published) in the content list
- Managing PO files for UI string translations (admin panel or file upload)
- URL structure: how culture-prefixed URLs work (`/tr/...`, `/en/...`)
- RTL support: how to enable Arabic or other RTL languages

### Teknik Dokumantasyon (Technical Documentation)
- `ILocalizationService` and `ICultureService` abstraction interfaces and their Orchard Core mappings
- `LocalizationPart` configuration: Culture assignment, LocalizationSet linking, translation graph
- `CulturePicker` widget: shape template customization, cookie vs URL vs header culture detection
- `RequestLocalizationMiddleware` configuration: RouteDataRequestCultureProvider, CookieRequestCultureProvider
- `AutoroutePart` per-culture slug configuration and Turkish transliteration rules
- PO file format, storage location (`App_Data/Localization/`), fallback chain
- Per-culture search index configuration for Lucene and Elasticsearch
- Translation status tracking data model and dashboard widget
- MediatR command/query handlers for all localization operations

### API Endpoints
- `GET /api/v1/localization/cultures` -- List supported cultures for tenant
- `POST /api/v1/localization/cultures` -- Activate a new culture
- `DELETE /api/v1/localization/cultures/{culture}` -- Deactivate a culture
- `PUT /api/v1/localization/cultures/default` -- Set default culture
- `GET /api/v1/content/{contentType}/{contentItemId}/translations` -- List translations for a content item
- `POST /api/v1/content/{contentType}/{contentItemId}/translations` -- Create translation variant
- `GET /api/v1/localization/translations/status` -- Translation coverage dashboard data
- `GET /api/v1/localization/po/{culture}` -- Download PO file for culture
- `PUT /api/v1/localization/po/{culture}` -- Upload/update PO file for culture

### Configuration Parameters
- `Localization:DefaultCulture` -- Default culture code (default: `tr`)
- `Localization:SupportedCultures` -- Comma-separated list of supported culture codes
- `Localization:CulturePrefix:OmitDefault` -- Whether to omit culture prefix for default culture in URLs (default: `true`)
- `Localization:CulturePicker:CookieName` -- Cookie name for persisting culture selection (default: `.AspNetCore.Culture`)
- `Localization:CulturePicker:CookieExpiry` -- Cookie expiry in days (default: `365`)
- `Localization:PO:BasePath` -- Base path for PO file storage (default: `App_Data/Localization`)
- `Localization:Search:PerCultureIndex` -- Enable per-culture search indexes (default: `true`)
