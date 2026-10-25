using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RewardFlow.SuppleDesign.Infrastructure.Persistence.Models;

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Configurations;

internal sealed class RewardOperationStateConfiguration : IEntityTypeConfiguration<RewardOperationState>
{
    public void Configure(EntityTypeBuilder<RewardOperationState> builder)
    {
        builder.ToTable("reward_operations", table =>
            table.HasCheckConstraint("ck_reward_operations_kind_valid", "kind BETWEEN 0 AND 2"));
        builder.HasKey(item => item.OperationId);
        builder.Property(item => item.OperationId).HasColumnName("operation_id").ValueGeneratedNever();
        builder.Property(item => item.Kind).HasColumnName("kind").HasConversion<int>();
    }
}
