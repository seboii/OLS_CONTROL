using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Common;
using OLS.DataAccess.Context;

namespace OLS.Business.Services.Lookups;

/// <summary>
/// olsold'daki 27 referans/tanım modülü (iş tipi, ödeme tipi, para birimi, araç
/// tipi, vergi dairesi …) birebir aynı beş metodu tekrarlıyordu:
/// save / update / delete / all / single.
///
/// Bu tablolar ilişkisiz ve düz olduğu için — yani cari ve yükteki
/// "ilişki adı sütunu ezer" sorunu burada yok — tek bir generic servis
/// hepsini karşılıyor. Alanlar yansıma (reflection) ile eşleniyor, böylece
/// her tablonun kendine özgü ek sütunları (code, group_code, edikod, symbol,
/// special_code, order_no …) otomatik desteklenir.
/// </summary>
public interface ILookupService<TEntity> where TEntity : class
{
    Task<object> AllAsync(
        string? search, int? perPage, int page, string path, bool ascending,
        int? type = null, CancellationToken cancellationToken = default);

    Task<TEntity?> SingleAsync(long id, CancellationToken cancellationToken = default);

    Task<TEntity> CreateAsync(
        IDictionary<string, JsonElement> values, CancellationToken cancellationToken = default);

    Task<TEntity?> UpdateAsync(
        long id, IDictionary<string, JsonElement> values, CancellationToken cancellationToken = default);

    Task DeleteAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);
}

public sealed class LookupService<TEntity> : ILookupService<TEntity> where TEntity : class
{
    private readonly OlsDbContext _db;
    private readonly IClock _clock;

