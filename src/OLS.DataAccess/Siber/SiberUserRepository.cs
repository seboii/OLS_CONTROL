using System.Data;
using Dapper;

namespace OLS.DataAccess.Siber;

/// <summary>Siber şifre doğrulamasının sonucu; başarısızlığın SEBEBİ de taşınır.</summary>
public enum SiberPasswordResult
{
    /// <summary>Siber bağlantısı yapılandırılmamış — doğrulama hiç denenmedi.</summary>
    NotConfigured,

    /// <summary><c>sky_kullanici.kod</c> bulunamadı.</summary>
    UserNotFound,

    /// <summary>Siber'de engelli hesap (<c>engelle = 1</c>).</summary>
    Blocked,

    /// <summary>Siber'de şifresi HİÇ tanımlanmamış hesap (<c>pass IS NULL</c>).</summary>
    NoPassword,

    WrongPassword,
    Success,
}

/// <summary>
/// Siber kullanıcı hesabına karşı ŞİFRE DOĞRULAR — şifre okumaz, çözmez, taşımaz.
///
/// NASIL ÇALIŞIYOR: Siber şifreleri <c>sky_kullanici.pass</c> sütununda
/// <c>varbinary(255)</c> olarak, SQL Server'ın KENDİ şifre özetiyle duruyor
/// (<c>PWDENCRYPT</c>). Canlıda 121 dolu satırın 120'si <c>0x0200…</c> (SQL 2012+
/// tuzlu SHA-512, 70 bayt), 1'i eski <c>0x0100…</c> biçiminde. Bu özet GERİ
/// ÇEVRİLEMEZ; doğrulama <c>PWDCOMPARE</c> ile SUNUCUDA yapılır. Uygulama hiçbir
/// zaman bir Siber şifresini ne görür ne saklar.
///
/// Siber'in kendi doğrulayıcısı <c>dbo.sky_kullanici_ok(kod, sifre)</c> ve şu
/// karşılaştırmayı yapıyor: <c>PWDCOMPARE(UPPER(@sifre), pass)</c>. İki ayrıntı
/// birebir taşınmalı, yoksa doğru şifre reddedilir:
///   * Şifre KARŞILAŞTIRMADAN ÖNCE BÜYÜK HARFE çevrilir — yani Siber şifreleri
///     büyük/küçük harf duyarsızdır. Büyütme SQL tarafında yapılmalı: veritabanı
///     harmanlaması <c>Turkish_CI_AS</c>, dolayısıyla <c>UPPER('i') = 'İ'</c>.
///     .NET'in <c>ToUpper()</c>'ı farklı sonuç verebilirdi.
///   * Parametre <c>varchar(50)</c>'dir (Unicode DEĞİL, 50 karakterle sınırlı).
///     Aynı tip kullanılır ki Siber'in kendi girişiyle aynı baytlar karşılaşsın.
///
/// FONKSİYON DOĞRUDAN ÇAĞRILMAZ, çünkü iki adet "şifresiz geçiş" yolu içeriyor:
///   1. <c>sbr_parametre.ldap</c> açıksa ve kullanıcı <c>ldapbaglantisiz</c>
///      değilse fonksiyon şifreye HİÇ BAKMADAN 1 döner (bugün ldap = 0, ama
///      yarın açılabilir).
///   2. <c>pass IS NULL</c> olan hesapta BOŞ şifre 1 döner — canlıda böyle 5
///      hesap var, 1'i de engelli değil.
/// Bu yüzden karşılaştırma burada aynı kuralla ama bu iki kapı KAPALI olarak
/// tekrar edilir.
/// </summary>
public interface ISiberUserRepository
{
    bool IsConfigured { get; }

    /// <summary>
    /// <paramref name="userCode"/> <c>sky_kullanici.kod</c> değeridir
    /// (yereldeki <c>users.siber_code</c>).
    /// </summary>
    Task<SiberPasswordResult> VerifyPasswordAsync(
        string userCode, string password, CancellationToken cancellationToken = default);
}

public sealed class SiberUserRepository : ISiberUserRepository
{
    /// <summary>Siber'in kendi doğrulayıcısındaki <c>@_string varchar(50)</c>.</summary>
    private const int PasswordParameterLength = 50;

    /// <summary><c>sky_kullanici.kod</c> — <c>sbruser</c> = varchar(128).</summary>
    private const int UserCodeParameterLength = 128;

    private readonly ISiberConnectionFactory _factory;

    public SiberUserRepository(ISiberConnectionFactory factory) => _factory = factory;

    public bool IsConfigured => _factory.IsConfigured;

    public async Task<SiberPasswordResult> VerifyPasswordAsync(
        string userCode, string password, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return SiberPasswordResult.NotConfigured;

        // Boş şifre HİÇBİR koşulda gönderilmez: pass'i null olan hesaplarda
        // Siber'in kendi fonksiyonu boş şifreyi kabul ediyor.
        if (string.IsNullOrWhiteSpace(userCode) || string.IsNullOrEmpty(password))
            return SiberPasswordResult.WrongPassword;

        using var connection = await _factory.CreateOpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("Kod", userCode.Trim(), DbType.AnsiString, size: UserCodeParameterLength);
        parameters.Add("Password", password, DbType.AnsiString, size: PasswordParameterLength);

        var row = await connection.QuerySingleOrDefaultAsync<VerificationRow>(new CommandDefinition(
            """
            SELECT TOP 1
                   CAST(ISNULL(k.engelle, 0) AS INT)                        AS Blocked,
                   CASE WHEN k.pass IS NULL THEN 0 ELSE 1 END               AS HasPassword,
                   CASE WHEN k.pass IS NOT NULL
                             AND PWDCOMPARE(UPPER(@Password), k.pass) = 1
                        THEN 1 ELSE 0 END                                   AS Matches
            FROM sky_kullanici k
            WHERE k.kod = @Kod
            """,
            parameters,
            cancellationToken: cancellationToken));

        if (row is null) return SiberPasswordResult.UserNotFound;
        if (row.Blocked != 0) return SiberPasswordResult.Blocked;
        if (row.HasPassword == 0) return SiberPasswordResult.NoPassword;

        return row.Matches == 1
            ? SiberPasswordResult.Success
            : SiberPasswordResult.WrongPassword;
    }

    private sealed class VerificationRow
    {
        public int Blocked { get; set; }
        public int HasPassword { get; set; }
        public int Matches { get; set; }
    }
}
