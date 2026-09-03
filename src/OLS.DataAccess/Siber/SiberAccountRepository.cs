using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Siber (MSSQL) tarafındaki cari tabloları: <c>sbr_firma</c>, <c>sbr_firmatemsilci</c>.
/// olsold'daki FrontAccountController içindeki ham sqlsrv sorgularının karşılığı.
///
/// Not: docker-compose bu bağlantıyı yerel <c>siber-mock</c> konteynerine yönlendirir,
/// canlı ERP'ye değil. Gerçek Siber'e geçmek için yalnızca bağlantı dizesi değişir.
/// </summary>
public interface ISiberAccountRepository
{
    bool IsConfigured { get; }

    /// <summary>Çakışmayan bir firmaid üretir (olsold'daki do/while UUID döngüsü).</summary>
    Task<Guid> GenerateFirmaIdAsync(CancellationToken cancellationToken = default);

    Task InsertFirmaAsync(SiberFirma firma, CancellationToken cancellationToken = default);
    Task UpdateFirmaAsync(SiberFirma firma, CancellationToken cancellationToken = default);

    Task DeleteFirmaTemsilcileriAsync(string firmaId, CancellationToken cancellationToken = default);
    Task InsertFirmaTemsilcisiAsync(SiberFirmaTemsilcisi temsilci, CancellationToken cancellationToken = default);
}

/// <summary>sbr_firma satırı. Alan adları Siber şemasındaki sütun adlarıdır.</summary>
public sealed class SiberFirma
{
    public string FirmaId { get; init; } = string.Empty;
    public string SirketId { get; init; } = string.Empty;
    public string? Ad { get; init; }
    public string? Adres1 { get; init; }
    public string? Telefon1 { get; init; }
    public string? Email { get; init; }
    public string? VergiDaire { get; init; }
    public string? VergiDaireId { get; init; }
    public string? VergiNo { get; init; }
    public string? MuhasebeKod { get; init; }
    public string? UlkeId { get; init; }
    public string? SehirId { get; init; }
    public string? IlceId { get; init; }
    public string FirmaDurumId { get; init; } = string.Empty;
    public int Alici { get; init; }
    public int Satici { get; init; }
    public string? SahisTuzel { get; init; }
}

public sealed class SiberFirmaTemsilcisi
{
    public string FirmaTemsilciId { get; init; } = string.Empty;
    public string FirmaId { get; init; } = string.Empty;
    public string? Ad { get; init; }
    public DateTime InsTime { get; init; }
    public string? InsUser { get; init; }
    public int MusteriTemsilcisi { get; init; }
    public int SatisTemsilcisi { get; init; }

    /// <summary>
    /// sbr_firmatemsilci.operasyonyetkilisi — cari görevlisi senkronu
    /// (SyncAccountRepresentativesAsync) rolü BU sütun ile satistemsilcisi'nden
    /// okuyor. Kod eskiden bu sütunu hiç yazmıyordu.
    /// </summary>
    public int OperasyonYetkilisi { get; init; }

    /// <summary>Kaydın ait olduğu kullanıcının Siber kodu (kod sütunu).</summary>
    public string? Kod { get; init; }
}

public sealed class SiberAccountRepository : ISiberAccountRepository
{
    /// <summary>
    /// olsold'da sabit kodluydu (FrontAccountController). Siber'in şirket ve
    /// firma-durum kayıtlarına işaret eden magic GUID'ler.
    /// </summary>
    /// <summary>
    /// Varsayılan şirket. Siber'in kendi verisinde cariler iki şirkete
    /// bölünmüş (6.577 OLS / 865 AVRORA), bu yüzden çağıran kendi şirketini
    /// <see cref="SiberFirma.SirketId"/> ile geçirmeli.
    /// </summary>
    public const string SirketId = "BA4888B1-A2B0-4142-B273-92481D932EAD";
    public const string FirmaDurumId = "9B3980D3-6D2C-4524-923B-81231B0FBE1A";

    private readonly ISiberConnectionFactory _factory;

    public SiberAccountRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<Guid> GenerateFirmaIdAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // olsold: do { $uuid = Str::uuid(); } while (exists)
        while (true)
        {
            var candidate = Guid.NewGuid();

            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM sbr_firma WHERE firmaid = @firmaid",
                new { firmaid = candidate.ToString() });

            if (exists == 0)
                return candidate;
        }
    }

    public async Task InsertFirmaAsync(SiberFirma firma, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        // Sabit 1'ler olsold'dan birebir taşındı: cari her taşıma türü için
        // müşteri olarak işaretleniyordu.
        const string sql = """
            INSERT INTO sbr_firma
                (firmaid, sirketid, ad, adres1, telefon1, email, vergidaire, vergidaireid,
                 vergino, muhasebekod, ulkeid, sehirid, ilceid, firmadurumid, alici, satici,
                 sahistuzel, yicimusteri, ithmusteri, ihrmusteri, trmusteri, karamusteri,
                 havamusteri, denizmusteri, finodemesekil, demiryolumusteri, depomusteri,
                 antrepomusteri, finansfaturavadegun, aktif)
            VALUES
                (@FirmaId, @SirketId, @Ad, @Adres1, @Telefon1, @Email, @VergiDaire, @VergiDaireId,
                 @VergiNo, @MuhasebeKod, @UlkeId, @SehirId, @IlceId, @FirmaDurumId, @Alici, @Satici,
                 @SahisTuzel, 1, 1, 1, 1, 1,
                 1, 1, 1, 1, 1,
                 1, 15, 1)
            """;

        await connection.ExecuteAsync(sql, firma);
    }

    public async Task UpdateFirmaAsync(SiberFirma firma, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            UPDATE sbr_firma SET
                ad = @Ad, adres1 = @Adres1, telefon1 = @Telefon1, email = @Email,
                vergidaire = @VergiDaire, vergidaireid = @VergiDaireId, vergino = @VergiNo,
                muhasebekod = @MuhasebeKod, ulkeid = @UlkeId, sehirid = @SehirId,
                ilceid = @IlceId, alici = @Alici, satici = @Satici, sahistuzel = @SahisTuzel
            WHERE firmaid = @FirmaId
            """;

        await connection.ExecuteAsync(sql, firma);
    }

    public async Task DeleteFirmaTemsilcileriAsync(string firmaId, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM sbr_firmatemsilci WHERE firmaid = @firmaid",
            new { firmaid = firmaId });
    }

    public async Task InsertFirmaTemsilcisiAsync(
        SiberFirmaTemsilcisi temsilci, CancellationToken cancellationToken = default)
    {
        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO sbr_firmatemsilci
                (firmatemsilciid, firmaid, ad, kod, instime, insuser,
                 musteritemsilcisi, satistemsilcisi, operasyonyetkilisi)
            VALUES
                (@FirmaTemsilciId, @FirmaId, @Ad, @Kod, @InsTime, @InsUser,
                 @MusteriTemsilcisi, @SatisTemsilcisi, @OperasyonYetkilisi)
            """;

        await connection.ExecuteAsync(sql, temsilci);
    }
}
