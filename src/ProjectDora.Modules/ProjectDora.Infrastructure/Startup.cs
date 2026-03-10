using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.Infrastructure.Services;

namespace ProjectDora.Infrastructure;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, InfrastructureMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<ITenantService, OrchardTenantService>();
        services.AddScoped<ICacheService, OrchardCacheService>();
        services.AddScoped<IRecipeService, OrchardRecipeService>();
    }
}
