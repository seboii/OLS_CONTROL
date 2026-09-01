using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSiberChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "siber_change_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    table_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    record_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    operation = table.Column<short>(type: "smallint", nullable: true),
                    fields = table.Column<string>(type: "text", nullable: true),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    record_label = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: true),
                    module = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("siber_change_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "siber_change_logs_user_id_foreign",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_siber_change_logs_user_id",
                table: "siber_change_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "siber_change_logs_record_index",
                table: "siber_change_logs",
                columns: new[] { "table_name", "record_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "siber_change_logs_siber_id_unique",
                table: "siber_change_logs",
                column: "siber_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "siber_change_logs");
        }
    }
}
