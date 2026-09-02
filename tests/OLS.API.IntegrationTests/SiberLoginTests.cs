using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Services.Authentication;
using OLS.DataAccess.Context;
using OLS.DataAccess.Entities;
using OLS.DataAccess.Siber;

namespace OLS.API.IntegrationTests;

/// <summary>
/// SİBER HESABIYLA GİRİŞ.
///
/// Kullanıcılar zaten her gün Siber'e kendi kod+şifresiyle giriyor; ayrı bir
/// şifre ezberlemek zorunda kalmasınlar diye Siber şifresi de kabul edilir.
/// Şifre uygulamaya HİÇ GELMEZ: doğrulama Siber sunucusunda <c>PWDCOMPARE</c>
/// ile yapılır (bkz. <see cref="ISiberUserRepository"/>). Burada sınanan o sorgu
/// değil, KARAR MANTIĞI — hangi durumda Siber'e gidilir, hangi durumda gidilmez.
///
/// Test ortamında <c>ConnectionStrings:Siber</c> tanımsız olduğu için gerçek
/// depo <c>IsConfigured = false</c> döner; bu yüzden sahte depo kullanılır.
/// </summary>
[Collection("OlsApi")]
public sealed class SiberLoginTests
{
    private const string SiberPassword = "Siber!Sifre2026";
    private const string LocalPassword = "Yerel!Sifre2026";

    private readonly OlsApiFactory _factory;

    public SiberLoginTests(OlsApiFactory factory) => _factory = factory;

    /// <summary>Tek bir kodu tanıyan, o kod için tek bir şifreyi kabul eden sahte Siber.</summary>
    private sealed class FakeSiberUsers : ISiberUserRepository
    {
        private readonly string _code;
        private readonly SiberPasswordResult _resultForCorrectPassword;

        public FakeSiberUsers(
            string code,
            SiberPasswordResult resultForCorrectPassword = SiberPasswordResult.Success)
        {
            _code = code;
            _resultForCorrectPassword = resultForCorrectPassword;
        }

        public bool IsConfigured => true;

        /// <summary>Siber'e kaç kez sorulduğu — "boşuna gidilmiyor" iddiası için.</summary>
        public int CallCount { get; private set; }

        public Task<SiberPasswordResult> VerifyPasswordAsync(
            string userCode, string password, CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (!string.Equals(userCode, _code, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(SiberPasswordResult.UserNotFound);

            return Task.FromResult(password == SiberPassword
                ? _resultForCorrectPassword
                : SiberPasswordResult.WrongPassword);
        }
    }

    private static AuthService Build(
        IServiceScope scope, ISiberUserRepository siberUsers, bool enabled = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Siber:LoginEnabled"] = enabled ? "true" : "false",
            })
            .Build();

        return new AuthService(
            scope.ServiceProvider.GetRequiredService<OlsDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            scope.ServiceProvider.GetRequiredService<ITokenService>(),
            siberUsers,
            configuration);
    }

