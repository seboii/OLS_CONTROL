using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Entities;

namespace OLS.DataAccess.Context;

/// <summary>
/// Scaffold edilen DbContext — yalnızca 8 kapsam-içi modül + zorunlu lookup/ortak
/// altyapı tablolarını içerir (57 entity: 56 scaffold + code-first RevokedToken).
/// Kapsam dışı 34 tablo (Accounting, Excel, Goals, Messages/Mongo, Reports,
/// TransferData/Siber-e-fatura, TransitDeclaration/Ordino/AuthorizationLetter,
/// PDKS/WorkingTracking, Oauth/PasswordReset/PersonalAccessToken Laravel kalıntıları)
/// bilinçli olarak buraya taşınmadı — bkz. docs/SECILI-MODUL-PARITE-MATRISI.md.
///
/// Kolon adları/tipleri olsnew'in production şemasından scaffold edilmiş halinden
/// birebir alınmıştır (tahmin edilmemiştir). Elle yapılan ek yapılandırma
/// OlsDbContext.Extensions.cs partial dosyasındadır.
/// </summary>
public partial class OlsDbContext : DbContext
{
    public OlsDbContext(DbContextOptions<OlsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AccountContactPerson> AccountContactPeople { get; set; }

    public virtual DbSet<AccountingPlan> AccountingPlans { get; set; }

    public virtual DbSet<AccountType> AccountTypes { get; set; }

    public virtual DbSet<AccountTypeMapping> AccountTypeMappings { get; set; }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<CarOwner> CarOwners { get; set; }

    public virtual DbSet<CarStatusType> CarStatusTypes { get; set; }

    public virtual DbSet<CarType> CarTypes { get; set; }

    public virtual DbSet<CaseType> CaseTypes { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Destination> Destinations { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<EvrakTuru> EvrakTurus { get; set; }

    public virtual DbSet<Expedition> Expeditions { get; set; }

    public virtual DbSet<ExpeditionFinanceRecord> ExpeditionFinanceRecords { get; set; }

    public virtual DbSet<ExpeditionLoadMapping> ExpeditionLoadMappings { get; set; }

    public virtual DbSet<ExpeditionMovement> ExpeditionMovements { get; set; }

    public virtual DbSet<ExpeditionStatus> ExpeditionStatuses { get; set; }

    public virtual DbSet<ExpeditionType> ExpeditionTypes { get; set; }

    public virtual DbSet<FinanceInvoice> FinanceInvoices { get; set; }

    public virtual DbSet<FinanceInvoiceLine> FinanceInvoiceLines { get; set; }

    public virtual DbSet<FinancePayment> FinancePayments { get; set; }

    public virtual DbSet<FinanceVoucher> FinanceVouchers { get; set; }

    public virtual DbSet<FinanceVoucherLine> FinanceVoucherLines { get; set; }

    public virtual DbSet<FinancialItem> FinancialItems { get; set; }

    public virtual DbSet<Instruction> Instructions { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceFooter> InvoiceFooters { get; set; }

    public virtual DbSet<InvoiceStatus> InvoiceStatuses { get; set; }

    public virtual DbSet<InvoiceType> InvoiceTypes { get; set; }

    public virtual DbSet<ItemType> ItemTypes { get; set; }

    public virtual DbSet<Load> Loads { get; set; }

    public virtual DbSet<LoadChargePerson> LoadChargePeople { get; set; }

    public virtual DbSet<LoadContent> LoadContents { get; set; }

    public virtual DbSet<LoadEmail> LoadEmails { get; set; }

    public virtual DbSet<LoadFile> LoadFiles { get; set; }

    public virtual DbSet<LoadFinancialItem> LoadFinancialItems { get; set; }

    public virtual DbSet<LoadMovement> LoadMovements { get; set; }

    public virtual DbSet<LoadStatusType> LoadStatusTypes { get; set; }

    public virtual DbSet<LoadTransfer> LoadTransfers { get; set; }

    public virtual DbSet<LoadTransferDeliveryMethod> LoadTransferDeliveryMethods { get; set; }

    public virtual DbSet<LoadTransferDocument> LoadTransferDocuments { get; set; }

    public virtual DbSet<LoadTransferInvoiceItem> LoadTransferInvoiceItems { get; set; }

    public virtual DbSet<LoadTransferInvoiceMap> LoadTransferInvoiceMaps { get; set; }

    public virtual DbSet<LoadTransferMovement> LoadTransferMovements { get; set; }

    public virtual DbSet<LoadTransferPackage> LoadTransferPackages { get; set; }

    public virtual DbSet<LoadTransferType> LoadTransferTypes { get; set; }

    public virtual DbSet<LoadingType> LoadingTypes { get; set; }

    public virtual DbSet<MovementType> MovementTypes { get; set; }

    public virtual DbSet<PaymentType> PaymentTypes { get; set; }

    public virtual DbSet<ProductType> ProductTypes { get; set; }

    public virtual DbSet<RomorkType> RomorkTypes { get; set; }

    public virtual DbSet<StatusType> StatusTypes { get; set; }

    public virtual DbSet<TaxOffice> TaxOffices { get; set; }

    public virtual DbSet<TransportType> TransportTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<AccountRepresentative> AccountRepresentatives { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<UserAccountMapping> UserAccountMappings { get; set; }

    public virtual DbSet<UserGoal> UserGoals { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<UserPermissionPage> UserPermissionPages { get; set; }

    public virtual DbSet<WebsiteContactForm> WebsiteContactForms { get; set; }

    public virtual DbSet<WorkType> WorkTypes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_pkey");

            entity.ToTable("accounts");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountingCode)
                .HasMaxLength(191)
                .HasColumnName("accounting_code");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Avatar)
                .HasMaxLength(191)
                .HasColumnName("avatar");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.ContactLanguage)
                .HasMaxLength(191)
                .HasColumnName("contact_language");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(191)
                .HasColumnName("contact_person");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Discount)
                .HasDefaultValue(0)
                .HasColumnName("discount");
            entity.Property(e => e.DistrictId).HasColumnName("district_id");
            entity.Property(e => e.Email)
                .HasMaxLength(191)
                .HasColumnName("email");
            entity.Property(e => e.IndividualPersonal)
                .HasMaxLength(191)
                .HasColumnName("individual_personal");
            entity.Property(e => e.InvoiceType).HasColumnName("invoice_type");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(191)
                .HasColumnName("phone");
            entity.Property(e => e.PhoneCountryId).HasColumnName("phone_country_id");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.TaxNumber)
                .HasMaxLength(191)
                .HasColumnName("tax_number");
            entity.Property(e => e.TaxOffice)
                .HasMaxLength(191)
                .HasColumnName("tax_office");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<AccountContactPerson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_contact_people_pkey");

            entity.ToTable("account_contact_people");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(191)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<AccountType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_types_pkey");

            entity.ToTable("account_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<AccountTypeMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_type_mappings_pkey");

            entity.ToTable("account_type_mappings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountTypeId).HasColumnName("account_type_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cars_pkey");

            entity.ToTable("cars");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.CarType).HasColumnName("car_type");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(191)
                .HasColumnName("customer_id");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.InCountry).HasColumnName("in_country");
            entity.Property(e => e.International).HasColumnName("international");
            entity.Property(e => e.Km).HasColumnName("km");
            entity.Property(e => e.Length).HasColumnName("length");
            entity.Property(e => e.PlateNumber)
                .HasMaxLength(191)
                .HasColumnName("plate_number");
            entity.Property(e => e.RomorkType).HasColumnName("romork_type");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleOwner).HasColumnName("vehicle_owner");
            entity.Property(e => e.VehicleStatus).HasColumnName("vehicle_status");
            entity.Property(e => e.Width).HasColumnName("width");
        });

        modelBuilder.Entity<CarOwner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("car_owners_pkey");

            entity.ToTable("car_owners");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdditionalCode)
                .HasMaxLength(191)
                .HasColumnName("additional_code");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.SpecialCode).HasColumnName("special_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CarStatusType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("car_status_types_pkey");

            entity.ToTable("car_status_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.SpecialCode).HasColumnName("special_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CarType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("car_types_pkey");

            entity.ToTable("car_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.SpecialCode).HasColumnName("special_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CaseType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("case_types_pkey");

            entity.ToTable("case_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Edikod)
                .HasMaxLength(191)
                .HasColumnName("edikod");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cities_pkey");

            entity.ToTable("cities");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CountryId)
                .HasMaxLength(191)
                .HasColumnName("country_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(191)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("countries_pkey");

            entity.ToTable("countries");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(191)
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Flag)
                .HasMaxLength(191)
                .HasColumnName("flag");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.PhoneCode)
                .HasMaxLength(191)
                .HasColumnName("phone_code");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(191)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("currencies_pkey");

            entity.ToTable("currencies");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Symbol)
                .HasMaxLength(191)
                .HasColumnName("symbol");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("departments_pkey");

            entity.ToTable("departments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("destinations_pkey");

            entity.ToTable("destinations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DistrictId).HasColumnName("district_id");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.City).WithMany(p => p.Destinations)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("destinations_city_id_foreign");

            entity.HasOne(d => d.Country).WithMany(p => p.Destinations)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("destinations_country_id_foreign");

            entity.HasOne(d => d.District).WithMany(p => p.Destinations)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("destinations_district_id_foreign");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("districts_pkey");

            entity.ToTable("districts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CityId)
                .HasMaxLength(191)
                .HasColumnName("city_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(191)
                .HasColumnName("slug");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<EvrakTuru>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("evrak_turus_pkey");

            entity.ToTable("evrak_turus");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Expedition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expeditions_pkey");

            entity.ToTable("expeditions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.CarExitDate).HasColumnName("car_exit_date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.EndCityId).HasColumnName("end_city_id");
            entity.Property(e => e.ExpeditionId)
                .HasMaxLength(191)
                .HasColumnName("expedition_id");
            entity.Property(e => e.ExpeditionNumber)
                .HasMaxLength(191)
                .HasColumnName("expedition_number");
            entity.Property(e => e.ExpeditionTypeId).HasColumnName("expedition_type_id");
            entity.Property(e => e.LoadCityId).HasColumnName("load_city_id");
            entity.Property(e => e.LoadingDate).HasColumnName("loading_date");
            entity.Property(e => e.RegistrationLoginDate).HasColumnName("registration_login_date");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.RomorkId).HasColumnName("romork_id");
            entity.Property(e => e.SeferId)
                .HasMaxLength(191)
                .HasColumnName("sefer_id");
            entity.Property(e => e.StartCityId).HasColumnName("start_city_id");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkType).HasColumnName("work_type");
            entity.Property(e => e.YearWeek)
                .HasMaxLength(191)
                .HasColumnName("year_week");
        });

        modelBuilder.Entity<ExpeditionLoadMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expedition_load_mappings_pkey");

            entity.ToTable("expedition_load_mappings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.ExpeditionId)
                .HasMaxLength(191)
                .HasColumnName("expedition_id");
            entity.Property(e => e.LoadTransferId)
                .HasMaxLength(191)
                .HasColumnName("load_transfer_id");
            entity.Property(e => e.RomorkId)
                .HasMaxLength(191)
                .HasColumnName("romork_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UploadUnload).HasColumnName("upload_unload");
            entity.Property(e => e.YerId)
                .HasMaxLength(191)
                .HasColumnName("yer_id");
            entity.Property(e => e.Yukaktarmaid)
                .HasMaxLength(191)
                .HasColumnName("yukaktarmaid");
        });

        modelBuilder.Entity<ExpeditionFinanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expedition_finance_records_pkey");

            entity.ToTable("expedition_finance_records");

            entity.HasIndex(e => e.SiberId).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SiberId).HasColumnName("siber_id");
            entity.Property(e => e.ExpeditionId).HasColumnName("expedition_id");
            entity.Property(e => e.LoadTransferId).HasColumnName("load_transfer_id");
            entity.Property(e => e.ExpeditionNumber).HasColumnName("expedition_number");
            entity.Property(e => e.LoadNumber).HasColumnName("load_number");
            entity.Property(e => e.ItemName).HasColumnName("item_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DocumentDate).HasColumnName("document_date");
            entity.Property(e => e.ExpectedIncomeTry).HasColumnType("numeric(18,2)").HasColumnName("expected_income_try");
            entity.Property(e => e.ExpectedExpenseTry).HasColumnType("numeric(18,2)").HasColumnName("expected_expense_try");
            entity.Property(e => e.RealizedIncomeTry).HasColumnType("numeric(18,2)").HasColumnName("realized_income_try");
            entity.Property(e => e.RealizedExpenseTry).HasColumnType("numeric(18,2)").HasColumnName("realized_expense_try");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Expedition).WithMany()
                .HasForeignKey(d => d.ExpeditionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("expedition_finance_records_expedition_id_foreign");

            entity.HasOne(d => d.LoadTransfer).WithMany()
                .HasForeignKey(d => d.LoadTransferId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("expedition_finance_records_load_transfer_id_foreign");
        });

        modelBuilder.Entity<ExpeditionMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expedition_movements_pkey");

            entity.ToTable("expedition_movements");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DestinationId).HasColumnName("destination_id");
            entity.Property(e => e.ExpeditionId).HasColumnName("expedition_id");
            entity.Property(e => e.ExpeditionStatusId).HasColumnName("expedition_status_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Destination).WithMany(p => p.ExpeditionMovements)
                .HasForeignKey(d => d.DestinationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("expedition_movements_destination_id_foreign");

            entity.HasOne(d => d.Expedition).WithMany(p => p.ExpeditionMovements)
                .HasForeignKey(d => d.ExpeditionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("expedition_movements_expedition_id_foreign");

            entity.HasOne(d => d.ExpeditionStatus).WithMany(p => p.ExpeditionMovements)
                .HasForeignKey(d => d.ExpeditionStatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("expedition_movements_expedition_status_id_foreign");

            entity.HasOne(d => d.User).WithMany(p => p.ExpeditionMovements)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("expedition_movements_user_id_foreign");
        });

        modelBuilder.Entity<ExpeditionStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expedition_statuses_pkey");

            entity.ToTable("expedition_statuses");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpeditionStatusId).HasColumnName("expedition_status_id");
            entity.Property(e => e.LoadStatusId).HasColumnName("load_status_id");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.OrderNumber).HasColumnName("order_number");
            entity.Property(e => e.Rowguid)
                .HasMaxLength(191)
                .HasColumnName("rowguid");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ExpeditionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expedition_types_pkey");

            entity.ToTable("expedition_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");
            entity.ToTable("audit_logs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserName).HasMaxLength(191).HasColumnName("user_name");
            entity.Property(e => e.Action).HasMaxLength(32).HasColumnName("action");
            entity.Property(e => e.EntityType).HasMaxLength(64).HasColumnName("entity_type");
            entity.Property(e => e.EntityId).HasMaxLength(64).HasColumnName("entity_id");
            entity.Property(e => e.EntityLabel).HasMaxLength(191).HasColumnName("entity_label");
            entity.Property(e => e.Changes).HasColumnType("text").HasColumnName("changes");
            entity.Property(e => e.IpAddress).HasMaxLength(64).HasColumnName("ip_address");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");

            // Denetim ekranı "en son ne oldu" ile açılıyor ve etikete göre arıyor.
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EntityLabel);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");
            entity.ToTable("roles");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(191).HasColumnName("name");
            entity.Property(e => e.Slug).HasMaxLength(191).HasColumnName("slug");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");

            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_permissions_pkey");
            entity.ToTable("role_permissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserPermissionPageId).HasColumnName("user_permission_page_id");
            entity.Property(e => e.Read).HasColumnName("read");
            entity.Property(e => e.Create).HasColumnName("create");
            entity.Property(e => e.Update).HasColumnName("update");
            entity.Property(e => e.Delete).HasColumnName("delete");

            entity.HasIndex(e => new { e.RoleId, e.UserPermissionPageId }).IsUnique();

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("role_permissions_role_id_foreign");
        });

        // ------------------------------------------------------------------
        // Muhasebe / finans — Siber sfy_* tablolarının yerel aynası.
        //
        // Tutarlar numeric(18,2): Siber'de tek cari bakiyesi milyarlı
        // rakamlara çıkıyor, projedeki (10,2) ölçeği taşımaz. Kurlar (18,6).
        //
        // BAKİYE SAKLANMIYOR. Cari bakiye her sorguda fiş satırlarından
        // toplanır; kolonda tutulan bakiye ilk kaçan kayıtta sessizce yanlışa
        // düşer.
        // ------------------------------------------------------------------
        modelBuilder.Entity<AccountingPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounting_plans_pkey");

            entity.ToTable("accounting_plans");

            // Hesap kodu TEKİL DEĞİL: Siber'de aynı kodun birden fazla satırı
            // var (3.938 satır, eşleşmede 49.442/49.438 fazlası buradan).
            entity.HasIndex(e => e.Code, "accounting_plans_code_index");
            entity.HasIndex(e => e.SiberId, "accounting_plans_siber_id_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasMaxLength(64).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.IsPassive).HasColumnName("is_passive");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.Name).HasMaxLength(191).HasColumnName("name");
            entity.Property(e => e.Name2).HasMaxLength(191).HasColumnName("name2");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.SiberId).HasMaxLength(64).HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");
        });

        modelBuilder.Entity<FinanceInvoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("finance_invoices_pkey");

            entity.ToTable("finance_invoices");

            entity.HasIndex(e => e.SiberId, "finance_invoices_siber_id_unique").IsUnique();
            entity.HasIndex(e => e.AccountId, "finance_invoices_account_id_index");
            entity.HasIndex(e => e.DueDate, "finance_invoices_due_date_index");
            entity.HasIndex(e => e.LoadTransferId, "finance_invoices_load_transfer_id_index");
            entity.HasIndex(e => new { e.ModuleCode, e.ModuleId }, "finance_invoices_module_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountName).HasMaxLength(255).HasColumnName("account_name");
            entity.Property(e => e.Amount).HasPrecision(18, 2).HasColumnName("amount");
            entity.Property(e => e.AmountTl).HasPrecision(18, 2).HasColumnName("amount_tl");
            entity.Property(e => e.ApprovalDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("approval_date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode).HasMaxLength(8).HasColumnName("currency_code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Direction).HasMaxLength(2).HasColumnName("direction");
            entity.Property(e => e.DocumentNumber).HasMaxLength(64).HasColumnName("document_number");
            entity.Property(e => e.DueDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("due_date");
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6).HasColumnName("exchange_rate");
            entity.Property(e => e.InvoiceDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("invoice_date");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(64).HasColumnName("invoice_number");
            entity.Property(e => e.InvoiceSeries).HasMaxLength(32).HasColumnName("invoice_series");
            entity.Property(e => e.IsApproved).HasColumnName("is_approved");
            entity.Property(e => e.LoadTransferId).HasColumnName("load_transfer_id");
            entity.Property(e => e.ModuleCode).HasMaxLength(16).HasColumnName("module_code");
            entity.Property(e => e.ModuleId).HasMaxLength(64).HasColumnName("module_id");
            entity.Property(e => e.SiberAccountId).HasMaxLength(64).HasColumnName("siber_account_id");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.SiberCreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("siber_created_at");
            entity.Property(e => e.SiberCreatedBy).HasMaxLength(128).HasColumnName("siber_created_by");
            entity.Property(e => e.SiberId).HasMaxLength(64).HasColumnName("siber_id");
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            entity.Property(e => e.TaxAmountTl).HasPrecision(18, 2).HasColumnName("tax_amount_tl");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2).HasColumnName("total_amount");
            entity.Property(e => e.TotalAmountTl).HasPrecision(18, 2).HasColumnName("total_amount_tl");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");

            // SetNull: Siber'den gelen fatura, carisi yerelde silinse bile
            // muhasebe kaydı olarak durmalı.
            entity.HasOne(d => d.Account).WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("finance_invoices_account_id_foreign");

            entity.HasOne(d => d.LoadTransfer).WithMany()
                .HasForeignKey(d => d.LoadTransferId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("finance_invoices_load_transfer_id_foreign");
        });

        modelBuilder.Entity<FinanceInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("finance_invoice_lines_pkey");

            entity.ToTable("finance_invoice_lines");

            entity.HasIndex(e => e.SiberId, "finance_invoice_lines_siber_id_unique").IsUnique();
            entity.HasIndex(e => e.FinanceInvoiceId, "finance_invoice_lines_invoice_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasPrecision(18, 2).HasColumnName("amount");
            entity.Property(e => e.AmountTl).HasPrecision(18, 2).HasColumnName("amount_tl");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode).HasMaxLength(8).HasColumnName("currency_code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DocumentDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("document_date");
            entity.Property(e => e.DocumentNumber).HasMaxLength(64).HasColumnName("document_number");
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6).HasColumnName("exchange_rate");
            entity.Property(e => e.FinanceInvoiceId).HasColumnName("finance_invoice_id");
            entity.Property(e => e.FinancialItemId).HasMaxLength(64).HasColumnName("financial_item_id");
            entity.Property(e => e.FinancialItemName).HasMaxLength(255).HasColumnName("financial_item_name");
            entity.Property(e => e.Quantity).HasPrecision(18, 4).HasColumnName("quantity");
            entity.Property(e => e.SiberId).HasMaxLength(64).HasColumnName("siber_id");
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            entity.Property(e => e.TaxAmountTl).HasPrecision(18, 2).HasColumnName("tax_amount_tl");
            entity.Property(e => e.TaxRate).HasPrecision(9, 4).HasColumnName("tax_rate");
            entity.Property(e => e.UnitPrice).HasPrecision(18, 4).HasColumnName("unit_price");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");

            entity.HasOne(d => d.FinanceInvoice).WithMany(p => p.Lines)
                .HasForeignKey(d => d.FinanceInvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("finance_invoice_lines_invoice_id_foreign");
        });

        modelBuilder.Entity<FinancePayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("finance_payments_pkey");

            entity.ToTable("finance_payments");

            entity.HasIndex(e => e.SiberId, "finance_payments_siber_id_unique").IsUnique();
            entity.HasIndex(e => e.DebitAccountId, "finance_payments_debit_account_id_index");
            entity.HasIndex(e => e.CreditAccountId, "finance_payments_credit_account_id_index");
            entity.HasIndex(e => e.ReceiptDate, "finance_payments_receipt_date_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasPrecision(18, 2).HasColumnName("amount");
            entity.Property(e => e.AmountTl).HasPrecision(18, 2).HasColumnName("amount_tl");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.CreditAccountCode).HasMaxLength(64).HasColumnName("credit_account_code");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.CreditName).HasMaxLength(255).HasColumnName("credit_name");
            entity.Property(e => e.CurrencyCode).HasMaxLength(8).HasColumnName("currency_code");
            entity.Property(e => e.DebitAccountCode).HasMaxLength(64).HasColumnName("debit_account_code");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.DebitName).HasMaxLength(255).HasColumnName("debit_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("due_date");
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6).HasColumnName("exchange_rate");
            entity.Property(e => e.ModuleCode).HasMaxLength(16).HasColumnName("module_code");
            entity.Property(e => e.ModuleId).HasMaxLength(64).HasColumnName("module_id");
            entity.Property(e => e.ReceiptDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("receipt_date");
            entity.Property(e => e.ReceiptNumber).HasMaxLength(64).HasColumnName("receipt_number");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.SiberCreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("siber_created_at");
            entity.Property(e => e.SiberCreatedBy).HasMaxLength(128).HasColumnName("siber_created_by");
            entity.Property(e => e.SiberCreditAccountId).HasMaxLength(64).HasColumnName("siber_credit_account_id");
            entity.Property(e => e.SiberDebitAccountId).HasMaxLength(64).HasColumnName("siber_debit_account_id");
            entity.Property(e => e.SiberId).HasMaxLength(64).HasColumnName("siber_id");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");

            entity.HasOne(d => d.DebitAccount).WithMany()
                .HasForeignKey(d => d.DebitAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("finance_payments_debit_account_id_foreign");

            entity.HasOne(d => d.CreditAccount).WithMany()
                .HasForeignKey(d => d.CreditAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("finance_payments_credit_account_id_foreign");
        });

        modelBuilder.Entity<FinanceVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("finance_vouchers_pkey");

            entity.ToTable("finance_vouchers");

            entity.HasIndex(e => e.SiberId, "finance_vouchers_siber_id_unique").IsUnique();
            entity.HasIndex(e => e.VoucherDate, "finance_vouchers_voucher_date_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode).HasMaxLength(8).HasColumnName("currency_code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DocumentDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("document_date");
            entity.Property(e => e.DocumentNumber).HasMaxLength(64).HasColumnName("document_number");
            entity.Property(e => e.IsChecked).HasColumnName("is_checked");
            entity.Property(e => e.JournalNumber).HasColumnName("journal_number");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.SiberCreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("siber_created_at");
            entity.Property(e => e.SiberCreatedBy).HasMaxLength(128).HasColumnName("siber_created_by");
            entity.Property(e => e.SiberId).HasMaxLength(64).HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");
            entity.Property(e => e.VoucherDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("voucher_date");
            entity.Property(e => e.VoucherNumber).HasColumnName("voucher_number");
            entity.Property(e => e.VoucherType).HasColumnName("voucher_type");
        });

        modelBuilder.Entity<FinanceVoucherLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("finance_voucher_lines_pkey");

            entity.ToTable("finance_voucher_lines");

            entity.HasIndex(e => e.SiberId, "finance_voucher_lines_siber_id_unique").IsUnique();
            entity.HasIndex(e => e.FinanceVoucherId, "finance_voucher_lines_voucher_id_index");
            entity.HasIndex(e => e.AccountCode, "finance_voucher_lines_account_code_index");
            entity.HasIndex(e => e.SourceId, "finance_voucher_lines_source_id_index");
            // Cari ekstre bu indeksten okunuyor: (cari, tarih) aralığı.
            entity.HasIndex(e => new { e.AccountId, e.DocumentDate },
                "finance_voucher_lines_account_date_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCode).HasMaxLength(64).HasColumnName("account_code");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("created_at");
            entity.Property(e => e.Credit).HasPrecision(18, 2).HasColumnName("credit");
            entity.Property(e => e.CreditFx).HasPrecision(18, 2).HasColumnName("credit_fx");
            entity.Property(e => e.CurrencyCode).HasMaxLength(8).HasColumnName("currency_code");
            entity.Property(e => e.Debit).HasPrecision(18, 2).HasColumnName("debit");
            entity.Property(e => e.DebitFx).HasPrecision(18, 2).HasColumnName("debit_fx");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DocumentDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("document_date");
            entity.Property(e => e.DocumentNumber).HasMaxLength(64).HasColumnName("document_number");
            entity.Property(e => e.DueDate)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("due_date");
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 6).HasColumnName("exchange_rate");
            entity.Property(e => e.FinanceVoucherId).HasColumnName("finance_voucher_id");
            entity.Property(e => e.LineNumber).HasColumnName("line_number");
            entity.Property(e => e.SiberAccountId).HasMaxLength(64).HasColumnName("siber_account_id");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.SiberId).HasMaxLength(64).HasColumnName("siber_id");
            entity.Property(e => e.SourceId).HasMaxLength(64).HasColumnName("source_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone").HasColumnName("updated_at");

            entity.HasOne(d => d.FinanceVoucher).WithMany(p => p.Lines)
                .HasForeignKey(d => d.FinanceVoucherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("finance_voucher_lines_voucher_id_foreign");

            entity.HasOne(d => d.Account).WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("finance_voucher_lines_account_id_foreign");
        });

        modelBuilder.Entity<FinancialItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("financial_items_pkey");

            entity.ToTable("financial_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DefaultAccountId).HasColumnName("default_account_id");
            entity.Property(e => e.DefaultAccountName).HasMaxLength(255).HasColumnName("default_account_name");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Instruction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("instructions_pkey");

            entity.ToTable("instructions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoices_pkey");

            entity.ToTable("invoices");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.BoxType)
                .HasComment("0 = Inbox, 1 = Outbox")
                .HasColumnName("box_type");
            entity.Property(e => e.CommercialType)
                .HasComment("0 = Temel Fatura, 1 = Ticari Fatura")
                .HasColumnName("commercial_type");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByIntegration)
                .HasDefaultValue(false)
                .HasComment("0 = Manuel, 1 = Integration")
                .HasColumnName("created_by_integration");
            entity.Property(e => e.DocumentCurrencyCode)
                .HasMaxLength(191)
                .HasColumnName("document_currency_code");
            entity.Property(e => e.DocumentId)
                .HasMaxLength(191)
                .HasColumnName("document_id");
            entity.Property(e => e.EnvelopeIdentifier)
                .HasMaxLength(191)
                .HasColumnName("envelope_identifier");
            entity.Property(e => e.EnvelopeStatusCode).HasColumnName("envelope_status_code");
            entity.Property(e => e.ExchangeDate).HasColumnName("exchange_date");
            entity.Property(e => e.ExchangeRate)
                .HasPrecision(13, 5)
                .HasColumnName("exchange_rate");
            entity.Property(e => e.InvoiceCreateDate)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("invoice_create_date");
            entity.Property(e => e.InvoiceExecutionDate)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("invoice_execution_date");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(191)
                .HasColumnName("invoice_id");
            entity.Property(e => e.InvoiceStatusId).HasColumnName("invoice_status_id");
            entity.Property(e => e.InvoiceTypeId).HasColumnName("invoice_type_id");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.OrderDocumentId)
                .HasMaxLength(191)
                .HasColumnName("order_document_id");
            entity.Property(e => e.PayableAmount)
                .HasPrecision(15, 2)
                .HasColumnName("payable_amount");
            entity.Property(e => e.TargetIdentityNo)
                .HasMaxLength(191)
                .HasColumnName("target_identity_no");
            entity.Property(e => e.TargetTitle)
                .HasMaxLength(191)
                .HasColumnName("target_title");
            entity.Property(e => e.TaxAmount)
                .HasPrecision(15, 2)
                .HasColumnName("tax_amount");
            entity.Property(e => e.TaxExclusiveAmount)
                .HasPrecision(15, 2)
                .HasColumnName("tax_exclusive_amount");
            entity.Property(e => e.TaxRate)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'0'::numeric")
                .HasColumnName("tax_rate");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Account).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("invoices_account_id_foreign");

            entity.HasOne(d => d.InvoiceStatus).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.InvoiceStatusId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("invoices_invoice_status_id_foreign");

            entity.HasOne(d => d.InvoiceType).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.InvoiceTypeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("invoices_invoice_type_id_foreign");
        });

        modelBuilder.Entity<InvoiceFooter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_footers_pkey");

            entity.ToTable("invoice_footers");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceFooters)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("invoice_footers_invoice_id_foreign");
        });

        modelBuilder.Entity<InvoiceStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_statuses_pkey");

            entity.ToTable("invoice_statuses");

            entity.HasIndex(e => e.Code, "invoice_statuses_code_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EnumValue)
                .HasMaxLength(191)
                .HasColumnName("enum_value");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<InvoiceType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_types_pkey");

            entity.ToTable("invoice_types");

            entity.HasIndex(e => e.Code, "invoice_types_code_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ItemType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("item_types_pkey");

            entity.ToTable("item_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Load>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("loads_pkey");

            entity.ToTable("loads");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.CompanyPayFreightId).HasColumnName("company_pay_freight_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.DepartureCountryId).HasColumnName("departure_country_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FinalTransportationByUs)
                .HasDefaultValue(0)
                .HasColumnName("final_transportation_by_us");
            entity.Property(e => e.FrontTransportationByUs)
                .HasDefaultValue(0)
                .HasColumnName("front_transportation_by_us");
            entity.Property(e => e.InstructionId).HasColumnName("instruction_id");
            entity.Property(e => e.LoadNumber)
                .HasMaxLength(191)
                .HasColumnName("load_number");
            entity.Property(e => e.LoadTransferTypeId).HasColumnName("load_transfer_type_id");
            entity.Property(e => e.LoadingTypeId).HasColumnName("loading_type_id");
            entity.Property(e => e.MailId)
                .HasMaxLength(191)
                .HasColumnName("mail_id");
            entity.Property(e => e.ApprovalDate).HasColumnName("approval_date");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.MarketingNotificationDate).HasColumnName("marketing_notification_date");
            entity.Property(e => e.OfferDate).HasColumnName("offer_date");
            entity.Property(e => e.OfferValidityDate).HasColumnName("offer_validity_date");
            entity.Property(e => e.PayerCompany)
                .HasMaxLength(191)
                .HasColumnName("payer_company");
            entity.Property(e => e.PaymentTypeId).HasColumnName("payment_type_id");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.ReservationNumber)
                .HasMaxLength(191)
                .HasColumnName("reservation_number");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.RomorkTypeId).HasColumnName("romork_type_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.StatusTypeId).HasColumnName("status_type_id");
            entity.Property(e => e.TargetCountryId).HasColumnName("target_country_id");
            entity.Property(e => e.TransferToSiber)
                .HasDefaultValue(0)
                .HasColumnName("transfer_to_siber");
            entity.Property(e => e.TransitCountryId).HasColumnName("transit_country_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.WayOfWorking)
                .HasDefaultValue(0)
                .HasColumnName("way_of_working");
            entity.Property(e => e.WorkTypeId).HasColumnName("work_type_id");
        });

        modelBuilder.Entity<LoadChargePerson>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_charge_people_pkey");

            entity.ToTable("load_charge_people");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserType)
                .HasComment("1: Operasyon Yetkilisi, 2: Satış Temsilcisi")
                .HasColumnName("user_type");
        });

        modelBuilder.Entity<LoadContent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_contents_pkey");

            entity.ToTable("load_contents");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CaseTypeId).HasColumnName("case_type_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GrossWeight)
                .HasPrecision(10, 2)
                .HasColumnName("gross_weight");
            entity.Property(e => e.Height)
                .HasPrecision(10, 2)
                .HasColumnName("height");
            entity.Property(e => e.Lademeter)
                .HasPrecision(10, 2)
                .HasColumnName("lademeter");
            entity.Property(e => e.Length)
                .HasPrecision(10, 2)
                .HasColumnName("length");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.NetWeight)
                .HasPrecision(10, 2)
                .HasColumnName("net_weight");
            entity.Property(e => e.ProductTypeId).HasColumnName("product_type_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Stackable).HasColumnName("stackable");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Volume)
                .HasPrecision(10, 2)
                .HasColumnName("volume");
            entity.Property(e => e.Width)
                .HasPrecision(10, 2)
                .HasColumnName("width");

            entity.HasOne(d => d.Load).WithMany(p => p.LoadContents)
                .HasForeignKey(d => d.LoadId)
                .HasConstraintName("load_contents_load_id_foreign");
        });

        modelBuilder.Entity<LoadEmail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_emails_pkey");

            entity.ToTable("load_emails");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(191)
                .HasColumnName("email");
            entity.Property(e => e.Key)
                .HasMaxLength(191)
                .HasColumnName("key");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<LoadFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_files_pkey");

            entity.ToTable("load_files");

            entity.Property(e => e.LoadTransferId).HasColumnName("load_transfer_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.File)
                .HasMaxLength(191)
                .HasColumnName("file");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.MimeType)
                .HasMaxLength(191)
                .HasColumnName("mime_type");
            entity.Property(e => e.OrgName)
                .HasMaxLength(191)
                .HasColumnName("org_name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<LoadFinancialItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_financial_items_pkey");

            entity.ToTable("load_financial_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Buysell).HasColumnName("buysell");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency).HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Item).HasColumnName("item");
            entity.Property(e => e.ItemTypeId).HasColumnName("item_type_id");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.NetPrice)
                .HasPrecision(10, 2)
                .HasColumnName("net_price");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TaxPrice)
                .HasPrecision(10, 2)
                .HasColumnName("tax_price");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.TransportTypeId).HasColumnName("transport_type_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Load).WithMany(p => p.LoadFinancialItems)
                .HasForeignKey(d => d.LoadId)
                .HasConstraintName("load_financial_items_load_id_foreign");
        });

        modelBuilder.Entity<LoadMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_movements_pkey");

            entity.ToTable("load_movements");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.MovementTypeId).HasColumnName("movement_type_id");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Load).WithMany(p => p.LoadMovements)
                .HasForeignKey(d => d.LoadId)
                .HasConstraintName("load_movements_load_id_foreign");
        });

        modelBuilder.Entity<LoadStatusType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_status_types_pkey");

            entity.ToTable("load_status_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.LoadStatusId).HasColumnName("load_status_id");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.OrderNo).HasColumnName("order_no");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<LoadTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfers_pkey");

            entity.ToTable("load_transfers");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.CarHeight)
                .HasPrecision(10, 2)
                .HasColumnName("car_height");
            entity.Property(e => e.CmrWaiting).HasColumnName("cmr_waiting");
            entity.Property(e => e.ConnectedLoadNumber)
                .HasMaxLength(191)
                .HasColumnName("connected_load_number");
            entity.Property(e => e.ConnectedLoadNumberWorkType)
                .HasMaxLength(191)
                .HasColumnName("connected_load_number_work_type");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerRepresentativeName).HasColumnName("customer_representative_name");
            entity.Property(e => e.DateOfReceiptCustomer).HasColumnName("date_of_receipt_customer");
            entity.Property(e => e.DeliveryMethodId).HasColumnName("delivery_method_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.DepartureCountryId)
                .HasMaxLength(191)
                .HasColumnName("departure_country_id");
            entity.Property(e => e.FcrWaiting).HasColumnName("fcr_waiting");
            entity.Property(e => e.FinalTransportationByUs).HasColumnName("final_transportation_by_us");
            entity.Property(e => e.FrontTransportationByUs).HasColumnName("front_transportation_by_us");
            entity.Property(e => e.InTail).HasColumnName("in_tail");
            entity.Property(e => e.InTruck).HasColumnName("in_truck");
            entity.Property(e => e.InstructionArrivalDate).HasColumnName("instruction_arrival_date");
            entity.Property(e => e.InstructionId).HasColumnName("instruction_id");
            entity.Property(e => e.LoadNumber)
                .HasMaxLength(191)
                .HasColumnName("load_number");
            entity.Property(e => e.LoadNumberWorkType)
                .HasMaxLength(191)
                .HasColumnName("load_number_work_type");
            entity.Property(e => e.LoadStatusId).HasColumnName("load_status_id");
            entity.Property(e => e.LoadTransferId)
                .HasMaxLength(191)
                .HasColumnName("load_transfer_id");
            entity.Property(e => e.LoadTransferTypeId).HasColumnName("load_transfer_type_id");
            entity.Property(e => e.LoadTypeId).HasColumnName("load_type_id");
            entity.Property(e => e.LoadingContinent)
                .HasMaxLength(191)
                .HasColumnName("loading_continent");
            entity.Property(e => e.OperationDepartmentId).HasColumnName("operation_department_id");
            entity.Property(e => e.PaymentTypeId).HasColumnName("payment_type_id");
            entity.Property(e => e.ReadinessDate).HasColumnName("readiness_date");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.RequestArrivalDate).HasColumnName("request_arrival_date");
            entity.Property(e => e.RomorkTypeId).HasColumnName("romork_type_id");
            entity.Property(e => e.SalesRepCode).HasColumnName("sales_rep_code");
            entity.Property(e => e.SecondCustomerRepresentativeName).HasColumnName("second_customer_representative_name");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.TargetCountryId)
                .HasMaxLength(191)
                .HasColumnName("target_country_id");
            entity.Property(e => e.TotalCap)
                .HasPrecision(10, 2)
                .HasColumnName("total_cap");
            entity.Property(e => e.TotalGrossWeight)
                .HasPrecision(10, 2)
                .HasColumnName("total_gross_weight");
            entity.Property(e => e.TotalLademeter)
                .HasPrecision(10, 2)
                .HasColumnName("total_lademeter");
            entity.Property(e => e.TotalLademeterM3)
                .HasPrecision(10, 2)
                .HasColumnName("total_lademeter_m3");
            entity.Property(e => e.TotalVolume)
                .HasPrecision(10, 2)
                .HasColumnName("total_volume");
            entity.Property(e => e.UnloadingContinent)
                .HasMaxLength(191)
                .HasColumnName("unloading_continent");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UsercodeWithNotification).HasColumnName("usercode_with_notification");
            entity.Property(e => e.WayOfWorking).HasColumnName("way_of_working");
            entity.Property(e => e.WeightFee)
                .HasPrecision(10, 2)
                .HasColumnName("weight_fee");
            entity.Property(e => e.WorkType).HasColumnName("work_type");
        });

        modelBuilder.Entity<LoadTransferDeliveryMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_delivery_methods_pkey");

            entity.ToTable("load_transfer_delivery_methods");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Edikod)
                .HasMaxLength(191)
                .HasColumnName("edikod");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<LoadTransferDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_documents_pkey");

            entity.ToTable("load_transfer_documents");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CopyCount).HasColumnName("copy_count");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.DeliveredTo)
                .HasMaxLength(191)
                .HasColumnName("delivered_to");
            entity.Property(e => e.DocumentNumber)
                .HasMaxLength(191)
                .HasColumnName("document_number");
            entity.Property(e => e.EvrakTuruId).HasColumnName("evrak_turu_id");
            entity.Property(e => e.LoadTransferId).HasColumnName("load_transfer_id");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OriginalCount).HasColumnName("original_count");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Yukevrakid)
                .HasMaxLength(191)
                .HasColumnName("yukevrakid");
        });

        modelBuilder.Entity<LoadTransferInvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_invoice_items_pkey");

            entity.ToTable("load_transfer_invoice_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Buysell)
                .HasMaxLength(191)
                .HasColumnName("buysell");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.InsertName)
                .HasMaxLength(191)
                .HasColumnName("insert_name");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Modulid)
                .HasMaxLength(191)
                .HasColumnName("modulid");
            entity.Property(e => e.Modulkalemid)
                .HasMaxLength(191)
                .HasColumnName("modulkalemid");
            entity.Property(e => e.Modulkod)
                .HasMaxLength(191)
                .HasColumnName("modulkod");
            entity.Property(e => e.NetPrice)
                .HasPrecision(10, 2)
                .HasColumnName("net_price");
            entity.Property(e => e.Quantity)
                .HasPrecision(10, 2)
                .HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(191)
                .HasDefaultValueSql("'pending'::character varying")
                .HasComment("pending, invoice_received, invoice_issued")
                .HasColumnName("status");
            entity.Property(e => e.TaxPrice)
                .HasPrecision(10, 2)
                .HasColumnName("tax_price");
            entity.Property(e => e.TaxRate)
                .HasPrecision(10, 2)
                .HasColumnName("tax_rate");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.TransferredFromReservation).HasColumnName("transferred_from_reservation");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<LoadTransferInvoiceMap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_invoice_maps_pkey");

            entity.ToTable("load_transfer_invoice_maps");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.InvoiceItemId).HasColumnName("invoice_item_id");
            entity.Property(e => e.LoadTransferId).HasColumnName("load_transfer_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Invoice).WithMany(p => p.LoadTransferInvoiceMaps)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("load_transfer_invoice_maps_invoice_id_foreign");

            entity.HasOne(d => d.InvoiceItem).WithMany(p => p.LoadTransferInvoiceMaps)
                .HasForeignKey(d => d.InvoiceItemId)
                .HasConstraintName("load_transfer_invoice_maps_invoice_item_id_foreign");

            entity.HasOne(d => d.LoadTransfer).WithMany(p => p.LoadTransferInvoiceMaps)
                .HasForeignKey(d => d.LoadTransferId)
                .HasConstraintName("load_transfer_invoice_maps_load_transfer_id_foreign");
        });

        modelBuilder.Entity<LoadTransferMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_movements_pkey");

            entity.ToTable("load_transfer_movements");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DestinationId).HasColumnName("destination_id");
            entity.Property(e => e.ExpeditionMovementId).HasColumnName("expedition_movement_id");
            entity.Property(e => e.ExpeditionStatusId).HasColumnName("expedition_status_id");
            entity.Property(e => e.LoadId).HasColumnName("load_id");
            entity.Property(e => e.LoadTransferId).HasColumnName("load_transfer_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Destination).WithMany(p => p.LoadTransferMovements)
                .HasForeignKey(d => d.DestinationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("load_transfer_movements_destination_id_foreign");

            entity.HasOne(d => d.ExpeditionMovement).WithMany(p => p.LoadTransferMovements)
                .HasForeignKey(d => d.ExpeditionMovementId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("load_transfer_movements_expedition_movement_id_foreign");

            entity.HasOne(d => d.ExpeditionStatus).WithMany(p => p.LoadTransferMovements)
                .HasForeignKey(d => d.ExpeditionStatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("load_transfer_movements_expedition_status_id_foreign");

            entity.HasOne(d => d.Load).WithMany(p => p.LoadTransferMovements)
                .HasForeignKey(d => d.LoadId)
                .HasConstraintName("load_transfer_movements_load_id_foreign");

            entity.HasOne(d => d.LoadTransfer).WithMany(p => p.LoadTransferMovements)
                .HasForeignKey(d => d.LoadTransferId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("load_transfer_movements_load_transfer_id_foreign");

            entity.HasOne(d => d.User).WithMany(p => p.LoadTransferMovements)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("load_transfer_movements_user_id_foreign");
        });

        modelBuilder.Entity<LoadTransferPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_packages_pkey");

            entity.ToTable("load_transfer_packages");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CaseTypeId)
                .HasMaxLength(191)
                .HasColumnName("case_type_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GrossWeight)
                .HasPrecision(10, 2)
                .HasColumnName("gross_weight");
            entity.Property(e => e.Height)
                .HasPrecision(10, 2)
                .HasColumnName("height");
            entity.Property(e => e.Lademeter)
                .HasPrecision(10, 2)
                .HasColumnName("lademeter");
            entity.Property(e => e.Length)
                .HasPrecision(10, 2)
                .HasColumnName("length");
            entity.Property(e => e.LoadTransferId)
                .HasMaxLength(191)
                .HasColumnName("load_transfer_id");
            entity.Property(e => e.NetWeight)
                .HasPrecision(10, 2)
                .HasColumnName("net_weight");
            entity.Property(e => e.ProductTypeId).HasColumnName("product_type_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Stackable).HasColumnName("stackable");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Volume)
                .HasPrecision(10, 2)
                .HasColumnName("volume");
            entity.Property(e => e.Width)
                .HasPrecision(10, 2)
                .HasColumnName("width");
            entity.Property(e => e.Yukkoliid)
                .HasMaxLength(191)
                .HasColumnName("yukkoliid");
        });

        modelBuilder.Entity<LoadTransferType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("load_transfer_types_pkey");

            entity.ToTable("load_transfer_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<LoadingType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("loading_types_pkey");

            entity.ToTable("loading_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<MovementType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("movement_types_pkey");

            entity.ToTable("movement_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PaymentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_types_pkey");

            entity.ToTable("payment_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_types_pkey");

            entity.ToTable("product_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<RomorkType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("romork_types_pkey");

            entity.ToTable("romork_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<StatusType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("status_types_pkey");

            entity.ToTable("status_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.Number)
                .HasMaxLength(191)
                .HasColumnName("number");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<TaxOffice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tax_offices_pkey");

            entity.ToTable("tax_offices");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.City)
                .HasMaxLength(191)
                .HasColumnName("city");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.SpecialCode).HasColumnName("special_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<TransportType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transport_types_pkey");

            entity.ToTable("transport_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Avatar)
                .HasMaxLength(191)
                .HasColumnName("avatar");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(191)
                .HasColumnName("email");
            entity.Property(e => e.EmailId)
                .HasMaxLength(191)
                .HasColumnName("email_id");
            entity.Property(e => e.EmailPassword)
                .HasMaxLength(191)
                .HasColumnName("email_password");
            entity.Property(e => e.EmailVerifiedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("email_verified_at");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.NotificationMail)
                .HasDefaultValue(false)
                .HasColumnName("notification_mail");
            entity.Property(e => e.NotificationSms)
                .HasDefaultValue(false)
                .HasColumnName("notification_sms");
            entity.Property(e => e.Password)
                .HasMaxLength(191)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PhoneCountryId).HasColumnName("phone_country_id");
            entity.Property(e => e.PkdsId)
                .HasMaxLength(191)
                .HasColumnName("pkds_id");
            entity.Property(e => e.RememberToken)
                .HasMaxLength(100)
                .HasColumnName("remember_token");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.SiberBlocked).HasColumnName("siber_blocked");
            entity.Property(e => e.SiberDepartmentName).HasMaxLength(191).HasColumnName("siber_department_name");
            entity.Property(e => e.SiberCompanyId).HasMaxLength(64).HasColumnName("siber_company_id");
            entity.Property(e => e.SiberCode)
                .HasMaxLength(191)
                .HasColumnName("siber_code");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.SiberName)
                .HasMaxLength(191)
                .HasColumnName("siber_name");
            entity.Property(e => e.Status)
                .HasDefaultValue(false)
                .HasColumnName("status");
            entity.Property(e => e.Surname)
                .HasMaxLength(191)
                .HasColumnName("surname");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.WorkingTracking)
                .HasDefaultValue(false)
                .HasColumnName("working_tracking");
        });

        modelBuilder.Entity<AccountRepresentative>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_representatives_pkey");

            entity.ToTable("account_representatives");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserType).HasColumnName("user_type");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");

            entity.HasIndex(e => new { e.AccountId, e.UserType });
        });

        modelBuilder.Entity<UserAccountMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_account_mappings_pkey");

            entity.ToTable("user_account_mappings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<UserGoal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_goals_pkey");

            entity.ToTable("user_goals");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.GoalPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("goal_price");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_permissions_pkey");

            entity.ToTable("user_permissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Create)
                .HasDefaultValueSql("'0'::smallint")
                .HasColumnName("create");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Delete)
                .HasDefaultValueSql("'0'::smallint")
                .HasColumnName("delete");
            entity.Property(e => e.Read)
                .HasDefaultValueSql("'0'::smallint")
                .HasColumnName("read");
            entity.Property(e => e.Update)
                .HasDefaultValueSql("'0'::smallint")
                .HasColumnName("update");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserPermissionPageId).HasColumnName("user_permission_page_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserPermissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_permissions_user_id_foreign");

            entity.HasOne(d => d.UserPermissionPage).WithMany(p => p.UserPermissions)
                .HasForeignKey(d => d.UserPermissionPageId)
                .HasConstraintName("user_permissions_user_permission_page_id_foreign");
        });

        modelBuilder.Entity<UserPermissionPage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_permission_pages_pkey");

            entity.ToTable("user_permission_pages");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.PermissionPageName)
                .HasMaxLength(191)
                .HasColumnName("permission_page_name");
            entity.Property(e => e.PermissionPageSlug)
                .HasMaxLength(191)
                .HasColumnName("permission_page_slug");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<WebsiteContactForm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("website_contact_forms_pkey");

            entity.ToTable("website_contact_forms");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(191)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(191)
                .HasColumnName("first_name");
            entity.Property(e => e.IsAnswered)
                .HasDefaultValue(false)
                .HasColumnName("is_answered");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.LastName)
                .HasMaxLength(191)
                .HasColumnName("last_name");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Phone)
                .HasMaxLength(191)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<WorkType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("work_types_pkey");

            entity.ToTable("work_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdditionalCode)
                .HasMaxLength(191)
                .HasColumnName("additional_code");
            entity.Property(e => e.Code)
                .HasMaxLength(191)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GroupCode)
                .HasMaxLength(191)
                .HasColumnName("group_code");
            entity.Property(e => e.Name)
                .HasMaxLength(191)
                .HasColumnName("name");
            entity.Property(e => e.SiberId)
                .HasMaxLength(191)
                .HasColumnName("siber_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("updated_at");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
