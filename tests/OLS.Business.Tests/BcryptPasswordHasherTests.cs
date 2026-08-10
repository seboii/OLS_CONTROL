using FluentAssertions;
using OLS.Business.Services.Authentication;

namespace OLS.Business.Tests;

/// <summary>
/// Bu oturumda canlı ortamda gerçek bir parola doğrulama arızası tespit edildi:
/// dev admin'in saklı hash'i "ChangeMe!Dev1" ile doğrulanamıyordu (kök neden veri
/// sürüklenmesiydi, hasher'ın kendisinde değil — bkz. TEST-RAPORU.md). Bu testler
/// hasher'ın hash/verify sözleşmesini kilitler: Hash() ile üretilen değer HER ZAMAN
/// Verify() ile aynı düz metne karşı doğrulanmalı.
/// </summary>
public sealed class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("ChangeMe!Dev1");

        _hasher.Verify("ChangeMe!Dev1", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("ChangeMe!Dev1");

        _hasher.Verify("yanlis-sifre", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_CalledTwiceWithSamePassword_ProducesDifferentHashes()
    {
        // Bcrypt her çağrıda rastgele salt kullanır; aynı şifre için aynı hash
        // çıkması salt üretiminin bozuk olduğuna işaret eder.
        var hash1 = _hasher.Hash("ayni-sifre");
        var hash2 = _hasher.Hash("ayni-sifre");

        hash1.Should().NotBe(hash2);
        _hasher.Verify("ayni-sifre", hash1).Should().BeTrue();
        _hasher.Verify("ayni-sifre", hash2).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithNullOrEmptyStoredHash_ReturnsFalseInsteadOfThrowing()
    {
        _hasher.Verify("herhangi-bir-sifre", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalseInsteadOfThrowing()
    {
        // Bozuk/eski formatta hash — SaltParseException yakalanıp false dönmeli
        // (BcryptPasswordHasher.Verify'daki try/catch).
        _hasher.Verify("herhangi-bir-sifre", "bu-gecerli-bir-bcrypt-hash-degil").Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesLaravelCompatibleBcryptFormat()
    {
        // olsold'un mevcut kullanıcı şifreleri "$2y$" önekli Laravel bcrypt hash'i;
        // BCrypt.Net "$2a$" üretir ama "$2y$" değerlerini de doğrulayabilir
        // (bkz. IPasswordHasher.cs XML doc'u). Burada YENİ üretilen hash'in
        // beklenen "$2a$" ailesinde ve standart 60 karakter uzunluğunda olduğunu
        // doğruluyoruz.
        var hash = _hasher.Hash("test");

        hash.Should().StartWith("$2a$");
        hash.Should().HaveLength(60);
    }
}
