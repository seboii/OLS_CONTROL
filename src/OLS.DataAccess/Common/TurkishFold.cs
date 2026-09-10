namespace OLS.DataAccess.Common;

/// <summary>
/// Türkçe harflerin ASCII karşılığına indirgenmesi — TEK KAYNAK.
///
/// Kural iki yerde birden uygulanmak zorunda: arama deseni .NET tarafında,
/// karşılaştırılan sütun ise SQL tarafında katlanıyor. İkisi ayrışırsa arama
/// SESSİZCE boş döner, bu yüzden ikisi de aşağıdaki tek tablodan besleniyor.
///
/// KAPSAM GENİŞLETİLDİ. Önce yalnızca İ/I/ı katlanıyordu; ş/ö/ü/ç/ğ aynen
/// kalıyordu. Sebep ölçüldü: Siber'in görevli ADI tutan sütunlarında Türkçe
/// harfler sıkça aksansız yazılmış ("RUSTEM FEYAZ", "HASAN CALISKAN",
/// "GUL TUREDİ"), yerel kullanıcı adı ise aksanlı — dar kural bunları
/// eşleştiremiyordu:
///
/// <list type="table">
/// <item><term>skn_yuk.musteritemsilcisiad</term><description>7.850 → 7.985 (+135)</description></item>
/// <item><term>skn_yuk.musteritemsilcisi2ad</term><description>7.712 → 7.792 (+80)</description></item>
/// <item><term>skn_rezervasyon.musteritemsilcisi</term><description>17.091 → 17.457 (+366)</description></item>
/// </list>
///
/// ÇAKIŞMA RİSKİ ÖLÇÜLDÜ VE YOK: 27 tanım tablosunun (ülke, şehir, ilçe, cari
/// tipi, kalem, vergi dairesi, kullanıcı, rol…) hiçbirinde geniş kural iki ayrı
/// satırı aynı anahtara düşürmüyor — dar kuraldaki çakışma sayısı neyse geniş
/// kuralda da o.
///
/// DİKKAT: <c>RoleCatalog.DepartmentToRoleSlug</c> anahtarları bu sınıfın
/// çıktısıdır. Kural her değiştiğinde o sözlük de değişmek zorunda.
/// </summary>
public static class TurkishFold
{
    /// <summary>
    /// Katlama çiftleri. BÜYÜK HARF DE LİSTEDE: katlama küçültmeden ÖNCE
    /// çalışıyor, çünkü 'İ'nin invariant küçüğü tek harf değil ('i' + birleşen
    /// nokta) ve o hâliyle hiçbir şeye eşleşmez.
    /// </summary>
    private static readonly (char From, char To)[] Pairs =
    [
        ('İ', 'i'), ('I', 'i'), ('ı', 'i'),
        ('Ş', 's'), ('ş', 's'),
        ('Ö', 'o'), ('ö', 'o'),
        ('Ü', 'u'), ('ü', 'u'),
        ('Ç', 'c'), ('ç', 'c'),
        ('Ğ', 'g'), ('ğ', 'g'),
    ];

    /// <summary>PostgreSQL <c>translate()</c>'in "kaynak" argümanı.</summary>
    public static readonly string From = new([.. Pairs.Select(p => p.From)]);

    /// <summary>PostgreSQL <c>translate()</c>'in "hedef" argümanı; konum konum eşleşir.</summary>
    public static readonly string To = new([.. Pairs.Select(p => p.To)]);

    /// <summary>
    /// .NET tarafı: deseni katlar.
    ///
    /// BAŞTAKİ/SONDAKİ BOŞLUK ATILIR. Kullanıcı adları eşleştirilirken bunun
    /// bedeli somut: yerel <c>users.siber_name</c> değerlerinin 130'unun
    /// 14'ünde sonda boşluk var ("HASAN ÇALIŞKAN "), Siber tarafı ise sorguda
    /// zaten <c>LTRIM(RTRIM(...))</c> ile geliyordu. Kırpılmayan anahtar
    /// yüzünden 76 yükün operasyon yetkilisi çözülemiyordu — kişi yerelde
    /// KAYITLI olduğu hâlde.
    /// </summary>
    public static string Normalize(string input)
    {
        var trimmed = input.AsSpan().Trim();

        Span<char> buffer = trimmed.Length <= 256 ? stackalloc char[trimmed.Length] : new char[trimmed.Length];

        for (var i = 0; i < trimmed.Length; i++)
        {
            var index = From.IndexOf(trimmed[i]);
            buffer[i] = index >= 0 ? To[index] : trimmed[i];
        }

        return new string(buffer).ToLowerInvariant();
    }

    /// <summary>
    /// SQL tarafı: <c>lower(translate(sütun, 'İIı…', 'iii…'))</c>. EF bunu
    /// <c>OlsDbContext</c>'teki <c>HasDbFunction</c> eşlemesiyle çevirir,
    /// .NET'te ÇAĞRILAMAZ.
    ///
    /// Önceki sürümde sütun tarafı her çağrı yerinde elle yazılmış bir
    /// <c>Replace</c> zinciriydi (10 dosyada 37 kez). Kural genişleyince hepsini
    /// tek tek güncellemek gerekiyordu ve biri unutulsa hata görünmezdi:
    /// desen katlanmış, sütun katlanmamış olur ve sorgu boş döner.
    /// </summary>
    public static string Fold(string? value) =>
        throw new NotSupportedException(
            "Yalnızca EF sorgusu içinde kullanılır; SQL'e translate() olarak çevrilir.");
}
