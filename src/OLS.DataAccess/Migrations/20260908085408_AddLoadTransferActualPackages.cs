using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadTransferActualPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "load_transfer_actual_packages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    yukkolidepoid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_transfer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    case_type_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    width = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    length = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    volume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    gross_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    net_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    lademeter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    stackable = table.Column<int>(type: "integer", nullable: true),
                    product_type_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_actual_packages_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_actual_packages_load_transfer_id",
                table: "load_transfer_actual_packages",
                column: "load_transfer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "load_transfer_actual_packages");
        }
    }
}
