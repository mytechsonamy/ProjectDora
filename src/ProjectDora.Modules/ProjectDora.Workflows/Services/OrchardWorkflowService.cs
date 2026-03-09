using System.Globalization;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Workflows.Services;

public sealed class OrchardWorkflowService : IWorkflowService
{
    private readonly IWorkflowTypeStore _workflowTypeStore;
    private readonly IWorkflowManager _workflowManager;

    public OrchardWorkflowService(
        IWorkflowTypeStore workflowTypeStore,
        IWorkflowManager workflowManager)
    {
        _workflowTypeStore = workflowTypeStore;
        _workflowManager = workflowManager;
    }

    public async Task<WorkflowDefDto> CreateAsync(CreateWorkflowCommand command)
    {
        var workflowType = new WorkflowType
        {
            Name = command.Name,
            IsEnabled = command.IsEnabled,
        };

        await _workflowTypeStore.SaveAsync(workflowType);

        return MapToDto(workflowType, command.Activities, command.Transitions);
    }

    public async Task<WorkflowDefDto?> GetAsync(string workflowId)
    {
        var id = long.Parse(workflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(id);
        return workflowType is null ? null : MapToDto(workflowType);
    }

    public async Task<IReadOnlyList<WorkflowDefDto>> ListAsync()
    {
        var workflowTypes = await _workflowTypeStore.ListAsync();
        return workflowTypes.Select(wt => MapToDto(wt)).ToList();
    }

    public async Task<WorkflowDefDto> UpdateAsync(string workflowId, UpdateWorkflowCommand command)
    {
        var id = long.Parse(workflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(id)
            ?? throw new KeyNotFoundException($"Workflow '{workflowId}' not found.");

        if (!string.IsNullOrEmpty(command.DisplayName))
        {
            workflowType.Name = command.DisplayName;
        }

        await _workflowTypeStore.SaveAsync(workflowType);

        return MapToDto(workflowType, command.Activities, command.Transitions);
    }

    public async Task DeleteAsync(string workflowId)
    {
        var id = long.Parse(workflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(id)
            ?? throw new KeyNotFoundException($"Workflow '{workflowId}' not found.");

        await _workflowTypeStore.DeleteAsync(workflowType);
    }

    public async Task EnableAsync(string workflowId)
    {
        var id = long.Parse(workflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(id)
            ?? throw new KeyNotFoundException($"Workflow '{workflowId}' not found.");

        workflowType.IsEnabled = true;
        await _workflowTypeStore.SaveAsync(workflowType);
    }

    public async Task DisableAsync(string workflowId)
    {
        var id = long.Parse(workflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(id)
            ?? throw new KeyNotFoundException($"Workflow '{workflowId}' not found.");

        workflowType.IsEnabled = false;
        await _workflowTypeStore.SaveAsync(workflowType);
    }

    public async Task<string> TriggerAsync(string workflowId, IDictionary<string, object>? context = null)
    {
        var id = long.Parse(workflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(id)
            ?? throw new KeyNotFoundException($"Workflow '{workflowId}' not found.");

        if (!workflowType.IsEnabled)
        {
            throw new InvalidOperationException($"Workflow '{workflowType.Name}' is disabled and cannot be triggered.");
        }

        var input = context ?? new Dictionary<string, object>();
        await _workflowManager.TriggerEventAsync(
            "Signal",
            input,
            correlationId: workflowId);

        return Guid.NewGuid().ToString();
    }

    public Task<WorkflowExecutionDto?> GetExecutionAsync(string executionId)
    {
        // Execution retrieval requires IWorkflowStore
        return Task.FromResult<WorkflowExecutionDto?>(null);
    }

    public Task<PagedResult<WorkflowExecutionDto>> ListExecutionsAsync(ListExecutionsQuery query)
    {
        var result = new PagedResult<WorkflowExecutionDto>(
            Array.Empty<WorkflowExecutionDto>(),
            0,
            query.Page,
            query.PageSize);

        return Task.FromResult(result);
    }

    private static WorkflowDefDto MapToDto(
        WorkflowType wt,
        IReadOnlyList<WorkflowActivityDto>? activities = null,
        IReadOnlyList<WorkflowTransitionDto>? transitions = null)
    {
        return new WorkflowDefDto(
            wt.Id.ToString(CultureInfo.InvariantCulture),
            wt.Name,
            wt.Name,
            wt.IsEnabled,
            activities ?? Array.Empty<WorkflowActivityDto>(),
            transitions ?? Array.Empty<WorkflowTransitionDto>(),
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}
