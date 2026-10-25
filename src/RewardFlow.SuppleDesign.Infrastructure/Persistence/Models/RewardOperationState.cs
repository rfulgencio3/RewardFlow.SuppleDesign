using RewardFlow.SuppleDesign.Domain.Entities;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

/// <summary>Global operation identity shared by ordinary movements and transfers.</summary>
internal sealed class RewardOperationState
{
    public Guid OperationId { get; set; }
    public RewardOperationKind Kind { get; set; }
}
