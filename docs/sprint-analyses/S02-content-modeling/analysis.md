# Sprint S02 — Content Modeling

## Kapsam (Scope)
- Spec items: 4.1.2.1, 4.1.2.2, 4.1.2.3, 4.1.2.4, 4.1.2.5, 4.1.2.6, 4.1.2.7, 4.1.2.8, 4.1.2.9, 4.1.2.10, 4.1.2.11, 4.1.2.12, 4.1.2.13
- Stories: US-201, US-202, US-203, US-204, US-205, US-206, US-207, US-208, US-209, US-210, US-211
- Cross-references: S03 (Content Management), S04 (Theme Management, Query Engine) depend on content type definitions from S02

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Turkce Metin | English Summary |
|------|-------------|-----------------|
| 4.1.2.1 | Yonetim paneli uzerinden farkli tipte bilgi alanlarini icerecek sekilde icerikler olusturabilmek adina yeni turde icerik tasarlanmasina imkan vermesi | Design new content types with different field types via admin panel |
| 4.1.2.2 | Icerik tasarimi kapsaminda bir veri tabani tablo yonetimi mantiginda icerik turunun ve bu ture bagli cesitli farkli tipteki (metin, sayi, dogru/yanlis, dosya/gorsel, zengin metin vb.) bilgi alanlarinin olusturulabilmesinin saglanmasi | Create content type fields with various types (text, number, boolean, file/image, rich text, etc.) in a database table management logic |
| 4.1.2.3 | Urunun, zengin metin turundeki bilgi alanlarinin duzenlenmesi icin cesitli guncel populer metin editorlerinde yer alan temel ozelliklere sahip (bold, italic, vb.) zengin metin editorunu saglanmasi | Rich text editor with standard features (bold, italic, etc.) for rich text fields |
| 4.1.2.4 | Uretilen bu icerik turunun urun uzerindeki diger cesitli moduller tarafindan dogrudan erisilebilir ve kullanilabilir olmasi | Content types must be directly accessible and usable by other modules |
| 4.1.2.5 | Urun uzerinde uretilen farkli turdeki icerik yapilarinin veri tabani semasinda degisiklik gerceklestirmeyip veri tabani uzerinde herhangi bir araci teknoloji (JSON, XML, vb.) uzerinden saklanmasinin saglanmasi ve saklanan bu dosya yapisindaki icerigin dogrudan yonetim paneli uzerinden goruntulenebilinmesinin saglanmasi | Store content structures without schema changes using intermediate technology (JSON, XML, etc.) and allow viewing raw document data from admin panel |
| 4.1.2.6 | Farkli turdeki icerik yapilarinin SQL sorgulari ile kolayca erisilebilmesi amaciya kendi turlerine ait olacak sekilde (metin tipli olanlar, sayi tipli olanlar, dogru/yanlis tipli olanlar, vb.) veri tabani semasi uzerinde farkli tablo yapilarinda saklanmasinin saglanmasi | Store content in typed index tables (text, numeric, boolean, etc.) for SQL query accessibility |
| 4.1.2.7 | Urun uzerinde uretilen her turden icerik turune iliskin, o turden yonetim paneli uzerinde icerik olusturma ve duzenleme icin form yapilarinin otomatik olarak olusmasi ve bu form yapilarinin icerik turunde gerceklestirilecek degisikliklere yine otomatik olarak uyum saglamasi | Auto-generate create/edit forms for each content type that automatically adapt to content type changes |
| 4.1.2.8 | Tasarim yonetimi kapsaminda belirli icerik kayitlarina (logo, unvan, adres bilgileri, vb.) kolay erisimde kullanilabilmek adina olusturan herhangi bir icerigin bir takma ad (alias) ile dogrudan erisilebilir olmasinin saglanmasi | Content alias support for direct access to specific records (e.g., logo, title, address) |
| 4.1.2.9 | Olusturulan ozel icerik turlerine galeri ozelligi eklenebilmesi | Gallery feature attachable to custom content types |
| 4.1.2.10 | Olusturulan ozel icerik turlerinin diger ozel icerik turlerini icerebilecek sekilde liste yapida kullanilabilmesi | Content types can contain lists of other custom content types |
| 4.1.2.11 | Olusturulan ozel icerik turleri, eger web sitesinde yayinlanacak bir sayfa ozelligi kazandirilmissa arama motoru optimizasyonu kapsaminda yer almasi gereken anahtar kelime vb. yapilari destekler mahiyette olmasi | SEO support (keywords, meta) for content types published as web pages |
| 4.1.2.12 | Olusturulan ozel icerik turu ozelinde veya tum site genelinde yabanci dil destegi kapsaminda kullanilmak uzere anahtar/deger (key/value) ikililerin olusturulabilmesinin ve yonetilebilmesinin saglanmasi | Key/value pair management for localization at content-type or site-wide level |
| 4.1.2.13 | Olusturulan icerik turlerinde hangi ozel bilgi alanlarinin tam metin arama (full-text search) yetkinlikleri kapsaminda indekslenmesi gerektiginin belirtilebilir olmasi | Configurable full-text search indexing per field on content types |

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)
- **Dependency**: S01 complete (admin panel for UI host)
- **Orchard Core**: Content modeling is core to Orchard Core via `ContentTypeDefinition`, `ContentPartDefinition`, `ContentFieldDefinition`
- **YesSql**: Document store pattern — JSON storage with typed indexes (spec 4.1.2.5 + 4.1.2.6)
- **Multi-tenant**: Content type definitions are tenant-scoped
- **Abstraction**: All access through `IContentTypeService` (custom abstraction over `IContentDefinitionManager`)

