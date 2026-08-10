namespace OLS.Business.Services.Authentication;

public interface IPasswordHasher
{
    bool Verify(string plainPassword, string hash);
    string Hash(string plainPassword);
}

/// <summary>
/// Laravel'in <c>Hash::make</c> / <c>Hash::check</c> karşılığı.
///
/// Mevcut users tablosundaki şifreler Laravel bcrypt hash'i ("$2y$" önekli).
/// BCrypt.Net "$2y$" varyantını doğrular, bu yüzden kullanıcıların şifreleri
/// geçiş sonrası çalışmaya devam eder — şifre sıfırlama gerekmez.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>Laravel'in varsayılan bcrypt maliyeti 12.</summary>
    private const int WorkFactor = 12;

    public bool Verify(string plainPassword, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Bozuk/eski formatta hash — doğrulama başarısız sayılır.
            return false;
        }
    }

    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
}
