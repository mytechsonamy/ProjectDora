using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Infrastructure.Services;

internal static class OrchardRecipeServiceJsonOptions
{
    internal static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
}

public sealed class OrchardRecipeService : IRecipeService
{
    private readonly IRecipeExecutor _recipeExecutor;
    private readonly IEnumerable<IRecipeHarvester> _recipeHarvesters;

    public OrchardRecipeService(
        IRecipeExecutor recipeExecutor,
        IEnumerable<IRecipeHarvester> recipeHarvesters)
    {
        _recipeExecutor = recipeExecutor;
        _recipeHarvesters = recipeHarvesters;
    }

    public async Task<RecipeImportResultDto> ImportAsync(string recipeJson)
    {
        if (string.IsNullOrWhiteSpace(recipeJson))
        {
            return new RecipeImportResultDto(false, "Recipe JSON cannot be empty.", 0, TimeSpan.Zero);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            // Validate JSON and count steps
            using var jsonDoc = JsonDocument.Parse(recipeJson);
            var stepCount = 0;
            if (jsonDoc.RootElement.TryGetProperty("steps", out var steps) &&
                steps.ValueKind == JsonValueKind.Array)
            {
                stepCount = steps.GetArrayLength();
            }

            // Write recipe to a temp file for IRecipeExecutor
            var tempDir = Path.GetTempPath();
            var tempFile = Path.Combine(tempDir, $"recipe_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(tempFile, recipeJson, Encoding.UTF8);

            try
            {
                var fileProvider = new PhysicalFileProvider(tempDir);
                var fileInfo = fileProvider.GetFileInfo(Path.GetFileName(tempFile));

                var descriptor = new RecipeDescriptor
                {
                    Name = "import",
                    DisplayName = "Import",
                    RecipeFileInfo = fileInfo,
                };

                var executionId = Guid.NewGuid().ToString("N");
                await _recipeExecutor.ExecuteAsync(
                    executionId,
                    descriptor,
                    new Dictionary<string, object>(),
                    CancellationToken.None);

                sw.Stop();
                return new RecipeImportResultDto(true, null, stepCount, sw.Elapsed);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        catch (JsonException ex)
        {
            sw.Stop();
            return new RecipeImportResultDto(false, $"Invalid JSON: {ex.Message}", 0, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RecipeImportResultDto(false, ex.Message, 0, sw.Elapsed);
        }
    }

    public async Task<string> ExportAsync()
    {
        // Harvest all available recipes and return a manifest recipe JSON
        var recipes = new List<object>();
        foreach (var harvester in _recipeHarvesters)
        {
            var discovered = await harvester.HarvestRecipesAsync();
            foreach (var recipe in discovered)
            {
                recipes.Add(new
                {
                    name = recipe.Name,
                    displayName = recipe.DisplayName,
                    description = recipe.Description,
                    version = recipe.Version,
                    tags = recipe.Tags,
                });
            }
        }

        var export = new
        {
            name = "export",
            displayName = "ProjectDora Export",
            description = "Exported recipe manifest",
            steps = Array.Empty<object>(),
            availableRecipes = recipes,
        };

        return JsonSerializer.Serialize(export, OrchardRecipeServiceJsonOptions.Indented);
    }

    public async Task<IReadOnlyList<RecipeSummaryDto>> ListAvailableAsync()
    {
        var result = new List<RecipeSummaryDto>();
        foreach (var harvester in _recipeHarvesters)
        {
            var recipes = await harvester.HarvestRecipesAsync();
            foreach (var recipe in recipes)
            {
                result.Add(new RecipeSummaryDto(
                    recipe.Name,
                    recipe.Description ?? string.Empty,
                    recipe.Version ?? "1.0.0",
                    IsBuiltIn: true));
            }
        }

        return result;
    }
}
