using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using OrchardCore.Entities;
using OrchardCore.Settings;
using ProjectDora.Core.Abstractions;

namespace ProjectDora.Integration.Services;

/// <summary>
/// Integration settings persisted via ISiteService.
/// </summary>
internal sealed class IntegrationSettings
{
    public List<ApiClientRecord> ApiClients { get; set; } = new();
    public List<WebhookRecord> Webhooks { get; set; } = new();
    public List<QueryEndpointRecord> QueryEndpoints { get; set; } = new();
}

internal sealed class ApiClientRecord
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public List<string> AllowedScopes { get; set; } = new();
    public List<string> AllowedGrantTypes { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
}

internal sealed class WebhookRecord
{
    public string WebhookId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public string? Secret { get; set; }
    public int DeliveryTimeoutSeconds { get; set; } = 30;
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastDeliveredUtc { get; set; }
}

internal sealed class QueryEndpointRecord
{
    public string QueryId { get; set; } = string.Empty;
    public string QueryName { get; set; } = string.Empty;
    public string EndpointPath { get; set; } = string.Empty;
    public bool RequiresAuth { get; set; } = true;
    public List<string> RequiredScopes { get; set; } = new();
    public DateTime PublishedUtc { get; set; }
}

public sealed class OrchardIntegrationService : IIntegrationService
{
    private readonly ISiteService _siteService;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrchardIntegrationService(ISiteService siteService, IHttpClientFactory httpClientFactory)
    {
        _siteService = siteService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiClientDto> CreateClientAsync(CreateApiClientCommand command)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var clientId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        site.Alter<IntegrationSettings>(settings =>
        {
            settings.ApiClients.Add(new ApiClientRecord
            {
                ClientId = clientId,
                ClientName = command.ClientName,
                ClientType = command.ClientType,
                AllowedScopes = command.AllowedScopes?.ToList() ?? new List<string>(),
                AllowedGrantTypes = command.AllowedGrantTypes?.ToList() ?? new List<string>(),
                IsEnabled = true,
                CreatedUtc = DateTime.UtcNow,
            });
        });

        await _siteService.UpdateSiteSettingsAsync(site);

        return new ApiClientDto(
            clientId,
            command.ClientName,
            command.ClientType,
            command.AllowedScopes ?? Array.Empty<string>(),
            command.AllowedGrantTypes ?? Array.Empty<string>(),
            IsEnabled: true,
            DateTime.UtcNow);
    }

    public async Task<ApiClientDto?> GetClientAsync(string clientId)
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var settings = site.As<IntegrationSettings>();
        var record = settings?.ApiClients.Find(c => c.ClientId == clientId);

