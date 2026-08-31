using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferCompanyScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "siber_company_id",
                table: "loads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "siber_company_id",
                table: "loads");
        }
    }
}
