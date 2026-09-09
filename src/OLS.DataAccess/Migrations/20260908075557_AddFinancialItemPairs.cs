using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// NAVLUN KALEM EŞLEŞMELERİ + "navlun kalemi mi" bayrağı.
    ///
    /// Kullanıcı kuralı: navlun ALIŞI girilince satış satırı kendiliğinden
    /// açılsın ve fiyatı alışın %15 fazlası olsun; "Olumlu" teklifte en az bir
    /// navlun kalemi zorunlu olsun.
    ///
    /// EŞLEŞME SİBER'DE YOK, ADDAN DA TÜRETİLEMEZ. Siber'in kalem tablosunda
    /// alış↔satış bağı tutan hiçbir dolu alan yok (<c>alisfaturaiskalemid</c>
    /// 47.192 satırın tamamında boş, <c>skn_kalemdefault</c> boş, grup sütunları
    /// boş). "GİDERİ → GELİRİ" biçiminde ad çevirisi ise EN ÇOK KULLANILAN
    /// çiftte çalışmaz: kara navlununun geliri "KARA NAVLUN HİZMET BEDELİ"dir,
    /// "KARA NAVLUN GELİRİ" diye bir kalem YOKTUR.
    ///
    /// İLK ALTI ÇİFT gerçek kullanımdan ölçüldü — aynı yükte hangi alış
    /// kaleminin hangi satış kalemiyle birlikte girildiği sayıldı:
    ///
    /// <list type="table">
    /// <item><term>KARA → HİZMET BEDELİ</term><description>1.519 yük, %94,7 baskınlık</description></item>
    /// <item><term>HAVA → GELİRİ</term><description>656 yük, %93,1</description></item>
    /// <item><term>DENİZ → GELİRİ</term><description>335 yük, %94,9</description></item>
    /// <item><term>İTHALAT KARA</term><description>150 yük, %61,3</description></item>
    /// <item><term>İTHALAT DENİZ</term><description>35 yük, %68,6</description></item>
    /// <item><term>İTHALAT HAVA</term><description>22 yük, %54,5</description></item>
    /// </list>
    ///
    /// SON BEŞ ÇİFT hiç kullanılmamış ama adı simetrik ve iki tarafı da Siber'de
    /// AKTİF olan kalemler (ÇEKER, DORSE, EK, HAVA OLEX, NAVLUN FARKI) —
    /// kullanıcı isteğiyle eklendi ("navlunla alakalı diğer kalemler varsa
    /// onları da eşleştirelim").
    ///
    /// EŞLEŞTİRİLMEYENLER, bilinçli: BOŞ NAVLUN GİDERİ / NAVLUN GİDERİ /
    /// NAVLUN İADE'nin gelir karşılığı Siber'de YOK; YURTİÇİ ve YURTDIŞI
    /// NAVLUN GELİRİ satış-tek yönlü; "CZ/EU/NON EU PART / NAVLUN" satırlarının
    /// yönü hiç tanımlı değil. Eşleşmesi olmayan kalemde otomatik satır açılmaz.
    ///
    /// AD İLE EŞLEŞTİRİLİR, kimlikle değil: <c>financial_items.id</c> ETL'den
    /// gelen kimliklerdir ve ortamdan ortama değişebilir. Adlar <c>btrim</c> ile
    /// karşılaştırılıyor (kalemlerden birinin adında sondaki boşluk var) ve
    /// navlun adları yerelde benzersiz (ölçüldü: 0 mükerrer).
    /// </summary>
    public partial class AddFinancialItemPairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_freight",
                table: "financial_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "financial_item_pairs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_item_id = table.Column<long>(type: "bigint", nullable: false),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false),
                    markup_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("financial_item_pairs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "financial_item_pairs_purchase_item_id_foreign",
                        column: x => x.purchase_item_id,
                        principalTable: "financial_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "financial_item_pairs_sale_item_id_foreign",
                        column: x => x.sale_item_id,
                        principalTable: "financial_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_item_pairs_purchase_item_id",
                table: "financial_item_pairs",
                column: "purchase_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_item_pairs_sale_item_id",
                table: "financial_item_pairs",
                column: "sale_item_id");

            // NAVLUN BAYRAĞI: adı NAVLUN geçen 36 kalem. Geniş tutuldu — bu
            // bayrak yalnızca "Olumlu teklifte en az bir navlun kalemi" kuralını
            // besliyor ve dar tutmak geçerli teklifleri reddederdi.
            migrationBuilder.Sql("""
                UPDATE financial_items SET is_freight = TRUE
                WHERE btrim(name) ILIKE '%NAVLUN%';
                """);

            // ÇİFTLER. Ada göre çözülür; iki taraftan biri bulunamazsa o satır
            // sessizce atlanır (INNER JOIN) — eksik kalem migrasyonu düşürmez.
            migrationBuilder.Sql("""
                INSERT INTO financial_item_pairs
                    (purchase_item_id, sale_item_id, markup_percent, created_at, updated_at)
                SELECT a.id, s.id, 15, NOW(), NOW()
                FROM (VALUES
                    ('KARA NAVLUN GİDERİ',          'KARA NAVLUN HİZMET BEDELİ'),
                    ('HAVA NAVLUN GİDERİ',          'HAVA NAVLUN GELİRİ'),
                    ('DENİZ NAVLUN GİDERİ',         'DENİZ NAVLUN GELİRİ'),
                    ('İTHALAT KARA NAVLUN GİDERİ',  'İTHALAT KARA NAVLUN GELİRİ'),
                    ('İTHALAT DENİZ NAVLUN GİDERİ', 'İTHALAT DENİZ NAVLUN GELİRİ'),
                    ('İTHALAT HAVA NAVLUN GİDERİ',  'İTHALAT HAVA NAVLUN GELİRİ'),
                    ('ÇEKER NAVLUN GİDERİ',         'ÇEKER NAVLUN GELİRİ'),
                    ('DORSE NAVLUN GİDERİ',         'DORSE NAVLUN GELİRİ'),
                    ('EK NAVLUN GİDERİ',            'EK NAVLUN GELİRİ'),
                    ('HAVA NAVLUN GİDERİ OLEX',     'HAVA NAVLUN GELİRİ OLEX'),
                    ('NAVLUN FARKI GİDERİ',         'NAVLUN FARKI GELİRİ')
                ) AS ciftler(alis_ad, satis_ad)
                JOIN financial_items a ON btrim(a.name) = ciftler.alis_ad
                JOIN financial_items s ON btrim(s.name) = ciftler.satis_ad
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_item_pairs");

            migrationBuilder.DropColumn(
                name: "is_freight",
                table: "financial_items");
        }
    }
}
