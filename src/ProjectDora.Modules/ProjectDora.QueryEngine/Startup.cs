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
        // Register concrete Lucene rebuilder so OrchardQueryService.ReindexAsync() resolves
        // to a real implementation instead of null (which throws at runtime).
        // LuceneIndexRebuilderAdapter depends on OC Search.Lucene services; if the Lucene
        // module is not enabled in a deployment the DI container will surface a missing
        // dependency error at startup — intentional, as ReindexAsync requires Lucene.
        services.AddScoped<ILuceneIndexRebuilder, LuceneIndexRebuilderAdapter>();
    }
}
