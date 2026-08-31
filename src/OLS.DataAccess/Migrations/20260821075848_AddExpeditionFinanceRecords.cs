using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddExpeditionFinanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expedition_finance_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expedition_id = table.Column<long>(type: "bigint", nullable: true),
                    load_transfer_id = table.Column<long>(type: "bigint", nullable: true),
                    expedition_number = table.Column<string>(type: "text", nullable: true),
                    load_number = table.Column<string>(type: "text", nullable: true),
                    item_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    document_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expected_income_try = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    expected_expense_try = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    realized_income_try = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    realized_expense_try = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expedition_finance_records_pkey", x => x.id);
                    table.ForeignKey(
                        name: "expedition_finance_records_expedition_id_foreign",
                        column: x => x.expedition_id,
                        principalTable: "expeditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "expedition_finance_records_load_transfer_id_foreign",
                        column: x => x.load_transfer_id,
                        principalTable: "load_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expedition_finance_records_expedition_id",
                table: "expedition_finance_records",
                column: "expedition_id");

            migrationBuilder.CreateIndex(
                name: "IX_expedition_finance_records_load_transfer_id",
                table: "expedition_finance_records",
                column: "load_transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_expedition_finance_records_siber_id",
                table: "expedition_finance_records",
                column: "siber_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expedition_finance_records");
        }
    }
}
