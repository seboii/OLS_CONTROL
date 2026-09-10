using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.API.Controllers.Front;
using OLS.Business.Common;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// TEKLİFSİZ YÜK — GÖREVLİLER VE DÖVİZ SİBER'E GİDİYOR MU?
///
/// İki ayrı kusur birden vardı ve ikisi de SESSİZDİ (hata yok, ekran normal):
///
/// 1. <b>Biçim yanlıştı ve iki alan hiç yazılmıyordu.</b> Yük Siber'e yalnızca
///    <c>musteritemsilcisiad = kullanıcı KODU</c> ile açılıyordu. Oysa canlı
///    ölçüm bu sütunların AD taşıdığını gösteriyor: dolu 8.035 değerin 7.657'si
///    <c>sky_kullanici.ad</c> ile eşleşiyor, kodla eşleşen YOK. 2. yetkili
///    (<c>musteritemsilcisi2ad</c>, 7.842 kayıtta dolu), satış temsilcisi
///    (<c>satistemsilcisikod</c>, 7.655 kayıtta KODLA eşleşiyor) ve
///    fiyatlandıran hiç yazılmıyordu.
///
/// 2. <b>Döviz türü isteğe hiç bağlanmamıştı.</b> Form <c>currency_id</c>
///    gönderiyor, servis <c>skn_yuk.dovizkod</c>'a yazacak durumda, ama
///    <c>DirectLoadRequest</c>'te böyle bir alan yoktu — değer sunucuya varır
///    varmaz düşüyordu. Kullanıcının gördüğü belirti: "döviz türünü giriyoruz,
///    Siber'de dolmuyor."
/// </summary>
[Collection("OlsApi")]
public sealed class DirectLoadRepresentativeTests
{
    private readonly OlsApiFactory _factory;

    public DirectLoadRepresentativeTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<User> AddSiberUserAsync(OlsDbContext db, string ad, string kod)
    {
        var user = new User
        {
            Name = ad,
            Surname = "-",
            Email = $"{kod.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test",
            Password = "x",
            SiberName = ad,
            SiberCode = kod,
            SiberId = Guid.NewGuid().ToString(),
            Status = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Formun topladığı her alanın Siber'e ulaşabilmesi için önce MODELE
    /// ulaşması gerekiyor. Döviz türü tam burada kayboluyordu.
    /// </summary>
    [Fact]
    public void Istek_DovizVeGorevlileriModeleTasir()
    {
        var request = new DirectLoadRequest
        {
            CurrencyId = 42,
            OperationOfficerIds = [7, 9],
            SalesRepId = 11,
        };

        var model = request.ToModel();

        model.CurrencyId.Should().Be(42);
        model.OperationOfficerIds.Should().Equal(7, 9);
        model.SalesRepId.Should().Be(11);
    }

    [Fact]
    public async Task IkiYetkili_SiberdeAdlariylaSatisTemsilcisiKoduylaYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var birinci = await AddSiberUserAsync(db, "MAKSIM MOROZOV", $"MAKS{Random.Shared.Next(1000, 9999)}");
        var ikinci = await AddSiberUserAsync(db, "HEDIYE ARIDICI", $"HEDI{Random.Shared.Next(1000, 9999)}");
        var satisci = await AddSiberUserAsync(db, "UMUT AKBAS", $"UMUT{Random.Shared.Next(1000, 9999)}");

        var siber = new RecordingDirectLoadRepository();
        var model = await DirectLoadModelHelper.BuildAsync(db, officers: [birinci.Id, ikinci.Id], salesRepId: satisci.Id);

        var result = await CreateService(scope, db, siber)
            .CreateAsync(model, currentUserId: 1);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written.Should().NotBeNull();

        // 1. ve 2. yetkili AD, satış temsilcisi KOD — biçimler farklı.
        siber.Written!.MusteriTemsilcisiAd.Should().Be(birinci.SiberName);
        siber.Written!.MusteriTemsilcisi2Ad.Should().Be(ikinci.SiberName);
        siber.Written!.SatisTemsilcisiKod.Should().Be(satisci.SiberCode);
        // Fiyatlandıran GUID'dir ve 1. yetkiliye düşer (teklif yolundaki kural).
        siber.Written!.FiyatlandiranKullaniciId.Should().Be(birinci.SiberId);

        // Yerel ayna aynı kişileri göstermeli, kaydı açanı değil.
        var transfer = await db.LoadTransfers.AsNoTracking()
            .FirstAsync(t => t.LoadTransferId == siber.Written!.YukId);

        transfer.CustomerRepresentativeName.Should().Be((int)birinci.Id);
        transfer.SecondCustomerRepresentativeName.Should().Be((int)ikinci.Id);
        transfer.SalesRepCode.Should().Be((int)satisci.Id);
    }

    /// <summary>
    /// Tek yetkili seçilirse ikinci sütun BOŞ gider — Siber'de o alanı dolduran
    /// başka bir akış varsa UPDATE'teki ISNULL sayesinde silinmez.
    /// </summary>
    [Fact]
    public async Task TekYetkiliVarsa_IkinciSutunBosGider()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var birinci = await AddSiberUserAsync(db, "ORCUN OZDEMIR", $"ORCU{Random.Shared.Next(1000, 9999)}");

        var siber = new RecordingDirectLoadRepository();
        var model = await DirectLoadModelHelper.BuildAsync(db, officers: [birinci.Id], salesRepId: null);

        var result = await CreateService(scope, db, siber)
            .CreateAsync(model, currentUserId: 1);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written!.MusteriTemsilcisi2Ad.Should().BeNull();
    }

