using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSiberDeleterInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SiberDeletedBy",
                table: "loads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SiberDeletedByUserId",
                table: "loads",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SiberDeletedOn",
                table: "loads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiberDeletedBy",
                table: "load_transfers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SiberDeletedByUserId",
                table: "load_transfers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SiberDeletedOn",
                table: "load_transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_deleted_by",
                table: "expeditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_deleted_by_user_id",
                table: "expeditions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_deleted_on",
                table: "expeditions",
                type: "timestamp(0) without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiberDeletedBy",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "SiberDeletedByUserId",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "SiberDeletedOn",
                table: "loads");

            migrationBuilder.DropColumn(
                name: "SiberDeletedBy",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "SiberDeletedByUserId",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "SiberDeletedOn",
                table: "load_transfers");

            migrationBuilder.DropColumn(
                name: "siber_deleted_by",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_deleted_by_user_id",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "siber_deleted_on",
                table: "expeditions");
        }
    }
}
