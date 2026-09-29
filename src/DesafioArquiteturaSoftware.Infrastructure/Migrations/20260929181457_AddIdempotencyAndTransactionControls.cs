using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesafioArquiteturaSoftware.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyAndTransactionControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "financial_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "financial_transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "financial_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "financial_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_idempotency_records_key",
                table: "idempotency_records",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "financial_transactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "financial_transactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "financial_transactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "financial_transactions");
        }
    }
}
