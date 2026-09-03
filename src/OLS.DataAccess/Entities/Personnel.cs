namespace OLS.DataAccess.Entities;

/// <summary>
/// Siber'in <c>sbr_personel</c> tablosunun yerel aynası — sefer sürücüsü
/// seçilebilsin diye tutulur.
///
/// Küçük bir tablo: canlıda 25 personel, 22'si sürücü işaretli.
/// <c>skn_pozisyon.surucuid</c> bu tablonun kimliğini bekliyor ve FK'lidir,
/// yani karşılığı olmayan bir değer INSERT'i düşürür.
/// </summary>
public partial class Personnel
{
    public long Id { get; set; }

    /// <summary><c>sbr_personel.personelid</c>.</summary>
    public string? SiberId { get; set; }

    public string? Name { get; set; }

    /// <summary><c>sbr_personel.surucu</c> — sefer sürücüsü seçicisi bunu süzer.</summary>
    public bool IsDriver { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
