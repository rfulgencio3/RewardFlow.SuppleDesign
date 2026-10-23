using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardTransactionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reward_transactions",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    balance_after = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_transactions", x => x.operation_id);
                    table.CheckConstraint("ck_reward_transactions_balance_non_negative", "balance_after >= 0");
                    table.CheckConstraint("ck_reward_transactions_points_positive", "points > 0");
                    table.CheckConstraint("ck_reward_transactions_reason_not_blank", "length(btrim(reason)) > 0");
                    table.CheckConstraint("ck_reward_transactions_type_valid", "type BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_reward_transactions_rewards_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "rewards_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reward_transactions_wallet_id_occurred_at_utc",
                table: "reward_transactions",
                columns: new[] { "wallet_id", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reward_transactions");
        }
    }
}
