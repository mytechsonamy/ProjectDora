using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Models;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Infrastructure.Services;

public sealed class OrchardTenantService : ITenantService
{
    private readonly IShellHost _shellHost;
    private readonly IShellSettingsManager _shellSettingsManager;

    public OrchardTenantService(IShellHost shellHost, IShellSettingsManager shellSettingsManager)
    {
        _shellHost = shellHost;
        _shellSettingsManager = shellSettingsManager;
    }

    public async Task<TenantDto> CreateAsync(CreateTenantCommand command)
    {
        var settings = _shellSettingsManager.CreateDefaultSettings();
        settings.Name = command.TenantName;
        settings.RequestUrlPrefix = command.RequestUrlPrefix ?? command.TenantName.ToLowerInvariant();
        settings.State = TenantState.Uninitialized;
        settings["DatabaseProvider"] = command.DatabaseProvider;
        settings["ConnectionString"] = command.ConnectionString ?? string.Empty;

        await _shellSettingsManager.SaveSettingsAsync(settings);
        await _shellHost.UpdateShellSettingsAsync(settings);

        return MapToDto(settings, DateTime.UtcNow, null);
    }

    public Task<TenantDto?> GetAsync(string tenantName)
    {
        if (_shellHost.TryGetSettings(tenantName, out var settings))
        {
            return Task.FromResult<TenantDto?>(MapToDto(settings, DateTime.UtcNow, null));
        }

        return Task.FromResult<TenantDto?>(null);
    }

    public Task<IReadOnlyList<TenantDto>> ListAsync()
    {
        var allSettings = _shellHost.GetAllSettings();
        IReadOnlyList<TenantDto> result = allSettings
            .Select(s => MapToDto(s, DateTime.UtcNow, null))
            .ToList();

        return Task.FromResult(result);
    }

    public async Task SuspendAsync(string tenantName)
    {
        if (!_shellHost.TryGetSettings(tenantName, out var settings))
        {
            throw new KeyNotFoundException($"Tenant '{tenantName}' not found.");
        }

        settings.State = TenantState.Disabled;
        await _shellSettingsManager.SaveSettingsAsync(settings);
        await _shellHost.UpdateShellSettingsAsync(settings);
    }

    public async Task ResumeAsync(string tenantName)
    {
        if (!_shellHost.TryGetSettings(tenantName, out var settings))
        {
            throw new KeyNotFoundException($"Tenant '{tenantName}' not found.");
        }

        settings.State = TenantState.Running;
        await _shellSettingsManager.SaveSettingsAsync(settings);
        await _shellHost.UpdateShellSettingsAsync(settings);
    }

    public async Task DeleteAsync(string tenantName)
    {
        if (!_shellHost.TryGetSettings(tenantName, out var settings))
        {
            throw new KeyNotFoundException($"Tenant '{tenantName}' not found.");
        }

        await _shellSettingsManager.RemoveSettingsAsync(settings);
    }

    private static TenantDto MapToDto(ShellSettings settings, DateTime createdUtc, DateTime? suspendedUtc)
    {
        return new TenantDto(
            settings.Name,
            settings["DatabaseProvider"] ?? "Postgres",
            settings["ConnectionString"] ?? string.Empty,
            settings.State.ToString(),
            settings.RequestUrlPrefix,
            createdUtc,
            settings.State == TenantState.Disabled ? suspendedUtc ?? DateTime.UtcNow : null);
    }
}
