using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace ProjectDora.AdminPanel;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, AdminMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
    }
}
