using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Api.Errors;
using RewardFlow.SuppleDesign.Api.DTOs.Wallets;
using RewardFlow.SuppleDesign.Infrastructure;

Console.WriteLine();
Console.WriteLine("RewardFlow API");
Console.WriteLine("[1/3] Preparing application services...");

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("RewardFlow")
    ?? throw new InvalidOperationException("Connection string 'RewardFlow' is required.");
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RewardFlow API",
        Version = "v1",
        Description = "Loyalty and reward points management API."
    });
    options.SchemaFilter<WalletContractExamples>();
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddRewardFlowPersistence(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IOpenRewardsWallet, OpenRewardsWallet>();
builder.Services.AddScoped<IGetRewardsWallet, GetRewardsWallet>();
builder.Services.AddScoped<ICreditRewardPoints, CreditRewardPoints>();
builder.Services.AddScoped<IDebitRewardPoints, DebitRewardPoints>();
builder.Services.AddScoped<ITransferRewardPoints, TransferRewardPoints>();
builder.Services.AddScoped<IGetWalletBalance, GetWalletBalance>();
builder.Services.AddScoped<ICheckWalletAffordability, CheckWalletAffordability>();
builder.Services.AddScoped<ISuspendRewardsWallet, SuspendRewardsWallet>();
builder.Services.AddScoped<IReactivateRewardsWallet, ReactivateRewardsWallet>();
builder.Services.AddScoped<ICloseRewardsWallet, CloseRewardsWallet>();
builder.Services.AddScoped<IListRewardTransactions, ListRewardTransactions>();

var app = builder.Build();
Console.WriteLine("[2/3] Connecting to PostgreSQL and applying migrations...");
try
{
    await app.Services.ApplyRewardFlowMigrationsAsync();
    Console.WriteLine("[OK]  Database is ready.");
}
catch (RewardFlowDatabaseUnavailableException exception)
{
    Console.Error.WriteLine("[ERROR] PostgreSQL is unavailable.");
    Console.Error.WriteLine("        Start it with: docker compose up -d --wait");
    Console.Error.WriteLine($"        Details: {exception.InnerException?.Message ?? exception.Message}");
    Environment.ExitCode = 1;
    return;
}
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RewardFlow API v1");
    options.DocumentTitle = "RewardFlow API";
});
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();
app.MapControllers();

Console.WriteLine("[3/3] Starting the HTTP server...");
await app.StartAsync();
var applicationUrl = app.Urls.FirstOrDefault() ?? "http://localhost:5180";
Console.WriteLine("[OK]  RewardFlow API is ready.");
Console.WriteLine($"      Swagger: {applicationUrl.TrimEnd('/')}/swagger");
Console.WriteLine("      Press Ctrl+C to stop.");
Console.WriteLine();
await app.WaitForShutdownAsync();

/// <summary>Conceptual Contours: composition root connects application ports to infrastructure and HTTP adapters.</summary>
public partial class Program { }
