using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSiberDeleterColumnsForLoads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SiberDeletedOn",
                table: "loads",
                newName: "siber_deleted_on");

            migrationBuilder.RenameColumn(
                name: "SiberDeletedByUserId",
                table: "loads",
                newName: "siber_deleted_by_user_id");

            migrationBuilder.RenameColumn(
                name: "SiberDeletedBy",
                table: "loads",
                newName: "siber_deleted_by");

            migrationBuilder.RenameIndex(
                name: "IX_loads_SiberDeletedByUserId",
                table: "loads",
                newName: "IX_loads_siber_deleted_by_user_id");

            migrationBuilder.RenameColumn(
                name: "SiberDeletedOn",
                table: "load_transfers",
                newName: "siber_deleted_on");

            migrationBuilder.RenameColumn(
                name: "SiberDeletedByUserId",
                table: "load_transfers",
                newName: "siber_deleted_by_user_id");

            migrationBuilder.RenameColumn(
                name: "SiberDeletedBy",
                table: "load_transfers",
                newName: "siber_deleted_by");

            migrationBuilder.RenameIndex(
                name: "IX_load_transfers_SiberDeletedByUserId",
                table: "load_transfers",
                newName: "IX_load_transfers_siber_deleted_by_user_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "siber_deleted_on",
                table: "loads",
                type: "timestamp(0) without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "siber_deleted_by",
                table: "loads",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "siber_deleted_on",
                table: "load_transfers",
                type: "timestamp(0) without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "siber_deleted_by",
                table: "load_transfers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "siber_deleted_on",
                table: "loads",
                newName: "SiberDeletedOn");

            migrationBuilder.RenameColumn(
                name: "siber_deleted_by_user_id",
                table: "loads",
                newName: "SiberDeletedByUserId");

            migrationBuilder.RenameColumn(
                name: "siber_deleted_by",
                table: "loads",
                newName: "SiberDeletedBy");

            migrationBuilder.RenameIndex(
                name: "IX_loads_siber_deleted_by_user_id",
                table: "loads",
                newName: "IX_loads_SiberDeletedByUserId");

            migrationBuilder.RenameColumn(
                name: "siber_deleted_on",
                table: "load_transfers",
                newName: "SiberDeletedOn");

            migrationBuilder.RenameColumn(
                name: "siber_deleted_by_user_id",
                table: "load_transfers",
                newName: "SiberDeletedByUserId");

            migrationBuilder.RenameColumn(
                name: "siber_deleted_by",
                table: "load_transfers",
                newName: "SiberDeletedBy");

            migrationBuilder.RenameIndex(
                name: "IX_load_transfers_siber_deleted_by_user_id",
                table: "load_transfers",
                newName: "IX_load_transfers_SiberDeletedByUserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SiberDeletedOn",
                table: "loads",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SiberDeletedBy",
                table: "loads",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SiberDeletedOn",
                table: "load_transfers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SiberDeletedBy",
                table: "load_transfers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
