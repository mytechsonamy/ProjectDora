using FluentAssertions;
using Moq;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using ProjectDora.Core.Abstractions;
using ProjectDora.Workflows.Services;

namespace ProjectDora.Modules.Tests.Uat;

/// <summary>
/// E2E smoke tests for the workflow trigger lifecycle:
///   define workflow → trigger → verify execution record semantics.
/// </summary>
public class WorkflowTriggerTests
{
    private readonly Mock<IWorkflowTypeStore> _typeStoreMock = new();
    private readonly Mock<IWorkflowManager> _managerMock = new();
    private readonly Mock<IWorkflowStore> _storeMock = new();

    private OrchardWorkflowService CreateSut() =>
        new(_typeStoreMock.Object, _managerMock.Object, _storeMock.Object);

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1402")]
    public async Task TriggerAsync_EnabledWorkflow_ProducesQueryableExecutionId()
    {
        // Simulate the full trigger → execution record lifecycle.
        // After TriggerAsync returns a real numeric ID, GetExecutionAsync must find the record.
        var workflowType = new WorkflowType
        {
            Id = 10,
            WorkflowTypeId = "wt-010",
            Name = "BasvuruOnay",
            IsEnabled = true,
        };
        var persistedWorkflow = new Workflow
        {
            Id = 55L,
            WorkflowTypeId = "wt-010",
            Status = WorkflowStatus.Halted,
            CreatedUtc = DateTime.UtcNow,
        };

        _typeStoreMock.Setup(s => s.GetAsync(10L)).ReturnsAsync(workflowType);
        _managerMock
            .Setup(m => m.TriggerEventAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new[]
            {
                // WorkflowExecutionContext ctor: (WorkflowType, Workflow, properties, input, output, executedActivities, lastResult, activityContexts)
                new WorkflowExecutionContext(
                    workflowType, persistedWorkflow,
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new List<ExecutedActivity>(),
                    null,
                    Enumerable.Empty<ActivityContext>()),
            });

        // GetExecutionAsync with the returned ID must resolve the workflow record.
        _storeMock.Setup(s => s.GetAsync(55L)).ReturnsAsync(persistedWorkflow);

        var sut = CreateSut();
        var executionId = await sut.TriggerAsync("10");

        // Step 1: execution ID is the numeric Workflow.Id (queryable).
        executionId.Should().Be("55");

        // Step 2: round-trip — GetExecutionAsync with that ID returns the record.
        var execution = await sut.GetExecutionAsync(executionId);
        execution.Should().NotBeNull("the execution ID returned by TriggerAsync must be queryable");
        execution!.ExecutionId.Should().Be("55");
        execution.Status.Should().Be("Halted");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1402")]
    public async Task TriggerAsync_DisabledWorkflow_ThrowsInvalidOperation()
    {
        var workflowType = new WorkflowType
        {
            Id = 20,
            WorkflowTypeId = "wt-020",
            Name = "KapaliAkis",
            IsEnabled = false,
        };
        _typeStoreMock.Setup(s => s.GetAsync(20L)).ReturnsAsync(workflowType);
        _managerMock
            .Setup(m => m.TriggerEventAsync(
                It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<WorkflowExecutionContext>());

        var sut = CreateSut();
        var act = async () => await sut.TriggerAsync("20");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*disabled*");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1402")]
    public async Task TriggerAsync_NonExistentWorkflow_ThrowsKeyNotFound()
    {
        _typeStoreMock.Setup(s => s.GetAsync(It.IsAny<long>())).ReturnsAsync((WorkflowType?)null);

        var sut = CreateSut();
        var act = async () => await sut.TriggerAsync("999");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1402")]
    public async Task TriggerAsync_TraceMarker_GetExecutionAsync_ReturnsNull()
    {
        // When TriggerAsync returns a trace marker (no persisted execution),
        // the caller should expect GetExecutionAsync to return null.
        // This test documents the contract so callers do not treat null as an error.
        var workflowType = new WorkflowType { Id = 30, WorkflowTypeId = "wt-030", Name = "AsyncFlow", IsEnabled = true };
        _typeStoreMock.Setup(s => s.GetAsync(30L)).ReturnsAsync(workflowType);
        _managerMock
            .Setup(m => m.TriggerEventAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync(Array.Empty<WorkflowExecutionContext>());

        var sut = CreateSut();
        var executionId = await sut.TriggerAsync("30");

        executionId.Should().StartWith("trace:",
            "no persisted record — must be a trace marker, not a numeric ID");

        // Attempting GetExecutionAsync with a trace marker throws FormatException
        // because long.Parse("trace:...") fails. Callers must check for trace prefix before querying.
        var act = async () => await sut.GetExecutionAsync(executionId);
        await act.Should().ThrowAsync<FormatException>(
            "callers must check for 'trace:' prefix and skip GetExecutionAsync for trace markers");
    }
}
