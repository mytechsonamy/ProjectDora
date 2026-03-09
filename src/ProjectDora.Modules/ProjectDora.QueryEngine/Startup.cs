using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.QueryEngine.Services;

namespace ProjectDora.QueryEngine;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, QueryEngineMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IQueryService, OrchardQueryService>();
    }
}
