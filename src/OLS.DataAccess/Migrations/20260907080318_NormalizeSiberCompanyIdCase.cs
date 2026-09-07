using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// ŞİRKET GUID'İNİ BÜYÜK HARFE ÇEKER — görünürlük kuralı bu sütuna dayanıyor.
    ///
    /// <c>siber_company_id</c> yerelde İKİ farklı yazımla birikmişti:
    ///
    /// | Tablo | küçük | BÜYÜK |
    /// |---|---|---|
    /// | loads | 0 | 19.528 |
    /// | load_transfers | 0 | 8.035 |
    /// | expeditions | 2 | 4.410 |
    /// | users | 0 | 6 |
    /// | accounting_plans | 3.949 | 0 |
    /// | finance_invoices | 38.789 | 0 |
    /// | finance_payments | 29.122 | 0 |
    /// | finance_vouchers | 55.210 | 0 |
    /// | finance_voucher_lines | 215.989 | 0 |
    ///
    /// Kaynak iki ayrı okuma konvansiyonuydu: <c>SiberSyncService</c> düz
    /// <c>CAST(sirketid AS VARCHAR)</c> (SQL Server BÜYÜK harf üretir),
    /// <c>SiberFinanceRepository</c> ise beş sorgunun hepsinde
    /// <c>LOWER(CAST(...))</c> kullanıyordu. İki sefer de şirket seçicisinden
    /// gelen dizgenin olduğu gibi saklanmasından küçük harfli kalmıştı.
    ///
    /// NEDEN ÖNEMLİ: görünürlük süzgeci bu sütunu BÜYÜK harfli sabitlerle
    /// (<c>CompanyScope.OlsCompanyId</c> / <c>AvroraCompanyId</c>)
    /// karşılaştırıyor ve PostgreSQL'in <c>=</c> işleci harfe DUYARLI. Sonuç
    /// iki yönde birden yanlıştı:
    ///
    ///   • Avrora kullanıcısı, kendi finans/muhasebe kayıtlarının HİÇBİRİNİ
    ///     göremiyordu (<c>küçük = BÜYÜK</c> hiç eşleşmez → ekranlar boş).
    ///   • OLS kullanıcısı, Avrora'nın finans kayıtlarının TAMAMINI görüyordu
    ///     (<c>küçük &lt;&gt; BÜYÜK</c> daima doğru → dışlama hiç çalışmaz).
    ///
    /// KANONİK BİÇİM BÜYÜK HARF — ve bu, <c>NormalizeSiberGuidKeyCase</c>
    /// migrasyonunun <c>load_transfer_id</c> için seçtiği küçük harften
    /// BİLEREK farklıdır: şirket kimlikleri kod genelinde sabit olarak BÜYÜK
    /// harfle duruyor (şube eşlemesi, yetenek kuralı, Siber INSERT'leri) ve
    /// onları küçültmek Siber'e yazan yolları da değiştirirdi. İki sütunu
    /// "tutarlılık olsun" diye aynı yöne çekmeyin.
    ///
    /// Yazan uçlar aynı değişiklikte düzeltildi (finans deposundaki
    /// <c>LOWER()</c> kaldırıldı, <c>ResolveWriteCompanyAsync</c> artık kanonik
    /// değeri döndürüyor), süzgeç de <c>upper()</c> ile harfe duyarsız hâle
    /// getirildi — yani bu migrasyon tek başına değil, üçüncü hat.
    /// </summary>
    public partial class NormalizeSiberCompanyIdCase : Migration
    {
        private static readonly string[] Tables =
        [
            "accounting_plans",
            "expeditions",
            "finance_invoices",
            "finance_payments",
            "finance_voucher_lines",
            "finance_vouchers",
            "load_transfers",
            "loads",
            "users",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql($"""
                    UPDATE {table}
                    SET siber_company_id = upper(siber_company_id)
                    WHERE siber_company_id IS NOT NULL
                      AND siber_company_id <> upper(siber_company_id);
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınamaz: hangi satırın hangi harf düzeninde olduğu bilgisi
            // kaybolur. Tutarsızlığın kendisi zaten hataydı — küçük harfe geri
            // çevirmek görünürlük hatasını da geri getirirdi.
        }
    }
}
