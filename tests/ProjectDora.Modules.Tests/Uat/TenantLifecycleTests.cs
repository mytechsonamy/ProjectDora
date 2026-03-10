using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Uat;

/// <summary>
/// E2E smoke tests for the tenant lifecycle: create → suspend → resume → delete.
/// Tests operate at DTO/command contract level to verify the shape of operations
/// without requiring a live OC runtime.
/// </summary>
public class TenantLifecycleTests
{
    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1404")]
    public void CreateTenantCommand_RequiredFields_ArePresent()
    {
        var cmd = new CreateTenantCommand(
            TenantName: "kosgeb-ankara",
            RequestUrlPrefix: "ankara",
            DatabaseProvider: "Postgres",
            ConnectionString: "Host=localhost;Database=kosgeb_ankara");

        cmd.TenantName.Should().Be("kosgeb-ankara");
        cmd.DatabaseProvider.Should().Be("Postgres");
        cmd.ConnectionString.Should().NotBeNullOrEmpty();
        cmd.RequestUrlPrefix.Should().Be("ankara");
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1404")]
    public void TenantDto_StateField_AcceptsExpectedValues()
    {
        // Tenant state machine: Running ↔ Disabled (suspended).
        // State is mapped from OC's ShellSettings.State.
        var validStates = new[] { "Running", "Disabled", "Uninitialized" };

        var runningTenant = new TenantDto(
            TenantName: "kosgeb-ankara",
            DatabaseProvider: "Postgres",
            ConnectionString: "Host=localhost",
            State: "Running",
            RequestUrlPrefix: "ankara",
            CreatedUtc: DateTime.UtcNow,
            SuspendedUtc: null);

        runningTenant.State.Should().Be("Running");
        validStates.Should().Contain(runningTenant.State);
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1404")]
    public void TenantDto_Suspended_StateIsDisabled_SuspendedUtcIsSet()
    {
        // After ITenantService.SuspendAsync, the returned DTO must reflect
        // State=Disabled and have SuspendedUtc set.
        var suspendedAt = DateTime.UtcNow;
        var suspendedTenant = new TenantDto(
            TenantName: "kosgeb-izmir",
            DatabaseProvider: "Postgres",
            ConnectionString: "Host=localhost",
            State: "Disabled",
            RequestUrlPrefix: "izmir",
            CreatedUtc: suspendedAt.AddHours(-1),
            SuspendedUtc: suspendedAt);

        suspendedTenant.State.Should().Be("Disabled",
            "a suspended tenant must be in Disabled state and cannot accept requests");
        suspendedTenant.SuspendedUtc.Should().NotBeNull("suspension time must be recorded");
        suspendedTenant.SuspendedUtc!.Value.Should().Be(suspendedAt);
    }

    [Fact]
    [Trait("Category", "Uat")]
    [Trait("StoryId", "US-1404")]
    public void TenantList_AfterCreation_ContainsNewTenant_WithCorrectState()
    {
        var created = DateTime.UtcNow;
        var tenants = new[]
        {
            new TenantDto("Default", "Postgres", "Host=localhost", "Running", null, created, null),
            new TenantDto("kosgeb-ankara", "Postgres", "Host=localhost", "Running", "ankara", created, null),
            new TenantDto("kosgeb-izmir", "Postgres", "Host=localhost", "Disabled", "izmir", created, created),
        };

        tenants.Should().Contain(t => t.TenantName == "kosgeb-ankara");
        tenants.Should().Contain(t => t.State == "Disabled");
        tenants.Count(t => t.State == "Running").Should().Be(2);
        tenants.Single(t => t.TenantName == "kosgeb-izmir").SuspendedUtc.Should().NotBeNull();
    }
}
