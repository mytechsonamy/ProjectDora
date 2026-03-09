# Skill: Orchard Core CMS

> Target agents: Developer, Test Architect, Architect

## 1. Content Model Hierarchy

```
ContentItem (document in YesSql)
 ├── ContentType (schema definition)
 │    ├── ContentPart[] (reusable building blocks)
 │    └── ContentField[] (type-specific fields)
 ├── TitlePart.Title
 ├── BodyPart.Body (HTML)
 ├── CommonPart (Owner, CreatedUtc, ModifiedUtc)
 ├── AutoroutePart (SEO slug)
 ├── LocalizationPart (culture, localization set)
 └── AuditTrailPart (version tracking)
```

**Key rule**: ContentItem is a JSON document stored in YesSql. It is NOT a relational row. Parts and fields are nested JSON within the document.

## 2. YesSql Document Store

YesSql is **not** an ORM. It stores .NET objects as JSON documents and uses index tables for querying.

### How it works

```csharp
// Store a document
session.Save(contentItem);

// Query via index
var items = await session
    .Query<ContentItem, ContentItemIndex>(x => x.ContentType == "Duyuru")
    .ListAsync();
```

### Index tables

```csharp
// Index definition — maps document fields to queryable columns
public class ContentItemIndex : MapIndex
{
    public string ContentItemId { get; set; }
    public string ContentType { get; set; }
    public string DisplayText { get; set; }
    public bool Published { get; set; }
    public bool Latest { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public string Owner { get; set; }
}

// Index provider — how to map document to index
public class ContentItemIndexProvider : IndexProvider<ContentItem>
{
    public override void Describe(DescribeContext<ContentItem> context)
    {
        context.For<ContentItemIndex>()
            .Map(ci => new ContentItemIndex
            {
                ContentItemId = ci.ContentItemId,
                ContentType = ci.ContentType,
                DisplayText = ci.DisplayText,
                Published = ci.Published,
                Latest = ci.Latest,
                CreatedUtc = ci.CreatedUtc,
                Owner = ci.Owner
            });
    }
}
```

**Anti-pattern**: Never query YesSql documents by deserializing all documents and filtering in memory. Always use indexes.

## 3. Content Type Definition

### Creating a content type (via Migration)

```csharp
public class Migrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public Migrations(IContentDefinitionManager cdm)
    {
        _contentDefinitionManager = cdm;
    }

    public int Create()
    {
        _contentDefinitionManager.AlterTypeDefinition("DestekProgrami", type => type
            .DisplayedAs("Destek Programı")
            .Creatable()
            .Listable()
            .Draftable()
            .Versionable()
            .WithPart("TitlePart", part => part
                .WithPosition("1"))
            .WithPart("BodyPart", part => part
                .WithPosition("2")
                .WithSettings(new BodyPartSettings { ContentType = "Html" }))
            .WithPart("AutoroutePart", part => part
                .WithPosition("3")
                .WithSettings(new AutoroutePartSettings
                {
                    Pattern = "{{ Model.ContentItem | display_text | slugify }}",
                    AllowCustomPath = true
                }))
            .WithPart("CommonPart")
            .WithPart("LocalizationPart")
            .WithPart("AuditTrailPart")
        );

        return 1;
    }
}
```

### Creating a content type (via Recipe JSON)

```json
{
  "steps": [
    {
      "name": "ContentDefinition",
      "ContentTypes": [
        {
          "Name": "DestekProgrami",
          "DisplayName": "Destek Programı",
          "Settings": {
            "ContentTypeSettings": {
              "Creatable": true,
              "Listable": true,
              "Draftable": true,
              "Versionable": true
            }
          },
          "ContentTypePartDefinitionRecords": [
            { "PartName": "TitlePart", "Position": "1" },
            { "PartName": "BodyPart", "Position": "2" },
            { "PartName": "AutoroutePart", "Position": "3" },
            { "PartName": "CommonPart" },
            { "PartName": "LocalizationPart" },
            { "PartName": "AuditTrailPart" }
          ]
        }
      ]
    }
  ]
}
```

## 4. ContentManager API

**Important**: In ProjectDora, we wrap `IContentManager` behind `IContentService` (abstraction layer). Agents should generate code against `IContentService`, not `IContentManager` directly.

### Orchard Core's IContentManager (reference only)

```csharp
// Create
var item = await _contentManager.NewAsync("DestekProgrami");
item.DisplayText = "KOBİ Teknoloji Desteği";
item.Alter<TitlePart>(p => p.Title = "KOBİ Teknoloji Desteği");
item.Alter<BodyPart>(p => p.Body = "<p>Program detayları...</p>");
await _contentManager.CreateAsync(item, VersionOptions.Draft);

// Publish
await _contentManager.PublishAsync(item);

// Get
var item = await _contentManager.GetAsync(contentItemId, VersionOptions.Latest);

// Query (via YesSql session)
var items = await _session
    .Query<ContentItem, ContentItemIndex>(x =>
        x.ContentType == "DestekProgrami" && x.Published)
    .OrderByDescending(x => x.CreatedUtc)
    .Take(10)
    .ListAsync();

// Version
var item = await _contentManager.GetAsync(contentItemId, VersionOptions.Number(2));

// Delete
await _contentManager.RemoveAsync(item);
```

