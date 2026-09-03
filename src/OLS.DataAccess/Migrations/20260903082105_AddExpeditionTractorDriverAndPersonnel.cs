using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// SEFERE ÇEKİCİ, SÜRÜCÜ VE KİRALANAN FİRMA EKLER.
    ///
    /// Siber'de römork ile çekici AYRI plakalar (<c>skn_pozisyon.romorkid</c> ve
    /// <c>cekiciid</c>); form yalnızca römorku topluyordu. Siber'in kendi
    /// verisinde bu alanlar aktif: 4.400 pozisyonun özmal olan 260'ında çekici
    /// %92 (240), sürücü %92 (239) dolu; kiralık 4.140 pozisyonun %20'sinde
    /// kiralanan firma dolu (831).
    ///
    /// <c>personnel</c> tablosu <c>sbr_personel</c>'in aynası — sürücü seçilebilsin
    /// diye. Küçük: canlıda 25 personel, 22'si sürücü işaretli. Yerelde personel
    /// AÇILMAZ, yalnızca içe aktarılır.
    /// </summary>
    public partial class AddExpeditionTractorDriverAndPersonnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "driver_id",
                table: "expeditions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rented_company_id",
                table: "expeditions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tractor_id",
                table: "expeditions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "personnel",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    is_driver = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("personnel_pkey", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "personnel");

            migrationBuilder.DropColumn(
                name: "driver_id",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "rented_company_id",
                table: "expeditions");

            migrationBuilder.DropColumn(
                name: "tractor_id",
                table: "expeditions");
        }
    }
}
