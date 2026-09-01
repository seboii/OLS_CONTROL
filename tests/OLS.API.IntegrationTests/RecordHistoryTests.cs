using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.Auditing;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;

namespace OLS.API.IntegrationTests;

/// <summary>
/// Siber değişiklik günlüğünün OKUNUR HÂLE getirilmesini kilitler.
///
/// Günlükte alan adları ve değerler üç ayrı metinde, satır satır KONUM
/// eşleşmesiyle duruyor. Buradaki hata sessizdir ve kullanıcıya yanlış bir
/// "şu değerden şu değere geçti" cümlesi gösterir — bu yüzden hem doğru
/// eşleştirme hem de hizalama bozukken eşleştirmeyi REDDETME davranışı
/// test ediliyor.
/// </summary>
[Collection("OlsApi")]
public sealed class RecordHistoryTests
{
    private const string LoadTable = "skn_yuk";

    private readonly OlsApiFactory _factory;

    public RecordHistoryTests(OlsApiFactory factory) => _factory = factory;

    private async Task<string> SeedAsync(
        short operation, string? fields, string? oldValues, string? newValues)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();

        var recordId = Guid.NewGuid().ToString();

        db.SiberChangeLogs.Add(new SiberChangeLog
        {
            SiberId = Guid.NewGuid().ToString(),
            TableName = LoadTable,
            RecordId = recordId,
            UserCode = "TESTKOD",
            ChangedAt = new DateTime(2026, 3, 1, 10, 0, 0),
            Operation = operation,
            Fields = fields,
            OldValues = oldValues,
            NewValues = newValues,
        });

        await db.SaveChangesAsync();
        return recordId;
    }

    private async Task<IReadOnlyList<RecordHistoryEntry>> ReadAsync(string recordId)
    {
        using var scope = _factory.Services.CreateScope();
        var history = scope.ServiceProvider.GetRequiredService<IRecordHistoryService>();
        return await history.GetAsync(LoadTable, recordId);
    }

    [Fact]
    public async Task Guncellemede_YalnizcaDegisenAlanlarDoner()
    {
        // Siber her güncellemede TÜM izlenen alanları yazıyor; değişmeyenleri de
        // listelemek geçmişi okunmaz hâle getiriyordu.
        var recordId = await SeedAsync(2,
            "Yük No\nYük Durumu\nMüşteri",
            "2300740EX\n10 - SİPARİŞ\nITB",
            "2300740EX\n90 - BOŞALTILDI\nITB");

        var entries = await ReadAsync(recordId);

        entries.Should().ContainSingle();
        entries[0].OperationLabel.Should().Be("Güncelledi");
        entries[0].Changes.Should().ContainSingle();
        entries[0].Changes[0].Field.Should().Be("Yük Durumu");
        entries[0].Changes[0].OldValue.Should().Be("10 - SİPARİŞ");
        entries[0].Changes[0].NewValue.Should().Be("90 - BOŞALTILDI");
    }

    [Fact]
    public async Task BosDegerler_KonumKaymasinaYolAcmaz()
    {
        // Değeri boş olan alan da bir satır işgal ediyor; boş satırlar atlanırsa
        // sonraki tüm alanların eşleşmesi kayar ve yanlış çiftler üretilir.
        var recordId = await SeedAsync(2,
            "Alan1\nAlan2\nAlan3",
            "\n\neski3",
            "\n\nyeni3");

        var entries = await ReadAsync(recordId);

        entries[0].ChangesUnparsed.Should().BeFalse();
        entries[0].Changes.Should().ContainSingle();
        entries[0].Changes[0].Field.Should().Be("Alan3");
        entries[0].Changes[0].NewValue.Should().Be("yeni3");
    }

    [Fact]
    public async Task HizalamaBozuksa_DegerEslestirmesiYapilmaz()
    {
        // Çok satırlı bir metin alanı (açıklama gibi) konum eşleşmesini bozuyor.
        // Bu durumda yanlış çift göstermek yerine yalnızca alan adları verilir.
        var recordId = await SeedAsync(2,
            "Alan1\nAlan2",
            "tek satir",
            "cok\nsatirli\ndeger");

        var entries = await ReadAsync(recordId);

        entries[0].ChangesUnparsed.Should().BeTrue();
        entries[0].Changes.Should().BeEmpty();
        entries[0].ChangedFieldNames.Should().BeEquivalentTo(["Alan1", "Alan2"]);
    }

    [Fact]
    public async Task Olusturmada_OncekiDegerBosGosterilir()
    {
        var recordId = await SeedAsync(1, "Yük No\nMüşteri", null, "2300740EX\nITB");

        var entries = await ReadAsync(recordId);

        entries[0].OperationLabel.Should().Be("Oluşturdu");
        entries[0].ChangesUnparsed.Should().BeFalse();
        entries[0].Changes.Should().HaveCount(2);
        entries[0].Changes[0].OldValue.Should().BeNull();
        entries[0].Changes[0].NewValue.Should().Be("2300740EX");
    }

    [Fact]
    public async Task Silmede_SonrakiDegerBosGosterilir()
    {
        var recordId = await SeedAsync(3, "Yük No", "2300740EX", null);

        var entries = await ReadAsync(recordId);

        entries[0].OperationLabel.Should().Be("Sildi");
        entries[0].Changes.Should().ContainSingle();
        entries[0].Changes[0].OldValue.Should().Be("2300740EX");
        entries[0].Changes[0].NewValue.Should().BeNull();
    }

    [Fact]
    public async Task SiberKarsiligiOlmayanKayit_BosListeDoner()
    {
        // Yalnızca yerelde açılmış kayıt için geçmiş yoktur; hata değil.
        (await ReadAsync(string.Empty)).Should().BeEmpty();
        (await ReadAsync(Guid.NewGuid().ToString())).Should().BeEmpty();
    }
}
