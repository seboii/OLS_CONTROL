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

    public virtual DbSet<Expedition> Expeditions { get; set; }

    public virtual DbSet<ExpeditionLoadMapping> ExpeditionLoadMappings { get; set; }

    public virtual DbSet<ExpeditionMovement> ExpeditionMovements { get; set; }

    public virtual DbSet<ExpeditionStatus> ExpeditionStatuses { get; set; }

    public virtual DbSet<ExpeditionType> ExpeditionTypes { get; set; }

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

    public virtual DbSet<UserAccountMapping> UserAccountMappings { get; set; }

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

        modelBuilder.Entity<Expedition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("expeditions_pkey");

            entity.ToTable("expeditions");

            entity.Property(e => e.Id).HasColumnName("id");
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

        modelBuilder.Entity<FinancialItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("financial_items_pkey");

            entity.ToTable("financial_items");

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
