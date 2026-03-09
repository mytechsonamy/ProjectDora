using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectDora.Core.Tests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HealthCheck_LiveEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HealthCheck_ReadyEndpoint_ReturnsStatusCode()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        // Ready may return 503 if dependencies are down, but endpoint should exist
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}
