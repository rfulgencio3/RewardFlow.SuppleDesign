using Microsoft.EntityFrameworkCore;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence;

/// <summary>EF Core unit for PostgreSQL persistence; it maps storage records rather than domain entities.</summary>
public sealed class RewardFlowDbContext(DbContextOptions<RewardFlowDbContext> options) : DbContext(options)
{
    /// <summary>Stored wallet state used only by the PostgreSQL adapter.</summary>
    internal DbSet<RewardsWalletState> RewardsWallets => Set<RewardsWalletState>();
    /// <summary>Immutable movement records used by the PostgreSQL adapter.</summary>
    internal DbSet<RewardTransactionState> RewardTransactions => Set<RewardTransactionState>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RewardFlowDbContext).Assembly);
    }
}