### RBAC Gereksinimleri
- **SuperAdmin / TenantAdmin**: Full content modeling (create, edit, delete content types/fields/parts)
- **Editor**: View content types (to use them for content creation), no type modification
- **Author**: View content types (read-only)
- **Viewer / Anonymous**: No access to content type definitions

### Story Decomposition

| Story | Spec Refs | Priority | Description |
|-------|-----------|----------|-------------|
| US-201 | 4.1.2.1 | P1 | Create new content types with field definitions |
| US-202 | 4.1.2.2 | P1 | Add typed fields to content types (text, number, boolean, media, rich text) |
| US-203 | 4.1.2.3 | P2 | Rich text editor for rich text fields |
| US-204 | 4.1.2.4, 4.1.2.5 | P1 | Content types accessible by other modules, JSON document storage |
| US-205 | 4.1.2.6 | P1 | SQL-queryable typed index tables for content fields |
| US-206 | 4.1.2.7 | P1 | Auto-generated admin forms for content types |
| US-207 | 4.1.2.8 | P2 | Content alias support for direct record access |
| US-208 | 4.1.2.9 | P2 | Gallery part attachable to content types |
| US-209 | 4.1.2.10 | P2 | List mode — content types containing lists of other types |
| US-210 | 4.1.2.11, 4.1.2.13 | P2 | SEO meta part and full-text search indexing configuration |
| US-211 | 4.1.2.12 | P2 | Localization key/value pair management for content types |

## Teknik Kararlar (Technical Decisions)

### D-001: Orchard Core ContentType System as Foundation
- Orchard Core's `IContentDefinitionManager` provides content type, part, and field management
- Custom `IContentTypeService` abstraction wraps Orchard Core to isolate dependency
- Content types defined via admin UI or programmatically via Recipes

### D-002: YesSql Document Store for Content Storage (4.1.2.5)
- YesSql stores content items as JSON documents — no schema migration needed per content type
- Satisfies spec requirement: "veri tabani semasinda degisiklik gerceklestirmeyip"
- Admin panel can display raw JSON document via a custom viewer

### D-003: YesSql Index Tables for SQL Access (4.1.2.6)
- YesSql indexes (e.g., `ContentItemIndex`, `UserPickerFieldIndex`) provide typed SQL tables
- Custom `MapIndex` classes for each field type (text, numeric, boolean)
- Enables direct SQL querying as required by spec

### D-004: Orchard Core Field Types Mapping
- TextField -> metin (text)
- NumericField -> sayi (number)
- BooleanField -> dogru/yanlis (boolean)
- MediaField -> dosya/gorsel (file/image)
- HtmlField -> zengin metin (rich text)
- ContentPickerField -> icerik referansi (content reference)
- DateField, TimeField, LinkField as additional types

### D-005: Rich Text Editor (4.1.2.3)
- Use Orchard Core's built-in `HtmlField` with configurable editor (TinyMCE/Trumbowyg)
- Bold, italic, lists, headings, links, images — standard toolbar

### D-006: AliasPart for Content Aliases (4.1.2.8)
- Use Orchard Core's `AliasPart` — assigns a string alias to any content item
- Enables direct access: `/alias/{aliasValue}` or programmatic lookup

### D-007: BagPart for List/Container Pattern (4.1.2.10)
- Use Orchard Core's `BagPart` — a content part that contains a list of content items
- Enables content types containing lists of other content types

