using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.AuditTrail;

public class AuditDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_Dto_AuditEventDto_PreservesAllMetadata()
    {
        var occurred = DateTime.UtcNow;

        var dto = new AuditEventDto(
            "evt-001",
            "tenant-1",
            "ContentPublished",
            "DestekProgrami",
            "content-001",
            ContentVersion: 3,
            UserName: "editor@kosgeb.gov.tr",
            UserIpAddress: "192.168.1.100",
            OldValue: null,
            NewValue: "{\"Title\":\"Destek Programı\"}",
            OccurredUtc: occurred,
            Metadata: null);

        dto.AuditEventId.Should().Be("evt-001");
        dto.EventType.Should().Be("ContentPublished");
        dto.ContentType.Should().Be("DestekProgrami");
        dto.ContentItemId.Should().Be("content-001");
        dto.ContentVersion.Should().Be(3);
        dto.UserName.Should().Be("editor@kosgeb.gov.tr");
        dto.UserIpAddress.Should().Be("192.168.1.100");
        dto.OccurredUtc.Should().Be(occurred);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_Dto_CreateAuditEventCommand_MinimalRequiredFields()
    {
        var command = new CreateAuditEventCommand(
            "ContentUpdated",
            "Duyuru",
            "content-002",
            ContentVersion: 1,
            UserName: "admin");

        command.EventType.Should().Be("ContentUpdated");
        command.ContentType.Should().Be("Duyuru");
        command.UserIpAddress.Should().BeNull();
        command.OldValue.Should().BeNull();
        command.NewValue.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-902")]
    public void AuditTrail_Dto_ListAuditEventsQuery_HasSensibleDefaults()
    {
        var query = new ListAuditEventsQuery();

        query.ContentType.Should().BeNull();
        query.UserName.Should().BeNull();
        query.FromDate.Should().BeNull();
        query.ToDate.Should().BeNull();
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-902")]
    public void AuditTrail_Dto_ListAuditEventsQuery_FilterByContentType()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var query = new ListAuditEventsQuery(
            ContentType: "DestekProgrami",
            FromDate: from,
            ToDate: to,
            PageSize: 50);

        query.ContentType.Should().Be("DestekProgrami");
        query.FromDate.Should().Be(from);
        query.ToDate.Should().Be(to);
        query.PageSize.Should().Be(50);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-904")]
    public void AuditTrail_Dto_ContentDiffDto_TracksVersionRange()
    {
        var changes = new[]
        {
            new FieldDiffEntry("Title.tr", "Modified", "Eski Başlık", "Yeni Başlık"),
            new FieldDiffEntry("Body.tr", "Modified", "Eski içerik", "Yeni içerik"),
        };

        var diff = new ContentDiffDto("content-001", 2, 3, changes);

        diff.ContentItemId.Should().Be("content-001");
        diff.FromVersion.Should().Be(2);
        diff.ToVersion.Should().Be(3);
        diff.Changes.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-904")]
    public void AuditTrail_Dto_FieldDiffEntry_ChangeTypes()
    {
        var added = new FieldDiffEntry("Tags", "Added", null, "[\"KOBİ\",\"Destek\"]");
        var removed = new FieldDiffEntry("Subtitle", "Removed", "Eski Alt Başlık", null);
        var modified = new FieldDiffEntry("Title.tr", "Modified", "Eski", "Yeni");

        added.ChangeType.Should().Be("Added");
        added.OldValue.Should().BeNull();
        removed.ChangeType.Should().Be("Removed");
        removed.NewValue.Should().BeNull();
        modified.ChangeType.Should().Be("Modified");
        modified.OldValue.Should().NotBeNull();
        modified.NewValue.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-906")]
    public void AuditTrail_Dto_AuditSettingsDto_DefaultRetentionIs365Days()
    {
        var settings = new AuditSettingsDto(
            IsEnabled: true,
            RetentionDays: 365,
            MaxRecords: 1_000_000,
            AuditedContentTypes: Array.Empty<string>(),
            PurgeSchedule: "0 2 * * *");

        settings.IsEnabled.Should().BeTrue();
        settings.RetentionDays.Should().Be(365);
        settings.MaxRecords.Should().Be(1_000_000);
        settings.PurgeSchedule.Should().Be("0 2 * * *");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-903")]
    public void AuditTrail_Dto_AuditSettingsDto_ContentTypeScopingPreservesTypes()
    {
        var settings = new AuditSettingsDto(
            true,
            365,
            1_000_000,
            new[] { "DestekProgrami", "Duyuru", "KobiProfili" },
            "0 2 * * *");

        settings.AuditedContentTypes.Should().HaveCount(3);
        settings.AuditedContentTypes.Should().Contain("DestekProgrami");
        settings.AuditedContentTypes.Should().Contain("Duyuru");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-906")]
    public void AuditTrail_Dto_UpdateAuditSettingsCommand_AllNullByDefault()
    {
        var command = new UpdateAuditSettingsCommand();

        command.IsEnabled.Should().BeNull();
        command.RetentionDays.Should().BeNull();
        command.AuditedContentTypes.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-901")]
    public void AuditTrail_Dto_AuditEventDto_TurkishUserNamePreserved()
    {
        var dto = new AuditEventDto(
            "evt-002",
            "tenant-1",
            "ContentDeleted",
            "KobiProfili",
            "content-003",
            null,
            "mehmet.yılmaz@kosgeb.gov.tr",
            null,
            null,
            null,
            DateTime.UtcNow,
            null);

        dto.UserName.Should().Contain("yılmaz");
        dto.EventType.Should().Be("ContentDeleted");
    }
}
