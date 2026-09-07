using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Common;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// PANEL VE RAPOR EKRANLARI DA ŞİRKET KAPSAMINA UYAR.
///
/// Bulunan hata: teklif/yük/sefer LİSTELERİ şirkete göre süzülürken
/// <c>DashboardService</c> ve <c>ReportingService</c> doğrudan
/// <c>_db.Loads</c>/<c>_db.LoadTransfers</c>/<c>_db.Expeditions</c> üzerinden
/// sayıyordu. Sonuç: Avrora kullanıcısı listesinde 762 yük görürken panelde
/// bütün şirketlerin toplamını görüyordu.
///
/// İkinci hata aynı yerdeydi: listeler <c>siber_deleted_at</c> damgalı
/// kayıtları gizlerken panel ve rapor onları sayıyordu — aynı ekranda iki
/// farklı toplam.
///
/// Testler MUTLAK sayı iddia etmez (veritabanı testler arasında paylaşılıyor);
/// kayıt eklemeden önce ve sonra ölçüp FARKA bakar.
/// </summary>
[Collection("OlsApi")]
public sealed class ReportScopeTests
{
    private const string Avrora = "46258A01-8D77-4F87-AAF5-6B331DEDD8A7";
    private const string Ols = "BA4888B1-A2B0-4142-B273-92481D932EAD";

    private readonly OlsApiFactory _factory;

    public ReportScopeTests(OlsApiFactory factory) => _factory = factory;

    private async Task SeedTransferAsync(string companyId, bool deleted = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        // Sütun "timestamp without time zone" — Npgsql Kind=Utc bir DateTime'ı
        // reddediyor. Uygulamanın kendi saati (IClock) zaten Unspecified üretir
        // ve panelin "bu ay" penceresiyle aynı takvimi kullanır.
        var now = scope.ServiceProvider.GetRequiredService<IClock>().Now;

        db.LoadTransfers.Add(new LoadTransfer
        {
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumberWorkType = $"KPS{Guid.NewGuid():N}"[..14],
            SiberCompanyId = companyId,
            CreatedAt = now,
            SiberDeletedAt = deleted ? now : null,
        });

        await db.SaveChangesAsync();
    }

    private static async Task<int> DashboardLoadCountAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/dashboard");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("metrics")
            .GetProperty("load_transfers_this_month").GetInt32();
    }

    private static async Task<int> ReportingLoadCountAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/reporting");
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("kpi").GetProperty("total_loads").GetInt32();
    }

    /// <summary>
    /// Avrora kullanıcısı için tek oturum açar — "auth" hız sınırı dakikada 10
    /// istekle sabit pencereli, her testte yeniden giriş yapmak koleksiyonu
    /// gereksizce yavaşlatıyor.
    /// </summary>
    private async Task<HttpClient> CreateAvroraClientAsync()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var email = $"kapsam-rapor-{Guid.NewGuid():N}@avroralog.com";
        var userId = await admin.CreateUserAsync(email);

        // Rapor ekranı kişi bazlı iş yükü gösterdiği için yetki ister
        // (panelin aksine); kullanıcı sıfır yetkiyle açılıyor.
        await admin.GrantPermissionAsync(userId, "report_management", "read");

        var token = await _factory.LoginAsync(email, "Test!2026Pw");
        return _factory.CreateAuthorizedClient(token);
    }

    [Fact]
    public async Task Panel_AvroraKullanicisi_BaskaSirketinYukunuSaymaz()
    {
        using var client = await CreateAvroraClientAsync();

        var before = await DashboardLoadCountAsync(client);

        await SeedTransferAsync(Ols);
        (await DashboardLoadCountAsync(client)).Should().Be(
            before, "OLS yükü Avrora kullanıcısının panelinde görünmemeli");

        await SeedTransferAsync(Avrora);
        (await DashboardLoadCountAsync(client)).Should().Be(
            before + 1, "kendi şirketinin yükü sayılmalı");
    }

    [Fact]
    public async Task Panel_SilinmisYuku_Saymaz()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var before = await DashboardLoadCountAsync(admin);

        await SeedTransferAsync(Ols, deleted: true);

        (await DashboardLoadCountAsync(admin)).Should().Be(
            before, "Siber'den silinmiş yük listede gizleniyor, panelde de sayılmamalı");
    }

    [Fact]
    public async Task Rapor_AvroraKullanicisi_BaskaSirketinYukunuSaymaz()
    {
        using var client = await CreateAvroraClientAsync();

        var before = await ReportingLoadCountAsync(client);

        await SeedTransferAsync(Ols);
        (await ReportingLoadCountAsync(client)).Should().Be(before);

        await SeedTransferAsync(Avrora);
        (await ReportingLoadCountAsync(client)).Should().Be(before + 1);
    }

    [Fact]
    public async Task Rapor_SilinmisYuku_Saymaz()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var before = await ReportingLoadCountAsync(admin);

        await SeedTransferAsync(Ols, deleted: true);

        (await ReportingLoadCountAsync(admin)).Should().Be(before);
    }

    /// <summary>
    /// TREND GRAFİĞİ ARTIK KOVA BAŞINA SORGU ATMIYOR.
    ///
    /// Eskiden her kova için ayrı iki COUNT çalışıyordu (varsayılan görünümde
    /// 24 gidiş-dönüş); artık tablo başına tek gruplu sorgu atılıp kovalar
    /// bellekte toplanıyor. Bu test SAYILARIN aynı kaldığını kilitler —
    /// hızlandırma sessizce yanlış sonuç üretmesin.
    ///
    /// Tek günlük aralık seçiliyor: bu durumda kırılım "gün" ve tek kova var,
    /// yani beklenen değer belirsizliğe yer bırakmıyor.
    /// </summary>
    [Fact]
    public async Task Rapor_TrendKovasi_EklenenKaydiDogruGunde_Sayar()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        DateOnly today;
        using (var scope = _factory.Services.CreateScope())
            today = DateOnly.FromDateTime(scope.ServiceProvider.GetRequiredService<IClock>().Now);

        var query = $"/api/v1/reporting?date_from={today:yyyy-MM-dd}&date_to={today:yyyy-MM-dd}";

        async Task<(string Granularity, int Points, int LoadCount)> TrendAsync()
        {
            var response = await admin.GetAsync(query);
            response.EnsureSuccessStatusCode();

            var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            var trend = data.GetProperty("trend");

            return (data.GetProperty("trend_granularity").GetString()!,
                    trend.GetArrayLength(),
                    trend[0].GetProperty("load_count").GetInt32());
        }

        var before = await TrendAsync();
        before.Granularity.Should().Be("day");
        before.Points.Should().Be(1, "tek günlük aralıkta tek kova olur");

        await SeedTransferAsync(Ols);
        await SeedTransferAsync(Avrora);

        var after = await TrendAsync();
        after.LoadCount.Should().Be(before.LoadCount + 2);
    }

    /// <summary>
    /// per_page üst sınırı: istemci ne isterse istesin sayfa boyu
    /// <see cref="QueryableExtensions.MaxPerPage"/> ile sınırlı.
    /// </summary>
    [Fact]
    public async Task ListeUcu_AsiriPerPage_UstSinirlaKisitlanir()
    {
        using var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/v1/load_transfer?per_page=100000");
        response.EnsureSuccessStatusCode();

        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        data.GetProperty("per_page").GetInt32().Should().Be(QueryableExtensions.MaxPerPage);
        data.GetProperty("data").GetArrayLength().Should()
            .BeLessThanOrEqualTo(QueryableExtensions.MaxPerPage);
    }
}
