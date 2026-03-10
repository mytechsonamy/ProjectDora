using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Integration;

public class IntegrationDtoTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Dto_ApiClientDto_PreservesProperties()
    {
        var scopes = new[] { "openid", "content.read", "content.write" };
        var grants = new[] { "client_credentials" };

        var dto = new ApiClientDto(
            "client-001",
            "KOSGEB Mobile App",
            "machine",
            scopes,
            grants,
            IsEnabled: true,
            DateTime.UtcNow);

        dto.ClientId.Should().Be("client-001");
        dto.ClientName.Should().Be("KOSGEB Mobile App");
        dto.ClientType.Should().Be("machine");
        dto.AllowedScopes.Should().HaveCount(3);
        dto.IsEnabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void Integration_Dto_CreateApiClientCommand_DefaultsToMachineType()
    {
        var command = new CreateApiClientCommand("Destek API Client");

        command.ClientName.Should().Be("Destek API Client");
        command.ClientType.Should().Be("machine");
        command.AllowedScopes.Should().BeNull();
        command.AllowedGrantTypes.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1103")]
    public void Integration_Dto_WebhookDto_PreservesProperties()
    {
        var events = new[] { "content.published", "content.updated" };
        var created = DateTime.UtcNow;

        var dto = new WebhookDto(
            "wh-001",
            "https://partner.kosgeb.gov.tr/hooks",
            events,
            IsEnabled: true,
            "s3cr3t",
            DeliveryTimeoutSeconds: 30,
            created,
            null);

        dto.WebhookId.Should().Be("wh-001");
        dto.Url.Should().Contain("kosgeb");
        dto.Events.Should().HaveCount(2);
        dto.IsEnabled.Should().BeTrue();
        dto.LastDeliveredUtc.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1103")]
    public void Integration_Dto_RegisterWebhookCommand_DefaultTimeout30Seconds()
    {
        var events = new[] { "content.published" };

        var command = new RegisterWebhookCommand(
            "https://api.partner.tr/webhook",
            events);

        command.DeliveryTimeoutSeconds.Should().Be(30);
        command.Secret.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1103")]
    public void Integration_Dto_WebhookDeliveryResultDto_SuccessResult()
    {
        var result = new WebhookDeliveryResultDto(
            "wh-001",
            "https://partner.kosgeb.gov.tr/hooks",
            HttpStatusCode: 200,
            Success: true,
            ErrorMessage: null,
            Duration: TimeSpan.FromMilliseconds(142));

        result.Success.Should().BeTrue();
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.Duration.TotalMilliseconds.Should().BePositive();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1103")]
    public void Integration_Dto_WebhookDeliveryResultDto_FailureResult()
    {
        var result = new WebhookDeliveryResultDto(
            "wh-002",
            "https://down.example.com/hooks",
            HttpStatusCode: 503,
            Success: false,
            ErrorMessage: "Service unavailable",
            Duration: TimeSpan.FromSeconds(30));

        result.Success.Should().BeFalse();
        result.HttpStatusCode.Should().Be(503);
        result.ErrorMessage.Should().Contain("unavailable");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1104")]
    public void Integration_Dto_ApiQueryEndpointDto_PreservesProperties()
    {
        var scopes = new[] { "content.read" };
        var dto = new ApiQueryEndpointDto(
            "q-001",
            "DestekProgramlariListesi",
            "/api/v1/queries/destek-programlari",
            RequiresAuth: true,
            scopes,
            DateTime.UtcNow);

        dto.QueryId.Should().Be("q-001");
        dto.EndpointPath.Should().Contain("destek-programlari");
        dto.RequiresAuth.Should().BeTrue();
        dto.RequiredScopes.Should().ContainSingle();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1104")]
    public void Integration_Dto_PublishQueryApiCommand_DefaultsToAuthenticated()
    {
        var command = new PublishQueryApiCommand("/api/v1/queries/public-content");

        command.EndpointPath.Should().Contain("public-content");
        command.RequiresAuth.Should().BeTrue();
        command.RequiredScopes.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1102")]
    public void Integration_Dto_ApiClientDto_TurkishClientNamePreserved()
    {
        var dto = new ApiClientDto(
            "client-002",
            "KOBİ Destek Portalı",
            "web",
            Array.Empty<string>(),
            Array.Empty<string>(),
            true,
            DateTime.UtcNow);

        dto.ClientName.Should().Contain("KOBİ");
        dto.ClientType.Should().Be("web");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1104")]
    public void Integration_Dto_ApiQueryEndpointDto_PublicEndpointNoAuth()
    {
        var dto = new ApiQueryEndpointDto(
            "q-002",
            "AktifDestekProgramlari",
            "/api/v1/queries/aktif-destek-programlari",
            RequiresAuth: false,
            null,
            DateTime.UtcNow);

        dto.RequiresAuth.Should().BeFalse();
        dto.RequiredScopes.Should().BeNull();
    }
}
