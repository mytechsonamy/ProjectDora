var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrchardCms();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseOrchardCore();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program;
