namespace MmoDemo.Gateway.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_ReturnsOkPayload()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("OK", payload.Status);
        Assert.Equal("MmoDemo.Gateway", payload.Service);
        Assert.Equal("Phase 2", payload.Phase);
    }

    private sealed record HealthResponse(string Status, string Service, string Phase);
}