    /// <summary>
    /// Döviz türü açılışta yazılır — eskiden yalnızca sonradan yapılan bir
    /// güncelleme ile Siber'e gidebiliyordu (o güncelleme yapılmazsa hiç).
    /// </summary>
    [Fact]
    public async Task SecilenDoviz_AcilistaSiberYazilir()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var currency = await db.Currencies.AsNoTracking()
            .Where(c => c.Code != null && c.Code != "").OrderBy(c => c.Id).FirstOrDefaultAsync();

        if (currency is null)
        {
            currency = new Currency
            {
                Name = "TEST DOLAR", Code = "USD", Symbol = "$",
                SiberId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            };
            db.Currencies.Add(currency);
            await db.SaveChangesAsync();
        }

        var siber = new RecordingDirectLoadRepository();
        var model = await DirectLoadModelHelper.BuildAsync(db, officers: [], salesRepId: null, currencyId: currency.Id);

        var result = await CreateService(scope, db, siber)
            .CreateAsync(model, currentUserId: 1);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        siber.Written!.DovizKod.Should().Be(currency.Code);
    }

    private DirectLoadService CreateService(
        IServiceScope scope, OlsDbContext db, RecordingDirectLoadRepository siber) =>
        new(db, siber,
            scope.ServiceProvider.GetRequiredService<ICompanyScope>(),
            scope.ServiceProvider.GetRequiredService<IPermissionService>(),
            scope.ServiceProvider.GetRequiredService<IClock>(),
            new PassThroughReferenceValidator(),
            new FakeSiberCountryResolver(db));

}


/// <summary>
/// Teklifsiz yükün Siber'e yazdığı kaydı yakalar; hiçbir G/Ç yapmaz.
/// Yalnızca bu akışın kullandığı metotlar çalışır, kalanı gürültülü düşer.
/// </summary>
internal sealed class RecordingDirectLoadRepository : ISiberLoadRepository
{
    public SiberYuk? Written { get; private set; }

    /// <summary>Siber'e giden mali kalemler — hangi tarafa (GC) yazıldığı sınanıyor.</summary>
    public List<SiberModulKalem> Kalemler { get; } = [];

    public bool IsConfigured => true;

    public Task<SiberYukNumberResult> InsertYukWithLockedNumberAsync(
        SiberYuk yuk, string year, string additionalCode, CancellationToken cancellationToken = default)
    {
        Written = yuk;
        return Task.FromResult(new SiberYukNumberResult
        {
            YukNo = Random.Shared.Next(100_000, 999_999),
            LoadNumberWorkType = $"26{Random.Shared.Next(10_000, 99_999)}EX",
        });
    }

    public Task<Guid> GenerateYukIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());
    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());
    public Task<Guid> GenerateModulKalemIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid());
    public Task InsertYukKoliAsync(SiberYukKoli koli, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    public Task InsertModulKalemAsync(SiberModulKalem kalem, CancellationToken cancellationToken = default)
    {
        Kalemler.Add(kalem);
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<string>> FindMissingKalemIdsAsync(
        IReadOnlyCollection<string> kalemIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
    /// <summary>
    /// Modül kaydı VAR sayılır: yoksa kalem satırı Siber'e hiç yazılmıyor ve
    /// taraf (GC) sınanamazdı.
    /// </summary>
    public Task<SiberModulKayit?> FindModulKayitAsync(
        string loadNumberWorkType, CancellationToken cancellationToken = default) =>
        Task.FromResult<SiberModulKayit?>(new SiberModulKayit
        {
            ModulId = Guid.NewGuid().ToString(),
            ModulKod = "0401",
        });

    public Task<SiberRezervasyon?> FindRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task LinkRezervasyonToYukAsync(string rezervasyonId, string yukId, CancellationToken cancellationToken = default) =>
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
    public Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default) =>
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
