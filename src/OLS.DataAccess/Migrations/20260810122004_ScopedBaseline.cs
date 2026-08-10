using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScopedBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_contact_people",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    email = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_contact_people_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_type_mappings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    account_type_id = table.Column<int>(type: "integer", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_type_mappings_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    tax_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    accounting_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    invoice_type = table.Column<int>(type: "integer", nullable: true),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    district_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    phone_country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    avatar = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    email = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    contact_person = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    individual_personal = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    discount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    contact_language = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "car_owners",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<int>(type: "integer", nullable: true),
                    additional_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    special_code = table.Column<int>(type: "integer", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("car_owners_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "car_status_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<int>(type: "integer", nullable: true),
                    special_code = table.Column<int>(type: "integer", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("car_status_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "car_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<int>(type: "integer", nullable: true),
                    special_code = table.Column<int>(type: "integer", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("car_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cars",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    plate_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    car_type = table.Column<int>(type: "integer", nullable: true),
                    romork_type = table.Column<int>(type: "integer", nullable: true),
                    vehicle_owner = table.Column<int>(type: "integer", nullable: true),
                    vehicle_status = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    km = table.Column<double>(type: "double precision", nullable: true),
                    in_country = table.Column<int>(type: "integer", nullable: true),
                    international = table.Column<int>(type: "integer", nullable: true),
                    width = table.Column<double>(type: "double precision", nullable: true),
                    length = table.Column<double>(type: "double precision", nullable: true),
                    height = table.Column<double>(type: "double precision", nullable: true),
                    capacity = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cars_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "case_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    edikod = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("case_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    slug = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cities_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    country_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    flag = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    phone_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    slug = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("countries_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    symbol = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("currencies_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("departments_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    slug = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("districts_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expedition_load_mappings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    yukaktarmaid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    upload_unload = table.Column<int>(type: "integer", nullable: true),
                    load_transfer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    expedition_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    romork_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    yer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expedition_load_mappings_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expedition_statuses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expedition_status_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_status_id = table.Column<int>(type: "integer", nullable: true),
                    rowguid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    order_number = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expedition_statuses_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expedition_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expedition_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expeditions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expedition_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    expedition_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    sefer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    work_type = table.Column<int>(type: "integer", nullable: true),
                    status_id = table.Column<int>(type: "integer", nullable: true),
                    romork_id = table.Column<int>(type: "integer", nullable: true),
                    year_week = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    registration_login_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expedition_type_id = table.Column<int>(type: "integer", nullable: true),
                    car_exit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    loading_date = table.Column<DateOnly>(type: "date", nullable: true),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    load_city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    end_city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expeditions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("financial_items_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instructions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("instructions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_statuses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enum_value = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("invoice_statuses_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("invoice_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("item_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_charge_people",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    user_type = table.Column<int>(type: "integer", nullable: true, comment: "1: Operasyon Yetkilisi, 2: Satış Temsilcisi"),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_charge_people_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_emails",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<int>(type: "integer", nullable: true),
                    key = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    email = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_emails_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<int>(type: "integer", nullable: true),
                    file = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    org_name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_files_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_status_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_status_id = table.Column<int>(type: "integer", nullable: true),
                    order_no = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_status_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_delivery_methods",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    edikod = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_delivery_methods_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_invoice_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    modulkalemid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    modulid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    modulkod = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    item_id = table.Column<int>(type: "integer", nullable: true),
                    buysell = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    currency_code = table.Column<int>(type: "integer", nullable: true),
                    net_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    tax_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    insert_name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    transferred_from_reservation = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false, defaultValueSql: "'pending'::character varying", comment: "pending, invoice_received, invoice_issued"),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_invoice_items_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_packages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    yukkoliid = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_transfer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    case_type_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    width = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    length = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    volume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    gross_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    net_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    lademeter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    stackable = table.Column<int>(type: "integer", nullable: true),
                    product_type_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_packages_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "load_transfers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_transfer_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    work_type = table.Column<int>(type: "integer", nullable: true),
                    connected_load_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_status_id = table.Column<int>(type: "integer", nullable: true),
                    load_type_id = table.Column<int>(type: "integer", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    sender_id = table.Column<int>(type: "integer", nullable: true),
                    receiver_id = table.Column<int>(type: "integer", nullable: true),
                    payment_type_id = table.Column<int>(type: "integer", nullable: true),
                    in_truck = table.Column<int>(type: "integer", nullable: true),
                    in_tail = table.Column<int>(type: "integer", nullable: true),
                    cmr_waiting = table.Column<int>(type: "integer", nullable: true),
                    fcr_waiting = table.Column<int>(type: "integer", nullable: true),
                    instruction_id = table.Column<int>(type: "integer", nullable: true),
                    romork_type_id = table.Column<int>(type: "integer", nullable: true),
                    total_gross_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    total_volume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    total_lademeter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    weight_fee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    customer_representative_name = table.Column<int>(type: "integer", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    operation_department_id = table.Column<int>(type: "integer", nullable: true),
                    load_number_work_type = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    connected_load_number_work_type = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    total_cap = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    second_customer_representative_name = table.Column<int>(type: "integer", nullable: true),
                    total_lademeter_m3 = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    car_height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    load_transfer_type_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_method_id = table.Column<int>(type: "integer", nullable: true),
                    departure_country_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    target_country_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    loading_continent = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    unloading_continent = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    usercode_with_notification = table.Column<int>(type: "integer", nullable: true),
                    sales_rep_code = table.Column<int>(type: "integer", nullable: true),
                    way_of_working = table.Column<int>(type: "integer", nullable: true),
                    front_transportation_by_us = table.Column<int>(type: "integer", nullable: true),
                    final_transportation_by_us = table.Column<int>(type: "integer", nullable: true),
                    instruction_arrival_date = table.Column<DateOnly>(type: "date", nullable: true),
                    request_arrival_date = table.Column<DateOnly>(type: "date", nullable: true),
                    readiness_date = table.Column<DateOnly>(type: "date", nullable: true),
                    date_of_receipt_customer = table.Column<DateOnly>(type: "date", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfers_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loading_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("loading_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loads",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reservation_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    load_number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    work_type_id = table.Column<int>(type: "integer", nullable: true),
                    loading_type_id = table.Column<int>(type: "integer", nullable: true),
                    payment_type_id = table.Column<int>(type: "integer", nullable: true),
                    status_type_id = table.Column<int>(type: "integer", nullable: true),
                    offer_date = table.Column<DateOnly>(type: "date", nullable: true),
                    offer_validity_date = table.Column<DateOnly>(type: "date", nullable: true),
                    marketing_notification_date = table.Column<DateOnly>(type: "date", nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: true),
                    sender_id = table.Column<int>(type: "integer", nullable: true),
                    receiver_id = table.Column<int>(type: "integer", nullable: true),
                    instruction_id = table.Column<int>(type: "integer", nullable: true),
                    romork_type_id = table.Column<int>(type: "integer", nullable: true),
                    agent_id = table.Column<int>(type: "integer", nullable: true),
                    load_transfer_type_id = table.Column<int>(type: "integer", nullable: true),
                    company_pay_freight_id = table.Column<int>(type: "integer", nullable: true),
                    payer_company = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    departure_country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transit_country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    front_transportation_by_us = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    final_transportation_by_us = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    mail_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    transfer_to_siber = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    way_of_working = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("loads_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movement_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("movement_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "romork_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("romork_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    number = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("status_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tax_offices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    special_code = table.Column<int>(type: "integer", nullable: true),
                    city = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tax_offices_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transport_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("transport_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_account_mappings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_account_mappings_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_permission_pages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    permission_page_name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    permission_page_slug = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_permission_pages_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "website_contact_forms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    last_name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    email = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    phone = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_answered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("website_contact_forms_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    group_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    additional_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("work_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    surname = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    email = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    phone_country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    password = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    avatar = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    notification_mail = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notification_sms = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    siber_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    email_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    email_password = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    working_tracking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    pkds_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    remember_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_users_countries_phone_country_id",
                        column: x => x.phone_country_id,
                        principalTable: "countries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "destinations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    district_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("destinations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "destinations_city_id_foreign",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "destinations_country_id_foreign",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "destinations_district_id_foreign",
                        column: x => x.district_id,
                        principalTable: "districts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_type_id = table.Column<long>(type: "bigint", nullable: true),
                    invoice_status_id = table.Column<long>(type: "bigint", nullable: true),
                    account_id = table.Column<long>(type: "bigint", nullable: true),
                    box_type = table.Column<short>(type: "smallint", nullable: false, comment: "0 = Inbox, 1 = Outbox"),
                    invoice_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    document_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    commercial_type = table.Column<int>(type: "integer", nullable: false, comment: "0 = Temel Fatura, 1 = Ticari Fatura"),
                    target_identity_no = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    target_title = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: false),
                    envelope_identifier = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    envelope_status_code = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    invoice_create_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    invoice_execution_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    payable_amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    tax_exclusive_amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, defaultValueSql: "'0'::numeric"),
                    document_currency_code = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    exchange_date = table.Column<DateOnly>(type: "date", nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(13,5)", precision: 13, scale: 5, nullable: true),
                    order_document_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by_integration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "0 = Manuel, 1 = Integration"),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("invoices_pkey", x => x.id);
                    table.ForeignKey(
                        name: "invoices_account_id_foreign",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "invoices_invoice_status_id_foreign",
                        column: x => x.invoice_status_id,
                        principalTable: "invoice_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "invoices_invoice_type_id_foreign",
                        column: x => x.invoice_type_id,
                        principalTable: "invoice_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "load_contents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    product_type_id = table.Column<int>(type: "integer", nullable: true),
                    case_type_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    width = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    length = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    gross_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    net_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    volume = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    lademeter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    stackable = table.Column<int>(type: "integer", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_contents_pkey", x => x.id);
                    table.ForeignKey(
                        name: "load_contents_load_id_foreign",
                        column: x => x.load_id,
                        principalTable: "loads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "load_financial_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    buysell = table.Column<int>(type: "integer", nullable: true),
                    item_type_id = table.Column<int>(type: "integer", nullable: true),
                    item = table.Column<int>(type: "integer", nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    transport_type_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: true),
                    net_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    tax_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    currency = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_financial_items_pkey", x => x.id);
                    table.ForeignKey(
                        name: "load_financial_items_load_id_foreign",
                        column: x => x.load_id,
                        principalTable: "loads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "load_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    movement_type_id = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    siber_id = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_movements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "load_movements_load_id_foreign",
                        column: x => x.load_id,
                        principalTable: "loads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "revoked_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    jti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revoked_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_revoked_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_permission_page_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    read = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'0'::smallint"),
                    update = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'0'::smallint"),
                    create = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'0'::smallint"),
                    delete = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "'0'::smallint"),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_permissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_permissions_user_id_foreign",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_permissions_user_permission_page_id_foreign",
                        column: x => x.user_permission_page_id,
                        principalTable: "user_permission_pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expedition_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expedition_id = table.Column<long>(type: "bigint", nullable: true),
                    destination_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    expedition_status_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("expedition_movements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "expedition_movements_destination_id_foreign",
                        column: x => x.destination_id,
                        principalTable: "destinations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "expedition_movements_expedition_id_foreign",
                        column: x => x.expedition_id,
                        principalTable: "expeditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "expedition_movements_expedition_status_id_foreign",
                        column: x => x.expedition_status_id,
                        principalTable: "expedition_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "expedition_movements_user_id_foreign",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_footers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("invoice_footers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "invoice_footers_invoice_id_foreign",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_invoice_maps",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_transfer_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_item_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_invoice_maps_pkey", x => x.id);
                    table.ForeignKey(
                        name: "load_transfer_invoice_maps_invoice_id_foreign",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "load_transfer_invoice_maps_invoice_item_id_foreign",
                        column: x => x.invoice_item_id,
                        principalTable: "load_transfer_invoice_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "load_transfer_invoice_maps_load_transfer_id_foreign",
                        column: x => x.load_transfer_id,
                        principalTable: "load_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "load_transfer_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    load_transfer_id = table.Column<long>(type: "bigint", nullable: true),
                    destination_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    expedition_status_id = table.Column<long>(type: "bigint", nullable: false),
                    expedition_movement_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("load_transfer_movements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "load_transfer_movements_destination_id_foreign",
                        column: x => x.destination_id,
                        principalTable: "destinations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "load_transfer_movements_expedition_movement_id_foreign",
                        column: x => x.expedition_movement_id,
                        principalTable: "expedition_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "load_transfer_movements_expedition_status_id_foreign",
                        column: x => x.expedition_status_id,
                        principalTable: "expedition_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "load_transfer_movements_load_id_foreign",
                        column: x => x.load_id,
                        principalTable: "loads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "load_transfer_movements_load_transfer_id_foreign",
                        column: x => x.load_transfer_id,
                        principalTable: "load_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "load_transfer_movements_user_id_foreign",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_destinations_city_id",
                table: "destinations",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_destinations_country_id",
                table: "destinations",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_destinations_district_id",
                table: "destinations",
                column: "district_id");

            migrationBuilder.CreateIndex(
                name: "IX_expedition_movements_destination_id",
                table: "expedition_movements",
                column: "destination_id");

            migrationBuilder.CreateIndex(
                name: "IX_expedition_movements_expedition_id",
                table: "expedition_movements",
                column: "expedition_id");

            migrationBuilder.CreateIndex(
                name: "IX_expedition_movements_expedition_status_id",
                table: "expedition_movements",
                column: "expedition_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_expedition_movements_user_id",
                table: "expedition_movements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_footers_invoice_id",
                table: "invoice_footers",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "invoice_statuses_code_unique",
                table: "invoice_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "invoice_types_code_unique",
                table: "invoice_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_account_id",
                table: "invoices",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_status_id",
                table: "invoices",
                column: "invoice_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_type_id",
                table: "invoices",
                column: "invoice_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_contents_load_id",
                table: "load_contents",
                column: "load_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_financial_items_load_id",
                table: "load_financial_items",
                column: "load_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_movements_load_id",
                table: "load_movements",
                column: "load_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_invoice_maps_invoice_id",
                table: "load_transfer_invoice_maps",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_invoice_maps_invoice_item_id",
                table: "load_transfer_invoice_maps",
                column: "invoice_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_invoice_maps_load_transfer_id",
                table: "load_transfer_invoice_maps",
                column: "load_transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_movements_destination_id",
                table: "load_transfer_movements",
                column: "destination_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_movements_expedition_movement_id",
                table: "load_transfer_movements",
                column: "expedition_movement_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_movements_expedition_status_id",
                table: "load_transfer_movements",
                column: "expedition_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_movements_load_id",
                table: "load_transfer_movements",
                column: "load_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_movements_load_transfer_id",
                table: "load_transfer_movements",
                column: "load_transfer_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_transfer_movements_user_id",
                table: "load_transfer_movements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_revoked_tokens_expires_at",
                table: "revoked_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_revoked_tokens_jti",
                table: "revoked_tokens",
                column: "jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revoked_tokens_user_id",
                table: "revoked_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_user_id",
                table: "user_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_user_permission_page_id",
                table: "user_permissions",
                column: "user_permission_page_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_country_id",
                table: "users",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_phone_country_id",
                table: "users",
                column: "phone_country_id");

            migrationBuilder.CreateIndex(
                name: "users_email_unique",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_contact_people");

            migrationBuilder.DropTable(
                name: "account_type_mappings");

            migrationBuilder.DropTable(
                name: "account_types");

            migrationBuilder.DropTable(
                name: "car_owners");

            migrationBuilder.DropTable(
                name: "car_status_types");

            migrationBuilder.DropTable(
                name: "car_types");

            migrationBuilder.DropTable(
                name: "cars");

            migrationBuilder.DropTable(
                name: "case_types");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "expedition_load_mappings");

            migrationBuilder.DropTable(
                name: "expedition_types");

            migrationBuilder.DropTable(
                name: "financial_items");

            migrationBuilder.DropTable(
                name: "instructions");

            migrationBuilder.DropTable(
                name: "invoice_footers");

            migrationBuilder.DropTable(
                name: "item_types");

            migrationBuilder.DropTable(
                name: "load_charge_people");

            migrationBuilder.DropTable(
                name: "load_contents");

            migrationBuilder.DropTable(
                name: "load_emails");

            migrationBuilder.DropTable(
                name: "load_files");

            migrationBuilder.DropTable(
                name: "load_financial_items");

            migrationBuilder.DropTable(
                name: "load_movements");

            migrationBuilder.DropTable(
                name: "load_status_types");

            migrationBuilder.DropTable(
                name: "load_transfer_delivery_methods");

            migrationBuilder.DropTable(
                name: "load_transfer_invoice_maps");

            migrationBuilder.DropTable(
                name: "load_transfer_movements");

            migrationBuilder.DropTable(
                name: "load_transfer_packages");

            migrationBuilder.DropTable(
                name: "load_transfer_types");

            migrationBuilder.DropTable(
                name: "loading_types");

            migrationBuilder.DropTable(
                name: "movement_types");

            migrationBuilder.DropTable(
                name: "payment_types");

            migrationBuilder.DropTable(
                name: "product_types");

            migrationBuilder.DropTable(
                name: "revoked_tokens");

            migrationBuilder.DropTable(
                name: "romork_types");

            migrationBuilder.DropTable(
                name: "status_types");

            migrationBuilder.DropTable(
                name: "tax_offices");

            migrationBuilder.DropTable(
                name: "transport_types");

            migrationBuilder.DropTable(
                name: "user_account_mappings");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "website_contact_forms");

            migrationBuilder.DropTable(
                name: "work_types");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "load_transfer_invoice_items");

            migrationBuilder.DropTable(
                name: "expedition_movements");

            migrationBuilder.DropTable(
                name: "loads");

            migrationBuilder.DropTable(
                name: "load_transfers");

            migrationBuilder.DropTable(
                name: "user_permission_pages");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "invoice_statuses");

            migrationBuilder.DropTable(
                name: "invoice_types");

            migrationBuilder.DropTable(
                name: "destinations");

            migrationBuilder.DropTable(
                name: "expeditions");

            migrationBuilder.DropTable(
                name: "expedition_statuses");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
