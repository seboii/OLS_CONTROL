using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadFileTransferLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "load_transfer_id",
                table: "load_files",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "load_transfer_id",
                table: "load_files");
        }
    }
}
