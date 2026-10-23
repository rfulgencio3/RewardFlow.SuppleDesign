using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RewardFlow.SuppleDesign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletProfileAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "rewards_wallets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "rewards_wallets",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "rewards_wallets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE rewards_wallets
                SET name = 'Imported wallet',
                    created_at_utc = TIMESTAMPTZ '1970-01-01 00:00:00+00'
                WHERE name IS NULL OR created_at_utc IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "rewards_wallets",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "rewards_wallets",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_rewards_wallets_name_not_blank",
                table: "rewards_wallets",
                sql: "length(btrim(name)) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_rewards_wallets_status_valid",
                table: "rewards_wallets",
                sql: "status BETWEEN 0 AND 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_rewards_wallets_name_not_blank",
                table: "rewards_wallets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rewards_wallets_status_valid",
                table: "rewards_wallets");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "rewards_wallets");

            migrationBuilder.DropColumn(
                name: "name",
                table: "rewards_wallets");

            migrationBuilder.DropColumn(
                name: "status",
                table: "rewards_wallets");
        }
    }
}
