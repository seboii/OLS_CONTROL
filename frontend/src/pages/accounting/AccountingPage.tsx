import { useEffect, useState } from "react";
import { clsx } from "clsx";
import { BookOpen, Scale, ListTree, AlertTriangle } from "lucide-react";
import { api, type DataMessage, type Paginated } from "@/lib/api";
import { useDebouncedValue } from "@/lib/hooks";
import { ModulePage } from "@/components/ui/ModulePage";
import { DataTable, EmptyState, Pagination, type Column } from "@/components/ui/DataTable";
import { Drawer } from "@/components/ui/Overlay";
import { Tabs } from "@/components/ui/primitives";

const PER_PAGE = 25;

const TAB_TRIAL = "Mizan";
const TAB_VOUCHERS = "Muhasebe Fişleri";
const TAB_PLAN = "Hesap Planı";

interface TrialRow {
  id: string;
  account_code: string;
  account_name: string | null;
  level: number | null;
  debit: number;
  credit: number;
  balance: number;
}

interface VoucherRow {
  id: number;
  voucher_type: number | null;
  voucher_date: string | null;
  voucher_number: number | null;
  journal_number: number | null;
  description: string | null;
  line_count: number;
  debit: number;
  credit: number;
}

interface VoucherLine {
  id: number;
  account_code: string | null;
  account_name: string | null;
  party_name: string | null;
  debit: number | null;
  credit: number | null;
  currency_code: string | null;
  description: string | null;
  document_number: string | null;
}

interface VoucherDetail extends VoucherRow {
  document_number: string | null;
  is_checked: boolean;
  is_balanced: boolean;
  lines: VoucherLine[];
}

interface PlanRow {
  id: number;
  code: string;
  name: string | null;
  level: number | null;
  is_passive: boolean;
}

const money = (value: number | null | undefined) =>
  (value ?? 0).toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const day = (value: string | null | undefined) =>
  value ? new Date(value).toLocaleDateString("tr-TR") : "—";

export function AccountingPage() {
  const [tab, setTab] = useState(TAB_TRIAL);

  return (
    <ModulePage title="Muhasebe">
      <Tabs
        tabs={[TAB_TRIAL, TAB_VOUCHERS, TAB_PLAN]}
        active={tab}
        onChange={setTab}
        className="px-6 bg-white"
      />
      {tab === TAB_TRIAL && <TrialBalanceTab />}
      {tab === TAB_VOUCHERS && <VouchersTab />}
      {tab === TAB_PLAN && <PlanTab />}
    </ModulePage>
  );
}

// ---------------------------------------------------------------------------
// Mizan
// ---------------------------------------------------------------------------
function TrialBalanceTab() {
  const [rows, setRows] = useState<TrialRow[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [prefix, setPrefix] = useState("");
  const [level, setLevel] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const debouncedPrefix = useDebouncedValue(prefix, 350);

  useEffect(() => setPage(1), [debouncedPrefix, level, from, to]);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<TrialRow>>>("/api/v1/finance/trial_balance", {
        code_prefix: debouncedPrefix || undefined,
        level: level || undefined,
        from: from || undefined,
        to: to || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data.map((r) => ({ ...r, id: r.account_code })));
        setTotal(res.data.total);
      })
      .finally(() => setLoading(false));
  }, [debouncedPrefix, level, from, to, page]);

  const pageDebit = rows.reduce((sum, r) => sum + r.debit, 0);
  const pageCredit = rows.reduce((sum, r) => sum + r.credit, 0);

  const columns: Column<TrialRow>[] = [
    {
      key: "account_code",
      header: "Hesap Kodu",
      width: "160px",
      render: (r) => <span className="font-mono text-xs">{r.account_code}</span>,
    },
    {
      key: "account_name",
      header: "Hesap Adı",
      // Fiş satırı hesap planına METİN ile bağlanıyor; planda karşılığı
      // olmayan (kapatılmış) kod adsız görünür ama tutarı kaybolmaz.
      render: (r) => r.account_name ?? <span className="text-gray-400 italic">planda yok</span>,
    },
    { key: "level", header: "Seviye", width: "70px", render: (r) => r.level ?? "—" },
    { key: "debit", header: "Borç", render: (r) => <span className="tabular-nums">{money(r.debit)}</span> },
    { key: "credit", header: "Alacak", render: (r) => <span className="tabular-nums">{money(r.credit)}</span> },
    {
      key: "balance",
      header: "Bakiye",
      render: (r) => (
        <span className={clsx("tabular-nums font-medium", r.balance < 0 ? "text-red-700" : "text-emerald-700")}>
          {money(r.balance)}
        </span>
      ),
    },
  ];

  return (
    <>
      <div className="flex items-center gap-2 px-6 py-3 bg-white border-b border-gray-200 flex-wrap">
        <input
          value={prefix}
          onChange={(e) => setPrefix(e.target.value)}
          placeholder="Hesap kodu ön eki (120, 320…)"
          className="h-8 px-3 text-sm border border-gray-300 rounded w-56"
        />
        <select
          value={level}
          onChange={(e) => setLevel(e.target.value)}
          className="h-8 px-2 text-sm border border-gray-300 rounded"
        >
          <option value="">Tüm seviyeler</option>
          <option value="1">1. seviye</option>
          <option value="2">2. seviye</option>
          <option value="3">3. seviye</option>
          <option value="4">4. seviye</option>
        </select>
        <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="h-8 px-2 text-sm border border-gray-300 rounded" />
        <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="h-8 px-2 text-sm border border-gray-300 rounded" />
        <span className="text-xs text-gray-400 ml-auto">{total} hesap</span>
      </div>

      {!loading && rows.length === 0 ? (
        <EmptyState icon={Scale} title="Kayıt yok" desc="Seçilen süzgeçlere uyan hesap bulunamadı." />
      ) : (
        <>
          <DataTable data={rows} columns={columns} loading={loading} />
          <div className="flex items-center justify-end gap-6 px-6 py-2 bg-gray-50 border-t border-gray-200 text-xs">
            <span className="text-gray-500">
              Sayfa borç <span className="tabular-nums font-medium text-gray-800">{money(pageDebit)}</span>
            </span>
            <span className="text-gray-500">
              Sayfa alacak <span className="tabular-nums font-medium text-gray-800">{money(pageCredit)}</span>
            </span>
          </div>
          <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
        </>
      )}
    </>
  );
}

