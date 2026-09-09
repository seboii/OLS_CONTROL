using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// GERÇEK KOLİ SATIRLARININ ANAHTARI DÜZELTİLDİ.
    ///
    /// <c>load_transfer_actual_packages.load_transfer_id</c> ilk sürümde YEREL
    /// sayısal kimliği ("2946") tutuyordu. Oysa beyan edilen koli tablosu
    /// (<c>load_transfer_packages</c>) ve yükün detay ucu Siber'in <c>yukid</c>
    /// GUID'ini kullanıyor.
    ///
    /// Sonuç: "Gerçek Koli Bilgilerine Aktar" düğmesi beyan edilen kolileri
    /// YEREL kimlikle arıyor, hiçbir satır bulamıyor ve "Aktarılacak koli
    /// bilgisi yok" diyordu — düğme hiç çalışmıyordu.
    ///
    /// Bu migrasyon mevcut satırları yeni anahtara taşır: yerel kimlik
    /// <c>load_transfers.id</c> ile eşleşiyorsa o yükün <c>load_transfer_id</c>
    /// (yukid) değeri yazılır. Karşılığı bulunamayan satır SİLİNMEZ, olduğu gibi
    /// bırakılır — bir sonraki senkron turu <c>skn_yukkolidepo</c>'dan doğru
    /// anahtarla yeniden yazar.
    /// </summary>
    public partial class RekeyActualPackagesByYukId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE load_transfer_actual_packages a
                SET load_transfer_id = t.load_transfer_id
                FROM load_transfers t
                WHERE a.load_transfer_id ~ '^[0-9]+$'
                  AND t.id = a.load_transfer_id::bigint
                  AND t.load_transfer_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma: yukid'den yerel sayısal kimliğe döndürür.
            migrationBuilder.Sql("""
                UPDATE load_transfer_actual_packages a
                SET load_transfer_id = t.id::text
                FROM load_transfers t
                WHERE t.load_transfer_id = a.load_transfer_id;
                """);
        }
    }
}
