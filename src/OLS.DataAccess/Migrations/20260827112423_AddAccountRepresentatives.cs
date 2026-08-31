using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountRepresentatives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_representatives",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    user_type = table.Column<int>(type: "integer", nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_representatives_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_representatives_account_id_user_type",
                table: "account_representatives",
                columns: new[] { "account_id", "user_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_representatives");
        }
    }
}