    /// <summary>Testin kendi kullanıcısı: yerel şifresi Siber şifresinden FARKLI.</summary>
    private static async Task<User> CreateUserAsync(
        OlsDbContext db, IPasswordHasher hasher, string? code, bool active = true)
    {
        var user = new User
        {
            Name = "Siber",
            Surname = "Giris",
            Email = $"siber-giris-{Guid.NewGuid():N}@test.local",
            Password = hasher.Hash(LocalPassword),
            Status = active,
            SiberCode = code,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task RemoveAsync(OlsDbContext db, params User[] users)
    {
        db.Users.RemoveRange(users);
        await db.SaveChangesAsync();
    }

    private static string NewCode() => "TESTKOD" + Random.Shared.Next(100_000, 999_999);

    [Fact]
    public async Task Login_WithSiberPassword_Succeeds_WhenLocalPasswordDoesNotMatch()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var code = NewCode();
        var user = await CreateUserAsync(db, hasher, code);

        try
        {
            var siber = new FakeSiberUsers(code);
            var auth = Build(scope, siber);

            // Yerel şifre hâlâ çalışır ve bunun için Siber'e HİÇ gidilmez.
            (await auth.LoginAsync(user.Email!, LocalPassword)).Outcome
                .Should().Be(LoginOutcome.Success);
            siber.CallCount.Should().Be(0, "yerel şifre tuttuğunda Siber'e gitmeye gerek yok");

            // Siber şifresi de kabul edilir.
            (await auth.LoginAsync(user.Email!, SiberPassword)).Outcome
                .Should().Be(LoginOutcome.Success);

            // Hiçbirine uymayan şifre reddedilir.
            (await auth.LoginAsync(user.Email!, "hicbiri-degil")).Outcome
                .Should().Be(LoginOutcome.InvalidCredentials);
        }
        finally
        {
            await RemoveAsync(db, user);
        }
    }

    /// <summary>
    /// Kullanıcı e-postasını değil, Siber kodunu yazarak da girebilmeli — Siber
    /// ekranında kullandığı kimlik bu. Arama harfe duyarsız.
    /// </summary>
    [Fact]
    public async Task Login_WithSiberUserCodeInsteadOfEmail_ResolvesUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var code = NewCode();
        var user = await CreateUserAsync(db, hasher, code);

        try
        {
            var auth = Build(scope, new FakeSiberUsers(code));

            (await auth.LoginAsync(code, SiberPassword)).Outcome.Should().Be(LoginOutcome.Success);
            (await auth.LoginAsync(code.ToLowerInvariant(), SiberPassword)).Outcome
                .Should().Be(LoginOutcome.Success);
            (await auth.LoginAsync(code, LocalPassword)).Outcome.Should().Be(LoginOutcome.Success);
        }
        finally
        {
            await RemoveAsync(db, user);
        }
    }

    /// <summary>
    /// REGRESYON — TÜRKÇE 'İ' YÜZÜNDEN KULLANICI BULUNAMIYORDU.
    ///
    /// Canlıda <c>FATİHT</c> doğru şifresiyle giremedi. Sebep: eşleştirme
    /// <c>u.SiberCode.ToLower() == trimmed.ToLower()</c> ile yapılıyordu ve
    /// <c>trimmed.ToLower()</c> .NET tarafında hesaplanıyordu — .NET'te
    /// <c>"İ".ToLower()</c> 'i' + BİRLEŞEN NOKTA (U+0307) üretiyor, yani iki kod
    /// noktası; PostgreSQL'in <c>lower('İ')</c>'si ise tek 'i'. İki taraf asla
    /// eşleşmiyordu. Canlıda İ içeren 5 kod var (MAVİLEI, NADİYEP, ALİHANT,
    /// CEMİLEA, FATİHT); 'ı' içeren kodlar da (VıACHESLAVK) SQL tarafında
    /// eşleşmiyordu çünkü <c>lower('I')='i'</c> ama <c>lower('ı')='ı'</c>.
    ///
    /// Artık iki tarafa da QueryableExtensions.NormalizeTurkish uygulanıyor.
    /// </summary>
    [Theory]
    [InlineData("FATİHT", "FATİHT")]   // kodun kendisi
    [InlineData("FATİHT", "FATIHT")]   // noktasız I ile yazılmış
    [InlineData("FATİHT", "fatiht")]   // küçük harf
    [InlineData("VıACHESLAVK", "VıACHESLAVK")]
    [InlineData("VıACHESLAVK", "VIACHESLAVK")] // noktasız ı yerine I
    [InlineData("MAVİLEI", "mavilei")]
    public async Task Login_WithTurkishDottedI_FindsUser(string storedCode, string typed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Paylaşılan test veritabanında çakışmasın diye koda ek bir sonek.
        var code = storedCode + Random.Shared.Next(100_000, 999_999);
        var suffix = code[storedCode.Length..];
        var user = await CreateUserAsync(db, hasher, code);

        try
        {
            var auth = Build(scope, new FakeSiberUsers(code));

            (await auth.LoginAsync(typed + suffix, SiberPassword)).Outcome
                .Should().Be(LoginOutcome.Success,
                    $"\"{storedCode}\" kodlu kullanıcı \"{typed}\" yazılarak da bulunmalı");
        }
        finally
        {
            await RemoveAsync(db, user);
        }
    }

