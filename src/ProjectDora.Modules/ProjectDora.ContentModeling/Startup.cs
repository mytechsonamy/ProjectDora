using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;
using ProjectDora.ContentModeling.Services;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.ContentModeling;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INavigationProvider, ContentModelingMenu>();
        services.AddScoped<IPermissionProvider, Permissions>();
        services.AddScoped<IContentTypeService, OrchardContentTypeService>();
        services.AddScoped<IContentService, OrchardContentService>();
    }
}
