using Microsoft.OpenApi.Models;
using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Api.Errors;
using RewardFlow.SuppleDesign.Api.DTOs.Wallets;
using RewardFlow.SuppleDesign.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("RewardFlow")
    ?? throw new InvalidOperationException("Connection string 'RewardFlow' is required.");
builder.Services.AddControllers();
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
builder.Services.AddScoped<IGetWalletBalance, GetWalletBalance>();
builder.Services.AddScoped<ICheckWalletAffordability, CheckWalletAffordability>();
builder.Services.AddScoped<ISuspendRewardsWallet, SuspendRewardsWallet>();
builder.Services.AddScoped<IReactivateRewardsWallet, ReactivateRewardsWallet>();
builder.Services.AddScoped<ICloseRewardsWallet, CloseRewardsWallet>();

var app = builder.Build();
await app.Services.ApplyRewardFlowMigrationsAsync();
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
app.Run();

/// <summary>Conceptual Contours: composition root connects application ports to infrastructure and HTTP adapters.</summary>
public partial class Program { }
