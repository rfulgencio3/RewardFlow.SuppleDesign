using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RewardFlow.SuppleDesign.Domain.Ports;
using RewardFlow.SuppleDesign.Infrastructure.Persistence;
using RewardFlow.SuppleDesign.Infrastructure.Repositories;

namespace RewardFlow.SuppleDesign.Infrastructure;

/// <summary>Composition helpers that register and initialize the PostgreSQL secondary adapter.</summary>
public static class DependencyInjection
{
    /// <summary>Registers EF Core and the PostgreSQL implementation of the repository port.</summary>
    public static IServiceCollection AddRewardFlowPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<RewardFlowDbContext>(options =>
            options.UseNpgsql(connectionString, postgres => postgres.EnableRetryOnFailure()));
        services.AddScoped<IRewardsWalletRepository, PostgresRewardsWalletRepository>();
        return services;
    }

    /// <summary>Applies every pending EF Core migration before the API begins accepting requests.</summary>
    public static async Task ApplyRewardFlowMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<RewardFlowDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
