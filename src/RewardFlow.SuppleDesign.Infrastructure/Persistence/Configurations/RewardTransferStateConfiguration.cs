using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Configurations;

internal sealed class RewardTransferStateConfiguration : IEntityTypeConfiguration<RewardTransferState>
{
    public void Configure(EntityTypeBuilder<RewardTransferState> builder)
    {
        builder.ToTable("reward_transfers", table =>
        {
            table.HasCheckConstraint("ck_reward_transfers_amount", "points BETWEEN 1 AND 100");
            table.HasCheckConstraint("ck_reward_transfers_distinct_wallets", "source_wallet_id <> destination_wallet_id");
            table.HasCheckConstraint("ck_reward_transfers_balances", "source_balance_after >= 0 AND destination_balance_after >= 0");
            table.HasCheckConstraint("ck_reward_transfers_reason", "length(btrim(reason)) > 0");
        });
        builder.HasKey(item => item.OperationId);
        builder.Property(item => item.OperationId).HasColumnName("operation_id").ValueGeneratedNever();
        builder.Property(item => item.SourceWalletId).HasColumnName("source_wallet_id");
        builder.Property(item => item.DestinationWalletId).HasColumnName("destination_wallet_id");
        builder.Property(item => item.Points).HasColumnName("points");
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(200).IsRequired();
        builder.Property(item => item.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(item => item.SourceBalanceAfter).HasColumnName("source_balance_after");
        builder.Property(item => item.DestinationBalanceAfter).HasColumnName("destination_balance_after");
        builder.HasOne<RewardOperationState>().WithOne().HasForeignKey<RewardTransferState>(item => item.OperationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RewardsWalletState>().WithMany().HasForeignKey(item => item.SourceWalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RewardsWalletState>().WithMany().HasForeignKey(item => item.DestinationWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