    /// <summary>
    /// PASİF HESAP SİBER ŞİFRESİYLE DE AÇILMAZ. Siber'de engellenen kullanıcı
    /// senkronda yerelde pasife çekiliyor (bkz. SiberImportService); o kapının
    /// yeni giriş yoluyla delinmediği burada kilitleniyor.
    /// </summary>
    [Fact]
    public async Task Login_WithSiberPassword_IsRejected_WhenLocalUserIsInactive()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var code = NewCode();
        var user = await CreateUserAsync(db, hasher, code, active: false);

        try
        {
            var siber = new FakeSiberUsers(code);
            var auth = Build(scope, siber);

            (await auth.LoginAsync(user.Email!, SiberPassword)).Outcome
                .Should().Be(LoginOutcome.Inactive);
            siber.CallCount.Should().Be(0, "pasif hesapta şifre hiç sorulmamalı");
        }
        finally
        {
            await RemoveAsync(db, user);
        }
    }

    /// <summary>
    /// Siber'deki hesap engelli ya da şifresizse giriş YOK. Özellikle şifresiz
    /// hesap önemli: Siber'in kendi doğrulayıcısı böyle bir hesapta BOŞ şifreyi
    /// kabul ediyor, bizim yolumuz etmiyor.
    /// </summary>
    [Theory]
    [InlineData(SiberPasswordResult.Blocked)]
    [InlineData(SiberPasswordResult.NoPassword)]
    [InlineData(SiberPasswordResult.UserNotFound)]
    public async Task Login_IsRejected_WhenSiberRefuses(SiberPasswordResult refusal)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var code = NewCode();
        var user = await CreateUserAsync(db, hasher, code);

        try
        {
            var auth = Build(scope, new FakeSiberUsers(code, resultForCorrectPassword: refusal));

            (await auth.LoginAsync(user.Email!, SiberPassword)).Outcome
                .Should().Be(LoginOutcome.InvalidCredentials);
        }
        finally
        {
            await RemoveAsync(db, user);
        }
    }

    /// <summary>
    /// Siber'e gidilmeyen iki durum: özellik kapalı ya da kullanıcının Siber
    /// kodu yok. İkisinde de yerel şifre tek geçerli yol olarak kalır.
    /// </summary>
    [Fact]
    public async Task Siber_IsNotConsulted_WhenDisabledOrUserHasNoSiberCode()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OlsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var code = NewCode();
        var withCode = await CreateUserAsync(db, hasher, code);
        var withoutCode = await CreateUserAsync(db, hasher, code: null);

        try
        {
            var disabled = new FakeSiberUsers(code);
            var disabledAuth = Build(scope, disabled, enabled: false);

            (await disabledAuth.LoginAsync(withCode.Email!, SiberPassword)).Outcome
                .Should().Be(LoginOutcome.InvalidCredentials);
            (await disabledAuth.LoginAsync(withCode.Email!, LocalPassword)).Outcome
                .Should().Be(LoginOutcome.Success);
            disabled.CallCount.Should().Be(0, "özellik kapalıyken Siber'e hiç gidilmez");

            var enabled = new FakeSiberUsers(code);
            var enabledAuth = Build(scope, enabled);

            (await enabledAuth.LoginAsync(withoutCode.Email!, SiberPassword)).Outcome
                .Should().Be(LoginOutcome.InvalidCredentials);
            enabled.CallCount.Should().Be(0, "Siber kodu olmayan kullanıcı için sorulmaz");
        }
        finally
        {
            await RemoveAsync(db, withCode, withoutCode);
        }
    }
}
