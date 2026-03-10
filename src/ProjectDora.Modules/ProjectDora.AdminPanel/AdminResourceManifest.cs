using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;

namespace ProjectDora.AdminPanel;

/// <summary>
/// KOSGEB admin teması için CSS kaynaklarını ResourceManagementOptions üzerinden kayıt eder.
/// </summary>
public sealed class AdminResourceManifestConfiguration : IConfigureOptions<ResourceManagementOptions>
{
    public void Configure(ResourceManagementOptions options)
    {
        var manifest = new ResourceManifest();

        manifest
            .DefineStyle("kosgeb-admin")
            .SetUrl("~/ProjectDora.AdminPanel/css/kosgeb-admin.css")
            .SetVersion("1.0.0");

        options.ResourceManifests.Add(manifest);
    }
}
