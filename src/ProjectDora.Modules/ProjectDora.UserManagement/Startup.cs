using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.UserManagement.Services;

namespace ProjectDora.UserManagement;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, UserManagementMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IUserService, OrchardUserService>();
        services.AddScoped<ProjectDora.Core.Abstractions.IRoleService, OrchardRoleService>();
        services.AddScoped<IAuthService, OrchardAuthService>();
    }
}
