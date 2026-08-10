using System.Data;
using Microsoft.Data.SqlClient;

namespace OLS.DataAccess.Siber;

/// <summary>
/// Legacy Siber ERP'sinin MS SQL Server bağlantısı (olsold'daki <c>sqlsrv</c>).
///
/// olsold'da bu bağlantının host/kullanıcı/şifresi <c>config/database.php</c>
/// içinde sabit kodluydu ve sysadmin (<c>sa</c>) hesabı kullanılıyordu.
/// Burada yalnızca yapılandırmadan okunur; kaynak koda gömülmez.
/// </summary>
public interface ISiberConnectionFactory
{
    /// <summary>Yapılandırmada Siber bağlantısı tanımlı mı?</summary>
    bool IsConfigured { get; }

    /// <summary>Açılmış bir bağlantı döndürür. Çağıran dispose etmelidir.</summary>
    Task<IDbConnection> CreateOpenAsync(CancellationToken cancellationToken = default);
}

public sealed class SiberConnectionFactory : ISiberConnectionFactory
{
    private readonly string? _connectionString;

    public SiberConnectionFactory(string? connectionString) =>
        _connectionString = connectionString;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<IDbConnection> CreateOpenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Siber (MSSQL) bağlantısı yapılandırılmamış. " +
                "ConnectionStrings:Siber değerini .env / User Secrets üzerinden verin.");

        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
