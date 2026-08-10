using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OLS.DataAccess.Context;
using OLS.DataAccess.Siber;

namespace OLS.DataAccess;

public static class DependencyInjection
{
    /// <summary>
    /// DAL kayıtları. olsold'daki iki bağlantının karşılığı:
    ///   pgsql   -> OlsDbContext (EF Core / Npgsql)  — ana veritabanı
    ///   sqlsrv  -> ISiberConnectionFactory (Dapper) — legacy Siber ERP
    /// MongoDB (kurum içi mesajlaşma) kapsam dışı olduğu için bu solution'a
    /// hiç dahil edilmedi.
    /// </summary>
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres tanımlı değil.");

        services.AddDbContext<OlsDbContext>(options =>
            options.UseNpgsql(postgres));

        // Siber bağlantısı isteğe bağlı: tanımlı değilse uygulama ayağa kalkar,
        // ancak Siber'e dokunan uçlar çağrıldığında anlamlı 503 döner.
        services.AddSingleton<ISiberConnectionFactory>(_ =>
            new SiberConnectionFactory(configuration.GetConnectionString("Siber")));

        services.AddScoped<ISiberAccountRepository, SiberAccountRepository>();
        services.AddScoped<ISiberCarRepository, SiberCarRepository>();
        services.AddScoped<ISiberExpeditionRepository, SiberExpeditionRepository>();
        services.AddScoped<ISiberLoadRepository, SiberLoadRepository>();
        services.AddScoped<ISiberLoadMappingRepository, SiberLoadMappingRepository>();
        services.AddScoped<ISiberLoadReleaseRepository, SiberLoadReleaseRepository>();
        services.AddScoped<ISiberReservationRepository, SiberReservationRepository>();

        return services;
    }
}
