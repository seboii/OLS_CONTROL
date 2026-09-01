using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSiberAuditAndDeletionMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "siber_created_at",
                table: "loads",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_created_by",
                table: "loads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_created_by_user_id",
                table: "loads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_deleted_at",
                table: "loads",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_updated_at",
                table: "loads",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_updated_by",
                table: "loads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_updated_by_user_id",
                table: "loads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_created_at",
                table: "load_transfers",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_created_by",
                table: "load_transfers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_created_by_user_id",
                table: "load_transfers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_deleted_at",
                table: "load_transfers",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_updated_at",
                table: "load_transfers",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_updated_by",
                table: "load_transfers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_updated_by_user_id",
                table: "load_transfers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_created_at",
                table: "expeditions",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_created_by",
                table: "expeditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_created_by_user_id",
                table: "expeditions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_deleted_at",
                table: "expeditions",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_updated_at",
                table: "expeditions",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_updated_by",
                table: "expeditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_updated_by_user_id",
                table: "expeditions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_loads_siber_created_by_user_id",
                table: "loads",
                column: "siber_created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_loads_siber_updated_by_user_id",
                table: "loads",
                column: "siber_updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "loads_siber_deleted_at_index",
                table: "loads",
                column: "siber_deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfers_siber_created_by_user_id",
                table: "load_transfers",
                column: "siber_created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfers_siber_updated_by_user_id",
                table: "load_transfers",
                column: "siber_updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "load_transfers_siber_deleted_at_index",
                table: "load_transfers",
                column: "siber_deleted_at");

            migrationBuilder.CreateIndex(
                name: "expeditions_siber_deleted_at_index",
                table: "expeditions",
                column: "siber_deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_expeditions_siber_created_by_user_id",
                table: "expeditions",
                column: "siber_created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expeditions_siber_updated_by_user_id",
                table: "expeditions",
                column: "siber_updated_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "expeditions_siber_created_by_user_id_foreign",
                table: "expeditions",
                column: "siber_created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "expeditions_siber_updated_by_user_id_foreign",
                table: "expeditions",
                column: "siber_updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "load_transfers_siber_created_by_user_id_foreign",
                table: "load_transfers",
                column: "siber_created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "load_transfers_siber_updated_by_user_id_foreign",
                table: "load_transfers",
                column: "siber_updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "loads_siber_created_by_user_id_foreign",
                table: "loads",
                column: "siber_created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "loads_siber_updated_by_user_id_foreign",
                table: "loads",
                column: "siber_updated_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "expeditions_siber_created_by_user_id_foreign",
                table: "expeditions");

            migrationBuilder.DropForeignKey(
                name: "expeditions_siber_updated_by_user_id_foreign",
                table: "expeditions");

            migrationBuilder.DropForeignKey(
                name: "load_transfers_siber_created_by_user_id_foreign",
                table: "load_transfers");

            migrationBuilder.DropForeignKey(
                name: "load_transfers_siber_updated_by_user_id_foreign",
                table: "load_transfers");

            migrationBuilder.DropForeignKey(
                name: "loads_siber_created_by_user_id_foreign",
                table: "loads");

            migrationBuilder.DropForeignKey(
                name: "loads_siber_updated_by_user_id_foreign",
                table: "loads");

            migrationBuilder.DropIndex(
                name: "IX_loads_siber_created_by_user_id",
                table: "loads");

            migrationBuilder.DropIndex(
                name: "IX_loads_siber_updated_by_user_id",
                table: "loads");

            migrationBuilder.DropIndex(
                name: "loads_siber_deleted_at_index",
                table: "loads");

            migrationBuilder.DropIndex(
                name: "IX_load_transfers_siber_created_by_user_id",
                table: "load_transfers");

            migrationBuilder.DropIndex(
                name: "IX_load_transfers_siber_updated_by_user_id",
                table: "load_transfers");

            migrationBuilder.DropIndex(
                name: "load_transfers_siber_deleted_at_index",
                table: "load_transfers");

            migrationBuilder.DropIndex(
                name: "expeditions_siber_deleted_at_index",
                table: "expeditions");

            migrationBuilder.DropIndex(
                name: "IX_expeditions_siber_created_by_user_id",
                table: "expeditions");

            migrationBuilder.DropIndex(
                name: "IX_expeditions_siber_updated_by_user_id",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_created_at",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_created_by",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_created_by_user_id",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_deleted_at",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_updated_at",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_updated_by",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_updated_by_user_id",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "siber_created_at",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_created_by",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_created_by_user_id",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_deleted_at",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_updated_at",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_updated_by",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_updated_by_user_id",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_created_at",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_created_by",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_created_by_user_id",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_deleted_at",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_updated_at",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_updated_by",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_updated_by_user_id",
                table: "expeditions");
        }
    }
}
