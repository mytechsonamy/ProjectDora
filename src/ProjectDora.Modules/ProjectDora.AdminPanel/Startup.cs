using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.ResourceManagement;
using OrchardCore.Security.Permissions;
using ProjectDora.AdminPanel.Filters;

namespace ProjectDora.AdminPanel;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, AdminMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();

        // KOSGEB admin theme
        services.AddTransient<IConfigureOptions<ResourceManagementOptions>, AdminResourceManifestConfiguration>();
        services.Configure<MvcOptions>(o => o.Filters.Add<AdminStylesFilter>());
    }
}
