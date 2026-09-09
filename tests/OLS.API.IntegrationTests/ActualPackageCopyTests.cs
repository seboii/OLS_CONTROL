using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.LoadTransfers;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// "GERÇEK KOLİ BİLGİLERİNE AKTAR" — beyan edilen kolileri gerçek koli setine
/// kopyalar (Siber'de <c>skn_yukkoli</c> → <c>skn_yukkolidepo</c>).
///
/// BULUNAN GERÇEK HATA — ANAHTAR UYUŞMAZLIĞI: koli satırları
/// <c>load_transfer_id</c> sütununda yerel sayısal kimliği DEĞİL, Siber'in
/// <c>yukid</c> GUID'ini tutuyor (senkron öyle yazıyor, yükün detay ucu da öyle
/// okuyor). Aktarma ilk sürümde yerel kimlikle arıyordu; sorgu hiçbir zaman
/// satır bulamıyor ve düğme "Aktarılacak koli bilgisi yok" diyordu — yani
/// hiç çalışmıyordu.
/// </summary>
[Collection("OlsApi")]
public sealed class ActualPackageCopyTests
{
    private readonly OlsApiFactory _factory;

    public ActualPackageCopyTests(OlsApiFactory factory) => _factory = factory;

    private static async Task<LoadTransfer> SeedWithPackagesAsync(OlsDbContext db, int packageCount)
    {
        var transfer = new LoadTransfer
        {
            // Siber kimliği: koli satırlarının anahtarı budur.
            LoadTransferId = Guid.NewGuid().ToString(),
            LoadNumber = $"KOLI-{Guid.NewGuid():N}"[..18],
            LoadNumberWorkType = $"KOLI-{Guid.NewGuid():N}"[..18],
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };
        db.LoadTransfers.Add(transfer);
        await db.SaveChangesAsync();

        for (var i = 0; i < packageCount; i++)
        {
            db.LoadTransferPackages.Add(new LoadTransferPackage
            {
                Yukkoliid = Guid.NewGuid().ToString(),
                LoadTransferId = transfer.LoadTransferId,
                Quantity = i + 1,
                GrossWeight = 100 * (i + 1),
                Volume = 2 * (i + 1),
                Lademeter = i + 1,
                Stackable = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            });
        }

        await db.SaveChangesAsync();
        return transfer;
    }

    [Fact]
    public async Task Aktar_BeyanEdilenKolileriGercekKoliyeKopyalar()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var packages = scope.ServiceProvider.GetRequiredService<IActualPackageService>();

        var transfer = await SeedWithPackagesAsync(db, packageCount: 3);

        var result = await packages.CopyFromDeclaredAsync(transfer.Id, replaceExisting: false);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        result.Affected.Should().Be(3);

        var rows = await packages.ListAsync(transfer.Id);
        rows.Should().HaveCount(3);
        rows.Select(r => r.Quantity).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        rows.Select(r => r.GrossWeight).Should().BeEquivalentTo(new[] { 100m, 200m, 300m });
    }

    /// <summary>
    /// Satırlar Siber'in yukid'siyle anahtarlanır — beyan edilen koli tablosuyla
    /// AYNI kural. Yerel sayısal kimlik yazılsaydı aktarma bir daha eşleşme
    /// bulamazdı.
    /// </summary>
    [Fact]
    public async Task Aktar_SatirlariSiberYukIdIleAnahtarlar()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var packages = scope.ServiceProvider.GetRequiredService<IActualPackageService>();

        var transfer = await SeedWithPackagesAsync(db, packageCount: 2);

        await packages.CopyFromDeclaredAsync(transfer.Id, replaceExisting: false);

        var keys = await db.LoadTransferActualPackages.AsNoTracking()
            .Where(p => p.LoadTransferId == transfer.LoadTransferId)
            .Select(p => p.LoadTransferId)
            .ToListAsync();

        keys.Should().HaveCount(2);
        keys.Should().OnlyContain(k => k == transfer.LoadTransferId);
    }

    /// <summary>
    /// Gerçek koli seti doluysa üzerine YAZILMAZ — o satırlar depoda elle
    /// düzeltilmiş olabilir (canlıda ~144 yükte öyle). Kullanıcı onayıyla
    /// (<c>replaceExisting</c>) yeniden aktarılabilir.
    /// </summary>
    [Fact]
    public async Task Aktar_SetDoluysaOnaysizEzmez()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var packages = scope.ServiceProvider.GetRequiredService<IActualPackageService>();

        var transfer = await SeedWithPackagesAsync(db, packageCount: 2);
        await packages.CopyFromDeclaredAsync(transfer.Id, replaceExisting: false);

        var again = await packages.CopyFromDeclaredAsync(transfer.Id, replaceExisting: false);

        again.IsSuccess.Should().BeFalse();
        again.ErrorMessage.Should().Contain("zaten dolu");

        var forced = await packages.CopyFromDeclaredAsync(transfer.Id, replaceExisting: true);

        forced.IsSuccess.Should().BeTrue(forced.ErrorMessage);
        (await packages.ListAsync(transfer.Id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Aktar_KoliYoksaAcikHataDoner()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var packages = scope.ServiceProvider.GetRequiredService<IActualPackageService>();

        var transfer = await SeedWithPackagesAsync(db, packageCount: 0);

        var result = await packages.CopyFromDeclaredAsync(transfer.Id, replaceExisting: false);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("koli bilgisi yok");
    }
}
