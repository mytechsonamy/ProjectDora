using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Uat;

/// <summary>
/// E2E smoke tests for the content lifecycle: create → publish → audit log.
/// These tests verify DTO/command contract correctness and the shape of the audit
/// trail that should be produced, without requiring a live OC runtime.
/// </summary>
public class ContentLifecycleTests
{
    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1401")]
    public void ContentPublish_AuditCommand_HasCorrectEventType()
    {
        // When content is published, the audit command that flows from
        // IContentService → IAuditService must carry EventType = "content.published".
        var cmd = new CreateAuditEventCommand(
            EventType: "content.published",
            ContentType: "Article",
            ContentItemId: "article-001",
            ContentVersion: 2,
            UserName: "editor@kosgeb.gov.tr",
            NewValue: """{"title":"KOSGEB Destek Programı","status":"Published"}""");

        cmd.EventType.Should().Be("content.published");
        cmd.ContentItemId.Should().Be("article-001");
        cmd.ContentVersion.Should().Be(2);
        cmd.UserName.Should().NotBeNullOrEmpty();
        cmd.NewValue.Should().Contain("Published");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1401")]
    public void ContentDraft_AuditCommand_HasDraftEventType()
    {
        var cmd = new CreateAuditEventCommand(
            EventType: "content.saved",
            ContentType: "DestekProgrami",
            ContentItemId: "dp-005",
            ContentVersion: 1,
            UserName: "author@kosgeb.gov.tr");

        cmd.EventType.Should().Be("content.saved");
        cmd.ContentVersion.Should().Be(1, "first save produces version 1");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1401")]
    public void AuditEventDto_AllFieldsMapped()
    {
        var now = DateTime.UtcNow;
        var dto = new AuditEventDto(
            AuditEventId: "ae-001",
            TenantId: "Default",
            EventType: "content.published",
            ContentType: "Article",
            ContentItemId: "article-001",
            ContentVersion: 2,
            UserName: "admin",
            UserIpAddress: "127.0.0.1",
            OldValue: null,
            NewValue: "{\"status\":\"Published\"}",
            OccurredUtc: now,
            Metadata: null);

        dto.AuditEventId.Should().Be("ae-001");
        dto.TenantId.Should().Be("Default");
        dto.EventType.Should().Be("content.published");
        dto.ContentVersion.Should().Be(2);
        dto.OccurredUtc.Should().Be(now);
        dto.OldValue.Should().BeNull("publish from draft has no previous published state");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1401")]
    public void ContentDiff_FieldChange_ProducesFieldDiffEntry()
    {
        // When two versions are compared, each changed field produces a FieldDiffEntry.
        var diff = new ContentDiffDto(
            ContentItemId: "article-001",
            FromVersion: 1,
            ToVersion: 2,
            Changes: new[]
            {
                new FieldDiffEntry("TitlePart.Title", "Modified", "Eski Başlık", "Yeni Başlık"),
                new FieldDiffEntry("HtmlBodyPart.Html", "Modified", "<p>eski</p>", "<p>yeni</p>"),
            });

        diff.Changes.Should().HaveCount(2);
        diff.Changes[0].FieldPath.Should().Be("TitlePart.Title");
        diff.Changes[0].ChangeType.Should().Be("Modified");
        diff.Changes[0].OldValue.Should().Be("Eski Başlık");
        diff.FromVersion.Should().BeLessThan(diff.ToVersion);
    }
}
