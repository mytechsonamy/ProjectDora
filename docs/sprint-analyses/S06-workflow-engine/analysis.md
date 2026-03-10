# Sprint S06 — Workflow Engine (Is Akisi Yonetimi)

## Kapsam (Scope)
- Spec items: 4.1.7.1, 4.1.7.2, 4.1.7.3, 4.1.7.4, 4.1.7.5
- Stories: US-701, US-702, US-703, US-704, US-705, US-706, US-707, US-708
- Cross-references: Workflow engine is consumed by Content Management (4.1.3), Audit (4.1.9), Query Engine (4.1.5), Theme Management (4.1.4)
- Upstream dependencies: S01 (Admin Panel), S02 (Content Modeling), S03 (Content Management), S04 (Query Management), S05 (User/Role/Permission)

## Is Analizi Ozeti (Business Analysis Summary)

### Gereksinimler (Requirements from Teknik Sartname)

| Spec | Requirement (Turkish) | Summary (English) |
|------|----------------------|-------------------|
| 4.1.7.1 | Sunulacak urun uzerinde cesitli is akislari tasarlanabilmesi amaciyla kullanilabilecek bir is akisi yonetim aracinin bulunmasi | A workflow management tool for designing various business process workflows |
| 4.1.7.2 | Is akisi araci uzerinde akislarin kolayca tasarlanabilmesine imkan vermek amaciyla surukle birak yapida calisan bir tasarim aracinin bulunmasi | A drag-and-drop visual workflow designer for easy workflow creation |
| 4.1.7.3 | Urunun ilgili is akisinin calismasini / tetiklenmesini saglamak amaciyla on tanimli cesitli etkinlik / olay aktivitelerini (zaman bazli, icerik hareketleri kaynakli, http hareketleri kaynakli, vb.) yapilandirmaya hazir olarak sunmasi | Pre-defined trigger/event activities: time-based, content-based, HTTP-based triggers configurable out-of-box |
| 4.1.7.4 | Urunun, ilgili is akisi tetiklendikten sonra gerceklesmesi beklenen aksiyonlara iliskin cesitli gorev / is aktivitelerini (bilgi dogrulama, icerik olusturma / duzenleme / silme, cesitli mantiksal kontrol akislari, http istekleri, e-posta gonderme vb.) yapilandirmaya hazir olarak sunmasi | Pre-defined task/action activities: validation, content CRUD, conditional logic, HTTP requests, email sending, etc. |
| 4.1.7.5 | Urun uzerinde yer alan farkli tipteki aktivitelerin kendi gorevleri dogrultusunda basit yapilandirmalar veya kodsal cesitli duzenlemeler ile kendileri ile iliskili turlere (icerikler, sql sorgulari) dogrudan erisiminin olmasi | Activities can directly access related entities (content items, SQL queries) via simple configuration or code customization |

### Is Sureci Analizi (Business Process Analysis)

KOSGEB bunyesinde is akisi yonetiminin kritik oneme sahip oldugu temel surecleri:

1. **Destek Basvurusu Onay Akisi**: KOBi destek programina basvuru yapildiginda basvurunun ilgili birimlerce incelenmesi, onay/red karari verilmesi, basvuru sahibine bildirim gonderilmesi
2. **Icerik Yayin Onayi**: Yeni icerik (Duyuru, Haber, Destek Programi) olusturulup yayina alinmadan once yetkili kisiler tarafindan onaylanmasi
3. **Kullanici Kayit Bildirimi**: Yeni kullanici kaydedildiginde ilgili birimlere otomatik bilgilendirme yapilmasi
4. **Periyodik Rapor Uretimi**: Belirli zamanlarda (haftalik, aylik) otomatik rapor uretimi ve ilgili kisilere gonderimi
5. **Dis Sistem Entegrasyonu**: Harici sistemlerden HTTP webhook ile gelen bildirimlerin islenmesi ve platform icerisinde aksiyonlarin tetiklenmesi

### Kisitlar ve Bagimliliklar (Constraints & Dependencies)

