using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinCore.Transaction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RiskReasons",
                table: "Transactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IsSuspicious",
                table: "Transactions",
                column: "IsSuspicious");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_IsSuspicious",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RiskReasons",
                table: "Transactions");
        }
    }
}