// ---------------------------------------------------------------------------
// Muhasebe fişleri
// ---------------------------------------------------------------------------
function VouchersTab() {
  const [rows, setRows] = useState<VoucherRow[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [detail, setDetail] = useState<VoucherDetail | null>(null);
  const debounced = useDebouncedValue(search, 350);

  useEffect(() => setPage(1), [debounced]);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<VoucherRow>>>("/api/v1/finance/vouchers", {
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

  const columns: Column<VoucherRow>[] = [
    { key: "voucher_number", header: "Fiş No", width: "90px", render: (r) => r.voucher_number ?? "—" },
    { key: "voucher_date", header: "Tarih", render: (r) => day(r.voucher_date) },
    { key: "journal_number", header: "Yevmiye", render: (r) => r.journal_number ?? "—" },
    {
      key: "description",
      header: "Açıklama",
      render: (r) => (
        <span className="text-gray-600" title={r.description ?? ""}>
          {(r.description ?? "—").slice(0, 60)}
        </span>
      ),
    },
    { key: "line_count", header: "Satır", width: "70px", render: (r) => r.line_count },
    { key: "debit", header: "Borç", render: (r) => <span className="tabular-nums">{money(r.debit)}</span> },
    { key: "credit", header: "Alacak", render: (r) => <span className="tabular-nums">{money(r.credit)}</span> },
  ];

  return (
    <>
      <div className="flex items-center gap-2 px-6 py-3 bg-white border-b border-gray-200">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Açıklama, belge no…"
          className="h-8 px-3 text-sm border border-gray-300 rounded w-72"
        />
        <span className="text-xs text-gray-400 ml-auto">{total} fiş</span>
      </div>

      {!loading && rows.length === 0 ? (
        <EmptyState icon={BookOpen} title="Fiş yok" desc="Seçilen süzgeçlere uyan fiş bulunamadı." />
      ) : (
        <>
          <DataTable
            data={rows}
            columns={columns}
            loading={loading}
            onRowClick={(r) =>
              api
                .get<DataMessage<VoucherDetail>>(`/api/v1/finance/vouchers/${r.id}`)
                .then((res) => setDetail(res.data))
            }
          />
          <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
        </>
      )}

      <Drawer
        open={detail !== null}
        onClose={() => setDetail(null)}
        title={detail?.voucher_number ? `Fiş ${detail.voucher_number}` : "Muhasebe Fişi"}
        subtitle={detail ? day(detail.voucher_date) : undefined}
        width="w-[820px]"
      >
        {detail && (
          <div className="p-5 space-y-4">
            {/* Çift taraflı kayıtta borç ve alacak eşit olmak zorunda;
                eşit değilse fiş dengesizdir ve bu görünür olmalı. */}
            {!detail.is_balanced && (
              <div className="flex items-center gap-2 text-xs text-amber-800 bg-amber-50 border border-amber-200 rounded p-2.5">
                <AlertTriangle size={14} />
                Bu fişte borç ve alacak eşit değil ({money(detail.debit)} / {money(detail.credit)}).
              </div>
            )}

            {detail.description && (
              <div className="text-sm text-gray-600 border-l-2 border-gray-200 pl-3">{detail.description}</div>
            )}

            <div className="border border-gray-200 rounded overflow-hidden">
              <table className="w-full text-xs">
                <thead className="bg-gray-50 text-gray-500">
                  <tr>
                    <th className="text-left px-2 py-1.5 font-medium">Hesap</th>
                    <th className="text-left px-2 py-1.5 font-medium">Ad</th>
                    <th className="text-left px-2 py-1.5 font-medium">Cari</th>
                    <th className="text-left px-2 py-1.5 font-medium">Açıklama</th>
                    <th className="text-right px-2 py-1.5 font-medium">Borç</th>
                    <th className="text-right px-2 py-1.5 font-medium">Alacak</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.lines.map((l) => (
                    <tr key={l.id} className="border-t border-gray-100">
                      <td className="px-2 py-1.5 font-mono whitespace-nowrap">{l.account_code ?? "—"}</td>
                      <td className="px-2 py-1.5 text-gray-600">{l.account_name ?? "—"}</td>
                      <td className="px-2 py-1.5 text-gray-600">{l.party_name ?? "—"}</td>
                      <td className="px-2 py-1.5 text-gray-500 max-w-[200px] truncate" title={l.description ?? ""}>
                        {l.description ?? "—"}
                      </td>
                      <td className="px-2 py-1.5 text-right tabular-nums">{l.debit ? money(l.debit) : ""}</td>
                      <td className="px-2 py-1.5 text-right tabular-nums">{l.credit ? money(l.credit) : ""}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-gray-50 font-medium">
                  <tr className="border-t border-gray-200">
                    <td colSpan={4} className="px-2 py-1.5 text-right">Toplam</td>
                    <td className="px-2 py-1.5 text-right tabular-nums">{money(detail.debit)}</td>
                    <td className="px-2 py-1.5 text-right tabular-nums">{money(detail.credit)}</td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        )}
      </Drawer>
    </>
  );
}

// ---------------------------------------------------------------------------
// Hesap planı
// ---------------------------------------------------------------------------
function PlanTab() {
  const [rows, setRows] = useState<PlanRow[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [level, setLevel] = useState("");
  const [includePassive, setIncludePassive] = useState(false);
  const debounced = useDebouncedValue(search, 350);

  useEffect(() => setPage(1), [debounced, level, includePassive]);

  useEffect(() => {
    setLoading(true);
    api
      .get<DataMessage<Paginated<PlanRow>>>("/api/v1/finance/accounting_plan", {
        search: debounced || undefined,
        level: level || undefined,
        include_passive: includePassive || undefined,
        per_page: PER_PAGE,
        page,
      })
      .then((res) => {
        setRows(res.data.data);
        setTotal(res.data.total);
      })
      .finally(() => setLoading(false));
  }, [debounced, level, includePassive, page]);

  const columns: Column<PlanRow>[] = [
    {
      key: "code",
      header: "Hesap Kodu",
      width: "170px",
      // Girinti seviyeye göre: kod hiyerarşik ("100 01 01 0001") ve düz liste
      // olarak bakıldığında ağaç yapısı okunmuyor.
      render: (r) => (
        <span className="font-mono text-xs" style={{ paddingLeft: `${((r.level ?? 1) - 1) * 12}px` }}>
          {r.code}
        </span>
      ),
    },
    { key: "name", header: "Hesap Adı", render: (r) => r.name ?? "—" },
    { key: "level", header: "Seviye", width: "70px", render: (r) => r.level ?? "—" },
    {
      key: "is_passive",
      header: "Durum",
      width: "90px",
      render: (r) =>
        r.is_passive ? <span className="text-xs text-gray-400">Pasif</span> : <span className="text-xs text-emerald-600">Aktif</span>,
    },
  ];

  return (
    <>
      <div className="flex items-center gap-2 px-6 py-3 bg-white border-b border-gray-200 flex-wrap">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Kod veya hesap adı…"
          className="h-8 px-3 text-sm border border-gray-300 rounded w-64"
        />
        <select
          value={level}
          onChange={(e) => setLevel(e.target.value)}
          className="h-8 px-2 text-sm border border-gray-300 rounded"
        >
          <option value="">Tüm seviyeler</option>
          <option value="1">1. seviye</option>
          <option value="2">2. seviye</option>
          <option value="3">3. seviye</option>
          <option value="4">4. seviye</option>
        </select>
        <label className="flex items-center gap-1.5 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={includePassive} onChange={(e) => setIncludePassive(e.target.checked)} />
          Pasifleri de göster
        </label>
        <span className="text-xs text-gray-400 ml-auto">{total} hesap</span>
      </div>

      {!loading && rows.length === 0 ? (
        <EmptyState icon={ListTree} title="Hesap yok" desc="Seçilen süzgeçlere uyan hesap bulunamadı." />
      ) : (
        <>
          <DataTable data={rows} columns={columns} loading={loading} />
          <Pagination page={page} total={total} perPage={PER_PAGE} onChange={setPage} />
        </>
      )}
    </>
  );
}
