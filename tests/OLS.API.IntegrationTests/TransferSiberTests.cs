using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.LoadTransfers;
using OLS.Business.Services.TransferSiber;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Teklif→Yük dönüşüm zincirinin iki adımı (transfer_to_siber, load_transfer) için
/// BR-002/003/004/005 iş kurallarını ve Siber-yapılandırılmamış davranışlarını kilitler.
///
/// Bu test ortamında <c>ConnectionStrings:Siber</c> bilinçli olarak tanımsız (bkz.
/// OlsApiFactory) — bu yüzden HTTP üzerinden çağrılan `transfer_to_siber`/`load_transfer`
/// uçları her zaman "yapılandırılmamış" hatasıyla ERKEN dönerler ve BR kuralları hiç
/// çalışmaz. Bu, tam olarak canlı Docker'da bulunan (ama Siber kimlik eşlemesi eksik
/// olduğu için tamamlanamayan) durumun aynısı — bkz. TESLIM-RAPORU.md §8. BR kurallarının
/// kendisini test etmek için ilgili servisler DI'dan değil, sahte (fake) bir
/// ISiberLoadRepository/ISiberReservationRepository (IsConfigured=true, hiçbir Siber
/// G/Ç'sine ULAŞILMAMASI beklenir) ile DOĞRUDAN örnekleniyor.
/// </summary>
[Collection("OlsApi")]
public sealed class TransferSiberTests
{
    private readonly OlsApiFactory _factory;

    public TransferSiberTests(OlsApiFactory factory) => _factory = factory;

    // ── HTTP üzerinden: gerçek "Siber yapılandırılmamış" davranışı ──────────────

