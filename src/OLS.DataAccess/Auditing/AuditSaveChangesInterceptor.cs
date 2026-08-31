using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OLS.DataAccess.Entities;

namespace OLS.DataAccess.Auditing;

/// <summary>
/// Kullanıcı eylemlerini <see cref="AuditLog"/>'a yazar.
///
/// EF Core interceptor'ı seçilmesinin sebebi: yazma yolunu tek noktadan
/// yakalıyor. Alternatif her serviste elle log çağırmaktı — 20+ servis, her yeni
/// uçta unutulma riski. Interceptor'da unutmak mümkün değil.
///
/// Kapsam bilinçli olarak DAR: yalnızca <see cref="AuditedEntities"/> listesindeki
/// iş kayıtları izlenir. Senkronun dokunduğu referans/tanım tabloları listede yok;
/// zaten kullanıcı bağlamı olmadığı için de yazılmazlardı, ama iki katman koruma
/// tablonun şişmesini kesin olarak engelliyor.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>İzlenen kayıt türleri → arayüzde görünecek Türkçe ad.</summary>
    private static readonly Dictionary<string, string> AuditedEntities = new()
    {
        [nameof(Load)] = "Teklif",
        [nameof(LoadTransfer)] = "Yük",
        [nameof(LoadTransferPackage)] = "Yük Paketi",
        [nameof(LoadTransferInvoiceItem)] = "Yük Mali Kalemi",
        [nameof(LoadTransferDocument)] = "Yük Evrakı",
        [nameof(Expedition)] = "Sefer",
        [nameof(ExpeditionLoadMapping)] = "Sefer-Yük Bağı",
        [nameof(Account)] = "Cari",
        [nameof(AccountRepresentative)] = "Cari Görevlisi",
        [nameof(Invoice)] = "Fatura",
        [nameof(Car)] = "Araç",
        [nameof(User)] = "Kullanıcı",
        [nameof(UserPermission)] = "Yetki",
        [nameof(Role)] = "Rol",
        [nameof(RolePermission)] = "Rol Yetkisi",
    };

    /// <summary>
    /// Denetim kaydına ASLA girmeyecek alanlar. Parola hash'i ve jeton gibi
    /// değerlerin denetim tablosuna kopyalanması, onları gizlemek için harcanan
    /// çabayı (EloquentJsonModifiers.Hidden) boşa çıkarırdı.
    /// </summary>
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(User.Password), nameof(User.RememberToken), nameof(User.EmailPassword),
    };

    private readonly IAuditContext _audit;

    public AuditSaveChangesInterceptor(IAuditContext audit) => _audit = audit;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
            Capture(context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
            Capture(context);

        return base.SavingChanges(eventData, result);
    }

    private void Capture(DbContext context)
    {
        // Kullanıcı bağlamı yoksa bu bir arka plan işidir (Siber senkronu) — yazma.
        if (_audit.UserId is not { } userId)
            return;

        var logs = new List<AuditLog>();
        var now = DateTime.Now;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog)
                continue;

            var typeName = entry.Metadata.ClrType.Name;
            if (!AuditedEntities.TryGetValue(typeName, out var label))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Modified => "updated",
                EntityState.Deleted => "deleted",
                _ => null,
            };

            if (action is null)
                continue;

            var changes = DescribeChanges(entry, action);

            // Değişen alan yoksa (ör. yalnızca updated_at dokunulmuş) kayıt açma.
            if (action == "updated" && changes.Count == 0)
                continue;

            logs.Add(new AuditLog
            {
                UserId = userId,
                UserName = _audit.UserName,
                Action = action,
                EntityType = label,
                EntityId = PrimaryKey(entry),
                EntityLabel = HumanLabel(entry.Entity),
                Changes = changes.Count == 0 ? null : JsonSerializer.Serialize(changes),
                IpAddress = _audit.IpAddress,
                CreatedAt = now,
            });
        }

        if (logs.Count > 0)
            context.Set<AuditLog>().AddRange(logs);
    }

    private static Dictionary<string, object?> DescribeChanges(EntityEntry entry, string action)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (SensitiveProperties.Contains(name))
                continue;

            // Zaman damgaları gürültü: her güncellemede değişir, hiçbir şey anlatmaz.
            if (name is "CreatedAt" or "UpdatedAt")
                continue;

            if (action == "updated")
            {
                if (!property.IsModified)
                    continue;

                var before = property.OriginalValue?.ToString();
                var after = property.CurrentValue?.ToString();
                if (before == after)
                    continue;

                changes[name] = new { onceki = before, sonraki = after };
            }
            else if (action == "created")
            {
                if (property.CurrentValue is { } value && value.ToString() is { Length: > 0 } text)
                    changes[name] = text;
            }
        }

        return changes;
    }

    private static string? PrimaryKey(EntityEntry entry) =>
        entry.Metadata.FindPrimaryKey()?.Properties is { Count: > 0 } keys
            ? string.Join("-", keys.Select(k => entry.Property(k.Name).CurrentValue))
            : null;

    /// <summary>
    /// Aranabilir etiket. Kullanıcı denetim kaydında "2600838TR" ya da bir cari
    /// adı arıyor; yerel id'yi bilmiyor. Bu yüzden her tür için insanın tanıdığı
    /// alan seçilir.
    /// </summary>
    private static string? HumanLabel(object entity) => entity switch
    {
        Load l => l.LoadNumber ?? l.ReservationNumber,
        LoadTransfer t => t.LoadNumberWorkType ?? t.LoadNumber,
        LoadTransferPackage p => p.LoadTransferId,
        LoadTransferInvoiceItem i => i.InsertName,
        Expedition e => e.ExpeditionNumber,
        Account a => a.Name,
        Car c => c.PlateNumber,
        User u => u.Email,
        Role r => r.Name,
        Invoice inv => inv.InvoiceId,
        _ => null,
    };
}
