using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.Integration.Services;

namespace ProjectDora.Integration;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<INavigationProvider, IntegrationMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IIntegrationService, OrchardIntegrationService>();
    }
}