- **Dependency**: S01 (Admin Panel) must provide admin UI shell; S02-S03 (Content Modeling/Management) must provide content types and IContentService; S04 (Query Management) must provide IQueryService for RunQueryActivity; S05 (User/Role/Permission) must provide RBAC framework
- **Orchard Core**: Workflow engine is built on `OrchardCore.Workflows` module -- we configure, extend, and add custom activities
- **Abstraction Layer**: All workflow operations go through `IWorkflowService` abstraction defined in `ProjectDora.Core`
- **Multi-tenant**: Workflow definitions are tenant-scoped; executions strictly isolated per tenant; workflow triggers only fire within the triggering tenant's context
- **Audit**: All workflow CRUD and execution events must emit audit trail entries via `IAuditService`
- **Domain Events**: Workflow engine is the primary consumer of content and user lifecycle events: `ContentItemCreated`, `ContentItemPublished`, `ContentItemUpdated`, `ContentItemDeleted`, `UserCreated`
- **SMTP**: Email activities depend on SMTP configuration (4.1.10.10)
- **Liquid Templates**: Activity properties support Liquid template expressions for dynamic values

### RBAC Gereksinimleri

| Permission | Description | SuperAdmin | TenantAdmin | WorkflowAdmin | Editor | Author | Analyst | Denetci | SEOUzmani | Viewer | Anonymous |
|-----------|-------------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `Workflow.Manage` | Create, update, delete, enable/disable workflow definitions | Y | Y | Y | - | - | - | - | - | - | - |
| `Workflow.Execute` | Manually trigger workflow executions | Y | Y | Y | - | - | - | - | - | - | - |
| `Workflow.View` | View workflow definitions and execution history | Y | Y | Y | - | - | - | - | - | - | - |

- `Workflow.Manage` -- Full lifecycle management: CRUD on workflow definitions, visual designer save, enable/disable
- `Workflow.Execute` -- Manual trigger: Programmatic or UI-based execution of workflows with context data
- `Workflow.View` -- Read-only: View definitions, designer (read-only mode), execution history, monitoring dashboards
- **Denied roles**: Editor, Author, Analyst, Denetci, SEOUzmani, Viewer, Anonymous -- workflows are administrative-only tools that can modify system state

### Story Decomposition

| Story | Spec | Priority | Description |
|-------|------|----------|-------------|
| US-701 | 4.1.7.1 | P0 | Workflow definition CRUD (create, read, update, delete workflow definitions with validation) |
| US-702 | 4.1.7.1 | P1 | Workflow enable/disable and lifecycle management |
| US-703 | 4.1.7.2 | P1 | Drag-and-drop visual workflow designer (canvas with activity palette, connections, layout persistence) |
| US-704 | 4.1.7.3 | P0 | Pre-defined trigger/event activities (ContentCreated, ContentPublished, ContentUpdated, ContentDeleted, UserCreated, Timer, HTTP, Signal) |
| US-705 | 4.1.7.4 | P0 | Pre-defined task/action activities (Email, HTTP, Content CRUD, IfElse, Fork/Join, ForLoop, Log, Script, SetProperty, NotifyContentOwner) |
| US-706 | 4.1.7.4 | P1 | Workflow execution engine (trigger processing, activity pipeline, status tracking, fault handling) |
| US-707 | 4.1.7.5 | P1 | Custom activities with entity access (RunQueryActivity, ContentLookupActivity, ContentTransformActivity) |
| US-708 | 4.1.7.3, 4.1.7.4 | P2 | Workflow execution history and monitoring (paginated history, status filtering, activity-level logs, date range) |

### Priority Rationale

- **P0 (Foundation)**: US-701 (definition CRUD) is the data layer all other stories depend on. US-704 (triggers) and US-705 (actions) are the core primitives -- a workflow without triggers and activities has no value
- **P1 (Core Experience)**: US-702 (lifecycle), US-703 (visual designer), US-706 (execution engine), US-707 (entity access) form the complete workflow experience. Lifecycle enables production use; designer enables non-developers to build workflows; execution engine runs them; entity access enables data-driven workflows
- **P2 (Observability)**: US-708 (history/monitoring) is important for production operations but workflows can function without it initially

### Domain Model Entities

