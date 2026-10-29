using RewardFlow.SuppleDesign.Tests.Integration.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RewardFlow.SuppleDesign.Tests.Integration.Api;

/// <summary>Real HTTP contracts for transfers, validation and original-result replay.</summary>
[Collection(PostgresDatabaseCollection.Name)]
public sealed class TransferApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client = factory.CreateClient();
    private async Task<Guid> OpenAsync(string name)
    {
        var response = await client.PostAsJsonAsync("/api/wallets", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("walletId").GetGuid();
    }

    /// <summary>Transfers the exact source balance, exposes both ledger sides and replays after closure.</summary>
    [Fact]
    public async Task TransferJourneyIsAtomicAndIdempotent()
    {
        // Arrange
        var source = await OpenAsync("Transfer source");
        var destination = await OpenAsync("Transfer destination");
        var fundingId = Guid.NewGuid();
        (await client.PostAsJsonAsync($"/api/wallets/{source}/credit", new { operationId = fundingId, points = 100, reason = "Funding" })).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/wallets/{destination}/suspend", null)).EnsureSuccessStatusCode();
        var id = Guid.NewGuid();
        var body = new { operationId = id, destinationWalletId = destination, points = 100, reason = "Transfer" };
        var path = $"/api/wallets/{source}/transfers";
        // Act
        var response = await client.PostAsJsonAsync(path, body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, result.GetProperty("sourceBalanceAfter").GetInt64());
        Assert.Equal(100, result.GetProperty("destinationBalanceAfter").GetInt64());
        Assert.Equal(source, result.GetProperty("sourceWalletId").GetGuid());
        Assert.Equal(destination, result.GetProperty("destinationWalletId").GetGuid());
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>($"/api/wallets/{source}/balance")).GetProperty("points").GetInt64());
        Assert.Equal(100, (await client.GetFromJsonAsync<JsonElement>($"/api/wallets/{destination}/balance")).GetProperty("points").GetInt64());
        var ledger = await client.GetFromJsonAsync<JsonElement>($"/api/wallets/{destination}/transactions");
        var entry = Assert.Single(ledger.GetProperty("items").EnumerateArray());
        Assert.Equal(id, entry.GetProperty("operationId").GetGuid());
        Assert.Equal("credit", entry.GetProperty("type").GetString());
        await client.PostAsync($"/api/wallets/{source}/close", null);
        await client.PostAsync($"/api/wallets/{destination}/close", null);
        var replay = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(await response.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path,
            new { operationId = id, destinationWalletId = destination, points = 99, reason = "Transfer" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/wallets/{destination}/credit",
            new { operationId = id, points = 100, reason = "Transfer" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path,
            new { operationId = fundingId, destinationWalletId = destination, points = 100, reason = "Funding" })).StatusCode);
    }

    /// <summary>Only the tested points field is invalid; other required fields remain valid.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("101")]
    [InlineData("1.5")]
    [InlineData("null")]
    [InlineData("9223372036854775808")]
    public async Task InvalidTransferAmountsReturnBadRequest(string points)
    {
        // Arrange
        var json = $$"""{"operationId":"{{Guid.NewGuid()}}","destinationWalletId":"{{Guid.NewGuid()}}","points":{{points}},"reason":"Transfer"}""";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync($"/api/wallets/{Guid.NewGuid()}/transfers", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    /// <summary>Business rejections and missing identities have their expected HTTP status and no state change.</summary>
    [Fact]
    public async Task MissingWalletSameWalletAndInsufficientBalanceReturnProblemDetails()
    {
        // Arrange
        var source = await OpenAsync("Empty source");
        var destination = await OpenAsync("Destination");
        var path = $"/api/wallets/{source}/transfers";
        // Act
        foreach (var (target, status, code) in new[]
        {
            (destination, HttpStatusCode.UnprocessableEntity, "insufficient_points"),
            (source, HttpStatusCode.UnprocessableEntity, "same_wallet_transfer"),
            (Guid.Empty, HttpStatusCode.UnprocessableEntity, "invalid_wallet_id"),
            (Guid.NewGuid(), HttpStatusCode.NotFound, "wallet_not_found")
        })
        {
            var response = await client.PostAsJsonAsync(path,
                new { operationId = Guid.NewGuid(), destinationWalletId = target, points = 1, reason = "Transfer" });
            // Assert
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        var emptyOperation = await client.PostAsJsonAsync(path,
            new { operationId = Guid.Empty, destinationWalletId = destination, points = 1, reason = "Transfer" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, emptyOperation.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { points = 1, reason = "Transfer" })).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>($"/api/wallets/{source}/balance")).GetProperty("points").GetInt64());
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var operation = document.GetProperty("paths").GetProperty("/api/wallets/{sourceWalletId}/transfers").GetProperty("post");
        Assert.All(new[] { "200", "400", "404", "409", "422" }, code => Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _)));
    }
}
