using FluentAssertions;
using Moq;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using ProjectDora.Core.Abstractions;
using ProjectDora.Workflows.Services;

namespace ProjectDora.Modules.Tests.Workflows;

public class WorkflowExecutionServiceTests
{
    private readonly Mock<IWorkflowTypeStore> _typeStoreMock = new();
    private readonly Mock<IWorkflowManager> _managerMock = new();
    private readonly Mock<IWorkflowStore> _storeMock = new();

    private OrchardWorkflowService CreateSut() =>
        new(_typeStoreMock.Object, _managerMock.Object, _storeMock.Object);

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task GetExecutionAsync_ExistingId_ReturnsDto()
    {
        var workflow = new Workflow
        {
            Id = 42,
            WorkflowTypeId = "wt-001",
            Status = WorkflowStatus.Finished,
            CorrelationId = "corr-001",
            CreatedUtc = DateTime.UtcNow,
        };
        _storeMock.Setup(s => s.GetAsync(42L)).ReturnsAsync(workflow);

        var sut = CreateSut();
        var result = await sut.GetExecutionAsync("42");

        result.Should().NotBeNull();
        result!.ExecutionId.Should().Be("42");
        result.WorkflowId.Should().Be("wt-001");
        result.Status.Should().Be("Finished");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task GetExecutionAsync_MissingId_ReturnsNull()
    {
        _storeMock.Setup(s => s.GetAsync(It.IsAny<long>())).ReturnsAsync((Workflow?)null);

        var sut = CreateSut();
        var result = await sut.GetExecutionAsync("999");

        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task ListExecutionsAsync_UnknownWorkflowType_ReturnsEmptyPage()
    {
        _typeStoreMock.Setup(s => s.GetAsync(It.IsAny<long>())).ReturnsAsync((WorkflowType?)null);

        var sut = CreateSut();
        var query = new ListExecutionsQuery("1", Page: 1, PageSize: 10);
        var result = await sut.ListExecutionsAsync(query);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task ListExecutionsAsync_KnownWorkflowType_ReturnsPaginatedDtos()
    {
        var workflowType = new WorkflowType
        {
            Id = 7,
            WorkflowTypeId = "wt-007",
            Name = "OnayAkisi",
        };
        var workflow = new Workflow
        {
            Id = 1,
            WorkflowTypeId = "wt-007",
            Status = WorkflowStatus.Halted,
            CreatedUtc = DateTime.UtcNow,
        };

        _typeStoreMock.Setup(s => s.GetAsync(7L)).ReturnsAsync(workflowType);
        _storeMock.Setup(s => s.ListAsync("wt-007", It.IsAny<int?>(), It.IsAny<int?>()))
                  .ReturnsAsync(new[] { workflow });
        _storeMock.Setup(s => s.CountAsync("wt-007")).ReturnsAsync(1);

        var sut = CreateSut();
        var query = new ListExecutionsQuery("7", Page: 1, PageSize: 10);
        var result = await sut.ListExecutionsAsync(query);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Halted");
    }

    // ── P1-1: TriggerAsync execution ID behaviour ──────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task TriggerAsync_WhenOCReturnsExecutionContext_ReturnsRealDatabaseId()
    {
        // OC's TriggerEventAsync returns WorkflowExecutionContext with a persisted Workflow.
        // TriggerAsync must extract Workflow.Id and return it as a string — NOT a fake UUID.
        var workflowType = new WorkflowType { Id = 5, WorkflowTypeId = "wt-005", Name = "Test", IsEnabled = true };
        var persistedWorkflow = new Workflow { Id = 99L, WorkflowTypeId = "wt-005" };

        _typeStoreMock.Setup(s => s.GetAsync(5L)).ReturnsAsync(workflowType);

        // Simulate OC returning an execution context with a persisted Workflow record.
        // WorkflowExecutionContext ctor: (WorkflowType, Workflow, properties, input, output, executedActivities, lastResult, activityContexts)
        var execContext = new WorkflowExecutionContext(
            workflowType, persistedWorkflow,
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            new List<ExecutedActivity>(),
            null,
            Enumerable.Empty<ActivityContext>());

        _managerMock
            .Setup(m => m.TriggerEventAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new[] { execContext });

        var sut = CreateSut();
        var executionId = await sut.TriggerAsync("5");

        // Must return the real Workflow.Id, not a random UUID.
        executionId.Should().Be("99");
        executionId.Should().NotMatchRegex(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            "a random UUID would be an opaque non-queryable handle");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-601")]
    public async Task TriggerAsync_WhenNoExecutionContextReturned_ReturnsTraceMarker_NotFakeUuid()
    {
        // When OC creates no persisted Workflow record (signal matched no active handlers),
        // TriggerAsync must NOT return a random UUID (which would silently fail on GetExecutionAsync).
        // It must return a "trace:" prefixed marker so callers can detect the non-trackable case.
        var workflowType = new WorkflowType { Id = 3, WorkflowTypeId = "wt-003", Name = "Async", IsEnabled = true };

        _typeStoreMock.Setup(s => s.GetAsync(3L)).ReturnsAsync(workflowType);
        _managerMock
            .Setup(m => m.TriggerEventAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<WorkflowExecutionContext>());

        var sut = CreateSut();
        var executionId = await sut.TriggerAsync("3");

        // Must be a trace marker, not a queryable ID.
        executionId.Should().StartWith("trace:",
            "callers must be able to distinguish non-trackable signals from real execution IDs");
        executionId.Should().Contain("3",
            "trace marker must embed the workflowId for correlation");
        executionId.Should().NotMatchRegex(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            "a random UUID is misleading — GetExecutionAsync would return null with no explanation");
    }
}
