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

        // Denetim kaydı interceptor'ı: yazma yolunu tek noktadan yakalar
        // (bkz. AuditSaveChangesInterceptor). Kullanıcı bağlamı yoksa (arka plan
        // senkronu) hiçbir şey yazmaz.
        services.AddScoped<Auditing.AuditSaveChangesInterceptor>();

        services.AddDbContext<OlsDbContext>((provider, options) =>
            options
                .UseNpgsql(postgres)
                .AddInterceptors(provider.GetRequiredService<Auditing.AuditSaveChangesInterceptor>()));

        // Siber bağlantısı isteğe bağlı: tanımlı değilse uygulama ayağa kalkar,
        // ancak Siber'e dokunan uçlar çağrıldığında anlamlı 503 döner.
        services.AddSingleton<ISiberConnectionFactory>(_ =>
            new SiberConnectionFactory(configuration.GetConnectionString("Siber")));

        services.AddScoped<ISiberArchiveRepository, SiberArchiveRepository>();
        services.AddSingleton<ISiberArchiveFileReader, SiberArchiveFileReader>();
        services.AddScoped<ISiberArchiveWriter, SiberArchiveWriter>();
        services.AddScoped<ISiberAccountRepository, SiberAccountRepository>();
        services.AddScoped<ISiberCarRepository, SiberCarRepository>();
        services.AddScoped<ISiberExpeditionRepository, SiberExpeditionRepository>();
        services.AddScoped<ISiberFinanceRepository, SiberFinanceRepository>();
        services.AddScoped<ISiberInvoiceWriter, SiberInvoiceWriter>();
        services.AddScoped<ISiberLoadRepository, SiberLoadRepository>();
        services.AddScoped<ISiberReferenceRepository, SiberReferenceRepository>();
        services.AddScoped<ISiberLoadMappingRepository, SiberLoadMappingRepository>();
        services.AddScoped<ISiberLoadReleaseRepository, SiberLoadReleaseRepository>();
        services.AddScoped<ISiberReservationRepository, SiberReservationRepository>();

        return services;
    }
}
