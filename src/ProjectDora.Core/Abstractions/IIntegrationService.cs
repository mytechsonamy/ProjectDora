namespace ProjectDora.Core.Abstractions;

public interface IIntegrationService
{
    Task<ApiClientDto> CreateClientAsync(CreateApiClientCommand command);
    Task<ApiClientDto?> GetClientAsync(string clientId);
    Task<IReadOnlyList<ApiClientDto>> ListClientsAsync();
    Task DeleteClientAsync(string clientId);

    Task<WebhookDto> RegisterWebhookAsync(RegisterWebhookCommand command);
    Task<WebhookDto?> GetWebhookAsync(string webhookId);
    Task<IReadOnlyList<WebhookDto>> ListWebhooksAsync();
    Task DeleteWebhookAsync(string webhookId);
    Task<WebhookDeliveryResultDto> TestWebhookAsync(string webhookId);

    Task<ApiQueryEndpointDto> PublishQueryAsApiAsync(string queryId, PublishQueryApiCommand command);
    Task<IReadOnlyList<ApiQueryEndpointDto>> ListPublishedQueriesAsync();
    Task UnpublishQueryApiAsync(string queryId);
}

public record ApiClientDto(
    string ClientId,
    string ClientName,
    string ClientType,
    IReadOnlyList<string> AllowedScopes,
    IReadOnlyList<string> AllowedGrantTypes,
    bool IsEnabled,
    DateTime CreatedUtc);

public record CreateApiClientCommand(
    string ClientName,
    string ClientType = "machine",
    IReadOnlyList<string>? AllowedScopes = null,
    IReadOnlyList<string>? AllowedGrantTypes = null);

public record WebhookDto(
    string WebhookId,
    string Url,
    IReadOnlyList<string> Events,
    bool IsEnabled,
    string? Secret,
    int DeliveryTimeoutSeconds,
    DateTime CreatedUtc,
    DateTime? LastDeliveredUtc);

public record RegisterWebhookCommand(
    string Url,
    IReadOnlyList<string> Events,
    string? Secret = null,
    int DeliveryTimeoutSeconds = 30);

public record WebhookDeliveryResultDto(
    string WebhookId,
    string Url,
    int HttpStatusCode,
    bool Success,
    string? ErrorMessage,
    TimeSpan Duration);

public record ApiQueryEndpointDto(
    string QueryId,
    string QueryName,
    string EndpointPath,
    bool RequiresAuth,
    IReadOnlyList<string>? RequiredScopes,
    DateTime PublishedUtc);

public record PublishQueryApiCommand(
    string EndpointPath,
    bool RequiresAuth = true,
    IReadOnlyList<string>? RequiredScopes = null);
