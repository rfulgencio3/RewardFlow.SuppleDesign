using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Configurations;

/// <summary>Maps transaction records while keeping EF Core outside the domain model.</summary>
internal sealed class RewardTransactionStateConfiguration : IEntityTypeConfiguration<RewardTransactionState>
{
    public void Configure(EntityTypeBuilder<RewardTransactionState> builder)
    {
        builder.ToTable("reward_transactions", table =>
        {
            table.HasCheckConstraint("ck_reward_transactions_points_positive", "points > 0");
            table.HasCheckConstraint("ck_reward_transactions_balance_non_negative", "balance_after >= 0");
            table.HasCheckConstraint("ck_reward_transactions_type_valid", "type BETWEEN 0 AND 1");
            table.HasCheckConstraint("ck_reward_transactions_reason_not_blank", "length(btrim(reason)) > 0");
        });
        builder.HasKey(transaction => transaction.OperationId);
        builder.Property(transaction => transaction.OperationId).HasColumnName("operation_id").ValueGeneratedNever();
        builder.Property(transaction => transaction.WalletId).HasColumnName("wallet_id").IsRequired();
        builder.Property(transaction => transaction.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        builder.Property(transaction => transaction.Points).HasColumnName("points").IsRequired();
        builder.Property(transaction => transaction.Reason).HasColumnName("reason").HasMaxLength(200).IsRequired();
        builder.Property(transaction => transaction.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        builder.Property(transaction => transaction.BalanceAfter).HasColumnName("balance_after").IsRequired();
        builder.HasIndex(transaction => new { transaction.WalletId, transaction.OccurredAtUtc });
        builder.HasOne(transaction => transaction.Wallet).WithMany().HasForeignKey(transaction => transaction.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
