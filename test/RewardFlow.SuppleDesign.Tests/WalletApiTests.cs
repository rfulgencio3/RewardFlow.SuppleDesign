using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using RewardFlow.SuppleDesign.Api.DTOs.Wallets;
using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Verifies the HTTP primary adapter, composition root and Problem Details boundary.</summary>
[Collection(PostgresDatabaseCollection.Name)]
public sealed class WalletApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

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
        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.Equal(
            "Primary rewards",
            schemas.GetProperty(nameof(OpenWalletRequest)).GetProperty("properties")
                .GetProperty("name").GetProperty("example").GetString());
        Assert.Equal(
            100,
            schemas.GetProperty(nameof(RewardMovementRequest)).GetProperty("properties")
                .GetProperty("points").GetProperty("example").GetInt64());
        var statusSchema = schemas.GetProperty(nameof(RewardsWalletStatus));
        Assert.Equal(
            ["active", "suspended", "closed"],
            statusSchema.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        var creditResponses = document.GetProperty("paths").GetProperty("/api/wallets/{id}/credit")
            .GetProperty("post").GetProperty("responses");
        Assert.All(["200", "400", "404", "409", "422"], code => Assert.True(creditResponses.TryGetProperty(code, out _)));
    }

    /// <summary>Intention-Revealing Interfaces: complete earned-points and redemption journey.</summary>
    [Fact]
    public async Task WalletJourneyAndOverdraftProtection()
    {
        var (walletId, path) = await OpenWalletAsync("Primary rewards");
        var details = await client.GetFromJsonAsync<WalletDetailsResponse>(path, JsonOptions);
        Assert.Equal(walletId, details!.WalletId);
        Assert.Equal("Primary rewards", details.Name);
        Assert.Equal(RewardsWalletStatus.Active, details.Status);
        Assert.Equal(0, details.Points);
        Assert.Equal(TimeSpan.Zero, details.CreatedAtUtc.Offset);

        var creditOperationId = Guid.NewGuid();
        var credited = await client.PostAsJsonAsync(path + "/credit",
            new { operationId = creditOperationId, points = 100, reason = "Purchase reward" });
        Assert.Equal(HttpStatusCode.OK, credited.StatusCode);
        Assert.Equal(100, (await credited.Content.ReadFromJsonAsync<RewardMovementResponse>(JsonOptions))!.BalanceAfter);
        var debited = await client.PostAsJsonAsync(path + "/debit",
            new { operationId = Guid.NewGuid(), points = 40, reason = "Gift redemption" });
        Assert.Equal(HttpStatusCode.OK, debited.StatusCode);
        Assert.Equal(60, (await debited.Content.ReadFromJsonAsync<RewardMovementResponse>(JsonOptions))!.BalanceAfter);
        var overdraft = await client.PostAsJsonAsync(path + "/debit",
            new { operationId = Guid.NewGuid(), points = 61, reason = "Overspend" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, overdraft.StatusCode);
        Assert.Equal("application/problem+json", overdraft.Content.Headers.ContentType!.MediaType);
        var problem = await overdraft.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("insufficient_points", problem.GetProperty("code").GetString());
        var balance = await client.GetFromJsonAsync<WalletBalanceResponse>(path + "/balance");
        Assert.Equal(60, balance!.Points);

        var affordable = await client.GetFromJsonAsync<WalletAffordabilityResponse>(path + "/can-afford?requiredPoints=60");
        Assert.True(affordable!.CanAfford);
        Assert.Equal(60, affordable.AvailablePoints);
        Assert.Equal(60, affordable.RequiredPoints);

        var unaffordable = await client.GetFromJsonAsync<WalletAffordabilityResponse>(path + "/can-afford?requiredPoints=61");
        Assert.False(unaffordable!.CanAfford);
        Assert.Equal(60, (await client.GetFromJsonAsync<WalletBalanceResponse>(path + "/balance"))!.Points);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/wallets/{Guid.NewGuid()}/balance")).StatusCode);
    }

    /// <summary>HTTP validation: affordability requires an explicit non-negative whole-point amount.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("?requiredPoints=-1")]
    [InlineData("?requiredPoints=1.5")]
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
        var (_, path) = await OpenWalletAsync("Empty wallet");
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsJsonAsync(path + "/debit",
                new { operationId = Guid.NewGuid(), points = 1, reason = "Redemption" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.GetAsync($"/api/wallets/{Guid.Empty}")).StatusCode);
    }

    /// <summary>HTTP validation: opening requires a meaningful bounded name.</summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    [InlineData("{\"name\":\"   \"}")]
    public async Task InvalidWalletNameReturnsBadRequest(string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/wallets", content)).StatusCode);
    }

    /// <summary>HTTP validation: the public name limit matches the WalletName value object.</summary>
    [Fact]
    public async Task WalletNameAboveMaximumLengthReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/wallets", new { name = new string('x', 121) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Assertions: lifecycle endpoints expose aggregate transitions and state conflicts.</summary>
    [Fact]
    public async Task WalletLifecycleControlsMovements()
    {
        var (_, path) = await OpenWalletAsync("Lifecycle wallet");
        await client.PostAsJsonAsync(path + "/credit",
            new { operationId = Guid.NewGuid(), points = 100, reason = "Purchase reward" });

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync(path + "/suspend", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync(path + "/credit",
                new { operationId = Guid.NewGuid(), points = 10, reason = "Purchase reward" })).StatusCode);
        var blockedDebit = await client.PostAsJsonAsync(path + "/debit",
            new { operationId = Guid.NewGuid(), points = 10, reason = "Redemption" });
        Assert.Equal(HttpStatusCode.Conflict, blockedDebit.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync(path + "/reactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync(path + "/debit",
                new { operationId = Guid.NewGuid(), points = 10, reason = "Redemption" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync(path + "/close", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync(path + "/credit",
                new { operationId = Guid.NewGuid(), points = 1, reason = "Late reward" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync(path + "/close", null)).StatusCode);
        Assert.Equal(
            RewardsWalletStatus.Closed,
            (await client.GetFromJsonAsync<WalletDetailsResponse>(path, JsonOptions))!.Status);
    }

    /// <summary>Assertions: an operation retry is replayed and the ledger records it once.</summary>
    [Fact]
    public async Task CreditRetryIsIdempotentAndLedgerIsPaged()
    {
        var (_, path) = await OpenWalletAsync("Ledger wallet");
        var operationId = Guid.NewGuid();
        var request = new { operationId, points = 25, reason = "Welcome reward" };

        var first = await client.PostAsJsonAsync(path + "/credit", request);
        var replay = await client.PostAsJsonAsync(path + "/credit", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(
            await first.Content.ReadAsStringAsync(),
            await replay.Content.ReadAsStringAsync());

        var second = await client.PostAsJsonAsync(path + "/credit",
            new { operationId = Guid.NewGuid(), points = 5, reason = "Second reward" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var ledger = await client.GetFromJsonAsync<RewardTransactionPageResponse>(
            path + "/transactions?page=1&pageSize=1", JsonOptions);
        var movement = Assert.Single(ledger!.Items);
        Assert.Equal(RewardTransactionType.Credit, movement.Type);
        Assert.True(ledger.HasNextPage);
        var lastPage = await client.GetFromJsonAsync<RewardTransactionPageResponse>(
            path + "/transactions?page=2&pageSize=1", JsonOptions);
        Assert.Single(lastPage!.Items);
        Assert.False(lastPage.HasNextPage);
        Assert.Equal(30, (await client.GetFromJsonAsync<WalletBalanceResponse>(path + "/balance"))!.Points);

        var conflict = await client.PostAsJsonAsync(path + "/credit",
            new { operationId, points = 30, reason = "Changed retry" });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync(path + "/transactions?page=0&pageSize=101")).StatusCode);
    }

    private async Task<(Guid WalletId, string Path)> OpenWalletAsync(string name)
    {
        var opened = await client.PostAsJsonAsync("/api/wallets", new { name });
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var response = (await opened.Content.ReadFromJsonAsync<OpenWalletResponse>())!;
        var path = $"/api/wallets/{response.WalletId}";
        Assert.Equal(path, opened.Headers.Location!.AbsolutePath);
        return (response.WalletId, path);
    }
}
