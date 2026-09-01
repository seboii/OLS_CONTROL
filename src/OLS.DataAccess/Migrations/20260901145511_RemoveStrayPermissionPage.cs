using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <summary>
    /// Elle açılmış artık yetki sayfasını kaldırır: "Test Sayfa Canli"
    /// (<c>test_sayfa_canli</c>, 2026-08-14).
    ///
    /// Kodun hiçbir yerinde geçmiyor, menüde yok — hiçbir şeyi korumuyordu; ama
    /// 130 kullanıcının 48'inde birer yetki satırı taşıyordu ve yetki ekranında
    /// gerçek modüllerin arasında duruyordu. <c>POST /api/v1/permission</c> ile
    /// açılmış, silme ucu OLMADIĞI için de kalmıştı; silme artık var
    /// (<c>DELETE /api/v1/permission/{id}</c>) ve programın kullandığı sayfaları
    /// reddediyor.
    ///
    /// Silmek güvenli: bu slug'ı hiçbir yetki kontrolü sormuyor.
    /// </summary>
    public partial class RemoveStrayPermissionPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM user_permissions
                WHERE user_permission_page_id IN (
                    SELECT id FROM user_permission_pages
                    WHERE permission_page_slug = 'test_sayfa_canli');

                DELETE FROM user_permission_pages
                WHERE permission_page_slug = 'test_sayfa_canli';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınmaz: sayfa hiçbir şeyi korumuyordu, geri getirmek yalnızca
            // yetki ekranına anlamsız bir satır ekler.
        }
    }
}
