import { useCallback, useEffect, useMemo, useState } from "react";
import { clsx } from "clsx";
import { Wallet, Plus, Trash2, FileText, AlertTriangle } from "lucide-react";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useDebouncedValue } from "@/lib/hooks";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Badge, Btn, FormField, SelectInput, TextInput, Tabs } from "@/components/ui/primitives";
import { AccountPicker, type AccountOption } from "@/components/shared/AccountPicker";
import { FinancialItemPicker, type FinancialItemOption } from "@/components/shared/FinancialItemPicker";
import { useToast } from "@/components/ui/Toast";

const PER_PAGE = 25;

const TAB_BALANCES = "Cari Bakiyeler";
const TAB_INVOICES = "Faturalar";
const TAB_PAYMENTS = "Tahsilat & Ödeme";

interface BalanceRow {
  id: number;
  account_id: number;
  account_name: string | null;
  account_code: string | null;
  movement_count: number;
  debit: number;
  credit: number;
  balance: number;
  last_movement_date: string | null;
}

interface StatementLine {
  id: number;
  date: string | null;
  voucher_number: string | null;
  account_code: string | null;
  document_number: string | null;
  description: string | null;
  currency_code: string | null;
  debit_fx: number | null;
  credit_fx: number | null;
  debit: number;
  credit: number;
  running_balance: number;
  due_date: string | null;
}

interface OverdueInvoice {
  id: number;
  invoice_number: string | null;
  invoice_date: string | null;
  due_date: string | null;
  overdue_days: number;
  currency_code: string | null;
  total_amount: number | null;
}

interface Statement {
  account_id: number;
  account_name: string | null;
  opening_balance: number;
  debit: number;
  credit: number;
  closing_balance: number;
  lines: StatementLine[];
  overdue_invoices: OverdueInvoice[];
}

interface InvoiceRow {
  id: number;
  direction: string | null;
  invoice_number: string | null;
  invoice_date: string | null;
  due_date: string | null;
  account_id: number | null;
  account_name: string | null;
  currency_code: string | null;
  total_amount: number | null;
  total_amount_tl: number | null;
  load_transfer_id: number | null;
  load_number: string | null;
  is_approved: boolean;
}

interface InvoiceLine {
  id: number;
  financial_item_name: string | null;
  quantity: number | null;
  unit_price: number | null;
  currency_code: string | null;
  tax_rate: number | null;
  amount: number | null;
  tax_amount: number | null;
  description: string | null;
}

interface InvoiceDetail extends InvoiceRow {
  invoice_series: string | null;
  exchange_rate: number | null;
  amount: number | null;
  tax_amount: number | null;
  description: string | null;
  document_number: string | null;
  siber_created_by: string | null;
  siber_created_at: string | null;
  lines: InvoiceLine[];
}

interface PaymentRow {
  id: number;
  receipt_number: string | null;
  receipt_date: string | null;
  due_date: string | null;
  debit_name: string | null;
  debit_account_code: string | null;
  credit_name: string | null;
  credit_account_code: string | null;
  currency_code: string | null;
  amount: number | null;
  amount_tl: number | null;
  description: string | null;
}

const money = (value: number | null | undefined) =>
  (value ?? 0).toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const day = (value: string | null | undefined) =>
  value ? new Date(value).toLocaleDateString("tr-TR") : "—";

/** Bakiye yönü: pozitif borç (bize borçlu), negatif alacak (biz borçluyuz). */
function BalanceCell({ value }: { value: number }) {
  return (
    <span
      className={clsx(
        "font-medium tabular-nums",
        value > 0 && "text-emerald-700",
        value < 0 && "text-red-700",
        value === 0 && "text-gray-400",
      )}
    >
      {money(value)}
    </span>
  );
}

export function FinancePage() {
  const { can } = useAuth();
  const [tab, setTab] = useState(TAB_BALANCES);

  return (
    <ModulePage title="Finans">
      <Tabs
        tabs={[TAB_BALANCES, TAB_INVOICES, TAB_PAYMENTS]}
        active={tab}
        onChange={setTab}
        className="px-6 bg-white"
      />
      {tab === TAB_BALANCES && <BalancesTab />}
      {tab === TAB_INVOICES && <InvoicesTab canCreate={can("finance_management", "create")} />}
      {tab === TAB_PAYMENTS && <PaymentsTab />}
    </ModulePage>
  );
}