    public LookupService(OlsDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<object> AllAsync(
        string? search, int? perPage, int page, string path, bool ascending,
        int? type = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Set<TEntity>().AsNoTracking();

        // olsold: where('name', 'ILIKE', '%…%')
        if (!string.IsNullOrWhiteSpace(search) && LookupMap<TEntity>.HasName)
        {
            var pattern = $"%{search}%";
            query = query.Where(e =>
                EF.Functions.ILike(EF.Property<string>(e, LookupMap<TEntity>.NameProperty!), pattern));
        }

        // Yalnızca Type sütunu OLAN entity'lerde uygulanır (bugün yalnızca
        // FinancialItem) — olsold: SelectAjax fetchParams={type: buysell},
        // Alış/Satış'a göre farklı kalem listesi göstermek için.
        if (type is not null && LookupMap<TEntity>.HasType)
        {
            query = query.Where(e => EF.Property<int?>(e, LookupMap<TEntity>.TypeProperty!) == type);
        }

        // Sıralama kaynak kodda modülden modüle değişiyor (çoğu id desc, bazıları asc).
        query = ascending
            ? query.OrderBy(e => EF.Property<long>(e, LookupMap<TEntity>.IdProperty))
            : query.OrderByDescending(e => EF.Property<long>(e, LookupMap<TEntity>.IdProperty));

        return await query.ToPagedOrListAsync(perPage, page, path, cancellationToken);
    }

    public async Task<TEntity?> SingleAsync(long id, CancellationToken cancellationToken = default) =>
        await _db.Set<TEntity>().AsNoTracking()
            .FirstOrDefaultAsync(
                e => EF.Property<long>(e, LookupMap<TEntity>.IdProperty) == id, cancellationToken);

    public async Task<TEntity> CreateAsync(
        IDictionary<string, JsonElement> values, CancellationToken cancellationToken = default)
    {
        var entity = Activator.CreateInstance<TEntity>();

        LookupMap<TEntity>.Apply(entity, values);
        LookupMap<TEntity>.SetTimestamps(entity, _clock.Now, isNew: true);

        _db.Set<TEntity>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<TEntity?> UpdateAsync(
        long id, IDictionary<string, JsonElement> values, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Set<TEntity>()
            .FirstOrDefaultAsync(
                e => EF.Property<long>(e, LookupMap<TEntity>.IdProperty) == id, cancellationToken);

        if (entity is null)
            return null;

        LookupMap<TEntity>.Apply(entity, values);
        LookupMap<TEntity>.SetTimestamps(entity, _clock.Now, isNew: false);

        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>olsold: deletion_id dizisindeki her kaydı siler (hard delete).</summary>
    public async Task DeleteAsync(
        IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Set<TEntity>()
            .Where(e => ids.Contains(EF.Property<long>(e, LookupMap<TEntity>.IdProperty)))
            .ToListAsync(cancellationToken);

        _db.Set<TEntity>().RemoveRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Entity başına yansıma bilgisini bir kez hesaplayıp önbelleğe alır.
/// İstek gövdesindeki snake_case anahtarlar (ör. <c>group_code</c>) entity'nin
/// PascalCase özelliklerine (<c>GroupCode</c>) eşlenir.
/// </summary>
internal static class LookupMap<TEntity> where TEntity : class
{
    private static readonly Dictionary<string, PropertyInfo> Writable;

    public const string IdProperty = "Id";

    public static string? NameProperty { get; }
    public static bool HasName => NameProperty is not null;

    public static string? TypeProperty { get; }
    public static bool HasType => TypeProperty is not null;

    private static readonly PropertyInfo? CreatedAt;
    private static readonly PropertyInfo? UpdatedAt;

    static LookupMap()
    {
        var properties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        NameProperty = properties.Any(p => p.Name == "Name") ? "Name" : null;
        TypeProperty = properties.Any(p => p.Name == "Type") ? "Type" : null;
        CreatedAt = properties.FirstOrDefault(p => p.Name == "CreatedAt");
        UpdatedAt = properties.FirstOrDefault(p => p.Name == "UpdatedAt");

        // id ve zaman damgaları istemciden yazılamaz.
        Writable = properties
            .Where(p => p.Name is not ("Id" or "CreatedAt" or "UpdatedAt"))
            .Where(p => !p.PropertyType.IsGenericType ||
                        p.PropertyType.GetGenericTypeDefinition() != typeof(ICollection<>))
            .ToDictionary(ToSnakeCase, p => p, StringComparer.OrdinalIgnoreCase);
    }

    public static void Apply(TEntity entity, IDictionary<string, JsonElement> values)
    {
        foreach (var (key, element) in values)
        {
            if (!Writable.TryGetValue(key, out var property))
                continue;

            var converted = Convert(element, property.PropertyType);
            if (converted is not null || IsNullable(property.PropertyType))
                property.SetValue(entity, converted);
        }
    }

    public static void SetTimestamps(TEntity entity, DateTime now, bool isNew)
    {
        if (isNew) CreatedAt?.SetValue(entity, now);
        UpdatedAt?.SetValue(entity, now);
    }

    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    /// <summary>JSON değerini hedef .NET tipine çevirir; çeviremezse null döner.</summary>
    private static object? Convert(JsonElement element, Type targetType)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            // Frontend sayısal alanları bazen string olarak gönderiyor (FormData),
            // bu yüzden her iki gösterimi de kabul ediyoruz.
            var raw = element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : element.GetRawText();

            if (type == typeof(string)) return raw;
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (type == typeof(int)) return int.Parse(raw);
            if (type == typeof(long)) return long.Parse(raw);
            if (type == typeof(short)) return short.Parse(raw);
            if (type == typeof(decimal)) return decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return raw is "1" or "true" or "True";
            if (type == typeof(Guid)) return Guid.Parse(raw);
            if (type == typeof(DateTime)) return DateTime.Parse(raw);
            if (type == typeof(DateOnly)) return DateOnly.Parse(raw);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            return null;
        }

        return null;
    }

    private static string ToSnakeCase(PropertyInfo property)
    {
        var name = property.Name;
        var builder = new System.Text.StringBuilder(name.Length + 5);

        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                if (i > 0) builder.Append('_');
                builder.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                builder.Append(name[i]);
            }
        }

        return builder.ToString();
    }
}
