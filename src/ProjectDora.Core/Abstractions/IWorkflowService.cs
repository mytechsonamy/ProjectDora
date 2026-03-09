namespace ProjectDora.Core.Abstractions;

/// <summary>
/// Workflow definition and execution — wraps Orchard Core's workflow infrastructure.
/// </summary>
public interface IWorkflowService
{
    Task<WorkflowDefDto> CreateAsync(CreateWorkflowCommand command);
    Task<WorkflowDefDto?> GetAsync(string workflowId);
    Task<IReadOnlyList<WorkflowDefDto>> ListAsync();
    Task<WorkflowDefDto> UpdateAsync(string workflowId, UpdateWorkflowCommand command);
    Task DeleteAsync(string workflowId);
    Task EnableAsync(string workflowId);
    Task DisableAsync(string workflowId);
    Task<string> TriggerAsync(string workflowId, IDictionary<string, object>? context = null);
    Task<WorkflowExecutionDto?> GetExecutionAsync(string executionId);
    Task<PagedResult<WorkflowExecutionDto>> ListExecutionsAsync(ListExecutionsQuery query);
}

public record WorkflowDefDto(
    string WorkflowId,
    string Name,
    string DisplayName,
    bool IsEnabled,
    IReadOnlyList<WorkflowActivityDto> Activities,
    IReadOnlyList<WorkflowTransitionDto> Transitions,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public record WorkflowActivityDto(
    string ActivityId,
    string Name,
    string ActivityType,
    IDictionary<string, object>? Properties,
    int X,
    int Y);

public record WorkflowTransitionDto(
    string SourceActivityId,
    string SourceOutcomeName,
    string DestinationActivityId);

public record WorkflowExecutionDto(
    string ExecutionId,
    string WorkflowId,
    string WorkflowName,
    string Status,
    string? TriggerEvent,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    string? ErrorMessage,
    IReadOnlyList<ActivityLogEntryDto>? ActivityLog);

public record ActivityLogEntryDto(
    string ActivityId,
    string ActivityName,
    string Outcome,
    DateTime StartedUtc,
    DateTime? CompletedUtc);

public record CreateWorkflowCommand(
    string Name,
    string DisplayName,
    IReadOnlyList<WorkflowActivityDto> Activities,
    IReadOnlyList<WorkflowTransitionDto>? Transitions = null,
    bool IsEnabled = false);

public record UpdateWorkflowCommand(
    string? DisplayName = null,
    IReadOnlyList<WorkflowActivityDto>? Activities = null,
    IReadOnlyList<WorkflowTransitionDto>? Transitions = null);

public record ListExecutionsQuery(
    string WorkflowId,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20);
