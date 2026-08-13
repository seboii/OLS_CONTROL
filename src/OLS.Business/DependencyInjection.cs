using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OLS.Business.Common;
using OLS.Business.Services.Accounts;
using OLS.Business.Services.Authentication;
using OLS.Business.Services.Authorization;
using OLS.Business.Services.Cars;
using OLS.Business.Services.Dashboard;
using OLS.Business.Services.Expeditions;
using OLS.Business.Services.Invoices;
using OLS.Business.Services.Loads;
using OLS.Business.Services.LoadTransfers;
using OLS.Business.Services.Lookups;
using OLS.Business.Services.Roles;
using OLS.Business.Services.TransferData;
using OLS.Business.Services.TransferSiber;
using OLS.Business.Services.Users;
using OLS.Business.Services.Website;

namespace OLS.Business;

public static class DependencyInjection
{
    /// <summary>
    /// BLL kayıtları — yalnızca 8 kapsam-içi modül (Müşteri, Teklif, Yük, Sefer,
    /// Fatura, Araç, Kullanıcılar, Destek Talebi) + zorunlu ortak altyapı + Dashboard
    /// (hazır tasarımda mevcut olduğu görülüp sonradan kapsama eklendi — bkz.
    /// DashboardService.cs, yalnızca GERÇEK verilerden hesaplanan agregasyonlar,
    /// olsold'daki tam raporlama modülünün portu DEĞİL).
    /// Kapsam dışı bırakılanlar (Accounting, Excel, Goals, Messages, olsold'un tam
    /// Reports modülü, TransitDeclarations, AI/OCR, Currency admin+TCMB,
    /// PDKS/WorkingTracking) olsnew'de mevcut ama buraya bilinçli olarak taşınmadı —
    /// bkz. docs/SECILI-MODUL-PARITE-MATRISI.md §0.
    ///
    /// KRİTİK YÖN DEĞİŞİKLİĞİ (bu oturumda): "TransferData/Siber ETL" bu listede
    /// DAHA ÖNCE kapsam dışı sayılıyordu. Kullanıcı Teklif→Yük zincirinin (BR-002..005)
    /// bu ortamda TAMAMEN SAHTE Siber ile eksiksiz çalıştığından emin olunmasını
    /// istedi — inceleme sonucu bunun, referans/tanım tablolarının (payment_types,
    /// work_types, departments, ...) hiçbirinde siber_id doldurulmadığı için
    /// yapısal olarak İMKANSIZ olduğu ortaya çıktı; bu alanları dolduracak ETL
    /// (olsold: TransferDataController) hiç portlanmamıştı. SiberImportService bu
    /// boşluğu kapatır — bkz. TESLIM-RAPORU.md "Kritik yön değişikliği #3".
    /// </summary>
    public static IServiceCollection AddBusiness(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Laravel'in Europe/Istanbul saat dilimi + timestamp-without-timezone
        // davranışını taşır. Ayrıntı: Common/IClock.cs
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserPermissionService, UserPermissionService>();
        services.AddScoped<IPermissionPageService, PermissionPageService>();

        services.AddScoped<IAccountService, AccountService>();

        services.AddScoped<ILoadService, LoadService>();
        services.AddScoped<ILoadWriteService, LoadWriteService>();
        services.AddScoped<ILoadAiImportService, LoadAiImportService>();
        services.AddScoped<ILoadFileService, LoadFileService>();
        services.AddScoped<IOfferEmailService, OfferEmailService>();

        services.AddScoped<ILoadTransferService, LoadTransferService>();
        services.AddScoped<ILoadTransferWriteService, LoadTransferWriteService>();
        services.AddScoped<ILoadTransferUpdateService, LoadTransferUpdateService>();
        services.AddScoped<ILoadTransferInvoiceItemService, LoadTransferInvoiceItemService>();

        services.AddScoped<ITransferSiberService, TransferSiberService>();
        services.AddScoped<ILoadReleaseService, LoadReleaseService>();

        services.AddScoped<IExpeditionService, ExpeditionService>();
        services.AddScoped<IExpeditionWriteService, ExpeditionWriteService>();
        services.AddScoped<IExpeditionLoadMappingService, ExpeditionLoadMappingService>();
        services.AddScoped<IMovementService, MovementService>();

        services.AddScoped<IInvoiceService, InvoiceService>();

        services.AddScoped<ICarService, CarService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProfileService, ProfileService>();

        services.AddScoped<IContactFormService, ContactFormService>();

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddScoped<ISiberImportService, SiberImportService>();

        // 23 referans/tanım modülü (27'den EinvoicePrefix hariç) tek generic kayıtla karşılanır.
        services.AddScoped(typeof(ILookupService<>), typeof(LookupService<>));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