### ProjectDora's IContentService (what agents use)

```csharp
public interface IContentService
{
    Task<ContentItemDto> CreateAsync(string contentType, CreateContentItemCommand command);
    Task<ContentItemDto> GetAsync(string contentItemId, int? version = null);
    Task<PagedResult<ContentItemDto>> ListAsync(string contentType, ContentListQuery query);
    Task<ContentItemDto> UpdateAsync(string contentItemId, UpdateContentItemCommand command);
    Task PublishAsync(string contentItemId);
    Task UnpublishAsync(string contentItemId);
    Task DeleteAsync(string contentItemId, bool hard = false);
    Task<ContentItemDto> RollbackAsync(string contentItemId, int targetVersion);
    Task<IReadOnlyList<ContentVersionDto>> GetVersionsAsync(string contentItemId);
}
```

## 5. Parts Reference

| Part | Purpose | Key Properties |
|------|---------|---------------|
| `TitlePart` | Display title | `Title` (string) |
| `BodyPart` | Rich text content | `Body` (HTML string) |
| `CommonPart` | Metadata | `Owner`, `CreatedUtc`, `ModifiedUtc`, `PublishedUtc` |
| `AutoroutePart` | SEO-friendly URL | `Path` (slug), `Pattern` (Liquid) |
| `LocalizationPart` | Multi-language | `Culture` (BCP47), `LocalizationSet` (link ID) |
| `AuditTrailPart` | Change tracking | Auto-tracked by Orchard Core |
| `PublishLaterPart` | Scheduled publishing | `ScheduledPublishUtc` |

## 6. Recipes

Recipes are JSON files that define setup steps. Used for:
- Initial tenant setup
- Test data seeding
- Deployment plans

```json
{
  "name": "RecipeName",
  "displayName": "Human Readable Name",
  "description": "What this recipe does",
  "steps": [
    { "name": "ContentDefinition", "ContentTypes": [...] },
    { "name": "content", "data": [...] },
    { "name": "Roles", "Roles": [...] },
    { "name": "Settings", "..." : "..." }
  ]
}
```

## 7. Liquid Templates

Orchard Core uses Liquid (not Razor) for templates:

```liquid
{% assign items = ContentItem | display_text %}

<h1>{{ Model.ContentItem.DisplayText }}</h1>
<div class="body">{{ Model.ContentItem.Content.BodyPart.Body | raw }}</div>
<p>Yayınlanma: {{ Model.ContentItem.CreatedUtc | date: "%d.%m.%Y" }}</p>

{% for item in Model.Items %}
  <a href="{{ item | display_url }}">{{ item | display_text }}</a>
{% endfor %}
```

**Security**: Always use `{{ variable }}` (auto-escaped). Use `| raw` only for trusted HTML (BodyPart).

## 8. Multi-Tenancy

```csharp
// Orchard Core handles tenant resolution via middleware
// Each tenant has its own:
// - Shell (isolated DI container)
// - Database (or schema)
// - Settings
// - Content types

// Tenant setup via ShellSettings
var settings = new ShellSettings
{
    Name = "tenant-a",
    RequestUrlHost = "tenant-a.projectdora.gov.tr",
    State = TenantState.Running,
    DatabaseProvider = "Postgres",
    ConnectionString = "..."
};
```

## 9. Common Anti-Patterns

| Anti-Pattern | Correct Approach |
|-------------|-----------------|
| Calling `IContentManager` directly from handlers | Use `IContentService` (abstraction layer) |
| Querying without index | Create a MapIndex for the query pattern |
| Storing large blobs in ContentItem | Use MinIO via `IStorageService` |
| Hardcoding content type names | Use constants: `ContentTypes.DestekProgrami` |
| Modifying content without versioning | Always use `VersionOptions.DraftRequired` |
| Ignoring `Published` vs `Latest` flag | `Published` = live content; `Latest` = most recent draft |
| String concatenation in Liquid | Use Liquid filters: `| slugify`, `| date` |

## 10. Turkish-Specific Considerations

- `AutoroutePart` slug generation must handle: ş→s, ç→c, ğ→g, ı→i, ö→o, ü→u, İ→i
- `DisplayText` fields must support UTF-8 Turkish characters
- Date format: `dd.MM.yyyy` (Turkish convention)
- Currency: `₺` (Turkish Lira)
