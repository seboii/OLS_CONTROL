using System.Linq.Expressions;
using OLS.DataAccess.Entities;

namespace OLS.Business.Services.Authorization;

/// <summary>
/// Şirket görünürlüğü + "Siber'den silinmiş" süzgeci, sorgu üzerine tek satırda.
///
/// Bu iki filtre eskiden her liste servisinde ELLE yazılıyordu (teklif, yük,
/// sefer, yevmiye, finans belgeleri) ve panel ile rapor ekranlarında büsbütün
/// unutulmuştu. Filtre artık tek yerde durur.
///
/// HARFE DUYARSIZ KARŞILAŞTIRMA — canlıda bulunan hatanın kilidi.
///
/// <c>siber_company_id</c> metin bir sütun ve PostgreSQL'in <c>=</c> işleci
/// harfe DUYARLI. Yerelde iki farklı yazım birikmişti:
///
/// <list type="bullet">
///   <item>teklif/yük/sefer BÜYÜK harf (senkron <c>CAST(sirketid AS VARCHAR)</c>
///         kullanıyor, SQL Server büyük harf üretir),</item>
///   <item>bütün finans tabloları KÜÇÜK harf (<c>SiberFinanceRepository</c>
///         sorgularında <c>LOWER(...)</c> vardı) — 343.059 satırın tamamı,</item>
///   <item>iki sefer küçük harf (şirket seçicisinden gelen değer olduğu gibi
///         saklanıyordu).</item>
/// </list>
///
/// Karşılaştırma sabitleri (<see cref="CompanyScope.OlsCompanyId"/>,
/// <see cref="CompanyScope.AvroraCompanyId"/>) BÜYÜK harf olduğu için sonuç
/// iki yönde birden yanlıştı: Avrora kullanıcısı kendi finans kayıtlarının
/// HİÇBİRİNİ göremiyor, OLS kullanıcısı ise Avrora'nınkilerin TAMAMINI
/// görüyordu (<c>küçük != BÜYÜK</c> daima doğru).
///
/// Veri <c>NormalizeSiberCompanyIdCase</c> migrasyonuyla büyük harfe çekildi ve
/// yazan uçlar düzeltildi; buradaki <c>upper()</c> ise ikinci hat: ileride bir
/// yazım yolu yeniden küçük harf üretse bile görünürlük kuralı bozulmasın.
/// </summary>
public static class CompanyVisibilityExtensions
{
    /// <summary>
    /// Şirket sütununu seçen bir ifadeyle çalışan genel süzgeç. Tür başına
    /// aşırı yükleme yazmak yerine ifade ağacı kuruluyor çünkü
    /// <c>Load</c>/<c>LoadTransfer</c>/<c>Expedition</c>/finans varlıkları
    /// ortak bir arayüz taşımıyor.
    /// </summary>
    public static IQueryable<T> VisibleTo<T>(
        this IQueryable<T> query,
        CompanyVisibility visibility,
        Expression<Func<T, string?>> companySelector)
    {
        if (visibility.SeesEverything)
            return query;

        var parameter = companySelector.Parameters[0];
        var column = companySelector.Body;
        var upperColumn = Expression.Call(column, UpperMethod);

        Expression predicate;

        if (visibility.OnlyCompanyId is { } only)
        {
            // upper(NULL) NULL döner, yani şirketi boş kayıt eşleşmez — kapsamlı
            // kullanıcı yalnızca KENDİ şirketinin kaydını görür.
            predicate = Expression.Equal(upperColumn, Upper(only));
        }
        else
        {
            // Şirketi boş kayıt görünür kalır: eski senkron kayıtlarının bir
            // kısmında sütun hiç dolmamış ve bunları gizlemek listeyi eksiltirdi.
            predicate = Expression.OrElse(
                Expression.Equal(column, Expression.Constant(null, typeof(string))),
                Expression.NotEqual(upperColumn, Upper(visibility.ExcludeCompanyId)));
        }

        return query.Where(Expression.Lambda<Func<T, bool>>(predicate, parameter));
    }

    public static IQueryable<Load> VisibleTo(this IQueryable<Load> query, CompanyVisibility visibility) =>
        query.VisibleTo(visibility, l => l.SiberCompanyId);

    public static IQueryable<LoadTransfer> VisibleTo(
        this IQueryable<LoadTransfer> query, CompanyVisibility visibility) =>
        query.VisibleTo(visibility, t => t.SiberCompanyId);

    public static IQueryable<Expedition> VisibleTo(
        this IQueryable<Expedition> query, CompanyVisibility visibility) =>
        query.VisibleTo(visibility, e => e.SiberCompanyId);

    /// <summary>Siber ekranından silinmiş kayıtları eler (kayıt yerelde durur, listelenmez).</summary>
    public static IQueryable<Load> Live(this IQueryable<Load> query) =>
        query.Where(l => l.SiberDeletedAt == null);

    /// <inheritdoc cref="Live(IQueryable{Load})"/>
    public static IQueryable<LoadTransfer> Live(this IQueryable<LoadTransfer> query) =>
        query.Where(t => t.SiberDeletedAt == null);

    /// <inheritdoc cref="Live(IQueryable{Load})"/>
    public static IQueryable<Expedition> Live(this IQueryable<Expedition> query) =>
        query.Where(e => e.SiberDeletedAt == null);

    private static readonly System.Reflection.MethodInfo UpperMethod =
        typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;

    /// <summary>
    /// Sabit tarafı .NET'te büyütülür — SQL'e hazır değer gider, sütun tarafında
    /// tek bir <c>upper()</c> kalır.
    ///
    /// <c>ToUpperInvariant</c> şart: Türkçe kültürde <c>"i"</c> harfi
    /// <c>"İ"</c>'ye dönüşür ve GUID'in altıgen basamakları bozulurdu.
    /// </summary>
    private static ConstantExpression Upper(string? value) =>
        Expression.Constant(value?.ToUpperInvariant(), typeof(string));
}
