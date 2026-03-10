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
}
