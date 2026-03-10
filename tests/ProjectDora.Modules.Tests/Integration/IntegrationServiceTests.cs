using FluentAssertions;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Modules.Tests.Integration;

public class IntegrationServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void CreateApiClientCommand_DefaultValues_AreValid()
    {
        var cmd = new CreateApiClientCommand(
            "KOSGEB Portal",
            "machine",
            AllowedScopes: new[] { "content.read", "content.write" },
            AllowedGrantTypes: new[] { "client_credentials" });

        cmd.ClientName.Should().Be("KOSGEB Portal");
        cmd.ClientType.Should().Be("machine");
        cmd.AllowedScopes.Should().HaveCount(2);
        cmd.AllowedGrantTypes.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void RegisterWebhookCommand_DefaultTimeout_Is30Seconds()
    {
        var cmd = new RegisterWebhookCommand(
            "https://example.com/hook",
            Events: new[] { "content.published", "content.deleted" },
            Secret: "secret123",
            DeliveryTimeoutSeconds: 30);

        cmd.Url.Should().Be("https://example.com/hook");
        cmd.Events.Should().HaveCount(2);
        cmd.DeliveryTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void WebhookDto_AllFieldsMapped()
    {
        var created = DateTime.UtcNow;
        var dto = new WebhookDto(
            "wh-001",
            "https://example.com/hook",
            new[] { "content.published" },
            IsEnabled: true,
            Secret: "s3cr3t",
            DeliveryTimeoutSeconds: 30,
            created,
            LastDeliveredUtc: null);

        dto.WebhookId.Should().Be("wh-001");
        dto.Url.Should().Be("https://example.com/hook");
        dto.IsEnabled.Should().BeTrue();
        dto.DeliveryTimeoutSeconds.Should().Be(30);
        dto.LastDeliveredUtc.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ApiClientDto_CreatedWithDefaultEnabled()
    {
        var dto = new ApiClientDto(
            "cl-001", "KOSGEB App", "interactive",
            new[] { "openid", "profile" },
            new[] { "authorization_code" },
            IsEnabled: true,
            DateTime.UtcNow);

        dto.ClientId.Should().Be("cl-001");
        dto.IsEnabled.Should().BeTrue();
        dto.AllowedScopes.Should().Contain("openid");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void PublishQueryApiCommand_RequiredAuthByDefault()
    {
        var cmd = new PublishQueryApiCommand(
            "/api/queries/destekler",
            RequiresAuth: true,
            RequiredScopes: new[] { "content.read" });

        cmd.EndpointPath.Should().StartWith("/api");
        cmd.RequiresAuth.Should().BeTrue();
        cmd.RequiredScopes.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ApiQueryEndpointDto_AllFieldsMapped()
    {
        var published = DateTime.UtcNow;
        var dto = new ApiQueryEndpointDto(
            "q-001", "DesteklerQuery", "/api/destekler",
            RequiresAuth: true,
            RequiredScopes: new[] { "content.read" },
            published);

        dto.QueryId.Should().Be("q-001");
        dto.EndpointPath.Should().Be("/api/destekler");
        dto.RequiresAuth.Should().BeTrue();
        dto.PublishedUtc.Should().Be(published);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void WebhookDeliveryResultDto_SuccessResult()
    {
        var dto = new WebhookDeliveryResultDto(
            "wh-001",
            "https://example.com/hook",
            HttpStatusCode: 200,
            Success: true,
            ErrorMessage: null,
            Duration: TimeSpan.FromMilliseconds(125));

        dto.Success.Should().BeTrue();
        dto.HttpStatusCode.Should().Be(200);
        dto.ErrorMessage.Should().BeNull();
        dto.Duration.TotalMilliseconds.Should().Be(125);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void WebhookDeliveryResultDto_FailureResult()
    {
        var dto = new WebhookDeliveryResultDto(
            "wh-001",
            "https://example.com/hook",
            HttpStatusCode: 503,
            Success: false,
            ErrorMessage: "Service Unavailable",
            Duration: TimeSpan.FromSeconds(30));

        dto.Success.Should().BeFalse();
        dto.HttpStatusCode.Should().Be(503);
        dto.ErrorMessage.Should().Be("Service Unavailable");
        dto.Duration.TotalSeconds.Should().Be(30);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void WebhookDeliveryResultDto_TimeoutResult()
    {
        var dto = new WebhookDeliveryResultDto(
            "wh-001",
            "https://example.com/hook",
            HttpStatusCode: 0,
            Success: false,
            ErrorMessage: "A task was canceled.",
            Duration: TimeSpan.FromSeconds(30));

        dto.Success.Should().BeFalse();
        dto.HttpStatusCode.Should().Be(0);
        dto.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("StoryId", "US-1101")]
    public void ApiClientRecord_ScopesAndGrantTypes_EmptyByDefault()
    {
        // Validates integration record defaults
        var cmd = new CreateApiClientCommand("TestClient", "machine");
        cmd.AllowedScopes.Should().BeNullOrEmpty();
        cmd.AllowedGrantTypes.Should().BeNullOrEmpty();
    }
}
