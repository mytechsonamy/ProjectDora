# Sprint 0 — Setup Guide

> Version: 1.0 | Last Updated: 2026-03-09

## 1. Purpose

Sprint 0 creates the project skeleton, development infrastructure, and CI pipeline. No user stories — only technical setup. By end of S0, every developer can clone, build, run, and test.

## 2. Solution Setup

### 2.1 Create Solution

```bash
# Create solution
dotnet new sln -n ProjectDora

# Create projects
dotnet new web -n ProjectDora.Web -o src/ProjectDora.Web
dotnet new classlib -n ProjectDora.Core -o src/ProjectDora.Core
dotnet new classlib -n ProjectDora.ContentModeling -o src/ProjectDora.Modules/ProjectDora.ContentModeling
dotnet new classlib -n ProjectDora.AuditTrail -o src/ProjectDora.Modules/ProjectDora.AuditTrail
dotnet new classlib -n ProjectDora.Workflows -o src/ProjectDora.Modules/ProjectDora.Workflows
dotnet new classlib -n ProjectDora.QueryEngine -o src/ProjectDora.Modules/ProjectDora.QueryEngine
dotnet new classlib -n ProjectDora.Integration -o src/ProjectDora.Modules/ProjectDora.Integration
dotnet new xunit -n ProjectDora.Core.Tests -o tests/ProjectDora.Core.Tests
dotnet new xunit -n ProjectDora.Modules.Tests -o tests/ProjectDora.Modules.Tests

# Add to solution
dotnet sln add src/ProjectDora.Web/ProjectDora.Web.csproj
dotnet sln add src/ProjectDora.Core/ProjectDora.Core.csproj
dotnet sln add src/ProjectDora.Modules/ProjectDora.ContentModeling/ProjectDora.ContentModeling.csproj
dotnet sln add src/ProjectDora.Modules/ProjectDora.AuditTrail/ProjectDora.AuditTrail.csproj
dotnet sln add src/ProjectDora.Modules/ProjectDora.Workflows/ProjectDora.Workflows.csproj
dotnet sln add src/ProjectDora.Modules/ProjectDora.QueryEngine/ProjectDora.QueryEngine.csproj
dotnet sln add src/ProjectDora.Modules/ProjectDora.Integration/ProjectDora.Integration.csproj
dotnet sln add tests/ProjectDora.Core.Tests/ProjectDora.Core.Tests.csproj
dotnet sln add tests/ProjectDora.Modules.Tests/ProjectDora.Modules.Tests.csproj

# Add project references
dotnet add src/ProjectDora.Web reference src/ProjectDora.Core
dotnet add src/ProjectDora.Modules/ProjectDora.ContentModeling reference src/ProjectDora.Core
dotnet add src/ProjectDora.Modules/ProjectDora.AuditTrail reference src/ProjectDora.Core
dotnet add tests/ProjectDora.Core.Tests reference src/ProjectDora.Core
dotnet add tests/ProjectDora.Modules.Tests reference src/ProjectDora.Core
```

### 2.2 Orchard Core NuGet Packages

```xml
<!-- src/ProjectDora.Web/ProjectDora.Web.csproj -->
<ItemGroup>
  <PackageReference Include="OrchardCore.Application.Cms.Targets" Version="2.1.0" />
</ItemGroup>

<!-- src/ProjectDora.Core/ProjectDora.Core.csproj -->
<ItemGroup>
  <PackageReference Include="OrchardCore.Module.Targets" Version="2.1.0" />
  <PackageReference Include="MediatR" Version="12.2.0" />
  <PackageReference Include="FluentValidation" Version="11.9.0" />
</ItemGroup>
```

### 2.3 Startup Configuration

```csharp
// src/ProjectDora.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrchardCms();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!)
    .AddElasticsearch(new Uri(builder.Configuration["Elasticsearch:Url"]!));

var app = builder.Build();

app.UseOrchardCore();
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();
```

## 3. Abstraction Layer Interfaces

Create empty interfaces in `ProjectDora.Core` — implementations come in later sprints:

