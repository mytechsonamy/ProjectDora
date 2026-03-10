using FluentAssertions;
using Moq;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;
using ProjectDora.Core.Abstractions;
using ProjectDora.Infrastructure.Services;

namespace ProjectDora.Modules.Tests.Infrastructure;

public class RecipeServiceTests
{
    private readonly Mock<IRecipeExecutor> _executorMock = new();
    private readonly Mock<IRecipeHarvester> _harvesterMock = new();

    private OrchardRecipeService CreateSut() =>
        new(_executorMock.Object, new[] { _harvesterMock.Object });

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public async Task ImportAsync_EmptyJson_ReturnsFailure()
    {
        var sut = CreateSut();
        var result = await sut.ImportAsync(string.Empty);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public async Task ImportAsync_InvalidJson_ReturnsFailure()
    {
        var sut = CreateSut();
        var result = await sut.ImportAsync("not-valid-json");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public async Task ListAvailableAsync_NoHarvesters_ReturnsEmpty()
    {
        _harvesterMock.Setup(h => h.HarvestRecipesAsync())
                      .ReturnsAsync(Array.Empty<RecipeDescriptor>());

        var sut = CreateSut();
        var result = await sut.ListAvailableAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public async Task ListAvailableAsync_WithRecipes_ReturnsMappedDtos()
    {
        var descriptor = new RecipeDescriptor
        {
            Name = "setup",
            Description = "KOSGEB kurulum tarifesi",
            Version = "1.0.0",
        };
        _harvesterMock.Setup(h => h.HarvestRecipesAsync())
                      .ReturnsAsync(new[] { descriptor });

        var sut = CreateSut();
        var result = await sut.ListAvailableAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("setup");
        result[0].IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public async Task ExportAsync_ReturnsValidJson()
    {
        _harvesterMock.Setup(h => h.HarvestRecipesAsync())
                      .ReturnsAsync(Array.Empty<RecipeDescriptor>());

        var sut = CreateSut();
        var result = await sut.ExportAsync();

        result.Should().Contain("\"name\"");
        result.Should().Contain("export");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1002")]
    public async Task ImportAsync_ValidJsonNoSteps_ReturnsSuccess()
    {
        var json = "{\"name\":\"test\",\"steps\":[]}";
        _executorMock.Setup(e => e.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<RecipeDescriptor>(),
            It.IsAny<IDictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid().ToString());

        var sut = CreateSut();
        var result = await sut.ImportAsync(json);

        result.Success.Should().BeTrue();
        result.StepsExecuted.Should().Be(0);
    }
}
