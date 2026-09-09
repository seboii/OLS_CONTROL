using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// YÜK DETAY EKRANINDAKİ GÖREVLİ SEÇİMİ SİBER'E YAZILIR.
///
/// BULUNAN GERÇEK HATA: "Görevliler" sekmesindeki iki seçici yalnızca yerel
/// tabloya (<c>load_transfers.customer_representative_name</c> /
/// <c>second_customer_representative_name</c>) işliyordu; Siber'e ise
/// <c>musteritemsilcisiad</c> olarak KAYDEDEN kullanıcının adı gidiyordu ve
/// ikinci sütun (<c>musteritemsilcisi2ad</c>) hiç yazılmıyordu. Yani kullanıcı
/// yetkiliyi değiştiriyor, ekranda değişiyor, Siber'de her kaydedende kaydedene
/// dönüyordu.
///
/// skn_yuk iki yetkiliyi de AD olarak tutuyor (teklifteki ad/kod ayrımı YOK):
/// canlıda 8.040 yükün 8.020/7.827'sinde dolu, 7.644/7.660'ı sky_kullanici.ad
/// ile eşleşiyor ve ikisi 1.497 yükte FARKLI kişi.
///
/// BOŞ SEÇİM SİLMEZ: seçim boşsa (ya da seçilen kişinin Siber hesabı yoksa)
/// parametre null gider ve UPDATE'teki <c>ISNULL(@x, kolon)</c> Siber'deki
/// değeri korur. Yereldeki 8.066 yükün 241'inde birinci, 374'ünde ikinci
/// temsilci çözülemiyor (Siber'de dolu) — düz atama onları silerdi.
/// </summary>
[Collection("OlsApi")]
public sealed class LoadTransferRepresentativeTests
{
    private readonly OlsApiFactory _factory;

    public LoadTransferRepresentativeTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Siber adı olan bir kullanıcı açar (yetkili olarak seçilebilsin).</summary>
    private static async Task<long> AddUserWithSiberNameAsync(
        OlsDbContext db, string siberName, string siberCode)
    {
        var user = new User
        {
            Name = siberName,
            Surname = "-",
            Email = $"{siberCode.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test",
            Password = "x",
            SiberName = siberName,
            SiberCode = siberCode,
            // Fiyatlandıran GUID ile yazılıyor; sky_kullanici.kullaniciid'nin
            // yerel karşılığı bu sütun (130 kullanıcının 125'inde dolu).
            SiberId = Guid.NewGuid().ToString(),
            Status = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }

    private static async Task<LoadTransfer> AddTransferAsync(OlsDbContext db)
    {
        var transfer = new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumber = Random.Shared.Next(100_000, 999_999).ToString(),
            LoadNumberWorkType = $"26{Random.Shared.Next(10_000, 99_999)}EX",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        return transfer;
    }

    private LoadTransferUpdateService CreateService(
        IServiceScope scope, OlsDbContext db, RecordingSiberLoadRepository siber) =>
        new(db, siber, new FakeSiberCountryResolver(db),
            scope.ServiceProvider.GetRequiredService<ICompanyScope>(),
            scope.ServiceProvider.GetRequiredService<IClock>());

    [Fact]
    public async Task SecilenIkiYetkili_SiberinIkiSutununaYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var birinci = await AddUserWithSiberNameAsync(db, "MAKSIM MOROZOV", "MAKSIMM");
        var ikinci = await AddUserWithSiberNameAsync(db, "HEDIYE ARIDICI", "HEDIYEA");
        var transfer = await AddTransferAsync(db);

        var siber = new RecordingSiberLoadRepository();

        var result = await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                CustomerRepresentativeUserId = (int)birinci,
                SecondCustomerRepresentativeUserId = (int)ikinci,
            },
            currentUserId: 1);

        result.IsSuccess.Should().BeTrue();

        siber.Written.Should().NotBeNull();
        siber.Written!.MusteriTemsilcisiAd.Should().Be("MAKSIM MOROZOV");
        siber.Written!.MusteriTemsilcisi2Ad.Should().Be("HEDIYE ARIDICI");
    }

    /// <summary>
    /// Kaydeden kullanıcının adı ARTIK YAZILMIYOR: seçim neyse o gider.
    /// Kurulum admininin Siber hesabı yok, eski davranışta yükün temsilcisi
    /// onun adıyla (yani boşla) eziliyordu.
    /// </summary>
    [Fact]
    public async Task KaydedenKullanicininAdi_YetkilininYerineYazilmaz()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var secilen = await AddUserWithSiberNameAsync(db, "UMUT AKBAS", "UMUTA");
        var kaydeden = await AddUserWithSiberNameAsync(db, "BASKA KISI", "BASKAK");
        var transfer = await AddTransferAsync(db);

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                CustomerRepresentativeUserId = (int)secilen,
            },
            currentUserId: kaydeden);

        siber.Written!.MusteriTemsilcisiAd.Should().Be("UMUT AKBAS");
    }

    /// <summary>
    /// FİYATLANDIRAN Siber'e GUID olarak yazılır
    /// (<c>skn_yuk.fiyatlandirankullaniciid</c> → <c>sky_kullanici.kullaniciid</c>),
    /// yerelde ise <c>users.siber_id</c>'den çözülür. Ad/kod DEĞİL.
    /// </summary>
    [Fact]
    public async Task Fiyatlandiran_SiberGuidiOlarakYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var yetkili = await AddUserWithSiberNameAsync(db, "MAKSIM MOROZOV", "MAKSIMM");
        var fiyatlandiran = await AddUserWithSiberNameAsync(db, "EDA E", "EDAE");
        var fiyatGuid = await db.Users.Where(u => u.Id == fiyatlandiran)
            .Select(u => u.SiberId).FirstAsync();
        var transfer = await AddTransferAsync(db);

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                CustomerRepresentativeUserId = (int)yetkili,
                PricingUserId = (int)fiyatlandiran,
            },
            currentUserId: 1);

        siber.Written!.FiyatlandiranKullaniciId.Should().Be(fiyatGuid);
    }

    /// <summary>
    /// Fiyatlandıran gönderilmezse 1. OPERASYON YETKİLİSİ yazılır — Siber'in
    /// kendi verisinde ikisi kayıtların %73'ünde aynı kişi.
    /// </summary>
    [Fact]
    public async Task Fiyatlandiran_GonderilmezseOperasyonYetkilisineDuser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var yetkili = await AddUserWithSiberNameAsync(db, "UMUT AKBAS", "UMUTA");
        var yetkiliGuid = await db.Users.Where(u => u.Id == yetkili)
            .Select(u => u.SiberId).FirstAsync();
        var transfer = await AddTransferAsync(db);

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                CustomerRepresentativeUserId = (int)yetkili,
            },
            currentUserId: 1);

        siber.Written!.FiyatlandiranKullaniciId.Should().Be(yetkiliGuid);
    }

    /// <summary>
    /// SATIŞ TEMSİLCİSİ Siber'e KOD olarak yazılır
    /// (<c>skn_yuk.satistemsilcisikod</c> → <c>sky_kullanici.kod</c>): dolu
    /// 7.645 kaydın 7.643'ü koda eşleşiyor, yalnızca 4'ü ada. Yükteki iki
    /// operasyon yetkilisi sütunu ise AD tutuyor — üçü farklı biçim.
    /// </summary>
    [Fact]
    public async Task SatisTemsilcisi_SiberKoduOlarakYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var yetkili = await AddUserWithSiberNameAsync(db, "MAKSIM MOROZOV", "MAKSIMM");
        var satisci = await AddUserWithSiberNameAsync(db, "SAHBAZ S", "SAHBAZS");
        var transfer = await AddTransferAsync(db);

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                CustomerRepresentativeUserId = (int)yetkili,
                SalesRepUserId = (int)satisci,
            },
            currentUserId: 1);

        // AD değil KOD, ve operasyon yetkilisinden bağımsız.
        siber.Written!.SatisTemsilcisiKod.Should().Be("SAHBAZS");
        siber.Written!.MusteriTemsilcisiAd.Should().Be("MAKSIM MOROZOV");
    }

    /// <summary>
    /// Satış temsilcisi gönderilmezse MEVCUT değer korunur — eskiden her
    /// kaydetmede kaydeden kullanıcıya sabitleniyordu.
    /// </summary>
    [Fact]
    public async Task SatisTemsilcisi_GonderilmezseMevcutKorunur()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var satisci = await AddUserWithSiberNameAsync(db, "SAHBAZ S", "SAHBAZS");
        var kaydeden = await AddUserWithSiberNameAsync(db, "BASKA KISI", "BASKAK");

        var transfer = await AddTransferAsync(db);
        transfer.SalesRepCode = (int)satisci;
        await db.SaveChangesAsync();

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest { Id = transfer.Id },
            currentUserId: kaydeden);

        siber.Written!.SatisTemsilcisiKod.Should().Be("SAHBAZS");

        var saved = await db.LoadTransfers.AsNoTracking().FirstAsync(t => t.Id == transfer.Id);
        saved.SalesRepCode.Should().Be((int)satisci);
    }

    /// <summary>
    /// KITA ARTIK SABİT DEĞİL — BULUNAN GERÇEK HATA.
    ///
    /// Yük her kaydedildiğinde <c>LoadingContinent</c> ve
    /// <c>UnloadingContinent</c> alanlarına "ASYA" sabiti yazılıyordu; gerçek
    /// kıta eziliyordu. Siber'in kendi verisinde boşaltma kıtası 4.474 yükte
    /// ASYA, 2.634'ünde AVRUPA, 79'unda AFRİKA, 50'sinde AMERİKA — yani
    /// Avrupa'ya giden her yük kaydedildiği anda "ASYA" oluyordu.
    ///
    /// Kıta artık seçilen ülkeden türetiliyor. (Sahte çözümleyici her ülke için
    /// "AVRUPA" döndürüyor; iddia "ASYA sabiti değil, ülkeden gelen değer".)
    /// </summary>
    [Fact]
    public async Task Kita_UlkedenTuretilir_SabitAsyaYazilmaz()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var country = await db.Countries.AsNoTracking()
            .OrderBy(c => c.Id).FirstAsync();

        var transfer = await AddTransferAsync(db);
        transfer.LoadingContinent = "ASYA";
        transfer.UnloadingContinent = "ASYA";
        await db.SaveChangesAsync();

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                DepartureCountryId = country.Id.ToString(),
                TargetCountryId = country.Id.ToString(),
            },
            currentUserId: 1);

        var saved = await db.LoadTransfers.AsNoTracking().FirstAsync(t => t.Id == transfer.Id);

        saved.LoadingContinent.Should().Be("AVRUPA");
        saved.UnloadingContinent.Should().Be("AVRUPA");
    }

    /// <summary>
    /// Boş seçim null gider — UPDATE'teki ISNULL Siber'deki değeri korur.
    /// Siber hesabı OLMAYAN bir kullanıcı seçilirse de aynısı geçerli.
    /// </summary>
    [Fact]
    public async Task SiberHesabiOlmayanSecim_NullGider()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var siberSiz = await AddUserWithSiberNameAsync(db, "SIBERSIZ", "SIBERSIZ");
        var user = await db.Users.FirstAsync(u => u.Id == siberSiz);
        user.SiberName = null;
        await db.SaveChangesAsync();

        var transfer = await AddTransferAsync(db);

        var siber = new RecordingSiberLoadRepository();

        await CreateService(scope, db, siber).UpdateAsync(
            new LoadTransferUpdateRequest
            {
                Id = transfer.Id,
                CustomerRepresentativeUserId = (int)siberSiz,
                SecondCustomerRepresentativeUserId = null,
            },
            currentUserId: 1);

        siber.Written!.MusteriTemsilcisiAd.Should().BeNull();
        siber.Written!.MusteriTemsilcisi2Ad.Should().BeNull();
    }
}

