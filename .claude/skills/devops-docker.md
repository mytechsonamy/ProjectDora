# Skill: DevOps & Docker

> Target agents: DevOps, Developer

## 1. Container Stack

```
docker-compose.yml
    │
    ├── projectdora-web         (.NET 8, Orchard Core)     :5000
    ├── projectdora-postgres    (PostgreSQL 16)              :5432
    ├── projectdora-redis       (Redis 7)                   :6379
    ├── projectdora-elasticsearch (Elasticsearch 8)         :9200
    └── projectdora-minio       (MinIO)                     :9000/:9001
```

## 2. Docker Compose Files

| File | Environment | Purpose |
|------|-------------|---------|
| `docker-compose.yml` | Base | Shared service definitions |
| `docker-compose.dev.yml` | Development | Debug config, volumes, hot reload |
| `docker-compose.test.yml` | CI/Test | Testcontainers config, golden dataset seed |
| `docker-compose.prod.yml` | Production | Resource limits, TLS, logging |

### Base Compose Template

```yaml
# docker/docker-compose.yml
version: '3.8'

services:
  web:
    build:
      context: ../
      dockerfile: docker/Dockerfile
    container_name: projectdora-web
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=postgres;Port=5432;Database=projectdora;Username=projectdora;Password=${DB_PASSWORD}
      - Redis__ConnectionString=redis:6379
      - Elasticsearch__Url=http://elasticsearch:9200
      - MinIO__Endpoint=minio:9000
      - MinIO__AccessKey=${MINIO_ACCESS_KEY}
      - MinIO__SecretKey=${MINIO_SECRET_KEY}
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
      interval: 10s
      timeout: 5s
      retries: 5

  postgres:
    image: postgres:16
    container_name: projectdora-postgres
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_DB=projectdora
      - POSTGRES_USER=projectdora
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-schemas.sql:/docker-entrypoint-initdb.d/01-schemas.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U projectdora"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: projectdora-redis
    ports:
      - "6379:6379"
    command: redis-server --maxmemory 256mb --maxmemory-policy allkeys-lru
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

  elasticsearch:
    image: elasticsearch:8.12.0
    container_name: projectdora-elasticsearch
    ports:
      - "9200:9200"
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    volumes:
      - es_data:/usr/share/elasticsearch/data
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:9200/_cluster/health || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10

  minio:
    image: minio/minio:latest
    container_name: projectdora-minio
    ports:
      - "9000:9000"
      - "9001:9001"
    command: server /data --console-address ":9001"
    environment:
      - MINIO_ROOT_USER=${MINIO_ACCESS_KEY}
      - MINIO_ROOT_PASSWORD=${MINIO_SECRET_KEY}
    volumes:
      - minio_data:/data

volumes:
  postgres_data:
  es_data:
  minio_data:
```

### Schema Init Script

```sql
-- docker/init-schemas.sql
CREATE SCHEMA IF NOT EXISTS orchard;
CREATE SCHEMA IF NOT EXISTS audit;
CREATE SCHEMA IF NOT EXISTS analytics;
```

## 3. Dockerfile

```dockerfile
# docker/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/ProjectDora.Web/ProjectDora.Web.csproj src/ProjectDora.Web/
COPY src/ProjectDora.Core/ProjectDora.Core.csproj src/ProjectDora.Core/
COPY src/ProjectDora.Modules/**/*.csproj src/ProjectDora.Modules/
RUN dotnet restore src/ProjectDora.Web/ProjectDora.Web.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/ProjectDora.Web/ProjectDora.Web.csproj \
    -c Release -o /app --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectDora.Web.dll"]
```

## 4. Testcontainers Integration

```csharp
// File: tests/ProjectDora.IntegrationTests/ProjectDoraWebApplicationFactory.cs
public class ProjectDoraWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("projectdora_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace connection strings with Testcontainers
            services.Configure<ConnectionStrings>(opts =>
            {
                opts.Default = _postgres.GetConnectionString();
            });

            services.Configure<RedisOptions>(opts =>
            {
                opts.ConnectionString = _redis.GetConnectionString();
            });

            // Seed golden dataset
            services.AddTransient<IStartupFilter, GoldenDatasetStartupFilter>();
        });

        builder.UseEnvironment("Testing");
    }

    // Helper: Create authenticated client
    public HttpClient CreateAuthenticatedClient(
        string username, string role, string tenantId = "default")
    {
        var client = CreateClient();
        var token = GenerateTestJwt(username, role, tenantId);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        return client;
    }
}
```

## 5. CI Pipeline

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --warnaserror

      - name: Unit Tests
        run: dotnet test --no-build --filter "Category!=Integration&Category!=E2E&Category!=Chaos"
          --collect:"XPlat Code Coverage"
          --results-directory ./coverage

      - name: Integration Tests
        run: dotnet test --no-build --filter "Category=Integration"
          --collect:"XPlat Code Coverage"
          --results-directory ./coverage

      - name: Coverage Report
        uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: './coverage/**/coverage.cobertura.xml'
          targetdir: './coverage-report'
          reporttypes: 'HtmlInline;Cobertura'

      - name: Coverage Threshold
        run: |
          # Check coverage meets minimum
          COVERAGE=$(dotnet tool run reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:. -reporttypes:TextSummary | grep "Line coverage" | awk '{print $NF}' | tr -d '%')
          if (( $(echo "$COVERAGE < 75" | bc -l) )); then
            echo "Coverage $COVERAGE% is below 75% threshold"
            exit 1
          fi

      - name: Upload Coverage
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: ./coverage-report
```

## 6. Environment Variables

| Variable | Dev Default | Description |
|----------|-----------|-------------|
| `DB_PASSWORD` | `devpassword` | PostgreSQL password |
| `MINIO_ACCESS_KEY` | `minioadmin` | MinIO access key |
| `MINIO_SECRET_KEY` | `minioadmin` | MinIO secret key |
| `ASPNETCORE_ENVIRONMENT` | `Development` | .NET environment |
**Never commit secrets.** Use `.env` file (gitignored) or Docker secrets.

## 7. Commands Reference

```bash
# Development
docker compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml up -d
docker compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml logs -f web

# Tests
dotnet test                                          # All tests
dotnet test --filter "Category=Unit"                # Unit only
dotnet test --filter "Category=Integration"         # Integration only
dotnet test --filter "StoryId=US-301"              # Specific story

# Build
dotnet build --warnaserror
dotnet publish -c Release -o ./publish

# Database
dotnet ef migrations add MigrationName --project src/ProjectDora.Modules/ProjectDora.AuditTrail --context AuditDbContext
dotnet ef database update --project src/ProjectDora.Modules/ProjectDora.AuditTrail --context AuditDbContext

# Reset everything
docker compose down -v && docker compose up -d
```
