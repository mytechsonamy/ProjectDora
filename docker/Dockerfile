FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/ProjectDora.Web/ProjectDora.Web.csproj src/ProjectDora.Web/
COPY src/ProjectDora.Core/ProjectDora.Core.csproj src/ProjectDora.Core/
COPY src/ProjectDora.Modules/**/*.csproj src/ProjectDora.Modules/
COPY Directory.Build.props .
RUN dotnet restore src/ProjectDora.Web/ProjectDora.Web.csproj

# Copy everything and build
COPY src/ src/
RUN dotnet publish src/ProjectDora.Web/ProjectDora.Web.csproj \
    -c Release -o /app --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectDora.Web.dll"]