/// <summary>
/// <c>UpdateYukAsync</c>'e gelen kaydı saklar; diğer metotlar bu testlerde
/// çağrılmamalı (paket/kalem gönderilmiyor) ve çağrılırsa gürültülü düşer.
/// </summary>
internal sealed class RecordingSiberLoadRepository : ISiberLoadRepository
{
    public SiberYuk? Written { get; private set; }

    public bool IsConfigured => true;

    public Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default)
    {
        Written = yuk;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> FindMissingKalemIdsAsync(
        IReadOnlyCollection<string> kalemIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<SiberRezervasyon?> FindRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<SiberModulKayit?> FindModulKayitAsync(string loadNumberWorkType, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<SiberYukNumberResult> InsertYukWithLockedNumberAsync(SiberYuk yuk, string year, string additionalCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task LinkRezervasyonToYukAsync(string rezervasyonId, string yukId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteYukAsync(string yukId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteYukKoliAsync(string yukKoliId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukKoliDepoIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertYukKoliDepoAsync(SiberYukKoliDepo koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateYukKoliDepoAsync(SiberYukKoliDepo koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteYukKoliDepoAsync(string yukKoliDepoId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DeleteModulKalemAsync(string modulKalemId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task MoveYukCompanyAsync(string yukId, string sirketId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukEvrakIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertYukEvrakAsync(SiberYukEvrak evrak, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateYukEvrakAsync(SiberYukEvrak evrak, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteYukEvrakAsync(string yukEvrakId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