| Entity | Schema | Storage | Description |
|--------|--------|---------|-------------|
| `WorkflowDef` | `orchard` | YesSql Document | Workflow definition with name, displayName, isEnabled, activities, transitions, version |
| `WFActivity` | `orchard` | Embedded in WorkflowDef | Activity node: activityId, name, type, properties, x/y position, outcomes |
| `WFTransition` | `orchard` | Embedded in WorkflowDef | Connection: sourceActivityId, sourceOutcomeName, destinationActivityId |
| `WorkflowExecution` | `orchard` | YesSql Document | Runtime instance: executionId, workflowId, status, startedUtc, completedUtc, context, activityLog, error |
| `ActivityLogEntry` | `orchard` | Embedded in WorkflowExecution | Per-activity record: activityId, startedUtc, completedUtc, outcome, outputData |

### State Machine: WorkflowExecution

```
    Idle
     | (trigger event or manual run)
    Running <------+
     |      |       | (retry on transient failure)
     | (ok) | (err) |
     v      v       |
    Done   Faulted -+
     |
     v
   (archived after retention period)
```

- **Idle**: Initial state, awaiting trigger
- **Running**: Active execution, stepping through activities
- **Done (Completed)**: All activities completed successfully
- **Faulted**: An activity returned Failed outcome with no error-handling branch, or max step limit exceeded

### State Machine: WorkflowDef Lifecycle

```
    Draft (isEnabled=false)
     | (enable)
    Active (isEnabled=true)
     | (disable)
    Inactive (isEnabled=false)
     | (delete)
    Deleted (soft-delete)
```

### IWorkflowService Abstraction

```csharp
public interface IWorkflowService
{
    // Definition CRUD (US-701)
    Task<WorkflowDefDto> CreateAsync(CreateWorkflowCommand command);
    Task<WorkflowDefDto> GetAsync(string workflowId);
    Task<IReadOnlyList<WorkflowDefDto>> ListAsync();
    Task UpdateAsync(string workflowId, UpdateWorkflowCommand command);
    Task DeleteAsync(string workflowId);

    // Lifecycle (US-702)
    Task EnableAsync(string workflowId);
    Task DisableAsync(string workflowId);

    // Execution (US-706)
    Task<string> TriggerAsync(string workflowId, Dictionary<string, object>? context = null);
    Task<WorkflowExecutionDto> GetExecutionAsync(string executionId);

    // History (US-708)
    Task<PagedResult<WorkflowExecutionDto>> ListExecutionsAsync(
        string workflowId, string? status = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        int page = 1, int pageSize = 20);
}
```

### KOSGEB-Specific Workflow Examples

#### Ornek 1: Destek Basvurusu Onay Akisi
```
ContentCreatedEvent (ContentType=DestekBasvurusu)
  -> ContentLookupActivity (basvuru detaylarini al)
  -> IfElseTask (belgeler tam mi?)
    -> True: SendEmailTask (onay birimine bildir: "Yeni destek basvurusu: {{ ContentItem.DisplayText }}")
    -> False: SendEmailTask (basvuru sahibine eksik belge bildirimi)
  -> LogTask ("Basvuru islendi: {{ ContentItem.ContentItemId }}")
```

#### Ornek 2: Icerik Yayin Onayi
```
ContentPublishedEvent (ContentType=Duyuru)
  -> ContentLookupActivity (icerik bilgilerini al)
  -> SendEmailTask (editore bildir: "Yayinlanan duyuru: {{ ContentItem.DisplayText }}")
  -> HttpRequestTask (POST to external notification API)
  -> LogTask ("Duyuru yayinlandi ve bildirimler gonderildi")
```

#### Ornek 3: Haftalik KOBi Destek Raporu
```
TimerEvent (cron: "0 9 * * 1")
  -> RunQueryActivity (queryId: "haftalik-destek-istatistikleri")
  -> SendEmailTask (yoneticiye rapor gonder: "Haftalik destek raporu: {{ QueryResult.TotalRows }} kayit")
  -> LogTask ("Haftalik rapor olusturuldu ve gonderildi")
```

#### Ornek 4: Dis Sistem Webhook Isleme
```
HttpRequestEvent (POST /workflows/hooks/dis-sistem-bildirimi)
  -> ScriptTask (gelen JSON'u isle ve validate et)
  -> IfElseTask (gecerli veri mi?)
    -> True: ContentTask (yeni Bildirim icerigi olustur)
    -> False: LogTask (hata logla: "Gecersiz webhook verisi")
```

