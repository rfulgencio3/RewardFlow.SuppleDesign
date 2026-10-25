using RewardFlow.SuppleDesign.Domain.Exceptions;

namespace RewardFlow.SuppleDesign.Domain.ValueObjects;

/// <summary>Standalone Class: a transfer-specific limit, separate from ordinary point arithmetic.</summary>
public sealed record TransferAmount
{
    /// <summary>Inclusive limit for one transfer; not a daily quota.</summary>
    public const int MaximumPoints = 100;
    /// <summary>Validated positive points to move.</summary>
    public RewardPoints Points { get; }

    /// <summary>Assertions: a transfer moves between one and one hundred whole points.</summary>
    public TransferAmount(long points)
    {
        if (points is < 1 or > MaximumPoints)
            throw new DomainException("invalid_transfer_amount", "A transfer must contain between 1 and 100 reward points.");
        Points = new RewardPoints(points);
    }
}
