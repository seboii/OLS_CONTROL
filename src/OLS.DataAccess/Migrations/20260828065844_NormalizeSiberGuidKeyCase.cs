using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSiberGuidKeyCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aynı Siber GUID'i iki tabloda FARKLI harf düzeninde saklanıyordu:
            // load_transfers.load_transfer_id %99 küçük harf (eski ETL + .NET'in
            // Guid.ToString() çıktısı), load_transfer_packages.load_transfer_id ise
            // %99 BÜYÜK harf (Siber'in CAST(... AS VARCHAR) çıktısı, her senkronda
            // üzerine yazılıyordu).
            //
            // Senkron kodu bunu hiç fark etmedi çünkü eşleştirmeyi bellekte
            // StringComparer.OrdinalIgnoreCase ile yapıyor. Ama PostgreSQL join'leri
            // HARFE DUYARLI: 8021 koli satırının yalnızca 39'u yüküne bağlanabiliyordu.
            // Görünen sonuç, Sefer ekranında yüklerin kap/kilo bilgisinin boş olmasıydı
            // (sefere bağlı 7666 yükün 7659'unda koli görünmüyordu); Yük ekranındaki
            // Paketler sekmesi, toplam hesaplama ve silme kaskadı da aynı join'i
            // kullandığı için etkileniyordu.
            //
            // Kanonik biçim KÜÇÜK harf seçildi: .NET Guid.ToString() bunu üretiyor,
            // dolayısıyla uygulamanın kendi açtığı kayıtlar zaten uyumlu. Okuma tarafı
            // da LOWER(CAST(...)) ile hizalandı (bkz. SiberSyncService).
            migrationBuilder.Sql("UPDATE load_transfers SET load_transfer_id = lower(load_transfer_id) WHERE load_transfer_id <> lower(load_transfer_id);");
            migrationBuilder.Sql("UPDATE load_transfer_packages SET load_transfer_id = lower(load_transfer_id) WHERE load_transfer_id <> lower(load_transfer_id);");
            migrationBuilder.Sql("UPDATE load_transfer_packages SET yukkoliid = lower(yukkoliid) WHERE yukkoliid <> lower(yukkoliid);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınamaz: hangi satırın hangi harf düzeninde olduğu bilgisi
            // kaybolur. Zaten tutarsızlığın kendisi hataydı.
        }
    }
}
