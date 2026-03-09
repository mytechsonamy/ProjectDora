using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Workflows;

public class WorkflowDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Dto_WorkflowDefDto_CreatesWithRequiredProperties()
    {
        var activities = new List<WorkflowActivityDto>
        {
            new("trigger-1", "ContentPublishedEvent", "ContentPublishedEvent", null, 100, 100),
            new("notify-1", "SendEmailTask", "SendEmailTask", null, 300, 100),
        };

        var transitions = new List<WorkflowTransitionDto>
        {
            new("trigger-1", "Done", "notify-1"),
        };

        var dto = new WorkflowDefDto(
            "wf-001",
            "IcerikOnayAkisi",
            "İçerik Onay İş Akışı",
            true,
            activities,
            transitions,
            DateTime.UtcNow,
            DateTime.UtcNow);

        dto.WorkflowId.Should().Be("wf-001");
        dto.Name.Should().Be("IcerikOnayAkisi");
        dto.DisplayName.Should().Contain("İçerik");
        dto.IsEnabled.Should().BeTrue();
        dto.Activities.Should().HaveCount(2);
        dto.Transitions.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Dto_CreateWorkflowCommand_DefaultsToDisabled()
    {
        var activities = new List<WorkflowActivityDto>
        {
            new("trigger-1", "ContentCreatedEvent", "ContentCreatedEvent", null, 0, 0),
        };

        var command = new CreateWorkflowCommand("DestekAkisi", "Destek Başvuru Akışı", activities);

        command.IsEnabled.Should().BeFalse();
        command.Transitions.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Dto_UpdateWorkflowCommand_AllNullByDefault()
    {
        var command = new UpdateWorkflowCommand();

        command.DisplayName.Should().BeNull();
        command.Activities.Should().BeNull();
        command.Transitions.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-704")]
    public void Workflow_Dto_WorkflowActivityDto_PreservesProperties()
    {
        var contentTypes = new[] { "DestekProgrami", "Duyuru" };
        var props = new Dictionary<string, object>
        {
            { "ContentTypes", contentTypes },
            { "Owner", "admin" },
        };

        var dto = new WorkflowActivityDto("trigger-1", "ContentPublishedEvent", "ContentPublishedEvent", props, 150, 200);

        dto.ActivityId.Should().Be("trigger-1");
        dto.ActivityType.Should().Be("ContentPublishedEvent");
        dto.Properties.Should().ContainKey("ContentTypes");
        dto.X.Should().Be(150);
        dto.Y.Should().Be(200);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-706")]
    public void Workflow_Dto_WorkflowExecutionDto_TracksDuration()
    {
        var started = DateTime.UtcNow.AddMinutes(-5);
        var completed = DateTime.UtcNow;

        var dto = new WorkflowExecutionDto(
            "exec-001",
            "wf-001",
            "IcerikOnayAkisi",
            "Completed",
            "ContentPublishedEvent",
            started,
            completed,
            null,
            null);

        dto.ExecutionId.Should().Be("exec-001");
        dto.Status.Should().Be("Completed");
        dto.TriggerEvent.Should().Be("ContentPublishedEvent");
        dto.ErrorMessage.Should().BeNull();
        dto.CompletedUtc.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-706")]
    public void Workflow_Dto_WorkflowExecutionDto_FaultedState()
    {
        var dto = new WorkflowExecutionDto(
            "exec-002",
            "wf-001",
            "HataliAkis",
            "Faulted",
            "TimerEvent",
            DateTime.UtcNow,
            null,
            "Activity 'SendEmailTask' failed: SMTP connection refused",
            null);

        dto.Status.Should().Be("Faulted");
        dto.CompletedUtc.Should().BeNull();
        dto.ErrorMessage.Should().Contain("SMTP");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-708")]
    public void Workflow_Dto_ListExecutionsQuery_HasSensibleDefaults()
    {
        var query = new ListExecutionsQuery("wf-001");

        query.WorkflowId.Should().Be("wf-001");
        query.Status.Should().BeNull();
        query.FromDate.Should().BeNull();
        query.ToDate.Should().BeNull();
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-701")]
    public void Workflow_Dto_WorkflowDefDto_TurkishDisplayNamePreserved()
    {
        var dto = new WorkflowDefDto(
            "wf-002",
            "DestekBasvurusuOnayAkisi",
            "Şırnak İlçesi KOBİ Destek Başvurusu Onay İş Akışı",
            false,
            Array.Empty<WorkflowActivityDto>(),
            Array.Empty<WorkflowTransitionDto>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        dto.DisplayName.Should().Contain("Şırnak");
        dto.DisplayName.Should().Contain("KOBİ");
        dto.DisplayName.Should().Contain("Başvurusu");
        dto.IsEnabled.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-707")]
    public void Workflow_Dto_ActivityLogEntryDto_TracksExecution()
    {
        var started = DateTime.UtcNow.AddSeconds(-3);
        var completed = DateTime.UtcNow;

        var entry = new ActivityLogEntryDto(
            "notify-1",
            "SendEmailTask",
            "Done",
            started,
            completed);

        entry.ActivityId.Should().Be("notify-1");
        entry.ActivityName.Should().Be("SendEmailTask");
        entry.Outcome.Should().Be("Done");
        entry.CompletedUtc.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-705")]
    public void Workflow_Dto_WorkflowTransitionDto_ConnectsActivities()
    {
        var transition = new WorkflowTransitionDto(
            "trigger-1",
            "Done",
            "notify-1");

        transition.SourceActivityId.Should().Be("trigger-1");
        transition.SourceOutcomeName.Should().Be("Done");
        transition.DestinationActivityId.Should().Be("notify-1");
    }
}
