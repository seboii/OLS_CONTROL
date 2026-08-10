using Microsoft.EntityFrameworkCore;
using OLS.DataAccess.Entities;

namespace OLS.DataAccess.Context;

/// <summary>
/// Scaffold edilen <see cref="OlsDbContext"/> üzerine code-first eklemeler.
///
/// Scaffold dosyası (OlsDbContext.cs) yeniden üretilebilir olmalı, bu yüzden
/// elle yapılan tüm yapılandırma bu partial dosyada tutulur.
/// </summary>
public partial class OlsDbContext
{
    public virtual DbSet<RevokedToken> RevokedTokens { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RevokedToken>(entity =>
        {
            entity.ToTable("revoked_tokens");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Jti).HasColumnName("jti").HasMaxLength(64).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");

            // Her jeton için tek kayıt; iptal kontrolü bu indeks üzerinden yapılır.
            entity.HasIndex(e => e.Jti).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RevokedTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // users -> countries ilişkileri: veritabanında FK kısıtı yok, sadece
        // eşleşen uuid sütunları var. FK oluşturmuyoruz (mevcut veride yetim
        // kayıtlar olabilir), yalnızca EF'in JOIN kurabilmesini sağlıyoruz.
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(u => u.Country)
                  .WithMany()
                  .HasForeignKey(u => u.CountryId)
                  .HasPrincipalKey(c => c.Id)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);

            entity.HasOne(u => u.PhoneCountry)
                  .WithMany()
                  .HasForeignKey(u => u.PhoneCountryId)
                  .HasPrincipalKey(c => c.Id)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });
    }
}