        return record is null ? null : MapClientToDto(record);
    }

    public async Task<IReadOnlyList<ApiClientDto>> ListClientsAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var settings = site.As<IntegrationSettings>();

        IReadOnlyList<ApiClientDto> result = (IReadOnlyList<ApiClientDto>?)settings?.ApiClients
            .Select(MapClientToDto)
            .ToList() ?? Array.Empty<ApiClientDto>();

        return result;
    }

    public async Task DeleteClientAsync(string clientId)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<IntegrationSettings>(settings =>
        {
            settings.ApiClients.RemoveAll(c => c.ClientId == clientId);
        });

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    public async Task<WebhookDto> RegisterWebhookAsync(RegisterWebhookCommand command)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var webhookId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var createdUtc = DateTime.UtcNow;

        site.Alter<IntegrationSettings>(settings =>
        {
            settings.Webhooks.Add(new WebhookRecord
            {
                WebhookId = webhookId,
                Url = command.Url,
                Events = command.Events.ToList(),
                IsEnabled = true,
                Secret = command.Secret,
                DeliveryTimeoutSeconds = command.DeliveryTimeoutSeconds,
                CreatedUtc = createdUtc,
            });
        });

        await _siteService.UpdateSiteSettingsAsync(site);

        return new WebhookDto(
            webhookId,
            command.Url,
            command.Events,
            IsEnabled: true,
            command.Secret,
            command.DeliveryTimeoutSeconds,
            createdUtc,
            null);
    }

    public async Task<WebhookDto?> GetWebhookAsync(string webhookId)
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var settings = site.As<IntegrationSettings>();
        var record = settings?.Webhooks.Find(w => w.WebhookId == webhookId);

        return record is null ? null : MapWebhookToDto(record);
    }

    public async Task<IReadOnlyList<WebhookDto>> ListWebhooksAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var settings = site.As<IntegrationSettings>();

        IReadOnlyList<WebhookDto> result = (IReadOnlyList<WebhookDto>?)settings?.Webhooks
            .Select(MapWebhookToDto)
            .ToList() ?? Array.Empty<WebhookDto>();

        return result;
    }

    public async Task DeleteWebhookAsync(string webhookId)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<IntegrationSettings>(settings =>
        {
            settings.Webhooks.RemoveAll(w => w.WebhookId == webhookId);
        });

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    public async Task<WebhookDeliveryResultDto> TestWebhookAsync(string webhookId)
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var settings = site.As<IntegrationSettings>();
        var record = settings?.Webhooks.Find(w => w.WebhookId == webhookId)
            ?? throw new KeyNotFoundException($"Webhook '{webhookId}' not found.");

        var payload = JsonSerializer.Serialize(new
        {
            webhookId,
            eventType = "ping",
            timestamp = DateTime.UtcNow,
        });

        var started = DateTime.UtcNow;
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(record.DeliveryTimeoutSeconds);

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(record.Secret))
            {
                content.Headers.Add("X-Webhook-Secret", record.Secret);
            }

            var response = await client.PostAsync(record.Url, content);
            var duration = DateTime.UtcNow - started;

            return new WebhookDeliveryResultDto(
                webhookId,
                record.Url,
                (int)response.StatusCode,
                response.IsSuccessStatusCode,
                response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}",
                duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;
            return new WebhookDeliveryResultDto(
                webhookId,
                record.Url,
                0,
                Success: false,
                ex.Message,
                duration);
        }
    }

    public async Task<ApiQueryEndpointDto> PublishQueryAsApiAsync(string queryId, PublishQueryApiCommand command)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        var publishedUtc = DateTime.UtcNow;

        site.Alter<IntegrationSettings>(settings =>
        {
            // Remove existing entry for this queryId if any
            settings.QueryEndpoints.RemoveAll(e => e.QueryId == queryId);
            settings.QueryEndpoints.Add(new QueryEndpointRecord
            {
                QueryId = queryId,
                QueryName = queryId,
                EndpointPath = command.EndpointPath,
                RequiresAuth = command.RequiresAuth,
                RequiredScopes = command.RequiredScopes?.ToList() ?? new List<string>(),
                PublishedUtc = publishedUtc,
            });
        });

        await _siteService.UpdateSiteSettingsAsync(site);

        return new ApiQueryEndpointDto(
            queryId,
            queryId,
            command.EndpointPath,
            command.RequiresAuth,
            command.RequiredScopes,
            publishedUtc);
    }

    public async Task<IReadOnlyList<ApiQueryEndpointDto>> ListPublishedQueriesAsync()
    {
        var site = await _siteService.GetSiteSettingsAsync();
        var settings = site.As<IntegrationSettings>();

        IReadOnlyList<ApiQueryEndpointDto> result = (IReadOnlyList<ApiQueryEndpointDto>?)settings?.QueryEndpoints
            .Select(e => new ApiQueryEndpointDto(
                e.QueryId,
                e.QueryName,
                e.EndpointPath,
                e.RequiresAuth,
                e.RequiredScopes,
                e.PublishedUtc))
            .ToList() ?? Array.Empty<ApiQueryEndpointDto>();

        return result;
    }

    public async Task UnpublishQueryApiAsync(string queryId)
    {
        var site = await _siteService.LoadSiteSettingsAsync();
        site.Alter<IntegrationSettings>(settings =>
        {
            settings.QueryEndpoints.RemoveAll(e => e.QueryId == queryId);
        });

        await _siteService.UpdateSiteSettingsAsync(site);
    }

    private static ApiClientDto MapClientToDto(ApiClientRecord record)
    {
        return new ApiClientDto(
            record.ClientId,
            record.ClientName,
            record.ClientType,
            record.AllowedScopes,
            record.AllowedGrantTypes,
            record.IsEnabled,
            record.CreatedUtc);
    }

    private static WebhookDto MapWebhookToDto(WebhookRecord record)
    {
        return new WebhookDto(
            record.WebhookId,
            record.Url,
            record.Events,
            record.IsEnabled,
            record.Secret,
            record.DeliveryTimeoutSeconds,
            record.CreatedUtc,
            record.LastDeliveredUtc);
    }
}
