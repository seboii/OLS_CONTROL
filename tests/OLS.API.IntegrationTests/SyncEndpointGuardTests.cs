using System.Net;
using FluentAssertions;

namespace OLS.API.IntegrationTests;

/// <summary>
/// SİBER SENKRON UÇLARI YALNIZCA SÜPER ADMİNE AÇIK.
///
/// Bulunan boşluk: <c>TransferDataController</c> ve <c>FinanceSyncController</c>
/// yalnızca <c>[Authorize]</c> taşıyordu. Oturum açmış HERHANGİ bir kullanıcı
/// tek istekle çok büyük çekimleri tetikleyebiliyordu —
/// <c>change_logs?full=true</c> 797.855 satır, <c>vouchers?full=true</c>
/// 214.954 satır.
///
/// Uçlar arayüzden hiç çağrılmıyor (frontend'de tek referansı yok): kurulum ve
/// onarım için elle çalıştırılan operatör araçları. Bu yüzden kısıtlama bir
/// akışı bozmuyor.
///
/// Test yalnızca REDDEDİLEN yolları dener — süper admin olarak çağırmak gerçek
/// bir Siber içe aktarımı başlatırdı.
/// </summary>
[Collection("OlsApi")]
public sealed class SyncEndpointGuardTests
{
    private readonly OlsApiFactory _factory;

    public SyncEndpointGuardTests(OlsApiFactory factory) => _factory = factory;

    public static TheoryData<string> SyncEndpoints =>
    [
        "/api/v1/transfer_data/getCarType",
        "/api/v1/transfer_data/change_logs",
        "/api/v1/finance_sync/accounting_plan",
        "/api/v1/finance_sync/vouchers",
    ];

    [Theory]
    [MemberData(nameof(SyncEndpoints))]
    public async Task SenkronUclari_JetonsuzErisimde401(string path)
    {
        using var client = _factory.CreateClient();

        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(SyncEndpoints))]
    public async Task SenkronUclari_SuperAdminOlmayanKullanicida403(string path)
    {
        // Paylasilan istemci — Dispose EDILMEZ, sonraki test durumu da kullanir.
        var client = await CreateOrdinaryClientAsync();

        (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Sıradan kullanıcı için TEK oturum açar: "auth" hız sınırı dakikada 10
    /// istekle sabit pencereli, test başına giriş yapmak koleksiyonu
    /// gereksizce yavaşlatır.
    /// </summary>
    private static HttpClient? _ordinaryClient;
    private static readonly SemaphoreSlim _ordinaryLock = new(1, 1);

    private async Task<HttpClient> CreateOrdinaryClientAsync()
    {
        await _ordinaryLock.WaitAsync();
        try
        {
            if (_ordinaryClient is not null)
                return _ordinaryClient;

            using var admin = await _factory.CreateAdminClientAsync();
            var email = $"senkron-yetkisiz-{Guid.NewGuid():N}@example.test";
            await admin.CreateUserAsync(email);

            var token = await _factory.LoginAsync(email, "Test!2026Pw");
            return _ordinaryClient = _factory.CreateAuthorizedClient(token);
        }
        finally
        {
            _ordinaryLock.Release();
        }
    }
}
