using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace OLS.Business.Common;

/// <summary>
/// olsold controller'larındaki <c>all()</c> davranışının birebir karşılığı:
/// <c>?search=</c> varsa ILIKE ile filtrele, <c>?per_page=</c> varsa sayfala,
/// yoksa tüm kayıtları düz dizi olarak döndür.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// PostgreSQL ILIKE araması. Laravel tarafındaki
    /// <c>where('name', 'ILIKE', '%'.$search.'%')</c> ifadesinin karşılığı.
    /// EF.Functions.ILike Npgsql sağlayıcısına özgüdür; MySQL'e taşınmaz.
    /// </summary>
    public static IQueryable<T> WhereILike<T>(
        this IQueryable<T> query,
        Expression<Func<T, string?>> selector,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var pattern = $"%{EscapeLike(NormalizeTurkish(search))}%";

        // x => EF.Functions.Like(selector(x).Replace("İ","i").Replace("I","i").Replace("ı","i").ToLower(), pattern)
        // Replace(string,string) kasıtlı - Replace(char,char) Npgsql/EF Core tarafından
        // SQL'e çevrilemiyor (query çalışma zamanında InvalidOperationException/500).
        var replaceStr = typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;
        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        Expression normalized = selector.Body;
        normalized = Expression.Call(normalized, replaceStr, Expression.Constant("İ"), Expression.Constant("i"));
        normalized = Expression.Call(normalized, replaceStr, Expression.Constant("I"), Expression.Constant("i"));
        normalized = Expression.Call(normalized, replaceStr, Expression.Constant("ı"), Expression.Constant("i"));
        normalized = Expression.Call(normalized, toLower);

        var body = Expression.Call(
            typeof(DbFunctionsExtensions).GetMethod(
                nameof(DbFunctionsExtensions.Like),
                [typeof(DbFunctions), typeof(string), typeof(string)])!,
            Expression.Constant(EF.Functions),
            normalized,
            Expression.Constant(pattern));

        var lambda = Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
        return query.Where(lambda);
    }

    /// <summary>
    /// LIKE joker karakterlerini kaçırır; kullanıcı girdisindeki % ve _ literal aranır.
    /// olsold bunu yapmıyordu — arama kutusuna "%" yazmak tüm kayıtları getiriyordu.
    /// internal: birden çok alanı OR ile birleştiren elle yazılmış aramalarda
    /// (tek alan için <see cref="WhereILike{T}"/> yeterli) da kullanılabilsin diye.
    /// </summary>
    internal static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>
    /// DÜRÜST NOT / gerçek bug: veritabanı en_US.utf8 yerelinde (bkz. Dockerfile) —
    /// bu yerelde ILIKE, Türkçe noktasız 'I' harfini büyük/küçük eşleştiremiyor:
    /// <c>'İSTANBUL' ILIKE '%istanbul%'</c> doğru eşleşiyor (İ/i standart Unicode
    /// katlaması), ama <c>'TAŞIMA' ILIKE '%taşıma%'</c> HİÇ eşleşmiyor - çünkü
    /// en_US'ta büyük 'I' küçük 'i'ye (noktalı) katlanıyor, Türkçe 'ı'ya (noktasız)
    /// DEĞİL. Sonuç: Türkçe yazan bir kullanıcı için normal yazım (ör. "taşıma",
    /// "vergisi", "çıkış") aramaların çoğu SESSİZCE boş dönüyor. Veritabanının
    /// yerelini canlı, dolu bir veritabanında değiştirmek riskli (yeniden
    /// oluşturma gerektirir) - bunun yerine İ/I/ı'nın tamamı karşılaştırmadan
    /// önce tek bir kanonik harfe indirgeniyor (aksan farkı gözetilmeden "aynı
    /// harf" kabul ediliyor - hem doğru hem rahat: kullanıcı İ/I/ı'yı karıştırsa
    /// bile arama çalışır).
    /// </summary>
    internal static string NormalizeTurkish(string input) =>
        input.Replace('İ', 'i').Replace('I', 'i').Replace('ı', 'i').ToLowerInvariant();

    /// <summary>
    /// per_page verilmişse Laravel paginator zarfı, verilmemişse düz liste döndürür.
    /// İki durumu da tek yerde tuttuk çünkü frontend ikisini farklı tüketiyor:
    /// sayfalıda <c>data.data</c>, sayfasızda doğrudan <c>data</c>.
    /// </summary>
    public static async Task<object> ToPagedOrListAsync<T>(
        this IQueryable<T> query,
        int? perPage,
        int page,
        string path,
        CancellationToken cancellationToken = default)
    {
        if (perPage is null or < 1)
            return await query.ToListAsync(cancellationToken);

        if (page < 1) page = 1;

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * perPage.Value)
            .Take(perPage.Value)
            .ToListAsync(cancellationToken);

        return LengthAwarePaginator<T>.Create(items, total, perPage.Value, page, path);
    }
}
