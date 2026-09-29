using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesafioArquiteturaSoftware.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountIdToFinancialTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "financial_transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_financial_transactions_account_id",
                table: "financial_transactions",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_financial_transactions_account_id",
                table: "financial_transactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "financial_transactions");
        }
    }
}
