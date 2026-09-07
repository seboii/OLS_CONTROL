using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// YEVMİYE SATIRLARINDA ŞİRKET SÜZGECİ İÇİN İFADE İNDEKSİ.
    ///
    /// Şirket görünürlüğü sorguyu <c>upper(siber_company_id) = ...</c> biçiminde
    /// süzüyor (bkz. <c>CompanyVisibilityExtensions</c>); düz bir sütun indeksi
    /// bu ifadeyle KULLANILMAZ, ifade indeksi gerekiyor.
    ///
    /// ÖLÇÜM (canlı veri, 2026-09-07) — şirkete göre sayım:
    ///
    /// | Tablo | satır | süre |
    /// |---|---|---|
    /// | finance_voucher_lines | 215.989 | **48 ms → 9,6 ms** (sıcak önbellek) |
    /// | finance_vouchers | 55.210 | 33 ms |
    /// | finance_invoices | 38.789 | 31 ms |
    /// | finance_payments | 29.122 | 25 ms |
    /// | loads | 19.528 | 25 ms |
    /// | load_transfers | 8.035 | 6 ms |
    /// | expeditions | 4.412 | 5 ms |
    ///
    /// SADECE EN BÜYÜK TABLOYA indeks ekleniyor. Diğerleri zaten 25-33 ms'de
    /// bitiyor ve tamamı her senkron turunda yeniden yazılıyor — spekülatif
    /// indeksler yazma maliyeti getirip okumada kayda değer bir şey
    /// kazandırmıyordu. Ölçüm değişirse liste genişletilebilir.
    ///
    /// İkinci ve daha önemli kazanç: ifade indeksi olmadan planlayıcı
    /// <c>upper()</c> için seçicilik istatistiği tutamıyor ve satır sayısını
    /// 12 kat yanlış tahmin ediyordu (450 tahmin / 5.638 gerçek). Mizan
    /// sorguları bu tabloyu hesap planına ve carilere bağladığı için yanlış
    /// tahmin yanlış birleştirme planı seçtirebiliyor.
    ///
    /// EF ifade indeksini modelden üretemiyor; ham SQL ile oluşturuluyor ve bu
    /// yüzden ModelSnapshot'ta görünmez.
    /// </summary>
    public partial class AddFinanceVoucherLineCompanyIndex : Migration
    {
        private const string IndexName = "finance_voucher_lines_company_upper_index";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            // ANALYZE ŞART: ifade indeksinin seçicilik istatistiği ancak
            // toplandıktan sonra oluşur. Onsuz planlayıcı 1.080 satır tahmin
            // edip bitmap taramaya düşüyor ve indeks kazanç getirmiyordu
            // (ölçüldü: 60 ms, ANALYZE sonrası 9,6 ms). Otomatik vacuum er geç
            // toplardı ama taze kurulumun ilk günü yavaş kalırdı.
            migrationBuilder.Sql($"""
                CREATE INDEX IF NOT EXISTS {IndexName}
                ON finance_voucher_lines (upper(siber_company_id));

                ANALYZE finance_voucher_lines;
                """);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql($"DROP INDEX IF EXISTS {IndexName};");
    }
}