## Test Plani (Test Plan)

### New Test Cases
| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-201-01 | Unit | US-201 | Create content type with valid name |
| TC-201-02 | Unit | US-201 | Reject duplicate content type name within tenant |
| TC-201-03 | Security | US-201 | Unauthorized user cannot create content type |
| TC-201-04 | Integration | US-201 | Content type persists in YesSql and is retrievable |
| TC-202-01 | Unit | US-202 | Add TextField to content type |
| TC-202-02 | Unit | US-202 | Add NumericField to content type |
| TC-202-03 | Unit | US-202 | Add BooleanField to content type |
| TC-202-04 | Unit | US-202 | Add MediaField to content type |
| TC-202-05 | Unit | US-202 | Add HtmlField (rich text) to content type |
| TC-202-06 | Unit | US-202 | Reject adding field with duplicate name |
| TC-203-01 | Unit | US-203 | Rich text editor renders with standard toolbar |
| TC-203-02 | Integration | US-203 | Rich text content round-trips correctly (save/load) |
| TC-204-01 | Unit | US-204 | Content type accessible from other module context |
| TC-204-02 | Unit | US-204 | JSON document viewable in admin panel |
| TC-205-01 | Integration | US-205 | ContentItemIndex populated for new content |
| TC-205-02 | Integration | US-205 | Custom field index queryable via SQL |
| TC-206-01 | Integration | US-206 | Auto-generated form matches content type definition |
| TC-206-02 | Integration | US-206 | Form updates when field added to content type |
| TC-207-01 | Unit | US-207 | Alias assigned to content item |
| TC-207-02 | Unit | US-207 | Content item retrievable by alias |
| TC-207-03 | Unit | US-207 | Duplicate alias rejected |
| TC-208-01 | Unit | US-208 | Gallery part added to content type |
| TC-208-02 | Integration | US-208 | Gallery stores multiple media references |
| TC-209-01 | Unit | US-209 | BagPart added to content type |
| TC-209-02 | Integration | US-209 | Child content items managed within bag |
| TC-210-01 | Unit | US-210 | SEO meta fields added to content type |
| TC-210-02 | Unit | US-210 | Full-text index configuration per field |
| TC-211-01 | Unit | US-211 | Localization key/value pairs created |
| TC-211-02 | Unit | US-211 | Key/value pairs scoped to content type |
| TC-211-03 | Integration | US-211 | Site-wide localization keys retrievable |

### Coverage Target
- Unit test coverage: >= 80% for ContentModeling module
- Integration test coverage: >= 60%
- Security tests: minimum 4 (RBAC on create, update, delete content type + tenant isolation)

## Sprint Sonucu (Sprint Outcome)
- [ ] US-201 complete
- [ ] US-202 complete
- [ ] US-203 complete
- [ ] US-204 complete
- [ ] US-205 complete
- [ ] US-206 complete
- [ ] US-207 complete
- [ ] US-208 complete
- [ ] US-209 complete
- [ ] US-210 complete
- [ ] US-211 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Content type designer: How to create a new content type from admin panel
- Field types available: text, number, boolean, file/image, rich text, date, link, content picker
- Rich text editor: Toolbar features (bold, italic, headings, lists, links, images)
- Alias assignment: How to assign and use aliases for quick content access
- Gallery: How to attach gallery part and manage images
- List/bag: How to create container types that hold lists of other content types
- SEO settings: How to configure meta keywords and descriptions on page-type content
- Localization keys: How to manage translation key/value pairs
- Search indexing: How to select which fields are full-text indexed

### Teknik Dokumantasyon (Technical Documentation)
- `IContentTypeService` abstraction interface and its Orchard Core implementation
- `IContentDefinitionManager` usage patterns for type/part/field CRUD
- YesSql document store: How content is stored as JSON, index table generation
- Custom `MapIndex` implementations for typed SQL access
- `AliasPart`, `BagPart`, `SeoMetaPart` configuration
- `HtmlField` editor configuration (TinyMCE settings)
- Localization service: `ILocalizationService` for key/value management
- Full-text search index configuration: `LuceneContentIndexSettings`

### API Endpoints
- `GET /api/v1/content-types` — List all content types
- `POST /api/v1/content-types` — Create a new content type
- `GET /api/v1/content-types/{typeName}` — Get content type definition
- `PUT /api/v1/content-types/{typeName}` — Update content type definition
- `DELETE /api/v1/content-types/{typeName}` — Delete content type
