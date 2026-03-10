using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.ThemeManagement.Services;

namespace ProjectDora.ThemeManagement;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, ThemeManagementMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IThemeService, OrchardThemeService>();
    }
}
