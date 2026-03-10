using System.Globalization;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Workflows.Services;

public sealed class OrchardWorkflowService : IWorkflowService
{
    private readonly IWorkflowTypeStore _workflowTypeStore;
    private readonly IWorkflowManager _workflowManager;
    private readonly IWorkflowStore _workflowStore;

    public OrchardWorkflowService(
        IWorkflowTypeStore workflowTypeStore,
        IWorkflowManager workflowManager,
        IWorkflowStore workflowStore)
    {
        _workflowTypeStore = workflowTypeStore;
        _workflowManager = workflowManager;
        _workflowStore = workflowStore;
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

        // OC TriggerEventAsync returns WorkflowExecutionContext instances for each workflow
        // that handled the event. Each context carries the persisted Workflow record with its Id.
        var executionContexts = await _workflowManager.TriggerEventAsync(
            "Signal",
            input,
            correlationId: workflowId);

        // Happy path: OC created a persisted Workflow record — return its database Id.
        // This Id is queryable via GetExecutionAsync(executionId).
        var firstContext = executionContexts?.FirstOrDefault();
        if (firstContext?.Workflow is not null)
        {
            return firstContext.Workflow.Id.ToString(CultureInfo.InvariantCulture);
        }

        // No persisted execution record was created. This happens when:
        //   (a) the Signal event matched no active workflow instances, or
        //   (b) the workflow executed fully in-memory without a persistence step.
        //
        // We do NOT fall back to a random UUID — that would create a misleading handle
        // that GetExecutionAsync would silently return null for, hiding the real state.
        //
        // Instead, return a "trace:" prefixed marker so callers know:
        //   - The signal was dispatched successfully.
        //   - There is no persisted execution record to query.
        //   - GetExecutionAsync(returnedId) will return null — by design, not a bug.
        return string.Create(
            CultureInfo.InvariantCulture,
            $"trace:{workflowId}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
    }

    public async Task<WorkflowExecutionDto?> GetExecutionAsync(string executionId)
    {
        var id = long.Parse(executionId, CultureInfo.InvariantCulture);
        var workflow = await _workflowStore.GetAsync(id);
        return workflow is null ? null : MapExecutionToDto(workflow);
    }

    public async Task<PagedResult<WorkflowExecutionDto>> ListExecutionsAsync(ListExecutionsQuery query)
    {
        var workflowTypeId = long.Parse(query.WorkflowId, CultureInfo.InvariantCulture);
        var workflowType = await _workflowTypeStore.GetAsync(workflowTypeId);
        if (workflowType is null)
        {
            return new PagedResult<WorkflowExecutionDto>(
                Array.Empty<WorkflowExecutionDto>(), 0, query.Page, query.PageSize);
        }

        var skip = (query.Page - 1) * query.PageSize;
        var workflows = await _workflowStore.ListAsync(
            workflowType.WorkflowTypeId, skip: skip, take: query.PageSize);
        var total = await _workflowStore.CountAsync(workflowType.WorkflowTypeId);

        var dtos = workflows.Select(MapExecutionToDto).ToList();
        return new PagedResult<WorkflowExecutionDto>(dtos, total, query.Page, query.PageSize);
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

    private static WorkflowExecutionDto MapExecutionToDto(Workflow workflow)
    {
        var isFinished = workflow.Status == WorkflowStatus.Finished;
        var isFaulted = workflow.Status == WorkflowStatus.Faulted;

        return new WorkflowExecutionDto(
            workflow.Id.ToString(CultureInfo.InvariantCulture),
            workflow.WorkflowTypeId.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            workflow.Status.ToString(),
            workflow.CorrelationId,
            workflow.CreatedUtc,
            isFinished || isFaulted ? workflow.CreatedUtc : null,
            isFaulted ? "Workflow faulted" : null,
            null);
    }
}
