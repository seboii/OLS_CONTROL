using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialItemDefaultAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "default_account_id",
                table: "financial_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_account_name",
                table: "financial_items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_account_id",
                table: "financial_items");

            migrationBuilder.DropColumn(
                name: "default_account_name",
                table: "financial_items");
        }
    }
}
