using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OrchardCore.ResourceManagement;

namespace ProjectDora.AdminPanel.Filters;

/// <summary>
/// Her admin sayfasında KOSGEB özel stillerini otomatik olarak yükler.
/// </summary>
public sealed class AdminStylesFilter : IAsyncResultFilter
{
    private readonly IResourceManager _resourceManager;

    public AdminStylesFilter(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult)
        {
            _resourceManager.RegisterResource("stylesheet", "kosgeb-admin").AtHead();
        }

        await next();
    }
}
