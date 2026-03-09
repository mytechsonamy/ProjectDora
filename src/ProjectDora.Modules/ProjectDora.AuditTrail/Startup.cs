using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.AuditTrail.Services;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.AuditTrail;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, AuditTrailMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IAuditService, OrchardAuditService>();
    }
}
