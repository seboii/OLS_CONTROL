using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadTransferDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evrak_turus",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("evrak_turus_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    yukevrakid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_transfer_id = table.Column<long>(type: "bigint", nullable: false),
                    evrak_turu_id = table.Column<long>(type: "bigint", nullable: true),
                    document_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    original_count = table.Column<int>(type: "integer", nullable: true),
                    copy_count = table.Column<int>(type: "integer", nullable: true),
                    delivered_to = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    delivered_at = table.Column<DateOnly>(type: "date", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_documents_pkey", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evrak_turus");

            migrationBuilder.DropTable(
                name: "load_transfer_documents");
        }
    }
}
