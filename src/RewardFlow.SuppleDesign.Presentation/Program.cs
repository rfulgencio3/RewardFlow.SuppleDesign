using RewardFlow.SuppleDesign.Application.Wallets;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Infrastructure.Repositories;
using RewardFlow.SuppleDesign.Presentation.Errors;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton<IRewardsWalletRepository, InMemoryRewardsWalletRepository>();
builder.Services.AddScoped<IOpenRewardsWallet, OpenRewardsWallet>();
builder.Services.AddScoped<ICreditRewardPoints, CreditRewardPoints>();
builder.Services.AddScoped<IDebitRewardPoints, DebitRewardPoints>();
builder.Services.AddScoped<IGetWalletBalance, GetWalletBalance>();
builder.Services.AddScoped<ICheckWalletAffordability, CheckWalletAffordability>();

var app = builder.Build();
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