## Teknik Kararlar (Technical Decisions)

### D-001: Leverage OrchardCore.Workflows Module
- Use `OrchardCore.Workflows` as the foundation for workflow engine
- Extend with custom activities for ProjectDora-specific needs (KOSGEB is surecleri)
- Visual designer provided by Orchard Core admin UI (Elsa-based workflow editor)
- Abstraction layer (`IWorkflowService`) wraps Orchard Core's `IWorkflowManager` and `IWorkflowStore`
- See `decisions.md` D-001 for full rationale

### D-002: Custom Activity Pattern
- All custom activities inherit from `Activity` base class provided by OrchardCore.Workflows
- Use `IContentService` / `IQueryService` for entity access (never direct DB access)
- Each activity exposes configurable properties via `WorkflowExpression<T>` for Liquid template support
- Minimum outcomes: `Done` + `Failed` for all task activities
- See `.claude/skills/workflow-engine.md` for template

### D-003: Workflow Module Structure
- New module: `ProjectDora.Modules.Workflows`
- Implements: custom activities, custom triggers, `IPermissionProvider`, workflow validators
- Depends on: `ProjectDora.Core` (abstractions), `OrchardCore.Workflows`
- No direct dependency on other ProjectDora modules -- uses only core abstractions

### D-004: Event-Driven Trigger Architecture
- Content events flow via Orchard Core's event bus (`IContentHandler` pipeline)
- Timer events via Orchard Core's background task scheduler (cron-based, no polling)
- HTTP triggers via dedicated webhook endpoint routes (configurable per workflow)
- Signal triggers via `IWorkflowService.TriggerAsync()` for programmatic invocation from other modules
- All triggers respect tenant isolation and workflow enabled state

### D-005: Workflow Definition Validation
- Validation enforced on create and update operations
- Rules: (1) exactly one start activity, (2) no orphan transitions, (3) all activities reachable from start, (4) max execution step limit (configurable, default 1000)
- Invalid definitions rejected with 400 response and descriptive validation errors

### D-006: Execution History Storage
- Stored in YesSql (orchard schema) via `WorkflowInstance` documents
- Includes: workflowId, trigger event, start/end timestamps, status, activity-level execution log, error details
- Retention follows tenant-level audit retention policy
- Paginated query support with status and date range filters

### D-007: Workflow Permission Model
- Three permissions: `Workflow.Manage`, `Workflow.Execute`, `Workflow.View`
- Only SuperAdmin, TenantAdmin, WorkflowAdmin roles get these permissions
- All other roles denied -- workflows can modify system state and must be restricted

## Test Plani (Test Plan)

### New Test Cases

