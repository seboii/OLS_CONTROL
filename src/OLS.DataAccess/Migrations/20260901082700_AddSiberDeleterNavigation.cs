using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSiberDeleterNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_loads_SiberDeletedByUserId",
                table: "loads",
                column: "SiberDeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfers_SiberDeletedByUserId",
                table: "load_transfers",
                column: "SiberDeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_expeditions_siber_deleted_by_user_id",
                table: "expeditions",
                column: "siber_deleted_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "expeditions_siber_deleted_by_user_id_foreign",
                table: "expeditions",
                column: "siber_deleted_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "load_transfers_siber_deleted_by_user_id_foreign",
                table: "load_transfers",
                column: "SiberDeletedByUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "loads_siber_deleted_by_user_id_foreign",
                table: "loads",
                column: "SiberDeletedByUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "expeditions_siber_deleted_by_user_id_foreign",
                table: "expeditions");

            migrationBuilder.DropForeignKey(
                name: "load_transfers_siber_deleted_by_user_id_foreign",
                table: "load_transfers");

            migrationBuilder.DropForeignKey(
                name: "loads_siber_deleted_by_user_id_foreign",
                table: "loads");

            migrationBuilder.DropIndex(
                name: "IX_loads_SiberDeletedByUserId",
                table: "loads");

            migrationBuilder.DropIndex(
                name: "IX_load_transfers_SiberDeletedByUserId",
                table: "load_transfers");

            migrationBuilder.DropIndex(
                name: "IX_expeditions_siber_deleted_by_user_id",
                table: "expeditions");
        }
    }
}
