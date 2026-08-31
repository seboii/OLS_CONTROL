using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace OLS.Business.Services.Authentication;

/// <summary>
/// Siber'den içe aktarılan kullanıcıların bizim formatımızda BİR ŞİFRESİ YOKTUR:
/// <c>skn_kullanici</c> bcrypt hash taşımaz, bu yüzden <see cref="SiberImportService"/>
/// tarafından açılan her kullanıcı <c>password = NULL</c> ile geliyordu ve HİÇBİRİ
/// giriş yapamıyordu (canlı veritabanında 126 kullanıcı tam olarak bu hâldeydi,
/// yalnızca elle açılan 5 kullanıcının şifresi vardı).
///
/// Ortak başlangıç şifresi <c>Seed:DefaultUserPassword</c> ile verilir
/// (docker: <c>Seed__DefaultUserPassword</c>). Development ortamında değer
/// verilmezse <see cref="DevelopmentDefault"/> kullanılır; DİĞER ortamlarda
/// değer verilmezse <see cref="IsEnabled"/> false döner ve hiçbir kullanıcıya
/// şifre atanmaz — üretimde sabit varsayılan parola bilinçli olarak yoktur
/// (aynı kural <c>DbSeeder.SeedAdminUserAsync</c>'te de geçerli).
///
/// Hash TEK SEFER üretilip tüm kullanıcılarda paylaşılır. Gerekçe: bcrypt
/// maliyeti 12'de tek hash ~0,3 sn sürüyor; 130+ kullanıcı için kullanıcı başına
/// ayrı hash, açılışta 40 saniyelik bir gecikme demekti. Şifre zaten herkeste
/// AYNI olduğu için ayrı salt kullanmak ek bir gizlilik sağlamaz.
/// </summary>
public interface IDefaultUserPassword
{
    /// <summary>Varsayılan şifre yapılandırılmış mı (üretimde değer verilmezse false).</summary>
    bool IsEnabled { get; }

    /// <summary>Paylaşılan bcrypt hash'i; <see cref="IsEnabled"/> false ise null.</summary>
    string? Hash();
}

public sealed class DefaultUserPassword : IDefaultUserPassword
{
    /// <summary>Yalnızca Development ortamında geçerli varsayılan.</summary>
    public const string DevelopmentDefault = "Admin123";

    private readonly string? _plain;
    private readonly Lazy<string?> _hash;

    public DefaultUserPassword(
        IConfiguration configuration, IHostEnvironment environment, IPasswordHasher hasher)
    {
        _plain = configuration["Seed:DefaultUserPassword"];

        if (string.IsNullOrWhiteSpace(_plain) && environment.IsDevelopment())
            _plain = DevelopmentDefault;

        _hash = new Lazy<string?>(() =>
            string.IsNullOrWhiteSpace(_plain) ? null : hasher.Hash(_plain));
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_plain);

    public string? Hash() => _hash.Value;
}