| Test ID | Category | Story | Description |
|---------|----------|-------|-------------|
| TC-701-01 | Unit | US-701 | Create workflow definition with valid data (name, displayName, activities, transitions) |
| TC-701-02 | Unit | US-701 | Reject workflow creation without Workflow.Manage permission |
| TC-701-03 | Unit | US-701 | Update workflow definition (rename, add/remove activities) |
| TC-701-04 | Unit | US-701 | Delete workflow definition (soft-delete, audit event) |
| TC-701-05 | Integration | US-701 | Full CRUD lifecycle via REST API |
| TC-701-06 | Unit | US-701 | Reject duplicate workflow name within same tenant |
| TC-701-07 | Unit | US-701 | Validate workflow structure (start activity, orphan transitions, reachability) |
| TC-702-01 | Unit | US-702 | Enable disabled workflow -- triggers start listening |
| TC-702-02 | Unit | US-702 | Disable enabled workflow -- stops new triggers, running executions continue |
| TC-702-03 | Unit | US-702 | Idempotent enable (already enabled) returns 200 without error |
| TC-703-01 | Integration | US-703 | Visual designer saves workflow with activities, transitions, and x/y positions |
| TC-703-02 | Integration | US-703 | Re-open existing workflow preserves layout and connections |
| TC-703-03 | Unit | US-703 | Reject save of empty canvas (no activities) |
| TC-704-01 | Unit | US-704 | ContentCreatedEvent trigger fires on content creation |
| TC-704-02 | Unit | US-704 | ContentPublishedEvent trigger fires on content publish |
| TC-704-03 | Unit | US-704 | ContentUpdatedEvent trigger fires on content update |
| TC-704-04 | Unit | US-704 | ContentDeletedEvent trigger fires on content deletion |
| TC-704-05 | Unit | US-704 | UserCreatedEvent trigger fires on user registration |
| TC-704-06 | Unit | US-704 | TimerEvent trigger fires on cron schedule |
| TC-704-07 | Unit | US-704 | HttpRequestEvent trigger fires on webhook HTTP call |
| TC-704-08 | Unit | US-704 | SignalEvent trigger fires on programmatic signal |
| TC-704-09 | Unit | US-704 | Content trigger filtered by type does NOT fire for non-matching types |
| TC-705-01 | Unit | US-705 | SendEmailTask sends email via SMTP with Liquid template rendering |
| TC-705-02 | Unit | US-705 | HttpRequestTask calls external API and stores response |
| TC-705-03 | Unit | US-705 | ContentTask creates/publishes/deletes content via IContentService |
| TC-705-04 | Unit | US-705 | IfElseTask branches correctly on Liquid condition |
| TC-705-05 | Unit | US-705 | ForkTask creates parallel execution branches |
| TC-705-06 | Unit | US-705 | JoinTask waits for all parallel branches to complete |
| TC-705-07 | Unit | US-705 | ForLoopTask iterates over collection |
| TC-705-08 | Unit | US-705 | LogTask writes message to Serilog at configured level |
| TC-705-09 | Unit | US-705 | ScriptTask executes JavaScript safely |
| TC-705-10 | Unit | US-705 | SetPropertyTask sets workflow context property |
| TC-706-01 | Integration | US-706 | Full workflow execution from trigger through all activities to completion |
| TC-706-02 | Integration | US-706 | Faulted workflow recorded with error details and audit event |
| TC-706-03 | Security | US-706 | Workflow.Execute permission required for manual trigger |
| TC-706-04 | Unit | US-706 | Disabled workflow rejects manual trigger with 400 |
| TC-706-05 | Unit | US-706 | Max step limit prevents infinite loop (Faulted with MaxStepsExceeded) |
| TC-706-06 | Unit | US-706 | Concurrent executions of same workflow are independent |
| TC-707-01 | Unit | US-707 | RunQueryActivity executes saved SQL query via IQueryService |
| TC-707-02 | Unit | US-707 | ContentLookupActivity retrieves content item via IContentService |
| TC-707-03 | Unit | US-707 | ContentTransformActivity maps content fields to workflow context |
| TC-707-04 | Unit | US-707 | RunQueryActivity with non-existent query returns Failed |
| TC-707-05 | Unit | US-707 | Cross-tenant query access blocked by tenant isolation |
| TC-708-01 | Integration | US-708 | Execution history records all activity steps with timestamps |
| TC-708-02 | Unit | US-708 | Filter executions by status (Running, Completed, Faulted) |
| TC-708-03 | Unit | US-708 | Filter executions by date range |
| TC-708-04 | Unit | US-708 | Paginated execution list |
| TC-708-05 | Unit | US-708 | Single execution detail with activity-level log |
| TC-S06-SEC-01 | Security | All | Viewer role cannot manage or view workflows (403) |
| TC-S06-SEC-02 | Security | All | Tenant A cannot see or trigger Tenant B workflows |
| TC-S06-SEC-03 | Security | All | Disabled workflow cannot be triggered by events or manually |
| TC-S06-SEC-04 | Security | All | Editor role cannot manage workflows (403) |
| TC-S06-SEC-05 | Security | All | Anonymous user cannot access workflow API endpoints |

### Coverage Target
- Unit test coverage: >= 80% for `ProjectDora.Modules.Workflows`
- Integration test coverage: >= 60%
- Security tests: 5 minimum (RBAC, tenant isolation, disabled workflow, anonymous access, role-specific denial)

