namespace OLS.DataAccess.Entities;

/// <summary>
/// Scaffold'ın üretemediği ilişkiler.
///
/// <c>users.country_id</c> ve <c>users.phone_country_id</c> sütunları uuid tipinde
/// ve <c>countries.id</c> ile aynı tipte, ancak veritabanında foreign key kısıtı
/// tanımlı değil — bu yüzden scaffold navigation üretmedi. Laravel tarafında bu
/// ilişkiler <c>belongsTo(Countrie::class, ...)</c> ile kod düzeyinde kuruluyordu.
/// Aynı ilişkileri burada tanımlıyoruz (bkz. OlsDbContext.Extensions.cs).
/// </summary>
public partial class User
{
    public virtual Country? Country { get; set; }

    public virtual Country? PhoneCountry { get; set; }

    public virtual ICollection<RevokedToken> RevokedTokens { get; set; } = new List<RevokedToken>();
}
