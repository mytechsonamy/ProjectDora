# Skill: Workflow Engine

> Target agents: Developer, Test Architect

## 1. Workflow Concepts

```
WorkflowDefinition
 ├── StartActivity (trigger)
 ├── Activities[] (actions)
 └── Transitions[] (connections)

WorkflowExecution (runtime instance)
 ├── Status: Idle → Running → Completed / Faulted
 ├── CurrentActivity
 └── Context (data passed between activities)
```

## 2. Orchard Core Workflow Components

### Triggers (Start Events)

| Trigger | Fires When | Properties |
|---------|-----------|-----------|
| `ContentCreatedEvent` | New content item created | ContentTypes filter |
| `ContentPublishedEvent` | Content item published | ContentTypes filter |
| `ContentUpdatedEvent` | Content item updated | ContentTypes filter |
| `ContentDeletedEvent` | Content item deleted | ContentTypes filter |
| `UserCreatedEvent` | New user registered | — |
| `TimerEvent` | Scheduled (cron) | CronExpression |
| `HttpRequestEvent` | HTTP endpoint called | URL, Method |
| `SignalEvent` | Manual or programmatic signal | SignalName |

### Activities (Actions)

| Activity | Purpose | Properties |
|----------|---------|-----------|
| `NotifyContentOwnerTask` | Send notification | Subject, Body (Liquid), Recipients |
| `SendEmailTask` | Send email via SMTP | To, Subject, Body (Liquid) |
| `HttpRequestTask` | Call external API | URL, Method, Headers, Body |
| `ContentTask` | Manipulate content | Action (Create/Publish/Delete), ContentType |
| `SetPropertyTask` | Set workflow variable | PropertyName, Value (Liquid) |
| `IfElseTask` | Conditional branch | Condition (JavaScript/Liquid) |
| `ForLoopTask` | Iterate collection | Collection, Variable |
| `LogTask` | Write to log | LogLevel, Message (Liquid) |
| `ScriptTask` | Execute custom logic | Script (JavaScript) |
| `ForkTask` | Parallel branches | Branches[] |
| `JoinTask` | Wait for parallel branches | Mode (WaitAll/WaitAny) |

### Custom Activity Template

```csharp
// File: src/ProjectDora.Modules/Workflows/Activities/RunQueryActivity.cs
public class RunQueryActivity : Activity
{
    private readonly IQueryService _queryService;

    public RunQueryActivity(IQueryService queryService)
    {
        _queryService = queryService;
    }

    public override string Name => "RunQuery";
    public override LocalizedString DisplayText => S["Sorgu Çalıştır"];
    public override LocalizedString Category => S["Queries"];

    // Properties (configurable in designer)
    public WorkflowExpression<string> QueryId
    {
        get => GetProperty<WorkflowExpression<string>>();
        set => SetProperty(value);
    }

    public override IEnumerable<Outcome> GetPossibleOutcomes(
        WorkflowExecutionContext context, ActivityContext activity)
    {
        return Outcomes(S["Done"], S["Failed"]);
    }

    public override async Task<ActivityExecutionResult> ExecuteAsync(
        WorkflowExecutionContext context, ActivityContext activity)
    {
        var queryId = await context.EvaluateAsync(QueryId, activity);

        try
        {
            var result = await _queryService.ExecuteAsync(queryId, new());
            context.Properties["QueryResult"] = result;
            return Outcomes("Done");
        }
        catch (Exception ex)
        {
            context.Properties["Error"] = ex.Message;
            return Outcomes("Failed");
        }
    }
}
```

## 3. Workflow Definition (JSON)

### Content Approval Workflow

```json
{
  "name": "ContentApproval",
  "displayName": "İçerik Onay Akışı",
  "isEnabled": true,
  "startActivity": {
    "activityId": "trigger-1",
    "name": "ContentPublishedEvent",
    "properties": {
      "ContentTypes": ["Duyuru", "DestekProgrami"]
    }
  },
  "activities": [
    {
      "activityId": "trigger-1",
      "name": "ContentPublishedEvent",
      "x": 100, "y": 100
    },
    {
      "activityId": "notify-1",
      "name": "NotifyContentOwnerTask",
      "properties": {
        "subject": "İçerik yayınlandı: {{ ContentItem.DisplayText }}",
        "body": "{{ ContentItem.DisplayText }} başarıyla yayınlanmıştır.",
        "recipients": "{{ ContentItem.Owner }}"
      },
      "x": 300, "y": 100
    },
    {
      "activityId": "audit-1",
      "name": "LogTask",
      "properties": {
        "level": "Information",
        "message": "Content published: {{ ContentItem.ContentItemId }}"
      },
      "x": 500, "y": 100
    }
  ],
  "transitions": [
    {
      "sourceActivityId": "trigger-1",
      "sourceOutcomeName": "Done",
      "destinationActivityId": "notify-1"
    },
    {
      "sourceActivityId": "notify-1",
      "sourceOutcomeName": "Done",
      "destinationActivityId": "audit-1"
    }
  ]
}
```

## 4. ProjectDora Abstraction Layer

```csharp
public interface IWorkflowService
{
    Task<WorkflowDefDto> CreateAsync(CreateWorkflowCommand command);
    Task<WorkflowDefDto> GetAsync(string workflowId);
    Task<IReadOnlyList<WorkflowDefDto>> ListAsync();
    Task UpdateAsync(string workflowId, UpdateWorkflowCommand command);
    Task DeleteAsync(string workflowId);
    Task EnableAsync(string workflowId);
    Task DisableAsync(string workflowId);
    Task<string> TriggerAsync(string workflowId, Dictionary<string, object>? context = null);
    Task<WorkflowExecutionDto> GetExecutionAsync(string executionId);
}
```

## 5. Testing Workflows

```csharp
// Unit test: Activity execution
[Fact]
public async Task Workflow_RunQueryActivity_WithValidQuery_ReturnsDone()
{
    // Arrange
    var mockQueryService = new Mock<IQueryService>();
    mockQueryService.Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
        .ReturnsAsync(new QueryResultDto { TotalCount = 5 });

    var activity = new RunQueryActivity(mockQueryService.Object);
    var context = CreateWorkflowContext();
    var activityContext = CreateActivityContext("query-123");

    // Act
    var result = await activity.ExecuteAsync(context, activityContext);

    // Assert
    result.Outcomes.Should().Contain("Done");
    context.Properties["QueryResult"].Should().NotBeNull();
}

// Integration test: Full workflow execution
[Fact]
public async Task Workflow_ContentApproval_OnPublish_SendsNotification()
{
    // Arrange
    var workflow = await _workflowService.CreateAsync(
        LoadWorkflow("ContentApproval"));
    await _workflowService.EnableAsync(workflow.WorkflowId);

    // Act — publish content (triggers workflow)
    await _contentService.PublishAsync(contentItemId);

    // Assert — verify notification was sent
    _mockEmailService.Verify(e => e.SendAsync(
        It.Is<EmailMessage>(m => m.Subject.Contains("yayınlandı"))),
        Times.Once);
}
```

## 6. Anti-Patterns

| Anti-Pattern | Correct |
|-------------|---------|
| Business logic in workflow activities | Activities orchestrate; services contain logic |
| Direct DB access in activity | Use Core interfaces (IContentService, IQueryService) |
| Infinite loop in workflow | Set max execution steps limit |
| No error handling in activity | Return "Failed" outcome, let workflow handle |
| Polling for timer events | Use Orchard Core background service scheduler |
