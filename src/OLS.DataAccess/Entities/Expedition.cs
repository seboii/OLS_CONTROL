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

    public virtual User? SiberCreatedByUser { get; set; }

    public virtual User? SiberUpdatedByUser { get; set; }

    public virtual ICollection<ExpeditionMovement> ExpeditionMovements { get; set; } = new List<ExpeditionMovement>();
}
