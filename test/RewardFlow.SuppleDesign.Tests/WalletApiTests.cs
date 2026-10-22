using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RewardFlow.SuppleDesign.Api.DTOs.Wallets;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Verifies the HTTP primary adapter, composition root and Problem Details boundary.</summary>
public sealed class WalletApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    /// <summary>Conceptual Contours: exercises real routing and application services through the HTTP adapter.</summary>
    public WalletApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    /// <summary>Intention-Revealing Interfaces: the API root leads developers to its interactive contract.</summary>
    [Fact]
    public async Task RootRedirectsToSwaggerAndOpenApiIsAvailable()
    {
        using var noRedirectClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var root = await noRedirectClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, root.StatusCode);
        Assert.Equal("/swagger", root.Headers.Location!.ToString());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.Equal("RewardFlow API", document.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", document.GetProperty("info").GetProperty("version").GetString());
    }

    /// <summary>Intention-Revealing Interfaces: complete earned-points and redemption journey.</summary>
    [Fact]
    public async Task WalletJourneyAndOverdraftProtection()
    {
        var path = $"/api/wallets/{Guid.NewGuid()}";
        var opened = await client.PostAsync(path, null);
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        Assert.EndsWith(path + "/balance", opened.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path, null)).StatusCode);
        var credited = await client.PostAsJsonAsync(path + "/credit", new { points = 100 });
        Assert.Equal(HttpStatusCode.OK, credited.StatusCode);
        Assert.Equal(100, (await credited.Content.ReadFromJsonAsync<WalletBalanceResponse>())!.Points);
        var debited = await client.PostAsJsonAsync(path + "/debit", new { points = 40 });
        Assert.Equal(HttpStatusCode.OK, debited.StatusCode);
        Assert.Equal(60, (await debited.Content.ReadFromJsonAsync<WalletBalanceResponse>())!.Points);
        var overdraft = await client.PostAsJsonAsync(path + "/debit", new { points = 61 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, overdraft.StatusCode);
        Assert.Equal("application/problem+json", overdraft.Content.Headers.ContentType!.MediaType);
        var problem = await overdraft.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("insufficient_points", problem.GetProperty("code").GetString());
        var balance = await client.GetFromJsonAsync<WalletBalanceResponse>(path + "/balance");
        Assert.Equal(60, balance!.Points);

        var affordable = await client.GetFromJsonAsync<WalletAffordabilityResponse>(path + "/can-afford?points=60");
        Assert.True(affordable!.CanAfford);
        Assert.Equal(60, affordable.Balance);
        Assert.Equal(60, affordable.RequiredPoints);

        var unaffordable = await client.GetFromJsonAsync<WalletAffordabilityResponse>(path + "/can-afford?points=61");
        Assert.False(unaffordable!.CanAfford);
        Assert.Equal(60, (await client.GetFromJsonAsync<WalletBalanceResponse>(path + "/balance"))!.Points);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/wallets/{Guid.NewGuid()}/balance")).StatusCode);
    }

    /// <summary>HTTP validation: affordability requires an explicit non-negative whole-point amount.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("?points=-1")]
    [InlineData("?points=1.5")]
    public async Task InvalidAffordabilityQueryReturnsBadRequest(string query)
    {
        var response = await client.GetAsync($"/api/wallets/{Guid.NewGuid()}/can-afford{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Conceptual Contours: malformed HTTP contracts are rejected before domain execution.</summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"points\":null}")]
    [InlineData("{\"points\":0}")]
    [InlineData("{\"points\":-1}")]
    [InlineData("{\"points\":1.5}")]
    [InlineData("{\"points\":9223372036854775808}")]
    [InlineData("invalid json")]
    public async Task InvalidPointsReturnBadRequest(string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/api/wallets/{Guid.NewGuid()}/credit", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Assertions: a zero balance cannot fund even a one-point redemption.</summary>
    [Fact]
    public async Task EmptyWalletAndEmptyIdentityAreProtected()
    {
        var path = $"/api/wallets/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync(path, null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync(path + "/debit", new { points = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsync($"/api/wallets/{Guid.Empty}", null)).StatusCode);
    }
}