```csharp
// src/ProjectDora.Core/Abstractions/IContentService.cs
namespace ProjectDora.Core.Abstractions;

public interface IContentService
{
    // S3: Content Management
}

// src/ProjectDora.Core/Abstractions/IContentTypeService.cs
public interface IContentTypeService
{
    // S2: Content Modeling
}

// src/ProjectDora.Core/Abstractions/IQueryService.cs
public interface IQueryService
{
    // S4: Query Management
}

// src/ProjectDora.Core/Abstractions/IWorkflowService.cs
public interface IWorkflowService
{
    // S6: Workflow Engine
}

// src/ProjectDora.Core/Abstractions/IAuthService.cs
public interface IAuthService
{
    // S5: User/Role/Permission
}

// src/ProjectDora.Core/Abstractions/IUserService.cs
public interface IUserService
{
    // S5: User Management
}

// src/ProjectDora.Core/Abstractions/IRoleService.cs
public interface IRoleService
{
    // S5: Role Management
}

// src/ProjectDora.Core/Abstractions/IAuditService.cs
public interface IAuditService
{
    // S8: Audit Logs
}
```

## 4. Docker Compose

See `.claude/skills/devops-docker.md` for the full Docker Compose template. Key services:

| Service | Image | Port |
|---------|-------|------|
| web | Custom Dockerfile | 5000 |
| postgres | `postgres:16` | 5432 |
| redis | `redis:7-alpine` | 6379 |
| elasticsearch | `elasticsearch:8.12.0` | 9200 |
| minio | `minio/minio:latest` | 9000, 9001 |

### Schema Init Script

```sql
-- docker/init-schemas.sql
CREATE SCHEMA IF NOT EXISTS orchard;
CREATE SCHEMA IF NOT EXISTS audit;
CREATE SCHEMA IF NOT EXISTS analytics;
```

## 5. First Migration

```bash
# Create audit schema migration
dotnet ef migrations add InitialCreate \
  --project src/ProjectDora.Modules/ProjectDora.AuditTrail \
  --context AuditDbContext \
  --output-dir Migrations
```

## 6. First Test

```csharp
// tests/ProjectDora.Core.Tests/HealthCheckTests.cs
[Fact]
[Trait("Category", "Integration")]
public async Task HealthCheck_LiveEndpoint_ReturnsOk()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/health/live");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## 7. CI Pipeline

See `.claude/skills/devops-docker.md` Section 5 for the full GitHub Actions CI pipeline. Key steps:

1. Checkout
2. Setup .NET 8
3. Restore
4. Build (`--warnaserror`)
5. Unit tests
6. Integration tests (Testcontainers)
7. Coverage report (threshold: 75%)

## 8. Git Configuration

### Branch Strategy

```
main          ← production releases only
develop       ← integration branch, CI must be green
feature/US-XXX-description  ← feature branches from develop
hotfix/description          ← hotfix from main
release/vX.Y.Z             ← release candidates
```

### .editorconfig

```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async:suggestion

[*.{yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

## 9. Sprint 0 Checklist

- [ ] Solution created with all projects
- [ ] Orchard Core NuGet packages installed
- [ ] `Program.cs` with health checks configured
- [ ] Abstraction interfaces created (empty)
- [ ] Docker Compose running (all 5 services healthy)
- [ ] `init-schemas.sql` creates `orchard`, `audit`, `analytics` schemas
- [ ] First migration created and applied
- [ ] Health check test passes
- [ ] GitHub Actions CI pipeline green
- [ ] `.editorconfig` and `Directory.Build.props` committed
- [ ] Git branch strategy documented
- [ ] `docs/sprint-analyses/S00-setup/` folder created with analysis.md
- [ ] CLAUDE.md reviewed and up to date

## 10. Cross-References

- **DevOps Skill**: [../skills/devops-docker.md](../skills/devops-docker.md) — Docker Compose, Dockerfile, CI
- **Sprint Roadmap**: [sprint-roadmap.md](sprint-roadmap.md) — full sprint plan
- **Definition of Done**: [definition-of-done.md](definition-of-done.md) — sprint completion criteria
- **Architecture Blueprint**: [../../docs/ProjectDora_Architecture_Blueprint.docx](../../docs/ProjectDora_Architecture_Blueprint.docx)
