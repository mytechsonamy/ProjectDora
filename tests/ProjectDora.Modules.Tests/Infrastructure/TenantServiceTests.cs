using FluentAssertions;
using Moq;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using ProjectDora.Core.Abstractions;
using ProjectDora.Infrastructure.Services;

namespace ProjectDora.Modules.Tests.Infrastructure;

public class TenantServiceTests
{
    private readonly Mock<IShellHost> _shellHostMock = new();
    private readonly Mock<IShellSettingsManager> _shellSettingsManagerMock = new();

    private OrchardTenantService CreateSut() =>
        new(_shellHostMock.Object, _shellSettingsManagerMock.Object);

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task GetAsync_UnknownTenant_ReturnsNull()
    {
        ShellSettings? outSettings = null;
        _shellHostMock.Setup(h => h.TryGetSettings("unknown", out outSettings)).Returns(false);

        var sut = CreateSut();
        var result = await sut.GetAsync("unknown");

        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task GetAsync_KnownTenant_ReturnsTenantDto()
    {
        var settings = new ShellSettings { Name = "Tenant1", State = TenantState.Running };
        settings["DatabaseProvider"] = "Postgres";
        settings["ConnectionString"] = "Host=localhost";
        _shellHostMock.Setup(h => h.TryGetSettings("Tenant1", out settings)).Returns(true);

        var sut = CreateSut();
        var result = await sut.GetAsync("Tenant1");

        result.Should().NotBeNull();
        result!.TenantName.Should().Be("Tenant1");
        result.State.Should().Be("Running");
        result.DatabaseProvider.Should().Be("Postgres");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task ListAsync_ReturnsMappedDtos()
    {
        var settings1 = new ShellSettings { Name = "Default", State = TenantState.Running };
        settings1["DatabaseProvider"] = "Sqlite";
        var settings2 = new ShellSettings { Name = "KOSGEB", State = TenantState.Uninitialized };
        settings2["DatabaseProvider"] = "Postgres";

        _shellHostMock.Setup(h => h.GetAllSettings()).Returns(new[] { settings1, settings2 });

        var sut = CreateSut();
        var result = await sut.ListAsync();

        result.Should().HaveCount(2);
        result.Select(t => t.TenantName).Should().Contain("Default").And.Contain("KOSGEB");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task SuspendAsync_UnknownTenant_ThrowsKeyNotFound()
    {
        ShellSettings? outSettings = null;
        _shellHostMock.Setup(h => h.TryGetSettings("ghost", out outSettings)).Returns(false);

        var sut = CreateSut();
        await sut.Invoking(s => s.SuspendAsync("ghost"))
                 .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task SuspendAsync_KnownTenant_SetsDisabledState()
    {
        var settings = new ShellSettings { Name = "Tenant1", State = TenantState.Running };
        settings["DatabaseProvider"] = "Postgres";
        _shellHostMock.Setup(h => h.TryGetSettings("Tenant1", out settings)).Returns(true);
        _shellSettingsManagerMock.Setup(m => m.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        _shellHostMock.Setup(h => h.UpdateShellSettingsAsync(settings)).Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.SuspendAsync("Tenant1");

        settings.State.Should().Be(TenantState.Disabled);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task ResumeAsync_DisabledTenant_SetsRunningState()
    {
        var settings = new ShellSettings { Name = "Tenant1", State = TenantState.Disabled };
        settings["DatabaseProvider"] = "Postgres";
        _shellHostMock.Setup(h => h.TryGetSettings("Tenant1", out settings)).Returns(true);
        _shellSettingsManagerMock.Setup(m => m.SaveSettingsAsync(settings)).Returns(Task.CompletedTask);
        _shellHostMock.Setup(h => h.UpdateShellSettingsAsync(settings)).Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.ResumeAsync("Tenant1");

        settings.State.Should().Be(TenantState.Running);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public async Task DeleteAsync_UnknownTenant_ThrowsKeyNotFound()
    {
        ShellSettings? outSettings = null;
        _shellHostMock.Setup(h => h.TryGetSettings("ghost", out outSettings)).Returns(false);

        var sut = CreateSut();
        await sut.Invoking(s => s.DeleteAsync("ghost"))
                 .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1001")]
    public void TenantDto_DisabledState_HasSuspendedAt()
    {
        var suspended = DateTime.UtcNow;
        var dto = new TenantDto(
            "TestTenant", "Postgres", "Host=localhost", "Disabled",
            "test", DateTime.UtcNow, suspended);

        dto.State.Should().Be("Disabled");
        dto.SuspendedUtc.Should().Be(suspended);
    }
}
