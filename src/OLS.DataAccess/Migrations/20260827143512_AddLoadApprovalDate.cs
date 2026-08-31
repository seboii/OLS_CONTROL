using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadApprovalDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "approval_date",
                table: "loads",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approval_date",
                table: "loads");
        }
    }
}
