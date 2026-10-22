using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence;

/// <summary>Creates the PostgreSQL context for EF Core migration tooling without starting the API.</summary>
public sealed class RewardFlowDbContextFactory : IDesignTimeDbContextFactory<RewardFlowDbContext>
{
    /// <inheritdoc />
    public RewardFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RewardFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=rewardflow;Username=rewardflow;Password=rewardflow_dev")
            .Options;

        return new RewardFlowDbContext(options);
    }
}