    [Fact]
    public async Task TransferToSiber_WhenSiberNotConfigured_ReturnsBadRequestWithMessage()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/transfer_to_siber", new { id = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("message")[0].GetString()
            .Should().Be("Siber bağlantısı yapılandırılmamış.");
    }

    /// <summary>Bu, TESLIM-RAPORU.md §8'de bahsedilen, o ana dek testsiz kalan "Siber-503" davranışı.</summary>
    [Fact]
    public async Task LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/v1/transfer_to_siber/loadSave", new { id = "some-siber-reservation-id" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task LoadSave_WithoutId_ReturnsValidationError_BeforeCheckingSiberConfiguration()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/transfer_to_siber/loadSave", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Doğrudan servis örneklemesiyle: BR kurallarının kendisi ─────────────────

    [Fact]
    public async Task TransferOfferAsync_WhenLoadAlreadyHasLoadNumber_ReturnsError()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var load = new Load
        {
            TransferToSiber = 1,
            SiberId = Guid.NewGuid().ToString(),
            LoadNumber = "26I0001", // zaten yüke dönüşmüş
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new TransferSiberService(db, new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.TransferOfferAsync(load.Id, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Yük oluşturulmuş teklifde güncelleme yapamazsınız.");
    }

    /// <summary>
    /// ValidateRequired'ın olsold'daki tam 21 kontrollük listesinde (bkz.
    /// TransferSiberService.cs) "Ödeme şekli boş olamaz" 9. sırada — bu yüzden
    /// öncesindeki 8 alan (talimat/römork/iş türü/yükleme tipi/yüktür/üç tarih)
    /// burada bilinçli olarak DOLU verilir; yalnızca ödeme tipi eksik bırakılır.
    /// </summary>
    [Fact]
    public async Task TransferOfferAsync_WithoutPaymentType_ReturnsPaymentTypeRequiredError()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var instruction = new Instruction { Name = "E-posta", Code = "1" };
        var romorkType = new RomorkType { Name = "Tenteli", Code = "1" };
        var workType = new WorkType { Name = "İhracat", Code = "IHR", GroupCode = "ISTURU", AdditionalCode = "IHR" };
        var loadingType = new LoadingType { Name = "Komple", Code = "1" };
        var loadTransferType = new LoadTransferType { Name = "Parsiyel", Code = "1" };
        db.AddRange(instruction, romorkType, workType, loadingType, loadTransferType);
        await db.SaveChangesAsync();

        var load = new Load
        {
            TransferToSiber = 0,
            PaymentTypeId = null,
            InstructionId = (int)instruction.Id,
            RomorkTypeId = (int)romorkType.Id,
            WorkTypeId = (int)workType.Id,
            LoadingTypeId = (int)loadingType.Id,
            LoadTransferTypeId = (int)loadTransferType.Id,
            MarketingNotificationDate = DateOnly.FromDateTime(DateTime.Now),
            OfferDate = DateOnly.FromDateTime(DateTime.Now),
            OfferValidityDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new TransferSiberService(db, new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.TransferOfferAsync(load.Id, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Ödeme şekli boş olamaz");
    }

    /// <summary>
    /// Gönderici (sender) kontrolü daha önce ValidateRequired'da hiç yoktu —
    /// eksik göndericili bir teklif sessizce Siber'e aktarılabiliyordu. Ödeme
    /// tipinden SONRA gelen bu kontrole ulaşmak için öncesindeki tüm alanlar
    /// (müşteri dahil) doldurulur.
    /// </summary>
    [Fact]
    public async Task TransferOfferAsync_WithoutSender_ReturnsSenderRequiredError()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var instruction = new Instruction { Name = "E-posta", Code = "1" };
        var romorkType = new RomorkType { Name = "Tenteli", Code = "1" };
        var workType = new WorkType { Name = "İhracat", Code = "IHR", GroupCode = "ISTURU", AdditionalCode = "IHR" };
        var loadingType = new LoadingType { Name = "Komple", Code = "1" };
        var loadTransferType = new LoadTransferType { Name = "Parsiyel", Code = "1" };
        var paymentType = new PaymentType { Name = "Peşin", SiberId = Guid.NewGuid().ToString() };
        var customer = new Account { Name = "Test Müşteri", SiberId = Guid.NewGuid().ToString() };
        db.AddRange(instruction, romorkType, workType, loadingType, loadTransferType, paymentType, customer);
        await db.SaveChangesAsync();

        var load = new Load
        {
            TransferToSiber = 0,
            InstructionId = (int)instruction.Id,
            RomorkTypeId = (int)romorkType.Id,
            WorkTypeId = (int)workType.Id,
            LoadingTypeId = (int)loadingType.Id,
            LoadTransferTypeId = (int)loadTransferType.Id,
            PaymentTypeId = (int)paymentType.Id,
            CustomerId = (int)customer.Id,
            SenderId = null,
            MarketingNotificationDate = DateOnly.FromDateTime(DateTime.Now),
            OfferDate = DateOnly.FromDateTime(DateTime.Now),
            OfferValidityDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new TransferSiberService(db, new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.TransferOfferAsync(load.Id, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Gönderici boş olamaz");
    }

    [Fact]
    public async Task ConvertOfferAsync_WhenLoadAlreadyConverted_ReturnsBR002Error()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var siberId = Guid.NewGuid().ToString();
        var load = new Load
        {
            SiberId = siberId,
            TransferToSiber = 1,
            LoadNumber = "26I0002", // BR-002: zaten oluşmuş
            StatusTypeId = 5,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Bu yük zaten oluşturuldu");
    }

    [Fact]
    public async Task ConvertOfferAsync_WhenStatusNotApproved_ReturnsBR003Error()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var siberId = Guid.NewGuid().ToString();
        var load = new Load
        {
            SiberId = siberId,
            TransferToSiber = 1,
            LoadNumber = null,
            StatusTypeId = 4, // Teklif (OFFER) - Olumlu (5) değil
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Yük durumu Olumlu değil");
    }

    [Fact]
    public async Task ConvertOfferAsync_WhenNotTransferredToSiber_ReturnsBR004Error()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var siberId = Guid.NewGuid().ToString();
        var load = new Load
        {
            SiberId = siberId,
            TransferToSiber = 0, // BR-004: henüz Siber'e aktarılmamış
            LoadNumber = null,
            StatusTypeId = 5,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.Loads.Add(load);
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Önce Teklif Oluşturun");
    }

    /// <summary>
    /// Bu oturumda BULUNAN gerçek bir bulgu: gerçek Siber'de <c>sfy_modulkalem.kalemid</c>
    /// NOT NULL — bir mali kalemin Kalem'i (item) boşsa <c>ConvertOfferAsync</c> bunu daha
    /// önce hiç kontrol etmiyordu; Siber INSERT'i (<c>WriteInvoiceItemsAsync</c>) sırasında
    /// yakalanmadan patlardı. Uygulamanın kendi oluşturma/güncelleme akışı Kalem'i zaten
    /// zorunlu tutuyor (<c>LoadController.cs</c> satır 321) — bu yüzden boş Kalem yalnızca
    /// Siber'den ETL ile senkronlanmış kayıtlarda görülür (gerçek Siber'de 18 kayıtta
    /// <c>kalemid</c> NULL doğrulandı). Burada tam geçerli bir teklif HTTP üzerinden
    /// oluşturulup ETL senaryosunu taklit etmek için Kalem doğrudan DB'de boşaltılıyor.
    /// </summary>
    [Fact]
    public async Task ConvertOfferAsync_WithFinancialItemMissingKalem_ReturnsClearErrorNotSiberCrash()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        using var accountForm = await TestAccountHelper.MinimalAccountFormAsync(
            admin, $"Kalem Boşluğu Testi {Guid.NewGuid():N}");
        var accountResponse = await admin.PostAsync("/api/v1/account", accountForm);
        accountResponse.EnsureSuccessStatusCode();
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var countryResponse = await admin.GetAsync("/api/v1/country");
        countryResponse.EnsureSuccessStatusCode();
        var countryId = (await countryResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").EnumerateArray().First().GetProperty("id").GetGuid().ToString();

        using var form = new MultipartFormDataContent
        {
            { new StringContent("1"), "work_type_id" },
            { new StringContent("1"), "loading_type_id" },
            { new StringContent("1"), "payment_type_id" },
            { new StringContent("5"), "status_type_id" },
            { new StringContent("1"), "department_id" },
            { new StringContent(accountId.ToString()), "customer_id" },
            { new StringContent(accountId.ToString()), "sender_id" },
            { new StringContent(accountId.ToString()), "receiver_id" },
            { new StringContent(countryId), "departure_country_id" },
            { new StringContent(countryId), "target_country_id" },
            { new StringContent("1"), "romork_type_id" },
            { new StringContent("1"), "load_transfer_type_id" },
            { new StringContent("1"), "way_of_working" },
            { new StringContent("1"), "instruction_id" },
            { new StringContent("2026-09-01"), "offer_date" },
            { new StringContent("2026-09-30"), "offer_validity_date" },
            { new StringContent("2026-09-01"), "marketing_notification_date" },
            { new StringContent("1"), "load_content[0][product_type_id]" },
            { new StringContent("1"), "load_content[0][case_type_id]" },
            { new StringContent("1"), "load_content[0][quantity]" },
            { new StringContent("100"), "load_content[0][width]" },
            { new StringContent("100"), "load_content[0][height]" },
            { new StringContent("100"), "load_content[0][length]" },
            { new StringContent("100"), "load_content[0][gross_weight]" },
            { new StringContent("1"), "load_content[0][lademeter]" },
            { new StringContent("1"), "load_content[0][stackable]" },
            { new StringContent("1"), "load_financial_item[0][item]" },
            { new StringContent("1"), "load_financial_item[0][quantity]" },
            { new StringContent("1"), "load_financial_item[0][buysell]" },
            { new StringContent("1"), "load_financial_item[0][transport_type_id]" },
            { new StringContent("1"), "load_financial_item[0][order]" },
            { new StringContent("100"), "load_financial_item[0][net_price]" },
            { new StringContent("100"), "load_financial_item[0][total_price]" },
            { new StringContent("1"), "load_financial_item[0][currency]" },
        };
        var createResponse = await admin.PostAsync("/api/v1/load", form);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            await createResponse.Content.ReadAsStringAsync());
        var loadId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetInt64();

        var siberId = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<OLS.Business.Common.IClock>();

        var load = await db.Loads.FirstAsync(l => l.Id == loadId);
        load.SiberId = siberId;
        load.TransferToSiber = 1;
        var financialItem = await db.LoadFinancialItems.FirstAsync(f => f.LoadId == loadId);
        financialItem.Item = null; // ETL senaryosu: gerçek Siber'de kalemid zaten NULL

        // ValidateRequired, kalem kontrolünden ÖNCE görevli (SiberCode/SiberName dolu
        // kullanıcı) arıyor. Teklif oluşturulurken işlemi yapan kullanıcı (seed admin,
        // SiberCode boş) otomatik olarak hem operasyon yetkilisi hem satış temsilcisi
        // olarak atanmış oluyor — bu iki satırı SiberCode/SiberName dolu testlik
        // kullanıcılara yönlendiriyoruz (yeni satır eklemek değil, var olanı güncellemek
        // gerekiyor; aksi hâlde ElementAtOrDefault(0/1) hâlâ admin'i buluyor).
        var opUser = new User
        {
            Name = "Test", Surname = "Yetkili", Email = $"op-{Guid.NewGuid():N}@test.local",
            SiberName = "TEST YETKİLİ", SiberCode = "1001", Status = true,
        };
        var repUser = new User
        {
            Name = "Test", Surname = "Temsilci", Email = $"rep-{Guid.NewGuid():N}@test.local",
            SiberName = "TEST TEMSİLCİ", SiberCode = "1002", Status = true,
        };
        db.AddRange(opUser, repUser);
        await db.SaveChangesAsync();

        var existingChargePeople = await db.LoadChargePeople
            .Where(p => p.LoadId == (int)loadId).OrderBy(p => p.Id).ToListAsync();
        existingChargePeople.Should().HaveCount(2, "teklif oluşturulunca işlemi yapan kullanıcı otomatik görevli atanıyor olmalı");
        existingChargePeople[0].UserId = (int)opUser.Id;
        existingChargePeople[1].UserId = (int)repUser.Id;
        await db.SaveChangesAsync();

        var service = new LoadTransferWriteService(db, new FakeSiberLoadRepository(isConfigured: true), new FakeSiberReservationRepository(isConfigured: true), clock);

        var result = await service.ConvertOfferAsync(siberId, currentUserId: 1);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Mali kalemlerden birinde kalem seçilmemiş");
    }
}

/// <summary>
/// Test dublörü: BR-002/003/004/005 kontrollerinin hepsi gerçek Siber G/Ç'sinden ÖNCE
/// çalışır — bu yüzden aşağıdaki metotların hiçbiri bu testlerde ÇAĞRILMAMALI. Çağrılırsa
/// (kontrollerden biri beklenenden geç devreye giriyorsa) test NotSupportedException ile
/// gürültülü şekilde başarısız olur, sessizce yanlış geçmez.
/// </summary>
internal sealed class FakeSiberLoadRepository : ISiberLoadRepository
{
    // Doğrulama uçları testte kullanılmıyor; kalem eksikliği simüle edilmiyor.
    public Task<IReadOnlyList<string>> FindMissingKalemIdsAsync(
        IReadOnlyCollection<string> kalemIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public FakeSiberLoadRepository(bool isConfigured) => IsConfigured = isConfigured;

    public bool IsConfigured { get; }

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
    public Task DeleteModulKalemAsync(string modulKalemId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateYukAsync(SiberYuk yuk, CancellationToken cancellationToken = default) =>
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

internal sealed class FakeSiberReservationRepository : ISiberReservationRepository
{
    public FakeSiberReservationRepository(bool isConfigured) => IsConfigured = isConfigured;

    public bool IsConfigured { get; }

    public Task<Guid> GenerateRezervasyonIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateYukKoliIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Guid> GenerateTarifeIdAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<int> InsertRezervasyonWithLockedNumberAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateRezervasyonAsync(SiberRezervasyonYaz rezervasyon, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task DeleteRezervasyonAsync(string rezervasyonId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<bool> YukKoliExistsAsync(string yukKoliId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateRezervasyonYukKoliAsync(SiberRezervasyonYukKoli koli, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<bool> TarifeExistsAsync(string tarifeId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    // Testlerde mali kalem dogrulamasi her zaman gecer — amac Siber FK'sini degil
    // aktarim akisini dogrulamak (bkz. ValidateFinancialItemsExistInSiberAsync).
    public Task<bool> KalemExistsAsync(string kalemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    // Testlerde Siber referans dogrulamasi her zaman gecer — amac Siber FK'lerini
    // degil aktarim akisini dogrulamak (bkz. ValidateSiberReferencesAsync).
    public Task<bool> ReferenceExistsAsync(string table, string idColumn, string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
    public Task<IReadOnlyList<SiberRezervasyonKoliSatir>> ReadReservationPackagesAsync(string reservationId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<IReadOnlyList<SiberRezervasyonTarifeSatir>> ReadReservationTariffsAsync(string reservationId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task InsertRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task UpdateRezervasyonTarifeAsync(SiberRezervasyonTarife tarife, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
