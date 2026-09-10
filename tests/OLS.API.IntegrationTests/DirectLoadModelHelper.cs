using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Teklifsiz yük testleri için en küçük geçerli model.
///
/// ENTEGRASYON TESTLERİ VERİTABANINI PAYLAŞIYOR: tanım tablolarında "Siber
/// kimlikli en az bir satır" garanti değil. Eksik olan burada açılır, var olan
/// kullanılır; böylece tur sırası testi etkilemez.
/// </summary>
internal static class DirectLoadModelHelper
{
    /// <summary>
    /// Formun zorunlu kıldığı en küçük geçerli yük. Tanım satırları test
    /// veritabanında tohumlu olanlardan seçilir — konu görevliler ve döviz.
    /// </summary>
    public static async Task<DirectLoadModel> BuildAsync(
        OlsDbContext db,
        IReadOnlyList<long>? officers = null,
        long? salesRepId = null,
        long? currencyId = null,
        IReadOnlyList<DirectLoadFinancialItem>? financialItems = null)
    {
        var account = new Account
        {
            Name = $"TEKLIFSIZ {Guid.NewGuid():N}"[..24],
            SiberId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        // ENTEGRASYON TESTLERİ VERİTABANINI PAYLAŞIYOR: tanım tablolarında
        // "Siber kimlikli en az bir satır" garanti değil. Eksik olan burada
        // açılır; var olan kullanılır, böylece tur sırası testi etkilemez.
        var country = await db.Countries.AsNoTracking()
            .Where(c => c.SiberId != null).OrderBy(c => c.Id).Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (country == Guid.Empty)
        {
            var row = new Country
            {
                Id = Guid.NewGuid(),
                Name = $"TEST ÜLKE {Guid.NewGuid():N}"[..24],
                SiberId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };
            db.Countries.Add(row);
            await db.SaveChangesAsync();
            country = row.Id;
        }

        return new DirectLoadModel
        {
            WorkTypeId = await EnsureAsync(db, db.WorkTypes, () => new WorkType
            {
                Name = "TEST İŞ TÜRÜ", Code = "0", GroupCode = "ISTURU", AdditionalCode = "EX",
                SiberId = Guid.NewGuid().ToString(),
            }),
            LoadingTypeId = await EnsureAsync(db, db.LoadingTypes, () => new LoadingType
            {
                Name = "TEST YÜKLEME TİPİ", Code = "1", GroupCode = "YUKLEMETIP",
                SiberId = Guid.NewGuid().ToString(),
            }),
            LoadTransferTypeId = await EnsureAsync(db, db.LoadTransferTypes, () => new LoadTransferType
            {
                Name = "TEST YÜK TÜRÜ", Code = "1", GroupCode = "YUKTUR",
                SiberId = Guid.NewGuid().ToString(),
            }),
            InstructionId = await EnsureAsync(db, db.Instructions, () => new Instruction
            {
                Name = "TEST TALİMAT", Code = "1", GroupCode = "TALIMATGELISSEKLI",
                SiberId = Guid.NewGuid().ToString(),
            }),
            RomorkTypeId = await EnsureAsync(db, db.RomorkTypes, () => new RomorkType
            {
                Name = "TEST RÖMORK", Code = "1", GroupCode = "ROMORKCINS",
                SiberId = Guid.NewGuid().ToString(),
            }),
            PaymentTypeId = await EnsureAsync(db, db.PaymentTypes, () => new PaymentType
            {
                Name = "TEST ÖDEME", Code = "1", SiberId = Guid.NewGuid().ToString(),
            }),
            DepartmentId = await EnsureAsync(db, db.Departments, () => new Department
            {
                Name = "TEST DEPARTMAN", SiberId = Guid.NewGuid().ToString(),
            }),
            CustomerId = account.Id,
            SenderId = account.Id,
            ReceiverId = account.Id,
            DepartureCountryId = country,
            TargetCountryId = country,
            CurrencyId = currencyId,
            OperationOfficerIds = officers ?? [],
            SalesRepId = salesRepId,
            InstructionArrivalDate = DateOnly.FromDateTime(DateTime.Today),
            Packages = [new DirectLoadPackage(null, null, 1, 100, 90, 2, 1, null, null, null, 1)],
            FinancialItems = financialItems ?? [],
        };
    }

    /// <summary>
    /// Siber kimliği olan ilk satırın kimliğini verir; yoksa <paramref name="create"/>
    /// ile bir satır açar. Tanım tabloları paylaşımlı test veritabanında dolu
    /// gelebilir de gelmeyebilir de.
    /// </summary>
    private static async Task<long> EnsureAsync<T>(
        OlsDbContext db, DbSet<T> set, Func<T> create) where T : class
    {
        var id = await set.AsNoTracking()
            .Where(e => EF.Property<string?>(e, "SiberId") != null)
            .Select(e => EF.Property<long>(e, "Id")).OrderBy(i => i).FirstOrDefaultAsync();

        if (id > 0)
            return id;

        var row = create();
        db.Entry(row).Property("CreatedAt").CurrentValue = DateTime.Now;
        db.Entry(row).Property("UpdatedAt").CurrentValue = DateTime.Now;
        set.Add(row);
        await db.SaveChangesAsync();

        return (long)db.Entry(row).Property("Id").CurrentValue!;
    }
}
