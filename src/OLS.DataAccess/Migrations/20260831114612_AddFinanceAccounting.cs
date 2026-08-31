using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OLS.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounting_plans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    name2 = table.Column<string>(type: "character varying(191)", maxLength: 191, nullable: true),
                    level = table.Column<short>(type: "smallint", nullable: true),
                    is_passive = table.Column<bool>(type: "boolean", nullable: false),
                    siber_company_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounting_plans_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "finance_invoices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    direction = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    invoice_series = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    invoice_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    account_id = table.Column<long>(type: "bigint", nullable: true),
                    siber_account_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    account_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_tl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    tax_amount_tl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    total_amount_tl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    module_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    module_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    load_transfer_id = table.Column<long>(type: "bigint", nullable: true),
                    document_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    approval_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    siber_company_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    siber_created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    siber_created_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("finance_invoices_pkey", x => x.id);
                    table.ForeignKey(
                        name: "finance_invoices_account_id_foreign",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "finance_invoices_load_transfer_id_foreign",
                        column: x => x.load_transfer_id,
                        principalTable: "load_transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "finance_payments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    receipt_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    transaction_type = table.Column<int>(type: "integer", nullable: true),
                    debit_account_id = table.Column<long>(type: "bigint", nullable: true),
                    siber_debit_account_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    debit_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    debit_account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    credit_account_id = table.Column<long>(type: "bigint", nullable: true),
                    siber_credit_account_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    credit_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    credit_account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_tl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    module_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    module_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    siber_company_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    siber_created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    siber_created_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("finance_payments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "finance_payments_credit_account_id_foreign",
                        column: x => x.credit_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "finance_payments_debit_account_id_foreign",
                        column: x => x.debit_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "finance_vouchers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    voucher_type = table.Column<short>(type: "smallint", nullable: true),
                    voucher_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    voucher_number = table.Column<int>(type: "integer", nullable: true),
                    journal_number = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    document_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    document_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    is_checked = table.Column<bool>(type: "boolean", nullable: false),
                    siber_company_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    siber_created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    siber_created_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("finance_vouchers_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "finance_invoice_lines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finance_invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    financial_item_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    financial_item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    tax_rate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_tl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    tax_amount_tl = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    document_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    document_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("finance_invoice_lines_pkey", x => x.id);
                    table.ForeignKey(
                        name: "finance_invoice_lines_invoice_id_foreign",
                        column: x => x.finance_invoice_id,
                        principalTable: "finance_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finance_voucher_lines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    siber_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finance_voucher_id = table.Column<long>(type: "bigint", nullable: false),
                    account_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    debit_fx = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    credit_fx = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<long>(type: "bigint", nullable: true),
                    siber_account_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    document_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    document_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    line_number = table.Column<long>(type: "bigint", nullable: true),
                    siber_company_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("finance_voucher_lines_pkey", x => x.id);
                    table.ForeignKey(
                        name: "finance_voucher_lines_account_id_foreign",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "finance_voucher_lines_voucher_id_foreign",
                        column: x => x.finance_voucher_id,
                        principalTable: "finance_vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "accounting_plans_code_index",
                table: "accounting_plans",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "accounting_plans_siber_id_unique",
                table: "accounting_plans",
                column: "siber_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "finance_invoice_lines_invoice_id_index",
                table: "finance_invoice_lines",
                column: "finance_invoice_id");

            migrationBuilder.CreateIndex(
                name: "finance_invoice_lines_siber_id_unique",
                table: "finance_invoice_lines",
                column: "siber_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "finance_invoices_account_id_index",
                table: "finance_invoices",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "finance_invoices_due_date_index",
                table: "finance_invoices",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "finance_invoices_load_transfer_id_index",
                table: "finance_invoices",
                column: "load_transfer_id");

            migrationBuilder.CreateIndex(
                name: "finance_invoices_module_index",
                table: "finance_invoices",
                columns: new[] { "module_code", "module_id" });

            migrationBuilder.CreateIndex(
                name: "finance_invoices_siber_id_unique",
                table: "finance_invoices",
                column: "siber_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "finance_payments_credit_account_id_index",
                table: "finance_payments",
                column: "credit_account_id");

            migrationBuilder.CreateIndex(
                name: "finance_payments_debit_account_id_index",
                table: "finance_payments",
                column: "debit_account_id");

            migrationBuilder.CreateIndex(
                name: "finance_payments_receipt_date_index",
                table: "finance_payments",
                column: "receipt_date");

            migrationBuilder.CreateIndex(
                name: "finance_payments_siber_id_unique",
                table: "finance_payments",
                column: "siber_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "finance_voucher_lines_account_code_index",
                table: "finance_voucher_lines",
                column: "account_code");

            migrationBuilder.CreateIndex(
                name: "finance_voucher_lines_account_date_index",
                table: "finance_voucher_lines",
                columns: new[] { "account_id", "document_date" });

            migrationBuilder.CreateIndex(
                name: "finance_voucher_lines_siber_id_unique",
                table: "finance_voucher_lines",
                column: "siber_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "finance_voucher_lines_source_id_index",
                table: "finance_voucher_lines",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "finance_voucher_lines_voucher_id_index",
                table: "finance_voucher_lines",
                column: "finance_voucher_id");

            migrationBuilder.CreateIndex(
                name: "finance_vouchers_siber_id_unique",
                table: "finance_vouchers",
                column: "siber_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "finance_vouchers_voucher_date_index",
                table: "finance_vouchers",
                column: "voucher_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_plans");

            migrationBuilder.DropTable(
                name: "finance_invoice_lines");

            migrationBuilder.DropTable(
                name: "finance_payments");

            migrationBuilder.DropTable(
                name: "finance_voucher_lines");

            migrationBuilder.DropTable(
                name: "finance_invoices");

            migrationBuilder.DropTable(
                name: "finance_vouchers");
        }
    }
}