### KOSGEB-Specific Test Scenarios
- TC-KOSGEB-01: "Destek Basvurusu Onay Akisi" end-to-end -- DestekBasvurusu created -> workflow triggers -> email sent to onay birimi
- TC-KOSGEB-02: "Icerik Yayin Onayi" end-to-end -- Duyuru published -> workflow triggers -> notification sent
- TC-KOSGEB-03: "Haftalik Rapor" timer workflow -- cron trigger fires -> RunQueryActivity executes -> email sent with results

## Sprint Sonucu (Sprint Outcome)
- [ ] US-701 complete
- [ ] US-702 complete
- [ ] US-703 complete
- [ ] US-704 complete
- [ ] US-705 complete
- [ ] US-706 complete
- [ ] US-707 complete
- [ ] US-708 complete

## Dokumantasyon Notlari (Documentation Notes)
> Information to include in end-of-project user manual and technical documentation

### Kullanici Kilavuzu (User Manual)
- Is akisi tasarimcisina erisim: Yonetim paneli -> Is Akislari -> Yeni Is Akisi
- Surukle-birak tasarim araci kullanimi: aktivite paletinden surekleme, baglanti olusturma
- Mevcut tetikleyiciler: icerik olaylari (olusturma, yayinlama, guncelleme, silme), zamanlayici (cron), HTTP webhook, sinyal
- Mevcut aktiviteler: e-posta gonderme, HTTP istegi, icerik islemleri (olustur/duzenle/sil), kosullu dallanma (IfElse), paralel calisma (Fork/Join), dongu (ForLoop), log, script, sorgu calistirma
- Is akisini etkinlestirme/devre disi birakma
- Calisma gecmisi izleme ve hata ayiklama
- Ornek: "Destek Basvurusu Onay Akisi" olusturma adim adim kilavuzu

### Teknik Dokumantasyon (Technical Documentation)
- `OrchardCore.Workflows` konfigurasyonu -- `Startup.cs` icinde modul kaydı
- Ozel aktivite gelistirme deseni -- `Activity` temel sinifini miras alma, `WorkflowExpression<T>` ozellikleri
- `IWorkflowService` soyutlama katmani API dokumantasyonu
- Is akisi JSON tanim formati (aktiviteler, gecisler, ozellikler)
- Olay-tetikleyici eslestirmesi (icerik olaylari, zamanlayici, HTTP, sinyal)
- MediatR komut/sorgu handler listesi
- Denetim izi (audit) entegrasyonu -- is akisi yasam dongusu olaylari
- Calisma gecmisi saklama ve bekletme politikasi

### API Endpoints
- `GET /api/v1/workflows` -- List workflow definitions (Workflow.View)
- `POST /api/v1/workflows` -- Create workflow definition (Workflow.Manage)
- `GET /api/v1/workflows/{workflowId}` -- Get workflow definition (Workflow.View)
- `PUT /api/v1/workflows/{workflowId}` -- Update workflow definition (Workflow.Manage)
- `DELETE /api/v1/workflows/{workflowId}` -- Delete workflow definition (Workflow.Manage)
- `POST /api/v1/workflows/{workflowId}/enable` -- Enable workflow (Workflow.Manage)
- `POST /api/v1/workflows/{workflowId}/disable` -- Disable workflow (Workflow.Manage)
- `POST /api/v1/workflows/{workflowId}/run` -- Manually trigger workflow execution (Workflow.Execute)
- `GET /api/v1/workflows/{workflowId}/executions` -- List execution history (Workflow.View)
- `GET /api/v1/workflows/executions/{executionId}` -- Get execution details with activity log (Workflow.View)

### Configuration Parameters
- `Workflows:MaxExecutionSteps` -- Maximum steps per execution (default: 1000)
- `Workflows:ExecutionTimeout` -- Maximum execution time in seconds (default: 300)
- `Workflows:HttpRequestTimeout` -- HTTP request activity timeout in seconds (default: 30)
- `Workflows:RetryPolicy:MaxRetries` -- Max retries for transient failures (default: 3)
- `Workflows:RetryPolicy:BackoffMs` -- Retry backoff in milliseconds (default: 1000)
- `Workflows:Timer:MinIntervalSeconds` -- Minimum cron interval in seconds (default: 60)
- `Workflows:History:RetentionDays` -- Execution history retention in days (default: 90)
