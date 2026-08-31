using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndUserSiberStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "role_id",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    slug = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("roles_pkey", x => x.id));

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    user_permission_page_id = table.Column<long>(type: "bigint", nullable: false),
                    read = table.Column<int>(type: "integer", nullable: false),
                    create = table.Column<int>(type: "integer", nullable: false),
                    update = table.Column<int>(type: "integer", nullable: false),
                    delete = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("role_permissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "role_permissions_role_id_foreign",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_roles_slug", table: "roles", column: "slug", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_user_permission_page_id",
                table: "role_permissions",
                columns: new[] { "role_id", "user_permission_page_id" },
                unique: true);

            migrationBuilder.AddColumn<bool>(
                name: "siber_blocked",
                table: "users",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siber_department_name",
                table: "users",
                type: "character varying(191)",
                maxLength: 191,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "role_permissions");
            migrationBuilder.DropTable(name: "roles");
            migrationBuilder.DropColumn(name: "role_id", table: "users");

            migrationBuilder.DropColumn(
                name: "siber_blocked",
                table: "users");

            migrationBuilder.DropColumn(
                name: "siber_department_name",
                table: "users");
        }
    }
}