// ---------------------------------------------------------------------------
// Cari bakiyeler + ekstre
// ---------------------------------------------------------------------------
function BalancesTab() {
  const [rows, setRows] = useState<BalanceRow[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [onlyOpen, setOnlyOpen] = useState(true);
  const [statement, setStatement] = useState<Statement | null>(null);
  const [statementLoading, setStatementLoading] = useState(false);
  const debounced = useDebouncedValue(search, 350);

  useEffect(() => setPage(1), [debounced, onlyOpen]);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<BalanceRow>>>("/api/v1/finance/balances", {
        search: debounced || undefined,
        only_open: onlyOpen || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        // Bakiye satırlarının kendi kimliği yok; tablo anahtarı için cari id kullanılır.
        setRows(res.data.data.map((r) => ({ ...r, id: r.account_id })));
        setTotal(res.data.total);
      })
      .finally(() => setLoading(false));
  }, [debounced, onlyOpen, page]);

  const openStatement = useCallback((accountId: number) => {
    setStatementLoading(true);
    const from = new Date();
    from.setFullYear(from.getFullYear() - 1);

    api
      .get<DataMessage<Statement>>(`/api/v1/finance/balances/${accountId}/statement`, {
        from: from.toISOString().slice(0, 10),
      })
      .then((res) => setStatement(res.data))
      .finally(() => setStatementLoading(false));
  }, []);

  const columns: Column<BalanceRow>[] = [
    { key: "account_name", header: "Cari", sortable: true, render: (r) => r.account_name ?? "—" },
    {
      key: "account_code",
      header: "Hesap Kodu",
      render: (r) => <span className="font-mono text-xs text-gray-500">{r.account_code ?? "—"}</span>,
    },
    { key: "movement_count", header: "Hareket", render: (r) => r.movement_count },
    { key: "debit", header: "Borç", render: (r) => <span className="tabular-nums">{money(r.debit)}</span> },
    { key: "credit", header: "Alacak", render: (r) => <span className="tabular-nums">{money(r.credit)}</span> },
    { key: "balance", header: "Bakiye", render: (r) => <BalanceCell value={r.balance} /> },
    { key: "last_movement_date", header: "Son Hareket", render: (r) => day(r.last_movement_date) },
  ];

  return (
    <>
      <div className="flex items-center gap-2 px-6 py-3 bg-white border-b border-gray-200 flex-wrap">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Cari ara…"
          className="h-8 px-3 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 w-64"
        />
        <label className="flex items-center gap-1.5 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={onlyOpen} onChange={(e) => setOnlyOpen(e.target.checked)} />
          Yalnızca bakiyesi olanlar
        </label>
        <span className="text-xs text-gray-400 ml-auto">{total} cari</span>
      </div>

      {!loading && rows.length === 0 ? (
        <EmptyState icon={Wallet} title="Cari hareketi yok" desc="Seçilen süzgeçlere uyan cari bulunamadı." />
      ) : (
        <>
          <DataTable data={rows} columns={columns} loading={loading} onRowClick={(r) => openStatement(r.account_id)} />
          <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
        </>
      )}

      <Drawer
        open={statement !== null || statementLoading}
        onClose={() => setStatement(null)}
        title={statement?.account_name ?? "Cari Ekstre"}
        subtitle="Son 12 ay"
        width="w-[860px]"
      >
        {statementLoading || !statement ? (
          <div className="p-6 text-sm text-gray-400">Yükleniyor…</div>
        ) : (
          <StatementBody statement={statement} />
        )}
      </Drawer>
    </>
  );
}

