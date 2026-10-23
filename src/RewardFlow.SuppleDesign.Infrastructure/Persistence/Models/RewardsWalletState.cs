using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

/// <summary>Persistence model kept outside the aggregate so EF Core concerns do not shape the domain.</summary>
internal sealed class RewardsWalletState
{
    /// <summary>Database key corresponding to the aggregate identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Normalized human-readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Lifecycle state stored as a stable integer enum value.</summary>
    public RewardsWalletStatus Status { get; set; }

    /// <summary>UTC instant at which the wallet was opened.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Flattened value of the immutable RewardPoints balance.</summary>
    public long Points { get; set; }

    /// <summary>Optimistic-concurrency revision used by the repository port contract.</summary>
    public long Revision { get; set; }
}
