namespace RewardFlow.SuppleDesign.Domain.Entities;

/// <summary>Conceptual Contours: explicit lifecycle states that govern wallet behavior.</summary>
public enum RewardsWalletStatus
{
    /// <summary>The wallet accepts earned points and redemptions.</summary>
    Active,

    /// <summary>The wallet accepts earned points while redemptions are temporarily blocked.</summary>
    Suspended,

    /// <summary>The wallet is permanently closed and rejects every movement.</summary>
    Closed
}
