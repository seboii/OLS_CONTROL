using FluentAssertions;
using OLS.Business.Services.Siber;
using OLS.DataAccess.Siber;

namespace OLS.Business.Tests;

/// <summary>
/// Siber referans doğrulaması — teklif, yük ve sefer akışlarının ortak kapısı.
///
/// Bu kontrolün varlık sebebi somut: Siber'e yazım GERİ ALINAMIYOR. Yazım
/// yarıda kalırsa yerel işlem geri alınıyor ama Siber'deki kayıt kalıyor.
/// Canlıda üç cari Siber ekranından silinmişti ve yerelde listede duruyordu;
/// teklifsiz yük açarken FK hatasına dönüşüyordu.
/// </summary>
public sealed class SiberReferenceValidatorTests
{
    /// <summary>Verilen kimlikleri "Siber'de yok" sayan sahte depo.</summary>
    private sealed class FakeRepository : ISiberReferenceRepository
    {
        private readonly HashSet<string> _missing;

        public FakeRepository(params string[] missing) =>
            _missing = missing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>Kaç kez sorgulandı — tablo başına tek sorgu iddiası için.</summary>
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<string>> FindMissingAsync(
            SiberReferenceTable table, IReadOnlyCollection<string> ids,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<IReadOnlyList<string>>(
                ids.Where(_missing.Contains).ToList());
        }
    }

    private static SiberReferenceCheck Check(string label, SiberReferenceTable table, string? id) =>
        new(label, table, id);

    [Fact]
    public async Task AllPresent_PassesWithNoMessage()
    {
        var validator = new SiberReferenceValidator(new FakeRepository());

        var result = await validator.ValidateAsync(
        [
            Check("Departman", SiberReferenceTable.Departman, Guid.NewGuid().ToString()),
            Check("Müşteri", SiberReferenceTable.Firma, Guid.NewGuid().ToString()),
        ]);

        result.Should().BeNull();
    }

    [Fact]
    public async Task MissingInSiber_NamesTheField()
    {
        var deleted = Guid.NewGuid().ToString();
        var validator = new SiberReferenceValidator(new FakeRepository(deleted));

        var result = await validator.ValidateAsync(
        [
            Check("Departman", SiberReferenceTable.Departman, Guid.NewGuid().ToString()),
            Check("Müşteri", SiberReferenceTable.Firma, deleted),
        ]);

        result.Should().NotBeNull();
        result.Should().Contain("Müşteri").And.Contain("bulunamadı");
        result.Should().NotContain("Departman", "sorunsuz alan mesajda anılmamalı");
    }

    /// <summary>
    /// Boş <c>SiberId</c> "seçilmiş ama karşılığı tanımlanmamış" demektir ve
    /// AYRI bir mesaj verir — kullanıcı listeyi yenilemekle çözemez, tanımın
    /// kendisi eksiktir.
    /// </summary>
    [Fact]
    public async Task EmptySiberId_ReportedAsUndefined()
    {
        var validator = new SiberReferenceValidator(new FakeRepository());

        var result = await validator.ValidateAsync([Check("Araç", SiberReferenceTable.Arac, "")]);

        result.Should().NotBeNull();
        result.Should().Contain("Araç").And.Contain("tanımlı değil");
    }

    /// <summary>Seçilmemiş (null) alan doğrulamanın konusu değildir.</summary>
    [Fact]
    public async Task NullSelection_IsSkipped()
    {
        var repository = new FakeRepository();
        var validator = new SiberReferenceValidator(repository);

        var result = await validator.ValidateAsync(
        [
            Check("Ödeme tipi", SiberReferenceTable.OdemeSekli, null),
            Check("Römork cinsi", SiberReferenceTable.SabitTanim, null),
        ]);

        result.Should().BeNull();
        repository.CallCount.Should().Be(0, "sorgulanacak bir şey yok");
    }

    /// <summary>
    /// Aynı tablodaki tüm kimlikler TEK sorguda sorulur. Teklifsiz yük formunda
    /// üç cari + paket başına kap tipi kontrol ediliyor; satır başına sorgu
    /// yazım öncesi gecikmeyi katlardı.
    /// </summary>
    [Fact]
    public async Task SameTable_QueriedOnce()
    {
        var repository = new FakeRepository();
        var validator = new SiberReferenceValidator(repository);

        await validator.ValidateAsync(
        [
            Check("Müşteri", SiberReferenceTable.Firma, Guid.NewGuid().ToString()),
            Check("Gönderici", SiberReferenceTable.Firma, Guid.NewGuid().ToString()),
            Check("Alıcı", SiberReferenceTable.Firma, Guid.NewGuid().ToString()),
            Check("Departman", SiberReferenceTable.Departman, Guid.NewGuid().ToString()),
        ]);

        repository.CallCount.Should().Be(2, "sbr_firma ve sbr_departman için birer sorgu");
    }
}
