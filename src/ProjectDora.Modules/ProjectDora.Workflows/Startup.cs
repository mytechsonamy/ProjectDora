using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.Core.Abstractions;
using ProjectDora.Workflows.Services;

namespace ProjectDora.Workflows;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, WorkflowMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IWorkflowService, OrchardWorkflowService>();
    }
}
