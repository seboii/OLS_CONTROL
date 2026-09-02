using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// Carilere program dışı silme izi ekler — yük/teklif/seferde zaten vardı.
    ///
    /// Cari Siber ekranından silinebiliyor ve bu kontrol olmadan yerelde sonsuza
    /// kadar canlı görünüyordu. Canlıda ölçüldü: üç firma (Logista Global,
    /// CK Boğaziçi, DLS Logistic) SERKANK ve ASLIY tarafından silinmişti, cari
    /// listesinde duruyordu ve teklifsiz yük açarken FK hatası veriyordu.
    ///
    /// Kayıt SİLİNMEZ, damgalanır: bağlı finans kayıtları, ilgili kişiler ve
    /// denetim izi korunmalı. Cari Siber'de yeniden görünürse damga temizlenir.
    ///
    /// İKİ DAMGA KARIŞTIRILMAMALI: <c>siber_deleted_on</c> Siber'deki gerçek
    /// silme anı (sbr_log.tarih), <c>siber_deleted_at</c> bizim fark ettiğimiz an.
    /// </summary>
    public partial class AddAccountSiberDeletionTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "siber_deleted_at",
                table: "accounts",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_deleted_by",
                table: "accounts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "siber_deleted_by_user_id",
                table: "accounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "siber_deleted_on",
                table: "accounts",
                type: "timestamp(0) without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_siber_deleted_by_user_id",
                table: "accounts",
                column: "siber_deleted_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "accounts_siber_deleted_by_user_id_foreign",
                table: "accounts",
                column: "siber_deleted_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "accounts_siber_deleted_by_user_id_foreign",
                table: "accounts");

            migrationBuilder.DropIndex(
                name: "IX_accounts_siber_deleted_by_user_id",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "siber_deleted_at",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "siber_deleted_by",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "siber_deleted_by_user_id",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "siber_deleted_on",
                table: "accounts");
        }
    }
}