function StatementBody({ statement }: { statement: Statement }) {
  return (
    <div className="p-5 space-y-5">
      <div className="grid grid-cols-4 gap-3">
        <SummaryTile label="Açılış" value={statement.opening_balance} />
        <SummaryTile label="Borç" value={statement.debit} neutral />
        <SummaryTile label="Alacak" value={statement.credit} neutral />
        <SummaryTile label="Kapanış" value={statement.closing_balance} />
      </div>

      {statement.overdue_invoices.length > 0 && (
        <div className="border border-amber-200 bg-amber-50 rounded p-3">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-amber-800 mb-2">
            <AlertTriangle size={13} />
            Vadesi geçmiş {statement.overdue_invoices.length} fatura
          </div>
          {/* Ödendi/ödenmedi bilgisi Siber'de tutulmuyor; bu liste yalnızca
              vadesi geçmiş faturaları gösterir, borç anlamına gelmez. */}
          <p className="text-[11px] text-amber-700 mb-2">
            Siber ödeme kapanışını kaydetmediği için bu liste ödeme durumunu göstermez; yalnızca
            vadesi geçen faturaları sıralar. Gerçek borç için üstteki bakiyeye bakın.
          </p>
          <div className="space-y-1">
            {statement.overdue_invoices.slice(0, 6).map((i) => (
              <div key={i.id} className="flex items-center gap-2 text-xs text-amber-900">
                <span className="font-mono">{i.invoice_number ?? "—"}</span>
                <span className="text-amber-600">{day(i.due_date)}</span>
                <span className="text-amber-600">{i.overdue_days} gün</span>
                <span className="ml-auto tabular-nums">
                  {money(i.total_amount)} {i.currency_code ?? ""}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="border border-gray-200 rounded overflow-hidden">
        <table className="w-full text-xs">
          <thead className="bg-gray-50 text-gray-500">
            <tr>
              <th className="text-left px-2 py-1.5 font-medium">Tarih</th>
              <th className="text-left px-2 py-1.5 font-medium">Belge</th>
              <th className="text-left px-2 py-1.5 font-medium">Açıklama</th>
              <th className="text-right px-2 py-1.5 font-medium">Borç</th>
              <th className="text-right px-2 py-1.5 font-medium">Alacak</th>
              <th className="text-right px-2 py-1.5 font-medium">Bakiye</th>
            </tr>
          </thead>
          <tbody>
            {statement.lines.map((l) => (
              <tr key={l.id} className="border-t border-gray-100">
                <td className="px-2 py-1.5 whitespace-nowrap">{day(l.date)}</td>
                <td className="px-2 py-1.5 font-mono text-gray-500">{l.document_number ?? "—"}</td>
                <td className="px-2 py-1.5 text-gray-600 max-w-[260px] truncate" title={l.description ?? ""}>
                  {l.description ?? "—"}
                </td>
                <td className="px-2 py-1.5 text-right tabular-nums">{l.debit ? money(l.debit) : ""}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">{l.credit ? money(l.credit) : ""}</td>
                <td className="px-2 py-1.5 text-right tabular-nums font-medium">{money(l.running_balance)}</td>
              </tr>
            ))}
            {statement.lines.length === 0 && (
              <tr>
                <td colSpan={6} className="px-2 py-6 text-center text-gray-400">
                  Bu dönemde hareket yok.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function SummaryTile({ label, value, neutral }: { label: string; value: number; neutral?: boolean }) {
  return (
    <div className="border border-gray-200 rounded p-2.5">
      <div className="text-[11px] text-gray-500 mb-0.5">{label}</div>
      <div className={clsx("text-sm font-semibold tabular-nums", !neutral && value < 0 && "text-red-700", !neutral && value > 0 && "text-emerald-700")}>
        {money(value)}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Faturalar
// ---------------------------------------------------------------------------
function InvoicesTab({ canCreate }: { canCreate: boolean }) {
  const [rows, setRows] = useState<InvoiceRow[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [direction, setDirection] = useState("");
  const [onlyOverdue, setOnlyOverdue] = useState(false);
  const [detail, setDetail] = useState<InvoiceDetail | null>(null);
  const [creating, setCreating] = useState(false);
  const debounced = useDebouncedValue(search, 350);
  const [reload, setReload] = useState(0);

  useEffect(() => setPage(1), [debounced, direction, onlyOverdue]);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<InvoiceRow>>>("/api/v1/finance/invoices", {
        search: debounced || undefined,
        direction: direction || undefined,
        only_overdue: onlyOverdue || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .finally(() => setLoading(false));
  }, [debounced, direction, onlyOverdue, page, reload]);

  const columns: Column<InvoiceRow>[] = [
    {
      key: "direction",
      header: "Tür",
      width: "70px",
      render: (r) => (
        <span
          className={clsx(
            "px-1.5 py-0.5 rounded text-[11px] font-medium border",
            r.direction === "C"
              ? "bg-emerald-50 text-emerald-700 border-emerald-200"
              : "bg-orange-50 text-orange-700 border-orange-200",
          )}
        >
          {r.direction === "C" ? "Gelir" : "Gider"}
        </span>
      ),
    },
    { key: "invoice_number", header: "Fatura No", render: (r) => r.invoice_number ?? "—" },
    { key: "account_name", header: "Cari", render: (r) => r.account_name ?? "—" },
    { key: "invoice_date", header: "Tarih", render: (r) => day(r.invoice_date) },
    { key: "due_date", header: "Vade", render: (r) => day(r.due_date) },
    {
      key: "total_amount",
      header: "Tutar",
      render: (r) => (
        <span className="tabular-nums">
          {money(r.total_amount)} {r.currency_code ?? ""}
        </span>
      ),
    },
    {
      key: "load_number",
      header: "Yük",
      render: (r) => (r.load_number ? <Badge label={r.load_number} /> : "—"),
    },
  ];

  return (
    <>
      <div className="flex items-center gap-2 px-6 py-3 bg-white border-b border-gray-200 flex-wrap">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Fatura no, cari, belge…"
          className="h-8 px-3 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 w-64"
        />
        <select
          value={direction}
          onChange={(e) => setDirection(e.target.value)}
          className="h-8 px-2 text-sm border border-gray-300 rounded"
        >
          <option value="">Tümü</option>
          <option value="C">Gelir</option>
          <option value="G">Gider</option>
        </select>
        <label className="flex items-center gap-1.5 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={onlyOverdue} onChange={(e) => setOnlyOverdue(e.target.checked)} />
          Vadesi geçmiş
        </label>
        <span className="text-xs text-gray-400 ml-auto">{total} fatura</span>
        {canCreate && (
          <Btn onClick={() => setCreating(true)}>
            <Plus size={14} />
            Yeni Fatura
          </Btn>
        )}
      </div>

      {!loading && rows.length === 0 ? (
        <EmptyState icon={FileText} title="Fatura yok" desc="Seçilen süzgeçlere uyan fatura bulunamadı." />
      ) : (
        <>
          <DataTable
            data={rows}
            columns={columns}
            loading={loading}
            onRowClick={(r) =>
              api
                .get<DataMessage<InvoiceDetail>>(`/api/v1/finance/invoices/${r.id}`)
                .then((res) => setDetail(res.data))
            }
          />
          <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
        </>
      )}

      <Drawer
        open={detail !== null}
        onClose={() => setDetail(null)}
        title={detail?.invoice_number ? `Fatura ${detail.invoice_number}` : "Fatura"}
        subtitle={detail?.account_name ?? undefined}
        width="w-[720px]"
      >
        {detail && <InvoiceBody invoice={detail} />}
      </Drawer>

      <InvoiceForm
        open={creating}
        onClose={() => setCreating(false)}
        onSaved={() => {
          setCreating(false);
          setReload((r) => r + 1);
        }}
      />
    </>
  );
}

function InvoiceBody({ invoice }: { invoice: InvoiceDetail }) {
  return (
    <div className="p-5 space-y-4">
      <div className="grid grid-cols-3 gap-3 text-sm">
        <Field label="Tür" value={invoice.direction === "C" ? "Gelir" : "Gider"} />
        <Field label="Seri" value={invoice.invoice_series ?? "—"} />
        <Field label="Belge No" value={invoice.document_number ?? "—"} />
        <Field label="Tarih" value={day(invoice.invoice_date)} />
        <Field label="Vade" value={day(invoice.due_date)} />
        <Field label="Kur" value={invoice.exchange_rate ? String(invoice.exchange_rate) : "—"} />
        <Field label="Yük" value={invoice.load_number ?? "—"} />
        <Field label="Kaydeden" value={invoice.siber_created_by ?? "—"} />
        <Field label="Kayıt Tarihi" value={day(invoice.siber_created_at)} />
      </div>

      {invoice.description && (
        <div className="text-sm text-gray-600 border-l-2 border-gray-200 pl-3">{invoice.description}</div>
      )}

      <div className="border border-gray-200 rounded overflow-hidden">
        <table className="w-full text-xs">
          <thead className="bg-gray-50 text-gray-500">
            <tr>
              <th className="text-left px-2 py-1.5 font-medium">Kalem</th>
              <th className="text-right px-2 py-1.5 font-medium">Miktar</th>
              <th className="text-right px-2 py-1.5 font-medium">Birim</th>
              <th className="text-right px-2 py-1.5 font-medium">KDV %</th>
              <th className="text-right px-2 py-1.5 font-medium">Tutar</th>
            </tr>
          </thead>
          <tbody>
            {invoice.lines.map((l) => (
              <tr key={l.id} className="border-t border-gray-100">
                <td className="px-2 py-1.5">{l.financial_item_name ?? "—"}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">{l.quantity ?? "—"}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">{money(l.unit_price)}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">{l.tax_rate ?? 0}</td>
                <td className="px-2 py-1.5 text-right tabular-nums">{money(l.amount)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot className="bg-gray-50 text-gray-700">
            <tr className="border-t border-gray-200">
              <td colSpan={4} className="px-2 py-1.5 text-right font-medium">Ara Toplam</td>
              <td className="px-2 py-1.5 text-right tabular-nums">{money(invoice.amount)}</td>
            </tr>
            <tr>
              <td colSpan={4} className="px-2 py-1.5 text-right font-medium">KDV</td>
              <td className="px-2 py-1.5 text-right tabular-nums">{money(invoice.tax_amount)}</td>
            </tr>
            <tr className="border-t border-gray-200">
              <td colSpan={4} className="px-2 py-1.5 text-right font-semibold">Genel Toplam</td>
              <td className="px-2 py-1.5 text-right tabular-nums font-semibold">
                {money(invoice.total_amount)} {invoice.currency_code ?? ""}
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-[11px] text-gray-500">{label}</div>
      <div className="text-gray-800">{value}</div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Yeni fatura
// ---------------------------------------------------------------------------
interface DraftLine {
  key: number;
  /** Picker'ın kendi seçeneği olduğu gibi tutulur; parçalamak type alanını kaybettiriyor. */
  item: FinancialItemOption | null;
  quantity: string;
  unit_price: string;
  tax_rate: string;
  description: string;
}

const emptyLine = (key: number): DraftLine => ({
  key,
  item: null,
  quantity: "1",
  unit_price: "",
  tax_rate: "20",
  description: "",
});

function InvoiceForm({
  open,
  onClose,
  onSaved,
}: {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { addToast } = useToast();
  const [direction, setDirection] = useState("C");
  const [account, setAccount] = useState<AccountOption | null>(null);
  const [series, setSeries] = useState("");
  const [invoiceNumber, setInvoiceNumber] = useState("");
  const [invoiceDate, setInvoiceDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [dueDate, setDueDate] = useState("");
  const [currency, setCurrency] = useState("TL ");
  const [rate, setRate] = useState("1");
  const [description, setDescription] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine(1)]);
  const [saving, setSaving] = useState(false);

  const isIncome = direction === "C";

  useEffect(() => {
    if (!open) return;
    setDirection("C");
    setAccount(null);
    setSeries("");
    setInvoiceNumber("");
    setInvoiceDate(new Date().toISOString().slice(0, 10));
    setDueDate("");
    setCurrency("TL ");
    setRate("1");
    setDescription("");
    setLines([emptyLine(1)]);
  }, [open]);

  // Toplamlar SUNUCUDA yeniden hesaplanıyor; buradaki değer yalnızca önizleme.
  const totals = useMemo(() => {
    let net = 0;
    let tax = 0;
    for (const l of lines) {
      const amount = (Number(l.quantity) || 0) * (Number(l.unit_price) || 0);
      net += amount;
      tax += (amount * (Number(l.tax_rate) || 0)) / 100;
    }
    return { net, tax, total: net + tax };
  }, [lines]);

  const valid =
    account !== null &&
    lines.some((l) => l.item !== null && Number(l.unit_price) > 0) &&
    (isIncome ? series.trim().length > 0 : invoiceNumber.trim().length > 0);

  function save() {
    if (!account || saving) return;
    setSaving(true);

    api
      .post<DataMessage<{ id: number; invoice_number: string | null }>>("/api/v1/finance/invoices", {
        direction,
        account_id: account.id,
        series: isIncome ? series.trim() : null,
        invoice_number: isIncome ? null : invoiceNumber.trim(),
        invoice_date: invoiceDate,
        due_date: dueDate || null,
        currency_code: currency,
        exchange_rate: Number(rate) || 1,
        description: description || null,
        lines: lines
          .filter((l) => l.item !== null && Number(l.unit_price) > 0)
          .map((l) => ({
            financial_item_id: l.item!.id,
            quantity: Number(l.quantity) || 1,
            unit_price: Number(l.unit_price) || 0,
            tax_rate: Number(l.tax_rate) || 0,
            description: l.description || null,
          })),
      })
      .then((res) => {
        addToast(`Fatura oluşturuldu: ${res.data.invoice_number ?? ""}`);
        onSaved();
      })
      .catch((e: Error) => addToast(e.message, "error"))
      .finally(() => setSaving(false));
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Yeni Fatura"
      subtitle="Kayıt Siber'e yazılır"
      width="w-[760px]"
      footer={
        <div className="flex items-center justify-between w-full">
          <div className="text-sm text-gray-600">
            Toplam: <span className="font-semibold tabular-nums">{money(totals.total)}</span> {currency.trim()}
          </div>
          <div className="flex gap-2">
            <Btn variant="secondary" onClick={onClose}>
              Vazgeç
            </Btn>
            <Btn onClick={save} disabled={!valid || saving}>
              {saving ? "Kaydediliyor…" : "Kaydet"}
            </Btn>
          </div>
        </div>
      }
    >
      <div className="p-5 space-y-4">
        <div className="grid grid-cols-3 gap-3">
          <FormField label="Tür">
            <SelectInput
              value={direction}
              onChange={setDirection}
              options={[
                { value: "C", label: "Gelir" },
                { value: "G", label: "Gider" },
              ]}
            />
          </FormField>

          {/* Gelir faturasında numarayı Siber üretir (seri+yıl sayacı);
              gider faturasında numara tedarikçinin belgesinden gelir. */}
          {isIncome ? (
            <FormField label="Seri" hint="Numara Siber tarafından üretilir">
              <TextInput value={series} onChange={setSeries} placeholder="DKT" />
            </FormField>
          ) : (
            <FormField label="Fatura No" hint="Tedarikçinin fatura numarası">
              <TextInput value={invoiceNumber} onChange={setInvoiceNumber} />
            </FormField>
          )}

          <FormField label="Belge Tarihi">
            <input
              type="date"
              value={invoiceDate}
              onChange={(e) => setInvoiceDate(e.target.value)}
              className="w-full h-8 px-2 text-sm border border-gray-300 rounded"
            />
          </FormField>
        </div>

        <div className="grid grid-cols-3 gap-3">
          <AccountPicker label="Cari" required value={account} onChange={setAccount} />
          <FormField label="Vade">
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="w-full h-8 px-2 text-sm border border-gray-300 rounded"
            />
          </FormField>
          <div className="grid grid-cols-2 gap-2">
            <FormField label="Döviz">
              <SelectInput
                value={currency}
                onChange={setCurrency}
                options={[
                  { value: "TL ", label: "TL" },
                  { value: "USD", label: "USD" },
                  { value: "EUR", label: "EUR" },
                  { value: "RUB", label: "RUB" },
                  { value: "CNY", label: "CNY" },
                ]}
              />
            </FormField>
            <FormField label="Kur">
              <TextInput value={rate} onChange={setRate} />
            </FormField>
          </div>
        </div>

        <FormField label="Açıklama">
          <TextInput value={description} onChange={setDescription} />
        </FormField>

        <div>
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium text-gray-700">Kalemler</span>
            <Btn variant="secondary" size="sm" onClick={() => setLines((ls) => [...ls, emptyLine(Date.now())])}>
              <Plus size={13} />
              Kalem Ekle
            </Btn>
          </div>

          <div className="space-y-2">
            {lines.map((line, index) => (
              <div key={line.key} className="grid grid-cols-[1fr_70px_100px_70px_32px] gap-2 items-end">
                <FinancialItemPicker
                  label={index === 0 ? "Kalem" : ""}
                  value={line.item}
                  onChange={(item) =>
                    setLines((ls) => ls.map((l) => (l.key === line.key ? { ...l, item } : l)))
                  }
                />
                <FormField label={index === 0 ? "Miktar" : ""}>
                  <TextInput
                    value={line.quantity}
                    onChange={(v) =>
                      setLines((ls) => ls.map((l) => (l.key === line.key ? { ...l, quantity: v } : l)))
                    }
                  />
                </FormField>
                <FormField label={index === 0 ? "Birim Fiyat" : ""}>
                  <TextInput
                    value={line.unit_price}
                    onChange={(v) =>
                      setLines((ls) => ls.map((l) => (l.key === line.key ? { ...l, unit_price: v } : l)))
                    }
                  />
                </FormField>
                <FormField label={index === 0 ? "KDV %" : ""}>
                  <TextInput
                    value={line.tax_rate}
                    onChange={(v) =>
                      setLines((ls) => ls.map((l) => (l.key === line.key ? { ...l, tax_rate: v } : l)))
                    }
                  />
                </FormField>
                <button
                  onClick={() => setLines((ls) => (ls.length === 1 ? ls : ls.filter((l) => l.key !== line.key)))}
                  disabled={lines.length === 1}
                  className="h-8 w-8 flex items-center justify-center rounded text-gray-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-30"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>

          <div className="flex justify-end gap-6 mt-3 text-sm">
            <span className="text-gray-500">
              Ara toplam <span className="tabular-nums text-gray-800">{money(totals.net)}</span>
            </span>
            <span className="text-gray-500">
              KDV <span className="tabular-nums text-gray-800">{money(totals.tax)}</span>
            </span>
          </div>
        </div>
      </div>
    </Drawer>
  );
}

// ---------------------------------------------------------------------------
// Tahsilat / ödeme
// ---------------------------------------------------------------------------
function PaymentsTab() {
  const [rows, setRows] = useState<PaymentRow[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debounced = useDebouncedValue(search, 350);

  useEffect(() => setPage(1), [debounced]);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<PaymentRow>>>("/api/v1/finance/payments", {
        search: debounced || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .finally(() => setLoading(false));
  }, [debounced, page]);

  const columns: Column<PaymentRow>[] = [
    { key: "receipt_number", header: "Makbuz", render: (r) => r.receipt_number ?? "—" },
    { key: "receipt_date", header: "Tarih", render: (r) => day(r.receipt_date) },
    // Kayıt çift taraflı: bir taraf cari, diğeri kasa/banka hesabı olabilir.
    { key: "debit_name", header: "Borç", render: (r) => r.debit_name ?? "—" },
    { key: "credit_name", header: "Alacak", render: (r) => r.credit_name ?? "—" },
    {
      key: "amount",
      header: "Tutar",
      render: (r) => (
        <span className="tabular-nums">
          {money(r.amount)} {r.currency_code ?? ""}
        </span>
      ),
    },
    {
      key: "description",
      header: "Açıklama",
      render: (r) => (
        <span className="text-gray-500 text-xs" title={r.description ?? ""}>
          {(r.description ?? "—").slice(0, 60)}
        </span>
      ),
    },
  ];

  return (
    <>
      <div className="flex items-center gap-2 px-6 py-3 bg-white border-b border-gray-200">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Makbuz no, taraf adı…"
          className="h-8 px-3 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 w-72"
        />
        <span className="text-xs text-gray-400 ml-auto">{total} kayıt</span>
      </div>

      {!loading && rows.length === 0 ? (
        <EmptyState icon={Wallet} title="Kayıt yok" desc="Tahsilat/ödeme bulunamadı." />
      ) : (
        <>
          <DataTable data={rows} columns={columns} loading={loading} />
          <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
        </>
      )}
    </>
  );
}
