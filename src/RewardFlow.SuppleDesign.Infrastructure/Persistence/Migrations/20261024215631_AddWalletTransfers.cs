using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reward_transactions_rewards_wallets_wallet_id",
                table: "reward_transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reward_transactions",
                table: "reward_transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "transaction_id",
                table: "reward_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Preserve every existing entry identity before changing the primary key.
            migrationBuilder.Sql("UPDATE reward_transactions SET transaction_id = operation_id;");
            migrationBuilder.Sql("ALTER TABLE reward_transactions ALTER COLUMN transaction_id DROP DEFAULT;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reward_transactions",
                table: "reward_transactions",
                column: "transaction_id");

            migrationBuilder.CreateTable(
                name: "reward_operations",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_operations", x => x.operation_id);
                    table.CheckConstraint("ck_reward_operations_kind_valid", "kind BETWEEN 0 AND 2");
                });

            migrationBuilder.Sql("INSERT INTO reward_operations (operation_id, kind) SELECT operation_id, type FROM reward_transactions;");

            migrationBuilder.CreateTable(
                name: "reward_transfers",
                columns: table => new
                {
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_balance_after = table.Column<long>(type: "bigint", nullable: false),
                    destination_balance_after = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_transfers", x => x.operation_id);
                    table.CheckConstraint("ck_reward_transfers_amount", "points BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_reward_transfers_balances", "source_balance_after >= 0 AND destination_balance_after >= 0");
                    table.CheckConstraint("ck_reward_transfers_distinct_wallets", "source_wallet_id <> destination_wallet_id");
                    table.CheckConstraint("ck_reward_transfers_reason", "length(btrim(reason)) > 0");
                    table.ForeignKey(
                        name: "FK_reward_transfers_reward_operations_operation_id",
                        column: x => x.operation_id,
                        principalTable: "reward_operations",
                        principalColumn: "operation_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reward_transfers_rewards_wallets_destination_wallet_id",
                        column: x => x.destination_wallet_id,
                        principalTable: "rewards_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reward_transfers_rewards_wallets_source_wallet_id",
                        column: x => x.source_wallet_id,
                        principalTable: "rewards_wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reward_transactions_operation_id_wallet_id",
                table: "reward_transactions",
                columns: new[] { "operation_id", "wallet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reward_transfers_destination_wallet_id",
                table: "reward_transfers",
                column: "destination_wallet_id");

            migrationBuilder.CreateIndex(
                name: "IX_reward_transfers_source_wallet_id",
                table: "reward_transfers",
                column: "source_wallet_id");

            migrationBuilder.AddForeignKey(
                name: "FK_reward_transactions_reward_operations_operation_id",
                table: "reward_transactions",
                column: "operation_id",
                principalTable: "reward_operations",
                principalColumn: "operation_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reward_transactions_rewards_wallets_wallet_id",
                table: "reward_transactions",
                column: "wallet_id",
                principalTable: "rewards_wallets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Two entries cannot fit the old global operation_id key. Never discard transfer history.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM reward_operations WHERE kind = 2) THEN
                        RAISE EXCEPTION 'Cannot downgrade AddWalletTransfers while transfer operations exist.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_reward_transactions_reward_operations_operation_id",
                table: "reward_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_reward_transactions_rewards_wallets_wallet_id",
                table: "reward_transactions");

            migrationBuilder.DropTable(
                name: "reward_transfers");

            migrationBuilder.DropTable(
                name: "reward_operations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reward_transactions",
                table: "reward_transactions");

            migrationBuilder.DropIndex(
                name: "IX_reward_transactions_operation_id_wallet_id",
                table: "reward_transactions");

            migrationBuilder.DropColumn(
                name: "transaction_id",
                table: "reward_transactions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_reward_transactions",
                table: "reward_transactions",
                column: "operation_id");

            migrationBuilder.AddForeignKey(
                name: "FK_reward_transactions_rewards_wallets_wallet_id",
                table: "reward_transactions",
                column: "wallet_id",
                principalTable: "rewards_wallets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
