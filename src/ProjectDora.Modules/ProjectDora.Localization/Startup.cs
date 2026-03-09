using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.Localization.Services;

namespace ProjectDora.Localization;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, LocalizationMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<ILocalizationService, OrchardLocalizationService>();
    }
}
