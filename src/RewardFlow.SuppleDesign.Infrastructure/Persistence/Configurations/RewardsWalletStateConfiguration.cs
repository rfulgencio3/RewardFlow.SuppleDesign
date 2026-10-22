using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Configurations;

/// <summary>Maps the persistence model to PostgreSQL without adding ORM annotations to the domain.</summary>
internal sealed class RewardsWalletStateConfiguration : IEntityTypeConfiguration<RewardsWalletState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RewardsWalletState> builder)
    {
        builder.ToTable("rewards_wallets", table =>
        {
            table.HasCheckConstraint("ck_rewards_wallets_points_non_negative", "points >= 0");
            table.HasCheckConstraint("ck_rewards_wallets_revision_non_negative", "revision >= 0");
        });

        builder.HasKey(wallet => wallet.Id);
        builder.Property(wallet => wallet.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(wallet => wallet.Points).HasColumnName("points").IsRequired();
        builder.Property(wallet => wallet.Revision).HasColumnName("revision").IsRequired();
    }
}
