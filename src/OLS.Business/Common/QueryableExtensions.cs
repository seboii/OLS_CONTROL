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

        var pattern = $"%{EscapeLike(search)}%";

        // x => EF.Functions.ILike(selector(x), pattern)
        var body = Expression.Call(
            typeof(NpgsqlDbFunctionsExtensions).GetMethod(
                nameof(NpgsqlDbFunctionsExtensions.ILike),
                [typeof(DbFunctions), typeof(string), typeof(string)])!,
            Expression.Constant(EF.Functions),
            selector.Body,
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
