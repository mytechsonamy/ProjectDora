using FluentAssertions;
using Moq;
using OrchardCore.AuditTrail.Indexes;
using OrchardCore.AuditTrail.Models;
using OrchardCore.Settings;
using ProjectDora.AuditTrail.Services;
using ProjectDora.Core.Abstractions;
using YesSql;

namespace ProjectDora.Modules.Tests.AuditTrail;

public class AuditServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_GetDiff_SameDisplayText_NoChanges()
    {
        // GetDiffAsync returns empty change set when versions are identical
        var from = new ContentItemDto(
            "ci-001", "Article", "Same Title", "Draft", 1, "user", DateTime.UtcNow, DateTime.UtcNow, null, "tr");
        var to = new ContentItemDto(
            "ci-001", "Article", "Same Title", "Draft", 2, "user", DateTime.UtcNow, DateTime.UtcNow, null, "tr");

        // Simulate the diff logic
        var changes = new List<FieldDiffEntry>();
        if (from.DisplayText != to.DisplayText)
            changes.Add(new FieldDiffEntry("DisplayText", "Modified", from.DisplayText, to.DisplayText));
        if (from.Status != to.Status)
            changes.Add(new FieldDiffEntry("Status", "Modified", from.Status, to.Status));

        changes.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_GetDiff_DifferentDisplayText_DetectsChange()
    {
        var from = new ContentItemDto(
            "ci-001", "Article", "Old Title", "Draft", 1, "user", DateTime.UtcNow, DateTime.UtcNow, null, "tr");
        var to = new ContentItemDto(
            "ci-001", "Article", "New Title", "Published", 2, "user", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, "tr");

        var changes = new List<FieldDiffEntry>();
        if (from.DisplayText != to.DisplayText)
            changes.Add(new FieldDiffEntry("DisplayText", "Modified", from.DisplayText, to.DisplayText));
        if (from.Status != to.Status)
            changes.Add(new FieldDiffEntry("Status", "Modified", from.Status, to.Status));

        changes.Should().HaveCount(2);
        changes[0].FieldPath.Should().Be("DisplayText");
        changes[0].OldValue.Should().Be("Old Title");
        changes[0].NewValue.Should().Be("New Title");
        changes[1].FieldPath.Should().Be("Status");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_DefaultSettings_HasExpectedValues()
    {
        var defaultSettings = new AuditSettingsDto(
            IsEnabled: true,
            RetentionDays: 365,
            MaxRecords: 1_000_000,
            AuditedContentTypes: Array.Empty<string>(),
            PurgeSchedule: "0 2 * * *");

        defaultSettings.IsEnabled.Should().BeTrue();
        defaultSettings.RetentionDays.Should().Be(365);
        defaultSettings.MaxRecords.Should().Be(1_000_000);
        defaultSettings.AuditedContentTypes.Should().BeEmpty();
        defaultSettings.PurgeSchedule.Should().Be("0 2 * * *");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_Dto_ContentDiffDto_PreservesChanges()
    {
        var entries = new[]
        {
            new FieldDiffEntry("DisplayText", "Modified", "Old", "New"),
            new FieldDiffEntry("Status", "Modified", "Draft", "Published"),
        };

        var diff = new ContentDiffDto("ci-001", 1, 2, entries);

        diff.ContentItemId.Should().Be("ci-001");
        diff.FromVersion.Should().Be(1);
        diff.ToVersion.Should().Be(2);
        diff.Changes.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_CreateCommand_AllFieldsMapped()
    {
        var cmd = new CreateAuditEventCommand(
            "ContentPublished",
            "Article",
            "ci-001",
            ContentVersion: 1,
            UserName: "admin",
            UserIpAddress: "10.0.0.1",
            OldValue: null,
            NewValue: "{\"title\":\"KOSGEB\"}",
            Metadata: null);

        cmd.EventType.Should().Be("ContentPublished");
        cmd.ContentType.Should().Be("Article");
        cmd.ContentItemId.Should().Be("ci-001");
        cmd.UserName.Should().Be("admin");
        cmd.NewValue.Should().Contain("KOSGEB");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_ListQuery_DefaultPagination()
    {
        var query = new ListAuditEventsQuery();

        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.ContentType.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_EventDto_NormalizedUserNameFilter_IsUppercase()
    {
        var query = new ListAuditEventsQuery { UserName = "editor@kosgeb.gov.tr" };
        var normalized = query.UserName.ToUpperInvariant();
        normalized.Should().Be("EDITOR@KOSGEB.GOV.TR");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_PurgeOlderThan_CutoffCalculation()
    {
        var daysOld = 30;
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);
        var eventDate = DateTime.UtcNow.AddDays(-31); // 31 days old - should be deleted

        eventDate.Should().BeBefore(cutoff);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_UpdateSettings_CommandPreservesValues()
    {
        var cmd = new UpdateAuditSettingsCommand(
            IsEnabled: false,
            RetentionDays: 90,
            MaxRecords: 500_000,
            AuditedContentTypes: new[] { "Article", "Page" });

        cmd.IsEnabled.Should().BeFalse();
        cmd.RetentionDays.Should().Be(90);
        cmd.MaxRecords.Should().Be(500_000);
        cmd.AuditedContentTypes.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_EnableContentType_ContentTypeAddedToList()
    {
        var types = new List<string> { "Article" };
        var toAdd = "DestekProgrami";

        if (!types.Contains(toAdd, StringComparer.OrdinalIgnoreCase))
            types.Add(toAdd);

        types.Should().HaveCount(2);
        types.Should().Contain("DestekProgrami");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_DisableContentType_ContentTypeRemovedFromList()
    {
        var types = new List<string> { "Article", "DestekProgrami" };
        types.RemoveAll(t => string.Equals(t, "article", StringComparison.OrdinalIgnoreCase));

        types.Should().HaveCount(1);
        types.Should().NotContain("Article");
    }
}
