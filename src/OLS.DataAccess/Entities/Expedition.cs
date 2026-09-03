using System;
using System.Collections.Generic;

namespace OLS.DataAccess.Entities;

public partial class Expedition
{
    public long Id { get; set; }

    /// <summary>
    /// Seferin ait olduğu Siber şirketi (skn_yuk/skn_pozisyon.sirketid).
    /// Siber'de iki şirket var: AVRORA ve OLS. Görünürlük ayrımı bu sütuna
    /// dayanıyor — bkz. CompanyScope.
    /// </summary>
    public string? SiberCompanyId { get; set; }

    public string? ExpeditionId { get; set; }

    public string? ExpeditionNumber { get; set; }

    public string? SeferId { get; set; }

    public int? WorkType { get; set; }

    public int? StatusId { get; set; }

    public int? RomorkId { get; set; }

    public string? YearWeek { get; set; }

    public int? DepartmentId { get; set; }

    public DateOnly? RegistrationLoginDate { get; set; }

    public int? ExpeditionTypeId { get; set; }

    public DateOnly? CarExitDate { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public DateOnly? LoadingDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public Guid? StartCityId { get; set; }

    public Guid? LoadCityId { get; set; }

    public Guid? EndCityId { get; set; }

    /// <summary>
    /// ÇEKİCİ. Siber'de römork ve çekici AYRI plakalar
    /// (<c>skn_pozisyon.romorkid</c> ve <c>cekiciid</c>, ikisi de
    /// <c>skn_arac</c>'a FK'li). Özmal seferlerin %92'sinde çekici dolu
    /// (240/260); kiralık seferlerde araç karşı firmanın olduğu için boş.
    /// </summary>
    public int? TractorId { get; set; }

    /// <summary>
    /// SÜRÜCÜ (<c>skn_pozisyon.surucuid</c> → <c>sbr_personel</c>). Özmal
    /// seferlerin %92'sinde dolu (239/260), kiralıkta hiç kullanılmıyor.
    /// </summary>
    public long? DriverId { get; set; }

    /// <summary>
    /// KİRALANAN FİRMA (<c>skn_pozisyon.kiralananfirmaid</c> → <c>sbr_firma</c>).
    /// Aracın kiralandığı nakliyeci; kiralık seferlerin %20'sinde dolu.
    /// </summary>
    public int? RentedCompanyId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // ------------------------------------------------------------------
    // Siber izleri — kaydı kim açtı, kim son dokundu, Siber'den silindi mi.
    //
    // Siber bu bilgiyi KULLANICI KODU olarak tutuyor (insuser/kayitgiren,
    // upduser). Kod hem ham hâliyle hem çözümlenmiş kullanıcı kimliğiyle
    // saklanıyor: ayrılmış personelin kodu yerel users tablosunda karşılık
    // bulmuyor (91 koddan 3'ü) ve o durumda ekranda hiç olmazsa kod görünsün.
    // ------------------------------------------------------------------

    /// <summary>Kaydı Siber'de açan kullanıcının kodu.</summary>
    public string? SiberCreatedBy { get; set; }

    public long? SiberCreatedByUserId { get; set; }

    /// <summary>Siber'in kendi kayıt tarihi (yerel CreatedAt'ten farklı).</summary>
    public DateTime? SiberCreatedAt { get; set; }

    /// <summary>Kayda Siber'de EN SON dokunan kullanıcının kodu.</summary>
    public string? SiberUpdatedBy { get; set; }

    public long? SiberUpdatedByUserId { get; set; }

    public DateTime? SiberUpdatedAt { get; set; }

    /// <summary>
    /// Kayıt Siber'de artık yoksa, silindiğinin FARK EDİLDİĞİ an. Kayıt
    /// yerelden SİLİNMEZ: geçmiş, bağlı finans kayıtları ve denetim izi
    /// korunmalı. Kayıt Siber'de yeniden görünürse bu alan temizlenir.
    /// </summary>
    public DateTime? SiberDeletedAt { get; set; }

    /// <summary>Kaydı Siber'de SİLEN kullanıcının kodu (sbr_log'dan).</summary>
    public string? SiberDeletedBy { get; set; }

    public long? SiberDeletedByUserId { get; set; }

    /// <summary>
    /// Siber'deki GERÇEK silme anı. <see cref="SiberDeletedAt"/> ise bizim
    /// fark ettiğimiz an — ikisi farklı şeydir ve karıştırılmamalı.
    /// Silme günlüğü bulunamazsa null kalır.
    /// </summary>
    public DateTime? SiberDeletedOn { get; set; }

    public virtual User? SiberCreatedByUser { get; set; }

    public virtual User? SiberUpdatedByUser { get; set; }

    public virtual User? SiberDeletedByUser { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();
}
