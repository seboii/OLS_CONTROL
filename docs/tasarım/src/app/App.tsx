import { useState, useEffect, useRef, useCallback } from "react";
import { motion, AnimatePresence } from "motion/react";
import {
  Users, FileText, Package, Truck, Receipt, Car, Headphones, Shield,
  ChevronLeft, ChevronRight, Search, Bell, Menu, X, Plus, Filter,
  Download, MoreHorizontal, ChevronDown, ArrowUpDown, Building2,
  Phone, Mail, Calendar, Hash, AlertCircle, Eye, Edit2, Trash2,
  CheckCircle, XCircle, Clock, AlertTriangle, Info, Send,
  MapPin, Weight, Route, FileCheck, Copy, Archive, Settings,
  LogOut, User, Layers, RefreshCw, Upload, File, Star, ChevronUp,
  LifeBuoy, UserCog, Zap, TrendingUp, Globe, Lock, Unlock,
  LayoutDashboard, ArrowUpRight, ArrowDownRight, Activity, BarChart2, Target
} from "lucide-react";
import { clsx } from "clsx";
import {
  AreaChart, Area, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, Legend
} from "recharts";

// ─────────────────────────────────────────────
// TYPES
// ─────────────────────────────────────────────
type ModuleKey = "dashboard" | "customers" | "quotes" | "loads" | "trips" | "invoices" | "vehicles" | "users" | "support";
type AppState = "login" | "main";

interface ToastData { id: number; message: string; type: "success" | "error" | "info"; }

interface NavItem {
  key: ModuleKey;
  label: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
  count?: number;
}

const NAV_ITEMS: NavItem[] = [
  { key: "dashboard", label: "Dashboard", icon: LayoutDashboard },
  { key: "customers", label: "Müşteriler", icon: Users },
  { key: "quotes", label: "Teklifler", icon: FileText, count: 3 },
  { key: "loads", label: "Yükler", icon: Package },
  { key: "trips", label: "Seferler", icon: Truck },
  { key: "invoices", label: "Faturalar", icon: Receipt, count: 2 },
  { key: "vehicles", label: "Araçlar", icon: Car },
  { key: "users", label: "Kullanıcılar", icon: Shield },
  { key: "support", label: "Destek Talepleri", icon: Headphones, count: 4 },
];

const MODULE_LABELS: Record<ModuleKey, string> = {
  dashboard: "Dashboard",
  customers: "Müşteriler",
  quotes: "Teklifler",
  loads: "Yükler",
  trips: "Seferler",
  invoices: "Faturalar",
  vehicles: "Araçlar",
  users: "Kullanıcılar",
  support: "Destek Talepleri",
};

// ─────────────────────────────────────────────
// MOCK DATA
// ─────────────────────────────────────────────
const CUSTOMERS = [
  { id: "C001", name: "Borusan Lojistik", type: "Kurumsal", taxNo: "4560123789", city: "İstanbul", contact: "Ahmet Yılmaz", phone: "+90 212 555 0101", email: "ahmet@borusan.com", status: "Aktif", since: "2019" },
  { id: "C002", name: "Arçelik A.Ş.", type: "Kurumsal", taxNo: "7834512300", city: "İstanbul", contact: "Fatma Kaya", phone: "+90 212 555 0202", email: "fatma@arcelik.com", status: "Aktif", since: "2020" },
  { id: "C003", name: "Migros Ticaret", type: "Kurumsal", taxNo: "2341876540", city: "İstanbul", contact: "Mehmet Demir", phone: "+90 212 555 0303", email: "m.demir@migros.com", status: "Aktif", since: "2018" },
  { id: "C004", name: "Koç Holding", type: "Kurumsal", taxNo: "8901234567", city: "Ankara", contact: "Zeynep Şahin", phone: "+90 312 555 0404", email: "z.sahin@koc.com", status: "Aktif", since: "2017" },
  { id: "C005", name: "Teklas Kauçuk", type: "KOBİ", taxNo: "3456789012", city: "Bursa", contact: "Ali Çelik", phone: "+90 224 555 0505", email: "ali@teklas.com", status: "Pasif", since: "2021" },
  { id: "C006", name: "Anadolu Efes", type: "Kurumsal", taxNo: "6789012345", city: "İstanbul", contact: "Selin Arslan", phone: "+90 212 555 0606", email: "selin@efes.com", status: "Aktif", since: "2019" },
  { id: "C007", name: "Türk Telekom", type: "Kurumsal", taxNo: "9012345678", city: "Ankara", contact: "Murat Aydın", phone: "+90 312 555 0707", email: "m.aydin@tt.com", status: "Aktif", since: "2020" },
  { id: "C008", name: "Yıldız Holding", type: "Kurumsal", taxNo: "1234567890", city: "İstanbul", contact: "Ayşe Kurt", phone: "+90 212 555 0808", email: "ayse@yildiz.com", status: "Aktif", since: "2016" },
  { id: "C009", name: "Ege Kimya", type: "KOBİ", taxNo: "4567890123", city: "İzmir", contact: "Hasan Güneş", phone: "+90 232 555 0909", email: "hasan@egekimya.com", status: "Beklemede", since: "2022" },
  { id: "C010", name: "Delta Tekstil", type: "KOBİ", taxNo: "7890123456", city: "Denizli", contact: "Emre Yıldız", phone: "+90 258 555 1010", email: "emre@delta.com", status: "Aktif", since: "2021" },
  { id: "C011", name: "Saygılı Yağ", type: "KOBİ", taxNo: "2345678901", city: "İzmir", contact: "Betül Kılıç", phone: "+90 232 555 1111", email: "betul@saygili.com", status: "Pasif", since: "2023" },
  { id: "C012", name: "Ülker Bisküvi", type: "Kurumsal", taxNo: "5678901234", city: "İstanbul", contact: "Okan Tan", phone: "+90 212 555 1212", email: "okan@ulker.com", status: "Aktif", since: "2018" },
];

const QUOTES = [
  { id: "T2025-0847", customer: "Borusan Lojistik", route: "İstanbul → Münih", type: "Karayolu", responsible: "Kemal Öztürk", amount: "48.500", currency: "EUR", status: "Beklemede", date: "08.01.2025" },
  { id: "T2025-0846", customer: "Arçelik A.Ş.", route: "İstanbul → Milano", type: "Denizyolu", responsible: "Seda Kara", amount: "62.000", currency: "EUR", status: "Onaylı", date: "07.01.2025" },
  { id: "T2025-0845", customer: "Migros Ticaret", route: "Ankara → İzmir", type: "Karayolu", responsible: "Kemal Öztürk", amount: "8.750", currency: "TRY", status: "Taslak", date: "06.01.2025" },
  { id: "T2025-0844", customer: "Koç Holding", route: "İstanbul → Rotterdam", type: "Havayolu", responsible: "Nilüfer Ay", amount: "34.200", currency: "EUR", status: "Reddedildi", date: "05.01.2025" },
  { id: "T2025-0843", customer: "Anadolu Efes", route: "İstanbul → Lyon", type: "Karayolu", responsible: "Seda Kara", amount: "41.800", currency: "EUR", status: "Onaylı", date: "04.01.2025" },
  { id: "T2025-0842", customer: "Türk Telekom", route: "Ankara → Bursa", type: "Karayolu", responsible: "Kemal Öztürk", amount: "5.200", currency: "TRY", status: "Beklemede", date: "03.01.2025" },
  { id: "T2025-0841", customer: "Yıldız Holding", route: "İstanbul → Londra", type: "Denizyolu", responsible: "Nilüfer Ay", amount: "78.500", currency: "GBP", status: "Taslak", date: "02.01.2025" },
  { id: "T2025-0840", customer: "Ülker Bisküvi", route: "İstanbul → Brüksel", type: "Karayolu", responsible: "Seda Kara", amount: "39.600", currency: "EUR", status: "Onaylı", date: "01.01.2025" },
];

const LOADS = [
  { id: "YUK-2025-1247", customer: "Arçelik A.Ş.", origin: "İstanbul", dest: "Milano", type: "Genel Kargo", weight: "18.400", volume: "72", responsible: "Seda Kara", trip: "SEF-2025-0312", status: "Yükleniyor" },
  { id: "YUK-2025-1246", customer: "Borusan Lojistik", origin: "İstanbul", dest: "Münih", type: "ADR", weight: "12.000", volume: "48", responsible: "Kemal Öztürk", trip: "SEF-2025-0311", status: "Yolda" },
  { id: "YUK-2025-1245", customer: "Migros Ticaret", origin: "Ankara", dest: "İzmir", type: "Soğuk Zincir", weight: "8.200", volume: "24", responsible: "Kemal Öztürk", trip: "SEF-2025-0310", status: "Tamamlandı" },
  { id: "YUK-2025-1244", customer: "Anadolu Efes", origin: "İstanbul", dest: "Lyon", type: "Genel Kargo", weight: "22.000", volume: "86", responsible: "Seda Kara", trip: "SEF-2025-0309", status: "Yolda" },
  { id: "YUK-2025-1243", customer: "Ülker Bisküvi", origin: "İstanbul", dest: "Brüksel", type: "Genel Kargo", weight: "19.600", volume: "78", responsible: "Seda Kara", trip: "SEF-2025-0308", status: "Tamamlandı" },
  { id: "YUK-2025-1242", customer: "Türk Telekom", origin: "Ankara", dest: "Bursa", type: "Genel Kargo", weight: "4.800", volume: "18", responsible: "Kemal Öztürk", trip: "-", status: "Beklemede" },
  { id: "YUK-2025-1241", customer: "Delta Tekstil", origin: "Denizli", dest: "İstanbul", type: "Tekstil", weight: "6.400", volume: "45", responsible: "Nilüfer Ay", trip: "-", status: "Beklemede" },
  { id: "YUK-2025-1240", customer: "Ege Kimya", origin: "İzmir", dest: "Ankara", type: "ADR", weight: "15.000", volume: "55", responsible: "Nilüfer Ay", trip: "SEF-2025-0307", status: "İptal" },
];

const TRIPS = [
  { id: "SEF-2025-0312", vehicle: "34 TRK 4521", trailer: "Tenteli Dorse", driver: "Osman Karaca", origin: "İstanbul", dest: "Trieste", startDate: "08.01.2025", endDate: "12.01.2025", loadCount: 1, status: "Yolda" },
  { id: "SEF-2025-0311", vehicle: "34 TRK 3308", trailer: "Frigorifik", driver: "Recep Yaman", origin: "İstanbul", dest: "Münih", startDate: "07.01.2025", endDate: "11.01.2025", loadCount: 2, status: "Yolda" },
  { id: "SEF-2025-0310", vehicle: "06 OLS 2271", trailer: "-", driver: "Serdar Taş", origin: "Ankara", dest: "İzmir", startDate: "06.01.2025", endDate: "07.01.2025", loadCount: 1, status: "Tamamlandı" },
  { id: "SEF-2025-0309", vehicle: "34 TRK 9984", trailer: "Tenteli Dorse", driver: "Mehmet Pınar", origin: "İstanbul", dest: "Lyon", startDate: "04.01.2025", endDate: "09.01.2025", loadCount: 1, status: "Tamamlandı" },
  { id: "SEF-2025-0308", vehicle: "34 TRK 4521", trailer: "Tenteli Dorse", driver: "Osman Karaca", origin: "İstanbul", dest: "Antwerp", startDate: "01.01.2025", endDate: "06.01.2025", loadCount: 1, status: "Tamamlandı" },
  { id: "SEF-2025-0307", vehicle: "35 OLS 1122", trailer: "-", driver: "Kamil Doğan", origin: "İzmir", dest: "Ankara", startDate: "03.01.2025", endDate: "03.01.2025", loadCount: 0, status: "İptal" },
  { id: "SEF-2025-0306", vehicle: "34 TRK 7733", trailer: "Açık Kasa", driver: "Sinan Kurt", origin: "İstanbul", dest: "Hamburg", startDate: "10.01.2025", endDate: "15.01.2025", loadCount: 0, status: "Beklemede" },
];

const INVOICES = [
  { id: "FAT-2025-0523", customer: "Arçelik A.Ş.", ref: "YUK-2025-1247", date: "08.01.2025", due: "22.01.2025", total: "62.000,00", currency: "EUR", status: "Beklemede" },
  { id: "FAT-2025-0522", customer: "Borusan Lojistik", ref: "SEF-2025-0310", date: "07.01.2025", due: "21.01.2025", total: "48.500,00", currency: "EUR", status: "Ödendi" },
  { id: "FAT-2025-0521", customer: "Migros Ticaret", ref: "YUK-2025-1245", date: "07.01.2025", due: "21.01.2025", total: "8.750,00", currency: "TRY", status: "Ödendi" },
  { id: "FAT-2025-0520", customer: "Anadolu Efes", ref: "YUK-2025-1244", date: "06.01.2025", due: "20.01.2025", total: "41.800,00", currency: "EUR", status: "Beklemede" },
  { id: "FAT-2025-0519", customer: "Ülker Bisküvi", ref: "YUK-2025-1243", date: "06.01.2025", due: "20.01.2025", total: "39.600,00", currency: "EUR", status: "Ödendi" },
  { id: "FAT-2025-0518", customer: "Koç Holding", ref: "SEF-2025-0308", date: "05.01.2025", due: "19.01.2025", total: "34.200,00", currency: "EUR", status: "Vadesi Geçti" },
  { id: "FAT-2025-0517", customer: "Türk Telekom", ref: "YUK-2025-1242", date: "03.01.2025", due: "17.01.2025", total: "5.200,00", currency: "TRY", status: "Beklemede" },
  { id: "FAT-2025-0516", customer: "Yıldız Holding", ref: "SEF-2025-0306", date: "02.01.2025", due: "16.01.2025", total: "78.500,00", currency: "GBP", status: "Ödendi" },
];

const VEHICLES = [
  { id: "34 TRK 4521", type: "Tır", trailer: "Tenteli Dorse", driver: "Osman Karaca", capacity: "24.000 kg", year: "2021", active: true },
  { id: "34 TRK 3308", type: "Tır", trailer: "Frigorifik", driver: "Recep Yaman", capacity: "20.000 kg", year: "2022", active: true },
  { id: "06 OLS 2271", type: "Kamyon", trailer: "-", driver: "Serdar Taş", capacity: "10.000 kg", year: "2020", active: true },
  { id: "34 TRK 9984", type: "Tır", trailer: "Tenteli Dorse", driver: "Mehmet Pınar", capacity: "24.000 kg", year: "2023", active: true },
  { id: "34 TRK 7733", type: "Tır", trailer: "Açık Kasa", driver: "Sinan Kurt", capacity: "24.000 kg", year: "2019", active: true },
  { id: "35 OLS 1122", type: "Kamyon", trailer: "-", driver: "Kamil Doğan", capacity: "8.000 kg", year: "2018", active: false },
  { id: "34 TRK 6612", type: "Tır", trailer: "Tanker", driver: "-", capacity: "24.000 kg", year: "2020", active: false },
  { id: "41 OLS 9901", type: "Minibüs", trailer: "-", driver: "Hüseyin Bal", capacity: "1.500 kg", year: "2022", active: true },
];

const USERS_DATA = [
  { id: 1, name: "Bülent Serhan", email: "bulent@olslojistik.com", phone: "+90 532 100 0001", role: "Admin", status: "Aktif", lastLogin: "08.01.2025 07:30", initials: "BS" },
  { id: 2, name: "Kemal Öztürk", email: "kemal@olslojistik.com", phone: "+90 532 111 2233", role: "Operasyon", status: "Aktif", lastLogin: "08.01.2025 09:41", initials: "KÖ" },
  { id: 3, name: "Seda Kara", email: "seda@olslojistik.com", phone: "+90 533 222 3344", role: "Satış", status: "Aktif", lastLogin: "08.01.2025 08:55", initials: "SK" },
  { id: 4, name: "Nilüfer Ay", email: "nilufer@olslojistik.com", phone: "+90 534 333 4455", role: "Satış", status: "Aktif", lastLogin: "07.01.2025 17:20", initials: "NA" },
  { id: 5, name: "Tolga Erman", email: "tolga@olslojistik.com", phone: "+90 535 444 5566", role: "Muhasebe", status: "Aktif", lastLogin: "08.01.2025 10:05", initials: "TE" },
  { id: 6, name: "Ayla Demirtaş", email: "ayla@olslojistik.com", phone: "+90 537 666 7788", role: "Operasyon", status: "Pasif", lastLogin: "15.12.2024 14:00", initials: "AD" },
  { id: 7, name: "Gökhan Tekin", email: "gokhan@olslojistik.com", phone: "+90 538 777 8899", role: "Operasyon", status: "Aktif", lastLogin: "08.01.2025 09:10", initials: "GT" },
];

const SUPPORT_DATA = [
  { id: "SUP-2025-0089", from: "Ahmet Yılmaz", company: "Borusan Lojistik", subject: "Teslimat gecikme bildirimi", date: "08.01.2025 11:23", category: "Operasyon", status: "Açık", priority: "Yüksek" },
  { id: "SUP-2025-0088", from: "Fatma Kaya", company: "Arçelik A.Ş.", subject: "Fatura tutarsızlığı", date: "07.01.2025 16:40", category: "Muhasebe", status: "Çözüldü", priority: "Orta" },
  { id: "SUP-2025-0087", from: "Mehmet Demir", company: "Migros Ticaret", subject: "Şifre sıfırlama talebi", date: "07.01.2025 09:15", category: "Teknik", status: "Çözüldü", priority: "Düşük" },
  { id: "SUP-2025-0086", from: "Zeynep Şahin", company: "Koç Holding", subject: "Yeni araç ekleme yardımı", date: "06.01.2025 14:30", category: "Operasyon", status: "Açık", priority: "Orta" },
  { id: "SUP-2025-0085", from: "Ali Çelik", company: "Teklas Kauçuk", subject: "Teklif formu erişim sorunu", date: "05.01.2025 10:00", category: "Teknik", status: "Açık", priority: "Yüksek" },
  { id: "SUP-2025-0084", from: "Selin Arslan", company: "Anadolu Efes", subject: "Müşteri kaydı güncellemesi", date: "04.01.2025 13:50", category: "Veri", status: "Çözüldü", priority: "Düşük" },
  { id: "SUP-2025-0083", from: "Murat Aydın", company: "Türk Telekom", subject: "API entegrasyon hatası", date: "03.01.2025 08:20", category: "Teknik", status: "Açık", priority: "Yüksek" },
];

// ─────────────────────────────────────────────
// DASHBOARD / CHART DATA
// ─────────────────────────────────────────────
const MONTHLY_SHIPMENTS = [
  { ay: "Ağu", sevkiyat: 98, gelir: 620, teslimOrani: 94 },
  { ay: "Eyl", sevkiyat: 112, gelir: 710, teslimOrani: 96 },
  { ay: "Eki", sevkiyat: 128, gelir: 785, teslimOrani: 92 },
  { ay: "Kas", sevkiyat: 119, gelir: 730, teslimOrani: 95 },
  { ay: "Ara", sevkiyat: 134, gelir: 842, teslimOrani: 97 },
  { ay: "Oca", sevkiyat: 142, gelir: 848, teslimOrani: 94 },
];

const LOAD_TYPE_DIST = [
  { name: "Genel Kargo", value: 58, color: "#2563EB" },
  { name: "ADR", value: 18, color: "#7C3AED" },
  { name: "Soğuk Zincir", value: 14, color: "#0891B2" },
  { name: "Tekstil", value: 10, color: "#059669" },
];

const WEEKLY_PERF = [
  { gun: "Pzt", teslim: 22, gecikme: 1 },
  { gun: "Sal", teslim: 18, gecikme: 2 },
  { gun: "Çar", teslim: 27, gecikme: 0 },
  { gun: "Per", teslim: 24, gecikme: 3 },
  { gun: "Cum", teslim: 30, gecikme: 1 },
  { gun: "Cmt", teslim: 12, gecikme: 0 },
  { gun: "Paz", teslim: 9, gecikme: 1 },
];

const RECENT_ACTIVITIES = [
  { icon: Package, color: "text-blue-600 bg-blue-50", text: "YUK-2025-1247 yükleme tamamlandı", time: "09:41", sub: "Arçelik A.Ş. · İstanbul → Milano" },
  { icon: Receipt, color: "text-emerald-600 bg-emerald-50", text: "FAT-2025-0522 ödeme alındı", time: "09:00", sub: "Borusan Lojistik · €48.500" },
  { icon: Truck, color: "text-indigo-600 bg-indigo-50", text: "SEF-2025-0312 yola çıktı", time: "06:45", sub: "34 TRK 4521 · Osman Karaca" },
  { icon: FileText, color: "text-amber-600 bg-amber-50", text: "T2025-0847 onay bekliyor", time: "08:30", sub: "Borusan Lojistik · €48.500" },
  { icon: AlertTriangle, color: "text-red-600 bg-red-50", text: "FAT-2025-0518 vadesi geçti", time: "Dün", sub: "Koç Holding · €34.200" },
  { icon: CheckCircle, color: "text-emerald-600 bg-emerald-50", text: "SEF-2025-0310 teslim edildi", time: "Dün", sub: "06 OLS 2271 · Ankara → İzmir" },
];

// ─────────────────────────────────────────────
// PRIMITIVE COMPONENTS
// ─────────────────────────────────────────────

const STATUS_STYLES: Record<string, string> = {
  "aktif": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "pasif": "bg-gray-100 text-gray-500 ring-1 ring-gray-200",
  "beklemede": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "onaylı": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "reddedildi": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "taslak": "bg-gray-100 text-gray-500 ring-1 ring-gray-200",
  "tamamlandı": "bg-blue-50 text-blue-700 ring-1 ring-blue-200",
  "yolda": "bg-indigo-50 text-indigo-700 ring-1 ring-indigo-200",
  "yükleniyor": "bg-violet-50 text-violet-700 ring-1 ring-violet-200",
  "iptal": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "ödendi": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "vadesi geçti": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "açık": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "çözüldü": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "yüksek": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "orta": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "düşük": "bg-gray-100 text-gray-500 ring-1 ring-gray-200",
};

function Badge({ label }: { label: string }) {
  return (
    <span className={clsx(
      "inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium font-mono tracking-wide",
      STATUS_STYLES[label.toLowerCase()] ?? "bg-gray-100 text-gray-600 ring-1 ring-gray-200"
    )}>
      {label}
    </span>
  );
}

function Btn({
  variant = "primary", size = "md", children, onClick, className, disabled, type = "button"
}: {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md";
  children: React.ReactNode;
  onClick?: () => void;
  className?: string;
  disabled?: boolean;
  type?: "button" | "submit";
}) {
  const base = "inline-flex items-center gap-1.5 font-medium rounded transition-all duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed select-none";
  const sizes = { sm: "px-2.5 py-1.5 text-xs", md: "px-3.5 py-2 text-sm" };
  const variants = {
    primary: "bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 shadow-sm",
    secondary: "bg-white text-gray-700 border border-gray-200 hover:bg-gray-50 active:bg-gray-100",
    ghost: "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
    danger: "bg-red-600 text-white hover:bg-red-700 shadow-sm",
  };
  return (
    <button type={type} onClick={onClick} disabled={disabled}
      className={clsx(base, sizes[size], variants[variant], className)}>
      {children}
    </button>
  );
}

function SearchInput({ value, onChange, placeholder = "Ara..." }: {
  value: string; onChange: (v: string) => void; placeholder?: string;
}) {
  return (
    <div className="relative">
      <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" />
      <input value={value} onChange={e => onChange(e.target.value)} placeholder={placeholder}
        className="pl-8 pr-3 py-1.5 text-sm border border-gray-200 rounded-md bg-white focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:border-blue-400 w-60 transition-all"
      />
    </div>
  );
}

function TextInput({ value, onChange, placeholder, error, disabled, type = "text" }: {
  value: string; onChange: (v: string) => void; placeholder?: string;
  error?: boolean; disabled?: boolean; type?: string;
}) {
  return (
    <input type={type} value={value} onChange={e => onChange(e.target.value)} placeholder={placeholder}
      disabled={disabled}
      className={clsx(
        "px-3 py-2 text-sm border rounded-md bg-white transition-all w-full",
        "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:border-blue-400",
        error ? "border-red-400 bg-red-50/30" : "border-gray-200",
        disabled && "bg-gray-50 text-gray-400 cursor-not-allowed"
      )}
    />
  );
}

function SelectInput({ value, onChange, options, disabled }: {
  value: string; onChange: (v: string) => void;
  options: { value: string; label: string }[]; disabled?: boolean;
}) {
  return (
    <select value={value} onChange={e => onChange(e.target.value)} disabled={disabled}
      className={clsx(
        "px-3 py-2 text-sm border border-gray-200 rounded-md bg-white w-full",
        "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500",
        disabled && "bg-gray-50 text-gray-400 cursor-not-allowed"
      )}>
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );
}

function TextareaInput({ value, onChange, placeholder, rows = 3 }: {
  value: string; onChange: (v: string) => void; placeholder?: string; rows?: number;
}) {
  return (
    <textarea value={value} onChange={e => onChange(e.target.value)} placeholder={placeholder} rows={rows}
      className="px-3 py-2 text-sm border border-gray-200 rounded-md bg-white w-full focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 resize-none"
    />
  );
}

function FormField({ label, required, error, hint, children }: {
  label: string; required?: boolean; error?: string; hint?: string; children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">
        {label}{required && <span className="text-red-500 ml-0.5">*</span>}
      </label>
      {children}
      {hint && !error && <span className="text-[11px] text-gray-400">{hint}</span>}
      {error && (
        <span className="text-[11px] text-red-600 flex items-center gap-1">
          <AlertCircle size={10} />{error}
        </span>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────
// DATA TABLE
// ─────────────────────────────────────────────
interface Column<T> {
  key: string; header: string; sortable?: boolean;
  render: (row: T) => React.ReactNode; width?: string;
}

function DataTable<T extends { id: string | number }>({
  data, columns, onRowClick, actions, loading
}: {
  data: T[]; columns: Column<T>[];
  onRowClick?: (row: T) => void;
  actions?: (row: T) => React.ReactNode;
  loading?: boolean;
}) {
  const [sortKey, setSortKey] = useState("");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");

  const sorted = [...data].sort((a, b) => {
    if (!sortKey) return 0;
    const av = String((a as Record<string, unknown>)[sortKey] ?? "");
    const bv = String((b as Record<string, unknown>)[sortKey] ?? "");
    return sortDir === "asc" ? av.localeCompare(bv, "tr") : bv.localeCompare(av, "tr");
  });

  if (loading) {
    return (
      <div className="overflow-x-auto">
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-gray-200 bg-gray-50/80">
              {columns.map(c => (
                <th key={c.key} className="text-left py-2.5 px-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 5 }).map((_, i) => (
              <tr key={i} className="border-b border-gray-100">
                {columns.map(c => (
                  <td key={c.key} className="py-3 px-3">
                    <div className="h-3 bg-gray-200 rounded animate-pulse" style={{ width: `${60 + Math.random() * 30}%` }} />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse">
        <thead>
          <tr className="border-b border-gray-200 bg-gray-50/80">
            {columns.map(c => (
              <th key={c.key}
                className={clsx(
                  "text-left py-2.5 px-3 text-[11px] font-semibold text-gray-500 uppercase tracking-wider select-none",
                  c.sortable && "cursor-pointer hover:text-gray-700 hover:bg-gray-100 transition-colors",
                  c.width
                )}
                onClick={() => {
                  if (!c.sortable) return;
                  if (sortKey === c.key) setSortDir(d => d === "asc" ? "desc" : "asc");
                  else { setSortKey(c.key); setSortDir("asc"); }
                }}>
                <span className="flex items-center gap-1">
                  {c.header}
                  {c.sortable && (
                    <ArrowUpDown size={10} className={clsx(sortKey === c.key ? "text-blue-600" : "text-gray-300")} />
                  )}
                </span>
              </th>
            ))}
            {actions && <th className="w-10 py-2.5 px-3" />}
          </tr>
        </thead>
        <tbody>
          {sorted.map((row, i) => (
            <motion.tr key={row.id}
              initial={{ opacity: 0, y: 4 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.15, delay: i * 0.02 }}
              className={clsx(
                "border-b border-gray-100 transition-colors",
                onRowClick && "cursor-pointer",
                i % 2 === 0 ? "bg-white" : "bg-gray-50/40",
                onRowClick && "hover:bg-blue-50/40"
              )}
              onClick={() => onRowClick?.(row)}>
              {columns.map(c => (
                <td key={c.key} className="py-2.5 px-3 text-sm text-gray-800">{c.render(row)}</td>
              ))}
              {actions && (
                <td className="py-2 px-3" onClick={e => e.stopPropagation()}>{actions(row)}</td>
              )}
            </motion.tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ─────────────────────────────────────────────
// SHARED LAYOUT COMPONENTS
// ─────────────────────────────────────────────
function EmptyState({ icon: Icon, title, desc }: {
  icon: React.ComponentType<{ size?: number; className?: string }>; title: string; desc: string;
}) {
  return (
    <div className="flex flex-col items-center justify-center py-20 text-center px-4">
      <div className="p-4 bg-gray-100 rounded-full mb-3">
        <Icon size={26} className="text-gray-400" />
      </div>
      <h3 className="text-sm font-semibold text-gray-700 mb-1">{title}</h3>
      <p className="text-xs text-gray-400 max-w-xs">{desc}</p>
    </div>
  );
}

function Pagination({ page, total, perPage, onChange }: {
  page: number; total: number; perPage: number; onChange: (p: number) => void;
}) {
  const pages = Math.ceil(total / perPage);
  const start = (page - 1) * perPage + 1;
  const end = Math.min(page * perPage, total);
  const pageNums = Array.from({ length: pages }, (_, i) => i + 1)
    .filter(p => p === 1 || p === pages || Math.abs(p - page) <= 1);

  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-white shrink-0">
      <span className="text-xs text-gray-500 font-mono">
        {total} kayıt · {start}–{end} gösteriliyor
      </span>
      <div className="flex items-center gap-1">
        <button onClick={() => onChange(page - 1)} disabled={page === 1}
          className="p-1.5 rounded text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
          <ChevronLeft size={13} />
        </button>
        {pageNums.map((p, i) => (
          <span key={p}>
            {i > 0 && pageNums[i - 1] !== p - 1 && (
              <span className="px-1 text-gray-300 text-xs">…</span>
            )}
            <button onClick={() => onChange(p)}
              className={clsx(
                "w-7 h-7 text-xs rounded font-mono transition-colors",
                p === page ? "bg-blue-600 text-white shadow-sm" : "text-gray-600 hover:bg-gray-100"
              )}>
              {p}
            </button>
          </span>
        ))}
        <button onClick={() => onChange(page + 1)} disabled={page === pages}
          className="p-1.5 rounded text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
          <ChevronRight size={13} />
        </button>
      </div>
    </div>
  );
}

function RowActions({ onView, onEdit, onDelete }: {
  onView?: () => void; onEdit?: () => void; onDelete?: () => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", h);
    return () => document.removeEventListener("mousedown", h);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button onClick={() => setOpen(o => !o)}
        className="p-1.5 rounded text-gray-400 hover:bg-gray-100 hover:text-gray-700 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500">
        <MoreHorizontal size={14} />
      </button>
      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, scale: 0.92, y: -4 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.92 }}
            transition={{ duration: 0.12, ease: "easeOut" }}
            className="absolute right-0 top-8 w-36 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-20">
            {onView && (
              <button onClick={() => { onView(); setOpen(false); }}
                className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors">
                <Eye size={12} />Görüntüle
              </button>
            )}
            {onEdit && (
              <button onClick={() => { onEdit(); setOpen(false); }}
                className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors">
                <Edit2 size={12} />Düzenle
              </button>
            )}
            {onDelete && (
              <button onClick={() => { onDelete(); setOpen(false); }}
                className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-red-600 hover:bg-red-50 transition-colors">
                <Trash2 size={12} />Sil
              </button>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

// ─────────────────────────────────────────────
// DRAWER
// ─────────────────────────────────────────────
function Drawer({ open, onClose, title, subtitle, children, width = "w-[580px]", footer }: {
  open: boolean; onClose: () => void; title: string; subtitle?: string;
  children: React.ReactNode; width?: string; footer?: React.ReactNode;
}) {
  useEffect(() => {
    document.body.style.overflow = open ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [open]);

  return (
    <AnimatePresence>
      {open && (
        <>
          <motion.div
            initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
            transition={{ duration: 0.18 }}
            className="fixed inset-0 bg-black/25 z-40 backdrop-blur-[1px]"
            onClick={onClose}
          />
          <motion.div
            initial={{ x: "100%", opacity: 0.5 }}
            animate={{ x: 0, opacity: 1 }}
            exit={{ x: "100%", opacity: 0 }}
            transition={{ duration: 0.22, ease: [0.25, 0.1, 0.25, 1] }}
            className={clsx("fixed right-0 top-0 bottom-0 z-50 bg-white shadow-2xl flex flex-col", width)}>
            <div className="flex items-start justify-between px-6 py-4 border-b border-gray-200 shrink-0">
              <div>
                <h2 className="text-base font-semibold text-gray-900">{title}</h2>
                {subtitle && <p className="text-xs text-gray-500 mt-0.5">{subtitle}</p>}
              </div>
              <button onClick={onClose}
                className="p-1.5 rounded hover:bg-gray-100 text-gray-400 hover:text-gray-700 transition-colors mt-0.5 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500">
                <X size={16} />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto">{children}</div>
            {footer && (
              <div className="px-6 py-4 border-t border-gray-200 bg-gray-50/50 shrink-0">{footer}</div>
            )}
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}

// ─────────────────────────────────────────────
// MODAL
// ─────────────────────────────────────────────
function Modal({ open, onClose, title, children }: {
  open: boolean; onClose: () => void; title: string; children: React.ReactNode;
}) {
  return (
    <AnimatePresence>
      {open && (
        <>
          <motion.div
            initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
            transition={{ duration: 0.16 }}
            className="fixed inset-0 bg-black/30 z-50 flex items-center justify-center p-4"
            onClick={onClose}>
            <motion.div
              initial={{ opacity: 0, scale: 0.94, y: 8 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.94, y: 8 }}
              transition={{ duration: 0.18, ease: "easeOut" }}
              className="bg-white rounded-xl shadow-2xl max-w-md w-full"
              onClick={e => e.stopPropagation()}>
              <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
                <h3 className="text-sm font-semibold text-gray-900">{title}</h3>
                <button onClick={onClose} className="p-1 rounded hover:bg-gray-100 text-gray-400">
                  <X size={14} />
                </button>
              </div>
              {children}
            </motion.div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}

// ─────────────────────────────────────────────
// TABS
// ─────────────────────────────────────────────
function Tabs({ tabs, active, onChange, className }: {
  tabs: string[]; active: string; onChange: (t: string) => void; className?: string;
}) {
  return (
    <div className={clsx("flex items-center gap-0 border-b border-gray-200", className)}>
      {tabs.map(t => (
        <button key={t} onClick={() => onChange(t)}
          className={clsx(
            "px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors whitespace-nowrap",
            active === t ? "border-blue-600 text-blue-600" : "border-transparent text-gray-500 hover:text-gray-700"
          )}>
          {t}
        </button>
      ))}
    </div>
  );
}

// ─────────────────────────────────────────────
// MODULE PAGE WRAPPER
// ─────────────────────────────────────────────
function ModulePage({ title, action, search, searchPlaceholder, onSearchChange, filters, children }: {
  title: string; action?: React.ReactNode; search: string;
  searchPlaceholder?: string; onSearchChange: (v: string) => void;
  filters?: React.ReactNode; children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between px-6 py-3.5 bg-white border-b border-gray-200 shrink-0 gap-3 flex-wrap">
        <h1 className="text-base font-semibold text-gray-900">{title}</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <SearchInput value={search} onChange={onSearchChange} placeholder={searchPlaceholder} />
          {filters}
          {action}
        </div>
      </div>
      <div className="flex-1 overflow-y-auto">{children}</div>
    </div>
  );
}

// ─────────────────────────────────────────────
// TOAST
// ─────────────────────────────────────────────
function ToastContainer({ toasts, onRemove }: {
  toasts: ToastData[]; onRemove: (id: number) => void;
}) {
  return (
    <div className="fixed bottom-6 right-6 z-[200] flex flex-col gap-2">
      <AnimatePresence>
        {toasts.map(t => (
          <motion.div key={t.id}
            initial={{ opacity: 0, y: 20, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, x: 40, scale: 0.95 }}
            transition={{ duration: 0.2, ease: "easeOut" }}
            className={clsx(
              "flex items-center gap-2.5 px-4 py-3 rounded-lg shadow-lg text-sm font-medium min-w-[260px]",
              t.type === "success" && "bg-gray-900 text-white",
              t.type === "error" && "bg-red-600 text-white",
              t.type === "info" && "bg-blue-600 text-white"
            )}>
            {t.type === "success" && <CheckCircle size={15} className="shrink-0" />}
            {t.type === "error" && <XCircle size={15} className="shrink-0" />}
            {t.type === "info" && <Info size={15} className="shrink-0" />}
            <span className="flex-1">{t.message}</span>
            <button onClick={() => onRemove(t.id)} className="ml-1 opacity-60 hover:opacity-100">
              <X size={13} />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
}

// ─────────────────────────────────────────────
// CUSTOMERS MODULE
// ─────────────────────────────────────────────
function CustomersModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof CUSTOMERS[0] | null>(null);
  const [tab, setTab] = useState("Genel Bilgiler");
  const perPage = 8;

  const filtered = CUSTOMERS.filter(c =>
    c.name.toLowerCase().includes(search.toLowerCase()) ||
    c.city.toLowerCase().includes(search.toLowerCase()) ||
    c.taxNo.includes(search)
  );

  const columns: Column<typeof CUSTOMERS[0]>[] = [
    { key: "id", header: "Kod", sortable: true, width: "w-24", render: r => <span className="font-mono text-[11px] text-blue-600">{r.id}</span> },
    { key: "name", header: "Müşteri Adı", sortable: true, render: r => <span className="font-semibold">{r.name}</span> },
    { key: "type", header: "Tip", render: r => <span className="text-xs text-gray-500">{r.type}</span> },
    { key: "taxNo", header: "Vergi No", render: r => <span className="font-mono text-xs">{r.taxNo}</span> },
    { key: "city", header: "Şehir", sortable: true, render: r => r.city },
    { key: "contact", header: "Yetkili", render: r => r.contact },
    { key: "phone", header: "Telefon", render: r => <span className="font-mono text-xs text-gray-500">{r.phone}</span> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
  ];

  function openNew() { setSelected(null); setTab("Genel Bilgiler"); setOpen(true); }
  function openEdit(r: typeof CUSTOMERS[0]) { setSelected(r); setTab("Genel Bilgiler"); setOpen(true); }

  return (
    <>
      <ModulePage title="Müşteriler" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Ad, şehir, vergi no..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={openNew}><Plus size={14} />Yeni Müşteri</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Users} title="Kayıt bulunamadı" desc="Arama kriterlerinize uygun müşteri bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => openEdit(r)}
                actions={r => <RowActions onView={() => openEdit(r)} onEdit={() => openEdit(r)} onDelete={() => addToast(`${r.name} silindi`, "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.name : "Yeni Müşteri"}
        subtitle={selected ? `${selected.id} · ${selected.type}` : "Yeni müşteri kaydı oluştur"}
        footer={
          <div className="flex gap-2">
            <Btn onClick={() => { setOpen(false); addToast(selected ? "Müşteri güncellendi" : "Müşteri oluşturuldu", "success"); }}>
              <CheckCircle size={14} />Kaydet
            </Btn>
            <Btn variant="secondary" onClick={() => setOpen(false)}>İptal</Btn>
          </div>
        }>
        <Tabs tabs={["Genel Bilgiler", "Adresler", "Yetkililer", "İlişkili İşlemler"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Genel Bilgiler" && (
          <div className="p-6 grid grid-cols-2 gap-4">
            <FormField label="Müşteri Adı" required>
              <TextInput value={selected?.name ?? ""} onChange={() => {}} placeholder="Şirket adı" />
            </FormField>
            <FormField label="Müşteri Tipi" required>
              <SelectInput value={selected?.type ?? "Kurumsal"} onChange={() => {}} options={[
                { value: "Kurumsal", label: "Kurumsal" },
                { value: "KOBİ", label: "KOBİ" },
                { value: "Bireysel", label: "Bireysel" },
              ]} />
            </FormField>
            <FormField label="Vergi No" required>
              <TextInput value={selected?.taxNo ?? ""} onChange={() => {}} placeholder="1234567890" />
            </FormField>
            <FormField label="Vergi Dairesi">
              <TextInput value="Büyük Mükellefler" onChange={() => {}} />
            </FormField>
            <FormField label="Şehir">
              <TextInput value={selected?.city ?? ""} onChange={() => {}} placeholder="İstanbul" />
            </FormField>
            <FormField label="Ülke">
              <SelectInput value="Türkiye" onChange={() => {}} options={[
                { value: "Türkiye", label: "Türkiye" },
                { value: "Almanya", label: "Almanya" },
                { value: "Hollanda", label: "Hollanda" },
              ]} />
            </FormField>
            <FormField label="Telefon">
              <TextInput value={selected?.phone ?? ""} onChange={() => {}} placeholder="+90 212 555 0000" />
            </FormField>
            <FormField label="E-posta">
              <TextInput value={selected?.email ?? ""} onChange={() => {}} type="email" placeholder="info@sirket.com" />
            </FormField>
            <div className="col-span-2">
              <FormField label="Durum">
                <SelectInput value={selected?.status ?? "Aktif"} onChange={() => {}} options={[
                  { value: "Aktif", label: "Aktif" },
                  { value: "Pasif", label: "Pasif" },
                  { value: "Beklemede", label: "Beklemede" },
                ]} />
              </FormField>
            </div>
            <div className="col-span-2">
              <FormField label="Not">
                <TextareaInput value="" onChange={() => {}} placeholder="Müşteri hakkında notlar..." />
              </FormField>
            </div>
          </div>
        )}
        {tab === "Adresler" && (
          <div className="p-6 space-y-3">
            {selected && (
              <div className="border border-gray-200 rounded-lg p-4">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-[11px] font-semibold text-blue-600 uppercase tracking-wide">Kayıtlı Adres</span>
                  <Btn variant="ghost" size="sm"><Edit2 size={11} />Düzenle</Btn>
                </div>
                <div className="flex items-start gap-2 text-sm text-gray-700">
                  <MapPin size={13} className="text-gray-400 mt-0.5 shrink-0" />
                  <div>
                    <p>Büyükdere Cad. No:40/A, Levent</p>
                    <p className="text-gray-500">{selected.city} / Türkiye · 34394</p>
                  </div>
                </div>
              </div>
            )}
            <Btn variant="secondary" size="sm"><Plus size={12} />Adres Ekle</Btn>
          </div>
        )}
        {tab === "Yetkililer" && (
          <div className="p-6 space-y-3">
            {selected && (
              <div className="border border-gray-200 rounded-lg p-4">
                <div className="flex items-center justify-between mb-2">
                  <span className="font-semibold text-sm text-gray-900">{selected.contact}</span>
                  <Badge label="Aktif" />
                </div>
                <div className="space-y-1.5">
                  <div className="flex items-center gap-2 text-xs text-gray-500">
                    <Phone size={11} /><span>{selected.phone}</span>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-gray-500">
                    <Mail size={11} /><span>{selected.email}</span>
                  </div>
                </div>
              </div>
            )}
            <Btn variant="secondary" size="sm"><Plus size={12} />Yetkili Ekle</Btn>
          </div>
        )}
        {tab === "İlişkili İşlemler" && (
          <div className="p-6 space-y-2">
            {selected && QUOTES.filter(q => q.customer === selected.name).map(q => (
              <div key={q.id} className="flex items-center justify-between p-3 border border-gray-100 rounded-lg hover:bg-gray-50 transition-colors">
                <div>
                  <span className="font-mono text-[11px] text-blue-600">{q.id}</span>
                  <p className="text-sm text-gray-700 mt-0.5">{q.route}</p>
                  <p className="text-xs text-gray-400">{q.date} · {q.type}</p>
                </div>
                <div className="flex items-center gap-3 text-right">
                  <span className="text-sm font-semibold text-gray-900">{q.amount} {q.currency}</span>
                  <Badge label={q.status} />
                </div>
              </div>
            ))}
            {selected && QUOTES.filter(q => q.customer === selected.name).length === 0 && (
              <p className="text-sm text-gray-400 py-10 text-center">Bu müşteriye ait işlem bulunamadı.</p>
            )}
          </div>
        )}
      </Drawer>
    </>
  );
}

// ─────────────────────────────────────────────
// QUOTES MODULE
// ─────────────────────────────────────────────
function QuotesModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof QUOTES[0] | null>(null);
  const [tab, setTab] = useState("Genel Bilgiler");
  const [convertModal, setConvertModal] = useState(false);
  const perPage = 7;

  const filtered = QUOTES.filter(q =>
    q.id.toLowerCase().includes(search.toLowerCase()) ||
    q.customer.toLowerCase().includes(search.toLowerCase()) ||
    q.route.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof QUOTES[0]>[] = [
    { key: "id", header: "Teklif No", sortable: true, render: r => <span className="font-mono text-[11px] text-blue-600">{r.id}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: r => <span className="font-semibold">{r.customer}</span> },
    { key: "route", header: "Güzergâh", render: r => <span className="text-gray-700">{r.route}</span> },
    { key: "type", header: "Taşıma Tipi", render: r => <span className="text-xs text-gray-500">{r.type}</span> },
    { key: "responsible", header: "Sorumlu", render: r => r.responsible },
    { key: "amount", header: "Tutar", render: r => <span className="font-mono text-xs font-semibold">{r.amount} {r.currency}</span> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
    { key: "date", header: "Tarih", render: r => <span className="font-mono text-xs text-gray-500">{r.date}</span> },
  ];

  return (
    <>
      <ModulePage title="Teklifler" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Teklif no, müşteri, güzergâh..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={() => { setSelected(null); setTab("Genel Bilgiler"); setOpen(true); }}><Plus size={14} />Yeni Teklif</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={FileText} title="Teklif bulunamadı" desc="Arama kriterlerine uygun teklif bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setTab("Genel Bilgiler"); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast("Teklif silindi", "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.id : "Yeni Teklif"}
        subtitle={selected ? `${selected.customer} · ${selected.route}` : "Yeni teklif oluştur"}
        width="w-[640px]"
        footer={
          <div className="flex items-center gap-2">
            <Btn onClick={() => { setOpen(false); addToast("Teklif kaydedildi", "success"); }}>
              <CheckCircle size={14} />Kaydet
            </Btn>
            {selected?.status === "Onaylı" && (
              <Btn variant="secondary" onClick={() => setConvertModal(true)}>
                <Package size={14} />Yüke Dönüştür
              </Btn>
            )}
            <Btn variant="ghost" onClick={() => setOpen(false)}>İptal</Btn>
          </div>
        }>
        <Tabs tabs={["Genel Bilgiler", "Taraflar", "Güzergâh", "Yük İçeriği", "Mali Kalemler", "Dosyalar"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Genel Bilgiler" && (
          <div className="p-6 grid grid-cols-2 gap-4">
            <FormField label="Teklif No">
              <TextInput value={selected?.id ?? "Otomatik"} onChange={() => {}} disabled />
            </FormField>
            <FormField label="Tarih" required>
              <TextInput value={selected?.date ?? "08.01.2025"} onChange={() => {}} />
            </FormField>
            <FormField label="Taşıma Tipi" required>
              <SelectInput value={selected?.type ?? "Karayolu"} onChange={() => {}} options={[
                { value: "Karayolu", label: "Karayolu" }, { value: "Denizyolu", label: "Denizyolu" },
                { value: "Havayolu", label: "Havayolu" }, { value: "Demiryolu", label: "Demiryolu" },
              ]} />
            </FormField>
            <FormField label="Sorumlu" required>
              <SelectInput value={selected?.responsible ?? "Kemal Öztürk"} onChange={() => {}} options={[
                { value: "Kemal Öztürk", label: "Kemal Öztürk" },
                { value: "Seda Kara", label: "Seda Kara" },
                { value: "Nilüfer Ay", label: "Nilüfer Ay" },
              ]} />
            </FormField>
            <FormField label="Durum">
              <SelectInput value={selected?.status ?? "Taslak"} onChange={() => {}} options={[
                { value: "Taslak", label: "Taslak" }, { value: "Beklemede", label: "Beklemede" },
                { value: "Onaylı", label: "Onaylı" }, { value: "Reddedildi", label: "Reddedildi" },
              ]} />
            </FormField>
            <FormField label="Geçerlilik Tarihi">
              <TextInput value="22.01.2025" onChange={() => {}} />
            </FormField>
            <div className="col-span-2">
              <FormField label="Not">
                <TextareaInput value="" onChange={() => {}} placeholder="Teklif notları..." />
              </FormField>
            </div>
          </div>
        )}
        {tab === "Taraflar" && (
          <div className="p-6 space-y-4">
            <FormField label="Müşteri" required>
              <SelectInput value={selected?.customer ?? ""} onChange={() => {}} options={CUSTOMERS.map(c => ({ value: c.name, label: c.name }))} />
            </FormField>
            <FormField label="Gönderici">
              <TextInput value="" onChange={() => {}} placeholder="Gönderici firma adı" />
            </FormField>
            <FormField label="Alıcı">
              <TextInput value="" onChange={() => {}} placeholder="Alıcı firma adı" />
            </FormField>
          </div>
        )}
        {tab === "Güzergâh" && (
          <div className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <FormField label="Yükleme Noktası" required>
                <TextInput value={selected?.route.split("→")[0].trim() ?? ""} onChange={() => {}} placeholder="Şehir / Adres" />
              </FormField>
              <FormField label="Teslimat Noktası" required>
                <TextInput value={selected?.route.split("→")[1]?.trim() ?? ""} onChange={() => {}} placeholder="Şehir / Adres" />
              </FormField>
              <FormField label="Yükleme Tarihi">
                <TextInput value="10.01.2025" onChange={() => {}} />
              </FormField>
              <FormField label="Teslim Tarihi">
                <TextInput value="14.01.2025" onChange={() => {}} />
              </FormField>
            </div>
          </div>
        )}
        {tab === "Yük İçeriği" && (
          <div className="p-6 space-y-4">
            <div className="grid grid-cols-3 gap-4">
              <FormField label="Yük Tipi">
                <SelectInput value="Genel Kargo" onChange={() => {}} options={[
                  { value: "Genel Kargo", label: "Genel Kargo" }, { value: "ADR", label: "ADR" },
                  { value: "Soğuk Zincir", label: "Soğuk Zincir" }, { value: "Tekstil", label: "Tekstil" },
                ]} />
              </FormField>
              <FormField label="Ağırlık (kg)">
                <TextInput value="18000" onChange={() => {}} type="number" />
              </FormField>
              <FormField label="Hacim (m³)">
                <TextInput value="72" onChange={() => {}} type="number" />
              </FormField>
            </div>
            <FormField label="Açıklama">
              <TextareaInput value="" onChange={() => {}} placeholder="Yük içeriği detayları..." />
            </FormField>
          </div>
        )}
        {tab === "Mali Kalemler" && (
          <div className="p-6 space-y-4">
            <div className="border border-gray-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">Kalem</th>
                    <th className="text-right px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">Tutar</th>
                    <th className="text-right px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">KDV</th>
                    <th className="text-right px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">Toplam</th>
                  </tr>
                </thead>
                <tbody>
                  {[
                    { name: "Nakliye Bedeli", amount: "44.000,00", kdv: "7.920,00", total: "51.920,00" },
                    { name: "Sigorta", amount: "2.500,00", kdv: "450,00", total: "2.950,00" },
                    { name: "Ek Hizmetler", amount: "1.500,00", kdv: "270,00", total: "1.770,00" },
                  ].map((item, i) => (
                    <tr key={i} className="border-b border-gray-100">
                      <td className="px-3 py-2.5">{item.name}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-xs">{item.amount}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-xs text-gray-500">%18 · {item.kdv}</td>
                      <td className="px-3 py-2.5 text-right font-mono text-xs font-semibold">{item.total}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-blue-50 border-t border-blue-200">
                  <tr>
                    <td colSpan={3} className="px-3 py-2.5 text-sm font-semibold text-blue-700">GENEL TOPLAM</td>
                    <td className="px-3 py-2.5 text-right font-mono text-sm font-bold text-blue-700">56.640,00 EUR</td>
                  </tr>
                </tfoot>
              </table>
            </div>
            <Btn variant="secondary" size="sm"><Plus size={12} />Kalem Ekle</Btn>
          </div>
        )}
        {tab === "Dosyalar" && (
          <div className="p-6">
            <div className="border-2 border-dashed border-gray-200 rounded-lg p-8 text-center mb-4">
              <Upload size={24} className="text-gray-300 mx-auto mb-2" />
              <p className="text-sm text-gray-500 mb-1">Dosyaları buraya sürükleyin</p>
              <p className="text-xs text-gray-400">PDF, DOC, XLS · Maks. 10 MB</p>
              <Btn variant="secondary" size="sm" className="mt-3"><Upload size={12} />Dosya Seç</Btn>
            </div>
            <div className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg">
              <File size={16} className="text-blue-600" />
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-800">Teklif_T2025-0846.pdf</p>
                <p className="text-xs text-gray-400">245 KB · 07.01.2025</p>
              </div>
              <Btn variant="ghost" size="sm"><Download size={12} /></Btn>
            </div>
          </div>
        )}
      </Drawer>

      <Modal open={convertModal} onClose={() => setConvertModal(false)} title="Yüke Dönüştür">
        <div className="p-6">
          <div className="flex items-start gap-3 p-3 bg-amber-50 border border-amber-200 rounded-lg mb-4">
            <AlertTriangle size={15} className="text-amber-600 mt-0.5 shrink-0" />
            <p className="text-sm text-amber-800">
              <strong>{selected?.id}</strong> numaralı teklif yüke dönüştürülecek. Bu işlem geri alınamaz.
            </p>
          </div>
          <p className="text-sm text-gray-600 mb-4">
            <strong>{selected?.customer}</strong> · {selected?.route}
          </p>
          <div className="flex gap-2 justify-end">
            <Btn onClick={() => { setConvertModal(false); setOpen(false); addToast("Yük oluşturuldu: YUK-2025-1248", "success"); }}>
              <CheckCircle size={14} />Dönüştür
            </Btn>
            <Btn variant="secondary" onClick={() => setConvertModal(false)}>Vazgeç</Btn>
          </div>
        </div>
      </Modal>
    </>
  );
}

// ─────────────────────────────────────────────
// LOADS MODULE
// ─────────────────────────────────────────────
function LoadsModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof LOADS[0] | null>(null);
  const [tab, setTab] = useState("Özet");
  const perPage = 7;

  const filtered = LOADS.filter(l =>
    l.id.toLowerCase().includes(search.toLowerCase()) ||
    l.customer.toLowerCase().includes(search.toLowerCase()) ||
    l.origin.toLowerCase().includes(search.toLowerCase()) ||
    l.dest.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof LOADS[0]>[] = [
    { key: "id", header: "Yük No", sortable: true, render: r => <span className="font-mono text-[11px] text-blue-600">{r.id}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: r => <span className="font-semibold">{r.customer}</span> },
    { key: "origin", header: "Güzergâh", render: r => <span className="text-gray-700">{r.origin} → {r.dest}</span> },
    { key: "type", header: "Tip", render: r => <span className="text-xs text-gray-500">{r.type}</span> },
    { key: "weight", header: "Ağırlık", render: r => <span className="font-mono text-xs">{r.weight} kg</span> },
    { key: "volume", header: "Hacim", render: r => <span className="font-mono text-xs">{r.volume} m³</span> },
    { key: "responsible", header: "Sorumlu", render: r => r.responsible },
    { key: "trip", header: "Sefer", render: r => r.trip === "-" ? <span className="text-gray-300">—</span> : <span className="font-mono text-[11px] text-indigo-600">{r.trip}</span> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
  ];

  const TIMELINE = [
    { label: "Teklif Onaylandı", date: "04.01.2025 10:30", done: true },
    { label: "Yük Oluşturuldu", date: "05.01.2025 09:00", done: true },
    { label: "Araç Tahsis Edildi", date: "06.01.2025 14:15", done: true },
    { label: "Yükleme Başladı", date: "08.01.2025 07:00", done: true },
    { label: "Yükleme Tamamlandı", date: "08.01.2025 11:30", done: false, active: true },
    { label: "Yolda", date: "—", done: false },
    { label: "Teslimat", date: "—", done: false },
  ];

  return (
    <>
      <ModulePage title="Yükler" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Yük no, müşteri, güzergâh..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={() => { setSelected(null); setOpen(true); }}><Plus size={14} />Yeni Yük</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Package} title="Yük bulunamadı" desc="Arama kriterlerine uygun yük bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setTab("Özet"); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast("Yük silindi", "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.id : "Yeni Yük"}
        subtitle={selected ? `${selected.customer} · ${selected.origin} → ${selected.dest}` : undefined}
        width="w-[600px]">
        {selected ? (
          <>
            <Tabs tabs={["Özet", "Zaman Çizelgesi", "Belgeler", "İlişkili Kayıtlar"]} active={tab} onChange={setTab} className="px-6" />
            {tab === "Özet" && (
              <div className="p-6 space-y-4">
                <div className="grid grid-cols-2 gap-3">
                  {[
                    ["Müşteri", selected.customer], ["Tip", selected.type],
                    ["Güzergâh", `${selected.origin} → ${selected.dest}`], ["Sorumlu", selected.responsible],
                    ["Ağırlık", `${selected.weight} kg`], ["Hacim", `${selected.volume} m³`],
                    ["Sefer", selected.trip], ["Durum", selected.status],
                  ].map(([k, v]) => (
                    <div key={k} className="bg-gray-50 rounded-lg p-3">
                      <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">{k}</p>
                      <p className="text-sm font-medium text-gray-800 mt-0.5">
                        {k === "Durum" ? <Badge label={v} /> : v}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            )}
            {tab === "Zaman Çizelgesi" && (
              <div className="p-6">
                <div className="space-y-0">
                  {TIMELINE.map((item, i) => (
                    <div key={i} className="flex gap-3">
                      <div className="flex flex-col items-center">
                        <div className={clsx(
                          "w-3 h-3 rounded-full border-2 mt-1 shrink-0 transition-colors",
                          item.done ? "bg-emerald-500 border-emerald-500" :
                          item.active ? "bg-blue-500 border-blue-500 ring-2 ring-blue-200" :
                          "bg-white border-gray-300"
                        )} />
                        {i < TIMELINE.length - 1 && (
                          <div className={clsx("w-0.5 flex-1 my-1", item.done ? "bg-emerald-300" : "bg-gray-200")} />
                        )}
                      </div>
                      <div className="pb-4">
                        <p className={clsx("text-sm font-medium", item.done || item.active ? "text-gray-900" : "text-gray-400")}>
                          {item.label}
                        </p>
                        <p className={clsx("text-xs", item.done ? "text-gray-500" : "text-gray-300")}>{item.date}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
            {tab === "Belgeler" && (
              <div className="p-6 space-y-2">
                {["CMR_YUK-2025-1247.pdf", "Fatura_YUK-1247.pdf"].map(f => (
                  <div key={f} className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg">
                    <File size={15} className="text-blue-600 shrink-0" />
                    <span className="text-sm text-gray-800 flex-1">{f}</span>
                    <Btn variant="ghost" size="sm"><Download size={12} /></Btn>
                  </div>
                ))}
                <Btn variant="secondary" size="sm" className="mt-2"><Upload size={12} />Belge Ekle</Btn>
              </div>
            )}
            {tab === "İlişkili Kayıtlar" && (
              <div className="p-6 space-y-2">
                {selected.trip !== "-" && (
                  <div className="p-3 border border-gray-200 rounded-lg">
                    <p className="text-[11px] text-gray-400 uppercase font-semibold">Bağlı Sefer</p>
                    <p className="font-mono text-sm text-indigo-600 mt-0.5">{selected.trip}</p>
                  </div>
                )}
                <div className="p-3 border border-gray-200 rounded-lg">
                  <p className="text-[11px] text-gray-400 uppercase font-semibold">Kaynak Teklif</p>
                  <p className="font-mono text-sm text-blue-600 mt-0.5">T2025-0846</p>
                </div>
              </div>
            )}
          </>
        ) : (
          <div className="p-6 grid grid-cols-2 gap-4">
            <FormField label="Müşteri" required>
              <SelectInput value="" onChange={() => {}} options={CUSTOMERS.map(c => ({ value: c.name, label: c.name }))} />
            </FormField>
            <FormField label="Yük Tipi" required>
              <SelectInput value="Genel Kargo" onChange={() => {}} options={[
                { value: "Genel Kargo", label: "Genel Kargo" }, { value: "ADR", label: "ADR" },
                { value: "Soğuk Zincir", label: "Soğuk Zincir" }, { value: "Tekstil", label: "Tekstil" },
              ]} />
            </FormField>
            <FormField label="Yükleme Noktası" required>
              <TextInput value="" onChange={() => {}} placeholder="Şehir" />
            </FormField>
            <FormField label="Teslimat Noktası" required>
              <TextInput value="" onChange={() => {}} placeholder="Şehir" />
            </FormField>
            <FormField label="Ağırlık (kg)">
              <TextInput value="" onChange={() => {}} type="number" placeholder="0" />
            </FormField>
            <FormField label="Hacim (m³)">
              <TextInput value="" onChange={() => {}} type="number" placeholder="0" />
            </FormField>
            <div className="col-span-2 flex gap-2 pt-2 border-t border-gray-100">
              <Btn onClick={() => { setOpen(false); addToast("Yük oluşturuldu", "success"); }}>
                <CheckCircle size={14} />Kaydet
              </Btn>
              <Btn variant="secondary" onClick={() => setOpen(false)}>İptal</Btn>
            </div>
          </div>
        )}
      </Drawer>
    </>
  );
}

// ─────────────────────────────────────────────
// TRIPS MODULE
// ─────────────────────────────────────────────
function TripsModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof TRIPS[0] | null>(null);
  const [tab, setTab] = useState("Özet");
  const [attachModal, setAttachModal] = useState(false);
  const [trailerWarn, setTrailerWarn] = useState(false);
  const perPage = 6;

  const filtered = TRIPS.filter(t =>
    t.id.toLowerCase().includes(search.toLowerCase()) ||
    t.vehicle.toLowerCase().includes(search.toLowerCase()) ||
    t.driver.toLowerCase().includes(search.toLowerCase()) ||
    `${t.origin} ${t.dest}`.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof TRIPS[0]>[] = [
    { key: "id", header: "Sefer No", sortable: true, render: r => <span className="font-mono text-[11px] text-blue-600">{r.id}</span> },
    { key: "vehicle", header: "Araç", render: r => <span className="font-mono text-xs font-semibold">{r.vehicle}</span> },
    { key: "trailer", header: "Romork", render: r => <span className="text-xs text-gray-500">{r.trailer}</span> },
    { key: "driver", header: "Sürücü", render: r => r.driver },
    { key: "origin", header: "Güzergâh", render: r => <span>{r.origin} → {r.dest}</span> },
    { key: "startDate", header: "Başlangıç", render: r => <span className="font-mono text-xs">{r.startDate}</span> },
    { key: "endDate", header: "Bitiş", render: r => <span className="font-mono text-xs">{r.endDate}</span> },
    { key: "loadCount", header: "Yük", render: r => <span className="font-mono text-xs text-center block">{r.loadCount}</span> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
  ];

  return (
    <>
      <ModulePage title="Seferler" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Sefer no, araç, sürücü, güzergâh..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={() => { setSelected(null); setOpen(true); }}><Plus size={14} />Yeni Sefer</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Truck} title="Sefer bulunamadı" desc="Arama kriterlerine uygun sefer bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setTab("Özet"); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast("Sefer silindi", "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.id : "Yeni Sefer"}
        subtitle={selected ? `${selected.vehicle} · ${selected.origin} → ${selected.dest}` : undefined}
        width="w-[620px]">
        {selected ? (
          <>
            <Tabs tabs={["Özet", "Araç", "Bağlı Yükler", "Hareketler"]} active={tab} onChange={setTab} className="px-6" />
            {tab === "Özet" && (
              <div className="p-6 space-y-4">
                <div className="grid grid-cols-2 gap-3">
                  {[
                    ["Araç", selected.vehicle], ["Romork", selected.trailer],
                    ["Sürücü", selected.driver], ["Güzergâh", `${selected.origin} → ${selected.dest}`],
                    ["Başlangıç", selected.startDate], ["Tahmini Bitiş", selected.endDate],
                    ["Yük Sayısı", String(selected.loadCount)], ["Durum", selected.status],
                  ].map(([k, v]) => (
                    <div key={k} className="bg-gray-50 rounded-lg p-3">
                      <p className="text-[11px] text-gray-400 uppercase font-semibold tracking-wide">{k}</p>
                      <p className="text-sm font-medium text-gray-800 mt-0.5">
                        {k === "Durum" ? <Badge label={v} /> : v}
                      </p>
                    </div>
                  ))}
                </div>
                <Btn variant="secondary" size="sm" onClick={() => setAttachModal(true)}>
                  <Package size={12} />Yük Bağla
                </Btn>
              </div>
            )}
            {tab === "Araç" && (
              <div className="p-6 space-y-3">
                <div className="border border-gray-200 rounded-lg p-4">
                  <div className="flex items-center justify-between mb-3">
                    <span className="font-mono text-base font-bold text-gray-900">{selected.vehicle}</span>
                    <Badge label="Aktif" />
                  </div>
                  {[["Tip", "Tır"], ["Romork", selected.trailer], ["Kapasite", "24.000 kg"], ["Model Yılı", "2021"]].map(([k, v]) => (
                    <div key={k} className="flex items-center justify-between py-1.5 border-b border-gray-100 last:border-0">
                      <span className="text-xs text-gray-500">{k}</span>
                      <span className="text-xs font-medium text-gray-800">{v}</span>
                    </div>
                  ))}
                </div>
                {selected.trailer === "Frigorifik" && (
                  <div className="flex items-start gap-2.5 p-3 bg-amber-50 border border-amber-200 rounded-lg">
                    <AlertTriangle size={14} className="text-amber-600 mt-0.5 shrink-0" />
                    <p className="text-xs text-amber-800">
                      <strong>Romork uyarısı:</strong> Frigorifik dorse seçildi. ADR yükler bu dorse ile taşınamaz.
                    </p>
                  </div>
                )}
              </div>
            )}
            {tab === "Bağlı Yükler" && (
              <div className="p-6 space-y-2">
                {LOADS.filter(l => l.trip === selected.id).map(l => (
                  <div key={l.id} className="flex items-center justify-between p-3 border border-gray-200 rounded-lg">
                    <div>
                      <span className="font-mono text-[11px] text-blue-600">{l.id}</span>
                      <p className="text-sm text-gray-700 mt-0.5">{l.customer} · {l.type}</p>
                      <p className="text-xs text-gray-400">{l.weight} kg · {l.volume} m³</p>
                    </div>
                    <Badge label={l.status} />
                  </div>
                ))}
                {LOADS.filter(l => l.trip === selected.id).length === 0 && (
                  <p className="text-sm text-gray-400 py-10 text-center">Bağlı yük bulunamadı.</p>
                )}
                <Btn variant="secondary" size="sm" onClick={() => setAttachModal(true)}>
                  <Plus size={12} />Yük Bağla
                </Btn>
              </div>
            )}
            {tab === "Hareketler" && (
              <div className="p-6 space-y-2">
                {[
                  { text: "Sefer oluşturuldu", time: "02.01.2025 08:00", user: "Kemal Öztürk" },
                  { text: "Araç tahsis edildi: 34 TRK 4521", time: "02.01.2025 08:30", user: "Kemal Öztürk" },
                  { text: "Yük bağlandı: YUK-2025-1247", time: "05.01.2025 10:00", user: "Seda Kara" },
                  { text: "Sefer yola çıktı", time: "08.01.2025 06:45", user: "Sistem" },
                ].map((h, i) => (
                  <div key={i} className="flex gap-3 text-sm">
                    <span className="font-mono text-[11px] text-gray-400 w-36 shrink-0 pt-0.5">{h.time}</span>
                    <div>
                      <p className="text-gray-700">{h.text}</p>
                      <p className="text-xs text-gray-400">{h.user}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        ) : (
          <div className="p-6 grid grid-cols-2 gap-4">
            {[
              { label: "Araç", child: <SelectInput value="34 TRK 4521" onChange={() => {}} options={VEHICLES.map(v => ({ value: v.id, label: v.id }))} /> },
              { label: "Sürücü", child: <SelectInput value="Osman Karaca" onChange={() => {}} options={VEHICLES.filter(v => v.driver).map(v => ({ value: v.driver, label: v.driver }))} /> },
              { label: "Kalkış Noktası", child: <TextInput value="" onChange={() => {}} placeholder="İstanbul" /> },
              { label: "Varış Noktası", child: <TextInput value="" onChange={() => {}} placeholder="Münih" /> },
              { label: "Başlangıç Tarihi", child: <TextInput value="" onChange={() => {}} placeholder="dd.mm.yyyy" /> },
              { label: "Tahmini Bitiş", child: <TextInput value="" onChange={() => {}} placeholder="dd.mm.yyyy" /> },
            ].map(({ label, child }) => (
              <FormField key={label} label={label}>{child}</FormField>
            ))}
            <div className="col-span-2 flex gap-2 pt-2 border-t border-gray-100">
              <Btn onClick={() => { setOpen(false); addToast("Sefer oluşturuldu", "success"); }}>
                <CheckCircle size={14} />Kaydet
              </Btn>
              <Btn variant="secondary" onClick={() => setOpen(false)}>İptal</Btn>
            </div>
          </div>
        )}
      </Drawer>

      <Modal open={attachModal} onClose={() => setAttachModal(false)} title="Yük Bağla">
        <div className="p-4 space-y-2">
          {LOADS.filter(l => l.trip === "-").map(l => (
            <div key={l.id} className="flex items-center justify-between p-3 border border-gray-200 rounded-lg hover:border-blue-300 hover:bg-blue-50/30 cursor-pointer transition-colors"
              onClick={() => {
                setAttachModal(false);
                addToast(`${l.id} sefere bağlandı`, "success");
                if (selected?.trailer === "Frigorifik" && l.type === "ADR") setTrailerWarn(true);
              }}>
              <div>
                <span className="font-mono text-[11px] text-blue-600">{l.id}</span>
                <p className="text-sm text-gray-700">{l.customer} · {l.type}</p>
                <p className="text-xs text-gray-400">{l.weight} kg</p>
              </div>
              <Badge label={l.status} />
            </div>
          ))}
        </div>
      </Modal>
    </>
  );
}

// ─────────────────────────────────────────────
// INVOICES MODULE
// ─────────────────────────────────────────────
function InvoicesModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof INVOICES[0] | null>(null);
  const perPage = 7;

  const filtered = INVOICES.filter(inv =>
    inv.id.toLowerCase().includes(search.toLowerCase()) ||
    inv.customer.toLowerCase().includes(search.toLowerCase()) ||
    inv.ref.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof INVOICES[0]>[] = [
    { key: "id", header: "Fatura No", sortable: true, render: r => <span className="font-mono text-[11px] text-blue-600">{r.id}</span> },
    { key: "customer", header: "Müşteri", sortable: true, render: r => <span className="font-semibold">{r.customer}</span> },
    { key: "ref", header: "Referans", render: r => <span className="font-mono text-[11px] text-indigo-600">{r.ref}</span> },
    { key: "date", header: "Fatura Tarihi", render: r => <span className="font-mono text-xs">{r.date}</span> },
    { key: "due", header: "Vade", render: r => <span className="font-mono text-xs">{r.due}</span> },
    { key: "total", header: "Toplam", render: r => <span className="font-mono text-xs font-bold">{r.total} {r.currency}</span> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
  ];

  return (
    <>
      <ModulePage title="Faturalar" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Fatura no, müşteri, referans..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={() => { setSelected(null); setOpen(true); }}><Plus size={14} />Yeni Fatura</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Receipt} title="Fatura bulunamadı" desc="Arama kriterlerine uygun fatura bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast("Fatura silindi", "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.id : "Yeni Fatura"}
        subtitle={selected ? `${selected.customer} · ${selected.date}` : undefined}
        width="w-[620px]"
        footer={
          <div className="flex items-center gap-2">
            <Btn onClick={() => { setOpen(false); addToast("Fatura kaydedildi", "success"); }}>
              <CheckCircle size={14} />Kaydet
            </Btn>
            <Btn variant="secondary" onClick={() => addToast("PDF hazırlanıyor...", "info")}>
              <Download size={14} />PDF İndir
            </Btn>
            <Btn variant="ghost" onClick={() => setOpen(false)}>İptal</Btn>
          </div>
        }>
        <div className="p-6 space-y-5">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Müşteri" required>
              <SelectInput value={selected?.customer ?? ""} onChange={() => {}} options={CUSTOMERS.map(c => ({ value: c.name, label: c.name }))} />
            </FormField>
            <FormField label="Referans (Yük / Sefer)">
              <TextInput value={selected?.ref ?? ""} onChange={() => {}} placeholder="YUK-2025-XXXX" />
            </FormField>
            <FormField label="Fatura Tarihi" required>
              <TextInput value={selected?.date ?? ""} onChange={() => {}} />
            </FormField>
            <FormField label="Vade Tarihi" required>
              <TextInput value={selected?.due ?? ""} onChange={() => {}} />
            </FormField>
          </div>
          <div>
            <p className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Kalemler</p>
            <div className="border border-gray-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">Açıklama</th>
                    <th className="text-right px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">Tutar</th>
                    <th className="text-right px-3 py-2 text-[11px] font-semibold text-gray-500 uppercase">KDV</th>
                    <th className="w-8" />
                  </tr>
                </thead>
                <tbody>
                  {[{ desc: "Nakliye Bedeli", amount: "52.000,00", kdv: "18" }, { desc: "Sigorta", amount: "2.500,00", kdv: "18" }].map((item, i) => (
                    <tr key={i} className="border-b border-gray-100">
                      <td className="px-3 py-2"><input className="text-sm text-gray-800 bg-transparent w-full focus:outline-none" defaultValue={item.desc} /></td>
                      <td className="px-3 py-2 text-right"><input className="text-sm font-mono text-right bg-transparent w-28 focus:outline-none" defaultValue={item.amount} /></td>
                      <td className="px-3 py-2 text-right"><span className="font-mono text-xs text-gray-500">%{item.kdv}</span></td>
                      <td className="px-2"><button className="text-gray-300 hover:text-red-500"><X size={12} /></button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Btn variant="secondary" size="sm" className="mt-2"><Plus size={12} />Kalem Ekle</Btn>
          </div>
          <div className="bg-gray-50 rounded-lg p-4 border border-gray-200 space-y-1.5">
            {[["Ara Toplam", "54.500,00 EUR"], ["KDV (%18)", "9.810,00 EUR"]].map(([k, v]) => (
              <div key={k} className="flex justify-between text-sm">
                <span className="text-gray-500">{k}</span>
                <span className="font-mono">{v}</span>
              </div>
            ))}
            <div className="flex justify-between text-base font-bold border-t border-gray-300 pt-2 mt-2">
              <span>TOPLAM</span>
              <span className="font-mono text-blue-700">{selected?.total ?? "64.310,00"} {selected?.currency ?? "EUR"}</span>
            </div>
          </div>
          <div className="flex items-start gap-2.5 p-3 bg-amber-50 border border-amber-200 rounded-lg">
            <AlertTriangle size={14} className="text-amber-600 mt-0.5 shrink-0" />
            <p className="text-xs text-amber-800">
              E-Fatura entegrasyonu: GİB bağlantısı kontrol ediliyor. Son senkronizasyon: 08.01.2025 09:00
            </p>
          </div>
        </div>
      </Drawer>
    </>
  );
}

// ─────────────────────────────────────────────
// VEHICLES MODULE
// ─────────────────────────────────────────────
function VehiclesModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof VEHICLES[0] | null>(null);
  const perPage = 7;

  const filtered = VEHICLES.filter(v =>
    v.id.toLowerCase().includes(search.toLowerCase()) ||
    v.driver.toLowerCase().includes(search.toLowerCase()) ||
    v.type.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof VEHICLES[0]>[] = [
    { key: "id", header: "Plaka", sortable: true, render: r => <span className="font-mono text-sm font-bold text-gray-900">{r.id}</span> },
    { key: "type", header: "Araç Tipi", render: r => r.type },
    { key: "trailer", header: "Romork", render: r => <span className="text-gray-600">{r.trailer}</span> },
    { key: "driver", header: "Sürücü", render: r => r.driver || <span className="text-gray-300">—</span> },
    { key: "capacity", header: "Kapasite", render: r => <span className="font-mono text-xs">{r.capacity}</span> },
    { key: "year", header: "Yıl", render: r => <span className="font-mono text-xs">{r.year}</span> },
    { key: "active", header: "Durum", render: r => <Badge label={r.active ? "Aktif" : "Pasif"} /> },
  ];

  return (
    <>
      <ModulePage title="Araçlar" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Plaka, sürücü, tip..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={() => { setSelected(null); setOpen(true); }}><Plus size={14} />Yeni Araç</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Car} title="Araç bulunamadı" desc="Arama kriterlerine uygun araç bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast("Araç silindi", "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.id : "Yeni Araç"}
        subtitle={selected ? `${selected.type} · ${selected.trailer}` : undefined}
        footer={
          <div className="flex gap-2">
            <Btn onClick={() => { setOpen(false); addToast(selected ? "Araç güncellendi" : "Araç eklendi", "success"); }}>
              <CheckCircle size={14} />Kaydet
            </Btn>
            <Btn variant="secondary" onClick={() => setOpen(false)}>İptal</Btn>
          </div>
        }>
        <div className="p-6 grid grid-cols-2 gap-4">
          <FormField label="Plaka" required>
            <TextInput value={selected?.id ?? ""} onChange={() => {}} placeholder="34 TRK 0000" />
          </FormField>
          <FormField label="Araç Tipi" required>
            <SelectInput value={selected?.type ?? "Tır"} onChange={() => {}} options={[
              { value: "Tır", label: "Tır" }, { value: "Kamyon", label: "Kamyon" },
              { value: "Minibüs", label: "Minibüs" }, { value: "Van", label: "Van" },
            ]} />
          </FormField>
          <FormField label="Romork Tipi">
            <SelectInput value={selected?.trailer ?? "-"} onChange={() => {}} options={[
              { value: "-", label: "Yok" }, { value: "Tenteli Dorse", label: "Tenteli Dorse" },
              { value: "Frigorifik", label: "Frigorifik" }, { value: "Açık Kasa", label: "Açık Kasa" },
              { value: "Tanker", label: "Tanker" },
            ]} />
          </FormField>
          <FormField label="Sürücü">
            <SelectInput value={selected?.driver ?? ""} onChange={() => {}} options={[
              { value: "", label: "Seçiniz" },
              ...VEHICLES.filter(v => v.driver).map(v => ({ value: v.driver, label: v.driver }))
            ]} />
          </FormField>
          <FormField label="Taşıma Kapasitesi (kg)">
            <TextInput value={selected?.capacity.replace(" kg", "") ?? ""} onChange={() => {}} type="number" />
          </FormField>
          <FormField label="Model Yılı">
            <TextInput value={selected?.year ?? ""} onChange={() => {}} type="number" />
          </FormField>
          <div className="col-span-2">
            <FormField label="Aktif">
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" defaultChecked={selected?.active} className="rounded border-gray-300 text-blue-600 focus:ring-blue-500" />
                <span className="text-sm text-gray-700">Araç aktif</span>
              </label>
            </FormField>
          </div>
        </div>
      </Drawer>
    </>
  );
}

// ─────────────────────────────────────────────
// USERS MODULE
// ─────────────────────────────────────────────
function UsersModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof USERS_DATA[0] | null>(null);
  const [tab, setTab] = useState("Profil");
  const perPage = 7;

  const MODULES_PERMS = ["Müşteriler", "Teklifler", "Yükler", "Seferler", "Faturalar", "Araçlar", "Kullanıcılar", "Destek"];
  const PERM_TYPES = ["Görüntüle", "Oluştur", "Düzenle", "Sil"];

  const filtered = USERS_DATA.filter(u =>
    u.name.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase()) ||
    u.role.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof USERS_DATA[0]>[] = [
    { key: "initials", header: "", width: "w-10", render: r => (
      <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-[10px] font-bold">
        {r.initials}
      </div>
    )},
    { key: "name", header: "Ad Soyad", sortable: true, render: r => <span className="font-semibold">{r.name}</span> },
    { key: "email", header: "E-posta", render: r => <span className="text-xs text-gray-500">{r.email}</span> },
    { key: "phone", header: "Telefon", render: r => <span className="font-mono text-xs text-gray-500">{r.phone}</span> },
    { key: "role", header: "Rol", render: r => <span className="text-xs">{r.role}</span> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
    { key: "lastLogin", header: "Son Giriş", render: r => <span className="font-mono text-[11px] text-gray-400">{r.lastLogin}</span> },
  ];

  const defaultPerms: Record<string, boolean> = {};
  MODULES_PERMS.forEach(m => {
    PERM_TYPES.forEach(p => {
      const key = `${m}-${p}`;
      defaultPerms[key] = (selected?.role === "Admin") ||
        (p === "Görüntüle") ||
        (selected?.role === "Operasyon" && ["Müşteriler","Yükler","Seferler","Araçlar"].includes(m) && p !== "Sil") ||
        (selected?.role === "Satış" && ["Müşteriler","Teklifler"].includes(m) && p !== "Sil") ||
        (selected?.role === "Muhasebe" && ["Faturalar","Müşteriler"].includes(m) && p !== "Sil");
    });
  });

  return (
    <>
      <ModulePage title="Kullanıcılar" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Ad, e-posta, rol..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={<Btn onClick={() => { setSelected(null); setTab("Profil"); setOpen(true); }}><Plus size={14} />Yeni Kullanıcı</Btn>}>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Shield} title="Kullanıcı bulunamadı" desc="Arama kriterlerine uygun kullanıcı bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setTab("Profil"); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast(`${r.name} silindi`, "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.name : "Yeni Kullanıcı"}
        subtitle={selected ? `${selected.role} · ${selected.email}` : undefined}
        width="w-[680px]"
        footer={
          <div className="flex gap-2">
            <Btn onClick={() => { setOpen(false); addToast(selected ? "Kullanıcı güncellendi" : "Kullanıcı oluşturuldu", "success"); }}>
              <CheckCircle size={14} />Kaydet
            </Btn>
            <Btn variant="secondary" onClick={() => setOpen(false)}>İptal</Btn>
          </div>
        }>
        <Tabs tabs={["Profil", "Parola", "Yetkiler"]} active={tab} onChange={setTab} className="px-6" />
        {tab === "Profil" && (
          <div className="p-6 grid grid-cols-2 gap-4">
            <FormField label="Ad Soyad" required>
              <TextInput value={selected?.name ?? ""} onChange={() => {}} placeholder="Ad Soyad" />
            </FormField>
            <FormField label="E-posta" required>
              <TextInput value={selected?.email ?? ""} onChange={() => {}} type="email" placeholder="ad@olslojistik.com" />
            </FormField>
            <FormField label="Telefon">
              <TextInput value={selected?.phone ?? ""} onChange={() => {}} placeholder="+90 5XX XXX XX XX" />
            </FormField>
            <FormField label="Rol" required>
              <SelectInput value={selected?.role ?? "Operasyon"} onChange={() => {}} options={[
                { value: "Admin", label: "Admin" }, { value: "Operasyon", label: "Operasyon" },
                { value: "Satış", label: "Satış" }, { value: "Muhasebe", label: "Muhasebe" },
              ]} />
            </FormField>
            <FormField label="Durum">
              <SelectInput value={selected?.status ?? "Aktif"} onChange={() => {}} options={[
                { value: "Aktif", label: "Aktif" }, { value: "Pasif", label: "Pasif" },
              ]} />
            </FormField>
          </div>
        )}
        {tab === "Parola" && (
          <div className="p-6 space-y-4 max-w-sm">
            <FormField label="Yeni Parola">
              <TextInput value="" onChange={() => {}} type="password" placeholder="••••••••" />
            </FormField>
            <FormField label="Parola Tekrar">
              <TextInput value="" onChange={() => {}} type="password" placeholder="••••••••" />
            </FormField>
            <Btn variant="secondary" size="sm" onClick={() => addToast("Parola sıfırlama e-postası gönderildi", "info")}>
              <Send size={12} />E-posta ile Sıfırla
            </Btn>
          </div>
        )}
        {tab === "Yetkiler" && (
          <div className="p-6">
            <div className="overflow-x-auto">
              <table className="w-full text-xs border-collapse">
                <thead>
                  <tr>
                    <th className="text-left py-2 pr-3 text-gray-500 font-semibold uppercase tracking-wide w-36">Modül</th>
                    {PERM_TYPES.map(p => (
                      <th key={p} className="text-center py-2 px-3 text-gray-500 font-semibold uppercase tracking-wide">{p}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {MODULES_PERMS.map((mod, i) => (
                    <tr key={mod} className={i % 2 === 0 ? "bg-gray-50/50" : ""}>
                      <td className="py-2.5 pr-3 font-medium text-gray-700">{mod}</td>
                      {PERM_TYPES.map(p => (
                        <td key={p} className="py-2.5 px-3 text-center">
                          <input type="checkbox"
                            defaultChecked={defaultPerms[`${mod}-${p}`]}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </Drawer>
    </>
  );
}

// ─────────────────────────────────────────────
// SUPPORT MODULE
// ─────────────────────────────────────────────
function SupportModule({ addToast }: { addToast: (m: string, t?: ToastData["type"]) => void }) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<typeof SUPPORT_DATA[0] | null>(null);
  const [publicForm, setPublicForm] = useState(false);
  const [formState, setFormState] = useState<"idle" | "sending" | "success" | "error">("idle");
  const perPage = 6;

  const filtered = SUPPORT_DATA.filter(s =>
    s.id.toLowerCase().includes(search.toLowerCase()) ||
    s.from.toLowerCase().includes(search.toLowerCase()) ||
    s.subject.toLowerCase().includes(search.toLowerCase()) ||
    s.category.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<typeof SUPPORT_DATA[0]>[] = [
    { key: "id", header: "Talep No", sortable: true, render: r => <span className="font-mono text-[11px] text-blue-600">{r.id}</span> },
    { key: "from", header: "Gönderen", render: r => (
      <div>
        <p className="font-semibold text-gray-900">{r.from}</p>
        <p className="text-[11px] text-gray-400">{r.company}</p>
      </div>
    )},
    { key: "subject", header: "Konu", render: r => <span className="text-gray-700">{r.subject}</span> },
    { key: "date", header: "Tarih", render: r => <span className="font-mono text-[11px] text-gray-500">{r.date}</span> },
    { key: "category", header: "Kategori", render: r => <span className="text-xs text-gray-500">{r.category}</span> },
    { key: "priority", header: "Öncelik", render: r => <Badge label={r.priority} /> },
    { key: "status", header: "Durum", render: r => <Badge label={r.status} /> },
  ];

  const handlePublicSend = () => {
    setFormState("sending");
    setTimeout(() => setFormState("success"), 1800);
  };

  return (
    <>
      <ModulePage title="Destek Talepleri" search={search} onSearchChange={v => { setSearch(v); setPage(1); }}
        searchPlaceholder="Talep no, gönderen, konu..."
        filters={<Btn variant="secondary" size="sm"><Filter size={12} />Filtrele</Btn>}
        action={
          <div className="flex gap-2">
            <Btn variant="secondary" onClick={() => setPublicForm(true)}><Globe size={14} />Herkese Açık Form</Btn>
            <Btn onClick={() => { setSelected(null); setOpen(true); }}><Plus size={14} />Yeni Talep</Btn>
          </div>
        }>
        <div className="bg-white">
          {filtered.length === 0
            ? <EmptyState icon={Headphones} title="Talep bulunamadı" desc="Arama kriterlerine uygun destek talebi bulunamadı." />
            : <>
              <DataTable data={filtered.slice((page - 1) * perPage, page * perPage)} columns={columns}
                onRowClick={r => { setSelected(r); setOpen(true); }}
                actions={r => <RowActions onView={() => { setSelected(r); setOpen(true); }} onEdit={() => { setSelected(r); setOpen(true); }} onDelete={() => addToast("Talep silindi", "success")} />}
              />
              <Pagination page={page} total={filtered.length} perPage={perPage} onChange={setPage} />
            </>
          }
        </div>
      </ModulePage>

      <Drawer open={open} onClose={() => setOpen(false)}
        title={selected ? selected.id : "Yeni Destek Talebi"}
        subtitle={selected ? selected.subject : undefined}
        footer={
          <div className="flex gap-2">
            {selected && <Btn onClick={() => { setOpen(false); addToast("Talep güncellendi", "success"); }}>Kaydet</Btn>}
            {!selected && <Btn onClick={() => { setOpen(false); addToast("Talep oluşturuldu", "success"); }}><CheckCircle size={14} />Kaydet</Btn>}
            <Btn variant="secondary" onClick={() => setOpen(false)}>Kapat</Btn>
          </div>
        }>
        {selected ? (
          <div className="p-6 space-y-5">
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-full bg-gray-200 flex items-center justify-center text-xs font-bold text-gray-600">
                {selected.from.split(" ").map(n => n[0]).join("")}
              </div>
              <div>
                <p className="text-sm font-semibold text-gray-900">{selected.from}</p>
                <p className="text-xs text-gray-400">{selected.company} · {selected.date}</p>
              </div>
              <div className="ml-auto flex gap-2">
                <Badge label={selected.priority} />
                <Badge label={selected.status} />
              </div>
            </div>
            <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
              <p className="text-sm font-semibold text-gray-800 mb-2">{selected.subject}</p>
              <p className="text-sm text-gray-600 leading-relaxed">
                Merhaba, {selected.category} ile ilgili destek talebimizi iletmek istiyoruz.
                Söz konusu konu hakkında en kısa sürede bilgilendirilmemizi rica ederiz.
              </p>
            </div>
            <div>
              <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wide mb-2">Yanıt Yaz</p>
              <TextareaInput value="" onChange={() => {}} placeholder="Yanıtınızı yazın..." rows={4} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <FormField label="Durum">
                <SelectInput value={selected.status} onChange={() => {}} options={[
                  { value: "Açık", label: "Açık" }, { value: "Çözüldü", label: "Çözüldü" },
                ]} />
              </FormField>
              <FormField label="Öncelik">
                <SelectInput value={selected.priority} onChange={() => {}} options={[
                  { value: "Yüksek", label: "Yüksek" }, { value: "Orta", label: "Orta" }, { value: "Düşük", label: "Düşük" },
                ]} />
              </FormField>
            </div>
          </div>
        ) : (
          <div className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <FormField label="Gönderen" required>
                <TextInput value="" onChange={() => {}} placeholder="Ad Soyad" />
              </FormField>
              <FormField label="Firma">
                <TextInput value="" onChange={() => {}} placeholder="Firma adı" />
              </FormField>
              <div className="col-span-2">
                <FormField label="Konu" required>
                  <TextInput value="" onChange={() => {}} placeholder="Talep konusu" />
                </FormField>
              </div>
              <FormField label="Kategori">
                <SelectInput value="Operasyon" onChange={() => {}} options={[
                  { value: "Operasyon", label: "Operasyon" }, { value: "Muhasebe", label: "Muhasebe" },
                  { value: "Teknik", label: "Teknik" }, { value: "Veri", label: "Veri" },
                ]} />
              </FormField>
              <FormField label="Öncelik">
                <SelectInput value="Orta" onChange={() => {}} options={[
                  { value: "Yüksek", label: "Yüksek" }, { value: "Orta", label: "Orta" }, { value: "Düşük", label: "Düşük" },
                ]} />
              </FormField>
              <div className="col-span-2">
                <FormField label="Açıklama" required>
                  <TextareaInput value="" onChange={() => {}} placeholder="Talebinizi detaylı açıklayın..." rows={4} />
                </FormField>
              </div>
            </div>
          </div>
        )}
      </Drawer>

      {/* Public support form */}
      <Modal open={publicForm} onClose={() => { setPublicForm(false); setFormState("idle"); }} title="Herkese Açık Destek Formu">
        <div className="p-5">
          <AnimatePresence mode="wait">
            {formState === "idle" && (
              <motion.div key="form" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="space-y-3">
                <FormField label="Ad Soyad" required>
                  <TextInput value="" onChange={() => {}} placeholder="Ad Soyad" />
                </FormField>
                <FormField label="E-posta" required>
                  <TextInput value="" onChange={() => {}} type="email" placeholder="email@adres.com" />
                </FormField>
                <FormField label="Konu" required>
                  <TextInput value="" onChange={() => {}} placeholder="Destek talebinizin konusu" />
                </FormField>
                <FormField label="Mesaj" required>
                  <TextareaInput value="" onChange={() => {}} placeholder="Mesajınızı yazın..." rows={3} />
                </FormField>
                <Btn onClick={handlePublicSend} className="w-full justify-center"><Send size={14} />Gönder</Btn>
              </motion.div>
            )}
            {formState === "sending" && (
              <motion.div key="sending" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                className="flex flex-col items-center py-8 gap-3">
                <RefreshCw size={24} className="text-blue-600 animate-spin" />
                <p className="text-sm text-gray-600">Gönderiliyor...</p>
              </motion.div>
            )}
            {formState === "success" && (
              <motion.div key="success" initial={{ opacity: 0, scale: 0.95 }} animate={{ opacity: 1, scale: 1 }}
                className="flex flex-col items-center py-8 gap-3 text-center">
                <div className="w-12 h-12 bg-emerald-100 rounded-full flex items-center justify-center">
                  <CheckCircle size={24} className="text-emerald-600" />
                </div>
                <p className="text-sm font-semibold text-gray-900">Talebiniz alındı</p>
                <p className="text-xs text-gray-500">En kısa sürede size dönüş yapılacaktır.</p>
                <Btn variant="secondary" size="sm" onClick={() => { setPublicForm(false); setFormState("idle"); }}>Kapat</Btn>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </Modal>
    </>
  );
}

// ─────────────────────────────────────────────
// SIDEBAR
// ─────────────────────────────────────────────
function Sidebar({ collapsed, active, onSelect, onToggle, mobileOpen, onMobileClose }: {
  collapsed: boolean; active: ModuleKey; onSelect: (k: ModuleKey) => void;
  onToggle: () => void; mobileOpen: boolean; onMobileClose: () => void;
}) {
  const SidebarContent = (
    <div className="flex flex-col h-full" style={{ backgroundColor: "#0D1B2E" }}>
      {/* Logo */}
      <div className={clsx("flex items-center gap-3 px-4 py-4 border-b shrink-0 transition-all", "border-white/10")}>
        <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center shrink-0">
          <span className="text-white text-xs font-bold font-mono">OLS</span>
        </div>
        {!collapsed && (
          <motion.div initial={{ opacity: 0, x: -8 }} animate={{ opacity: 1, x: 0 }} transition={{ duration: 0.15 }}>
            <p className="text-white text-sm font-semibold leading-none">OLS Lojistik</p>
            <p className="text-blue-300/70 text-[10px] mt-0.5 font-mono">Yönetim Sistemi</p>
          </motion.div>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto py-3 px-2 space-y-0.5">
        {NAV_ITEMS.map(item => {
          const Icon = item.icon;
          const isActive = active === item.key;
          return (
            <button key={item.key} onClick={() => { onSelect(item.key); onMobileClose(); }}
              title={collapsed ? item.label : undefined}
              className={clsx(
                "w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm font-medium transition-all duration-150 relative group",
                isActive
                  ? "bg-blue-600 text-white shadow-sm"
                  : "text-slate-400 hover:text-slate-100 hover:bg-white/8"
              )}>
              <Icon size={17} className="shrink-0" />
              {!collapsed && (
                <span className="flex-1 text-left leading-none whitespace-nowrap overflow-hidden text-ellipsis">
                  {item.label}
                </span>
              )}
              {!collapsed && item.count && !isActive && (
                <span className="ml-auto text-[10px] bg-blue-600 text-white rounded-full w-5 h-5 flex items-center justify-center font-mono shrink-0">
                  {item.count}
                </span>
              )}
              {collapsed && item.count && !isActive && (
                <span className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full" />
              )}
              {collapsed && (
                <div className="absolute left-full ml-2 px-2 py-1 bg-gray-900 text-white text-xs rounded whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity z-50">
                  {item.label}
                </div>
              )}
            </button>
          );
        })}
      </nav>

      {/* Footer */}
      <div className="px-2 py-3 border-t border-white/10 space-y-1 shrink-0">
        <button className={clsx(
          "w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm text-slate-400 hover:text-slate-100 hover:bg-white/8 transition-all"
        )}>
          <Settings size={16} className="shrink-0" />
          {!collapsed && <span>Ayarlar</span>}
        </button>
        <button className="w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm text-slate-400 hover:text-slate-100 hover:bg-white/8 transition-all">
          <LogOut size={16} className="shrink-0" />
          {!collapsed && <span>Çıkış Yap</span>}
        </button>
        <button onClick={onToggle}
          className="w-full flex items-center gap-3 px-2.5 py-2 rounded-lg text-sm text-slate-500 hover:text-slate-300 hover:bg-white/5 transition-all">
          {collapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
          {!collapsed && <span className="text-xs">Daralt</span>}
        </button>
      </div>
    </div>
  );

  return (
    <>
      {/* Desktop sidebar */}
      <div className={clsx(
        "hidden lg:flex flex-col shrink-0 transition-all duration-200 ease-out",
        collapsed ? "w-[60px]" : "w-[220px]"
      )}>
        {SidebarContent}
      </div>

      {/* Mobile drawer */}
      <AnimatePresence>
        {mobileOpen && (
          <>
            <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              transition={{ duration: 0.18 }}
              className="fixed inset-0 bg-black/40 z-40 lg:hidden"
              onClick={onMobileClose}
            />
            <motion.div
              initial={{ x: "-100%" }} animate={{ x: 0 }} exit={{ x: "-100%" }}
              transition={{ duration: 0.22, ease: [0.25, 0.1, 0.25, 1] }}
              className="fixed left-0 top-0 bottom-0 w-[220px] z-50 lg:hidden">
              {SidebarContent}
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </>
  );
}

// ─────────────────────────────────────────────
// TOP BAR
// ─────────────────────────────────────────────
function TopBar({ active, onMenuToggle }: { active: ModuleKey; onMenuToggle: () => void }) {
  const [searchOpen, setSearchOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [userOpen, setUserOpen] = useState(false);
  const notifRef = useRef<HTMLDivElement>(null);
  const userRef = useRef<HTMLDivElement>(null);

  const notifs = [
    { text: "YUK-2025-1247 yükleme tamamlandı", time: "09:41", type: "success" as const },
    { text: "FAT-2025-0518 vadesi geçti", time: "09:00", type: "error" as const },
    { text: "T2025-0847 onay bekliyor", time: "08:30", type: "info" as const },
    { text: "SEF-2025-0312 yola çıktı", time: "06:45", type: "info" as const },
  ];

  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) setNotifOpen(false);
      if (userRef.current && !userRef.current.contains(e.target as Node)) setUserOpen(false);
    };
    document.addEventListener("mousedown", h);
    return () => document.removeEventListener("mousedown", h);
  }, []);

  return (
    <div className="h-12 bg-white border-b border-gray-200 flex items-center px-4 gap-3 shrink-0 z-30">
      {/* Mobile menu */}
      <button onClick={onMenuToggle} className="lg:hidden p-1.5 rounded hover:bg-gray-100 text-gray-500">
        <Menu size={18} />
      </button>

      {/* Breadcrumb */}
      <div className="flex items-center gap-1.5 text-sm text-gray-500 min-w-0">
        <span className="text-gray-400 text-xs hidden sm:block">OLS Lojistik</span>
        <span className="text-gray-300 hidden sm:block">/</span>
        <span className="font-medium text-gray-800 truncate">{MODULE_LABELS[active]}</span>
      </div>

      <div className="flex-1" />

      {/* Global search */}
      <div className="relative hidden sm:block">
        <AnimatePresence>
          {searchOpen ? (
            <motion.div initial={{ width: 0, opacity: 0 }} animate={{ width: 240, opacity: 1 }} exit={{ width: 0, opacity: 0 }}
              transition={{ duration: 0.18 }}>
              <div className="relative">
                <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
                <input autoFocus placeholder="Kayıt ara..." onBlur={() => setSearchOpen(false)}
                  className="pl-8 pr-3 py-1.5 text-sm border border-blue-300 rounded-md bg-white focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 w-full" />
              </div>
            </motion.div>
          ) : (
            <button onClick={() => setSearchOpen(true)}
              className="p-1.5 rounded hover:bg-gray-100 text-gray-500 hover:text-gray-700 transition-colors">
              <Search size={16} />
            </button>
          )}
        </AnimatePresence>
      </div>

      {/* Notifications */}
      <div className="relative" ref={notifRef}>
        <button onClick={() => setNotifOpen(o => !o)}
          className="relative p-1.5 rounded hover:bg-gray-100 text-gray-500 hover:text-gray-700 transition-colors">
          <Bell size={16} />
          <span className="absolute top-0.5 right-0.5 w-2 h-2 bg-red-500 rounded-full" />
        </button>
        <AnimatePresence>
          {notifOpen && (
            <motion.div
              initial={{ opacity: 0, scale: 0.92, y: -4 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.92, y: -4 }}
              transition={{ duration: 0.14, ease: "easeOut" }}
              className="absolute right-0 top-10 w-72 bg-white rounded-xl shadow-xl border border-gray-200 z-50">
              <div className="flex items-center justify-between px-4 py-2.5 border-b border-gray-100">
                <span className="text-xs font-semibold text-gray-700">Bildirimler</span>
                <span className="text-[10px] text-blue-600 font-medium cursor-pointer">Tümünü işaretle</span>
              </div>
              <div className="divide-y divide-gray-50">
                {notifs.map((n, i) => (
                  <div key={i} className="flex items-start gap-3 px-4 py-3 hover:bg-gray-50 transition-colors cursor-pointer">
                    <div className={clsx("w-1.5 h-1.5 rounded-full mt-1.5 shrink-0",
                      n.type === "success" ? "bg-emerald-500" : n.type === "error" ? "bg-red-500" : "bg-blue-500")} />
                    <div className="flex-1 min-w-0">
                      <p className="text-xs text-gray-800 leading-relaxed">{n.text}</p>
                      <p className="text-[10px] text-gray-400 font-mono mt-0.5">{n.time}</p>
                    </div>
                  </div>
                ))}
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* User */}
      <div className="relative" ref={userRef}>
        <button onClick={() => setUserOpen(o => !o)}
          className="flex items-center gap-2 pl-2 pr-2.5 py-1 rounded-lg hover:bg-gray-100 transition-colors">
          <div className="w-6 h-6 rounded-full bg-blue-600 flex items-center justify-center text-[10px] text-white font-bold">
            BS
          </div>
          <span className="text-xs font-medium text-gray-700 hidden sm:block">Bülent S.</span>
          <ChevronDown size={12} className="text-gray-400 hidden sm:block" />
        </button>
        <AnimatePresence>
          {userOpen && (
            <motion.div
              initial={{ opacity: 0, scale: 0.92, y: -4 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.92 }}
              transition={{ duration: 0.12, ease: "easeOut" }}
              className="absolute right-0 top-10 w-48 bg-white rounded-xl shadow-xl border border-gray-200 py-1 z-50">
              <div className="px-3 py-2.5 border-b border-gray-100">
                <p className="text-xs font-semibold text-gray-800">Bülent Serhan</p>
                <p className="text-[10px] text-gray-500">bulent@olslojistik.com</p>
              </div>
              {[["Profil", User], ["Ayarlar", Settings], ["Çıkış Yap", LogOut]].map(([label, Icon]) => (
                <button key={String(label)}
                  className={clsx(
                    "flex items-center gap-2 w-full px-3 py-2 text-xs hover:bg-gray-50 transition-colors",
                    label === "Çıkış Yap" ? "text-red-600" : "text-gray-700"
                  )}>
                  {/* @ts-ignore */}
                  <Icon size={12} />{label}
                </button>
              ))}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────
// LOGIN PAGE
// ─────────────────────────────────────────────
function LoginPage({ onLogin }: { onLogin: () => void }) {
  const [email, setEmail] = useState("bulent@olslojistik.com");
  const [password, setPassword] = useState("••••••••");
  const [loading, setLoading] = useState(false);
  const [focused, setFocused] = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setTimeout(onLogin, 1600);
  }

  const FEATURES = [
    { icon: Truck, text: "Sefer & yük yönetimi" },
    { icon: FileText, text: "Teklif → fatura akışı" },
    { icon: Users, text: "Müşteri & kullanıcı portalı" },
    { icon: BarChart2, text: "Canlı raporlama ve metrikler" },
  ];

  return (
    <div className="flex h-screen overflow-hidden" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
      {/* ── Left panel ─────────────────────────── */}
      <motion.div
        initial={{ x: -60, opacity: 0 }}
        animate={{ x: 0, opacity: 1 }}
        transition={{ duration: 0.55, ease: [0.25, 0.1, 0.25, 1] }}
        className="hidden lg:flex flex-col w-[440px] xl:w-[520px] shrink-0 relative overflow-hidden"
        style={{ backgroundColor: "#0D1B2E" }}
      >
        {/* Animated grid background */}
        <div className="absolute inset-0 opacity-[0.04]"
          style={{ backgroundImage: "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)", backgroundSize: "48px 48px" }}
        />
        {/* Glow orb */}
        <motion.div
          animate={{ scale: [1, 1.08, 1], opacity: [0.12, 0.2, 0.12] }}
          transition={{ duration: 6, repeat: Infinity, ease: "easeInOut" }}
          className="absolute -top-32 -left-32 w-[500px] h-[500px] rounded-full"
          style={{ background: "radial-gradient(circle, #2563EB 0%, transparent 70%)" }}
        />
        <motion.div
          animate={{ scale: [1, 1.12, 1], opacity: [0.08, 0.15, 0.08] }}
          transition={{ duration: 8, repeat: Infinity, ease: "easeInOut", delay: 2 }}
          className="absolute -bottom-40 -right-20 w-[400px] h-[400px] rounded-full"
          style={{ background: "radial-gradient(circle, #3B82F6 0%, transparent 70%)" }}
        />

        <div className="relative z-10 flex flex-col h-full p-10 xl:p-12">
          {/* Logo */}
          <motion.div
            initial={{ opacity: 0, y: -20 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.25 }}
            className="flex items-center gap-3 mb-14"
          >
            <div className="w-10 h-10 bg-blue-600 rounded-xl flex items-center justify-center shadow-lg shadow-blue-900/40">
              <span className="text-white text-sm font-bold font-mono tracking-tight">OLS</span>
            </div>
            <div>
              <p className="text-white text-base font-semibold leading-none">OLS Lojistik</p>
              <p className="text-blue-400/70 text-[11px] font-mono mt-0.5">Yönetim Sistemi v2.5</p>
            </div>
          </motion.div>

          {/* Headline */}
          <motion.div
            initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.35 }}
            className="mb-10"
          >
            <h1 className="text-3xl xl:text-4xl font-bold text-white leading-tight tracking-tight">
              Lojistiğinizi<br />
              <span className="text-blue-400">tek ekrandan</span><br />
              yönetin.
            </h1>
            <p className="text-slate-400 text-sm mt-4 leading-relaxed max-w-xs">
              Seferden faturaya, tekliften teslimata — tüm operasyonunuz OLS&apos;te.
            </p>
          </motion.div>

          {/* Feature list */}
          <div className="space-y-3 mb-auto">
            {FEATURES.map((f, i) => {
              const Icon = f.icon;
              return (
                <motion.div key={i}
                  initial={{ opacity: 0, x: -16 }} animate={{ opacity: 1, x: 0 }}
                  transition={{ duration: 0.3, delay: 0.45 + i * 0.08 }}
                  className="flex items-center gap-3"
                >
                  <div className="w-8 h-8 rounded-lg bg-blue-600/20 border border-blue-500/20 flex items-center justify-center shrink-0">
                    <Icon size={14} className="text-blue-400" />
                  </div>
                  <span className="text-slate-300 text-sm">{f.text}</span>
                </motion.div>
              );
            })}
          </div>

          {/* Live stats ticker */}
          <motion.div
            initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, delay: 0.8 }}
            className="grid grid-cols-3 gap-3 mt-10"
          >
            {[["142", "Bu Ay Yük"], ["7", "Aktif Sefer"], ["94%", "Teslim Oranı"]].map(([val, lbl]) => (
              <div key={lbl} className="bg-white/5 border border-white/8 rounded-xl p-3 text-center">
                <p className="text-xl font-bold text-white font-mono">{val}</p>
                <p className="text-[10px] text-slate-400 mt-0.5">{lbl}</p>
              </div>
            ))}
          </motion.div>
        </div>
      </motion.div>

      {/* ── Right panel (form) ──────────────────── */}
      <div className="flex-1 flex items-center justify-center bg-[#F4F6FA] p-6 sm:p-10">
        <motion.div
          initial={{ opacity: 0, y: 28, scale: 0.97 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          transition={{ duration: 0.45, delay: 0.15, ease: [0.25, 0.1, 0.25, 1] }}
          className="w-full max-w-[380px]"
        >
          {/* Mobile logo */}
          <div className="flex items-center gap-2.5 mb-8 lg:hidden">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white text-xs font-bold font-mono">OLS</span>
            </div>
            <span className="font-semibold text-gray-800">OLS Lojistik</span>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900 tracking-tight">Hoş geldiniz</h2>
            <p className="text-sm text-gray-500 mt-1">Hesabınıza giriş yapın</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Email */}
            <div>
              <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider block mb-1.5">
                E-posta
              </label>
              <div className={clsx(
                "relative border rounded-xl bg-white transition-all duration-150 overflow-hidden",
                focused === "email" ? "border-blue-500 ring-2 ring-blue-500/20" : "border-gray-200"
              )}>
                <Mail size={14} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                <input type="email" value={email} onChange={e => setEmail(e.target.value)}
                  onFocus={() => setFocused("email")} onBlur={() => setFocused(null)}
                  className="w-full pl-9 pr-4 py-3 text-sm bg-transparent focus:outline-none text-gray-800"
                  placeholder="email@firma.com"
                />
              </div>
            </div>

            {/* Password */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">Parola</label>
                <span className="text-[11px] text-blue-600 cursor-pointer hover:underline">Parolamı unuttum</span>
              </div>
              <div className={clsx(
                "relative border rounded-xl bg-white transition-all duration-150 overflow-hidden",
                focused === "pass" ? "border-blue-500 ring-2 ring-blue-500/20" : "border-gray-200"
              )}>
                <Lock size={14} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400" />
                <input type="password" value={password} onChange={e => setPassword(e.target.value)}
                  onFocus={() => setFocused("pass")} onBlur={() => setFocused(null)}
                  className="w-full pl-9 pr-4 py-3 text-sm bg-transparent focus:outline-none text-gray-800"
                />
              </div>
            </div>

            {/* Submit */}
            <motion.button
              type="submit"
              disabled={loading}
              whileTap={{ scale: 0.98 }}
              className={clsx(
                "w-full flex items-center justify-center gap-2 py-3 rounded-xl text-sm font-semibold transition-all duration-200",
                "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2",
                loading
                  ? "bg-blue-400 text-white cursor-not-allowed"
                  : "bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 shadow-md shadow-blue-600/25"
              )}
            >
              <AnimatePresence mode="wait">
                {loading ? (
                  <motion.span key="loading" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                    className="flex items-center gap-2">
                    <RefreshCw size={14} className="animate-spin" />Giriş yapılıyor...
                  </motion.span>
                ) : (
                  <motion.span key="idle" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
                    Giriş Yap
                  </motion.span>
                )}
              </AnimatePresence>
            </motion.button>
          </form>

          <div className="mt-6 p-3.5 bg-blue-50 border border-blue-100 rounded-xl">
            <p className="text-[11px] text-blue-700 font-medium">Demo hesabı</p>
            <p className="text-[11px] text-blue-600/80 mt-0.5 font-mono">bulent@olslojistik.com · herhangi bir parola</p>
          </div>

          <p className="text-[11px] text-gray-400 text-center mt-8">
            © 2025 OLS Lojistik A.Ş. · Tüm hakları saklıdır.
          </p>
        </motion.div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────
// DASHBOARD MODULE
// ─────────────────────────────────────────────
const CUSTOM_TOOLTIP_STYLE = {
  contentStyle: { background: "#fff", border: "1px solid #E5E7EB", borderRadius: 8, fontSize: 12, boxShadow: "0 4px 12px rgba(0,0,0,0.08)" },
  labelStyle: { fontWeight: 600, color: "#374151" },
  itemStyle: { color: "#6B7280" },
};

function MetricCard({ label, value, sub, trend, trendUp, icon: Icon, color }: {
  label: string; value: string; sub: string; trend?: string; trendUp?: boolean;
  icon: React.ComponentType<{ size?: number; className?: string }>; color: string;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
      whileHover={{ y: -1 }}
      transition={{ duration: 0.2 }}
      className="bg-white rounded-xl p-4 border border-gray-200 shadow-sm hover:shadow-md transition-shadow"
    >
      <div className="flex items-start justify-between mb-3">
        <div className={clsx("w-9 h-9 rounded-lg flex items-center justify-center", color)}>
          <Icon size={17} />
        </div>
        {trend && (
          <div className={clsx("flex items-center gap-1 text-[11px] font-semibold px-1.5 py-0.5 rounded-md",
            trendUp ? "text-emerald-700 bg-emerald-50" : "text-red-600 bg-red-50"
          )}>
            {trendUp ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
            {trend}
          </div>
        )}
      </div>
      <p className="text-2xl font-bold text-gray-900 font-mono tracking-tight">{value}</p>
      <p className="text-xs font-semibold text-gray-700 mt-0.5">{label}</p>
      <p className="text-[11px] text-gray-400 mt-0.5">{sub}</p>
    </motion.div>
  );
}

function DashboardModule({ onNavigate }: { onNavigate: (k: ModuleKey) => void }) {
  const METRICS = [
    { label: "Aktif Seferler", value: "7", sub: "2'si uluslararası", trend: "+2", trendUp: true, icon: Truck, color: "bg-indigo-50 text-indigo-600" },
    { label: "Bu Ay Yükler", value: "142", sub: "Ocak 2025", trend: "+6.0%", trendUp: true, icon: Package, color: "bg-blue-50 text-blue-600" },
    { label: "Aylık Gelir", value: "€848K", sub: "Geçen ay €842K", trend: "+0.7%", trendUp: true, icon: TrendingUp, color: "bg-emerald-50 text-emerald-600" },
    { label: "Bekleyen Teklifler", value: "12", sub: "3 yüksek öncelikli", trend: "-4", trendUp: false, icon: FileText, color: "bg-amber-50 text-amber-600" },
    { label: "Aktif Müşteriler", value: "48", sub: "Son 90 gün", trend: "+3", trendUp: true, icon: Users, color: "bg-violet-50 text-violet-600" },
    { label: "Teslim Oranı", value: "94%", sub: "Hedef %96", trend: "-2%", trendUp: false, icon: Target, color: "bg-rose-50 text-rose-600" },
  ];

  const QUICK_LINKS: { label: string; key: ModuleKey; icon: React.ComponentType<{size?:number;className?:string}>; desc: string }[] = [
    { label: "Müşteriler", key: "customers", icon: Users, desc: "48 aktif" },
    { label: "Teklifler", key: "quotes", icon: FileText, desc: "12 beklemede" },
    { label: "Yükler", key: "loads", icon: Package, desc: "142 bu ay" },
    { label: "Faturalar", key: "invoices", icon: Receipt, desc: "2 vadesi geçti" },
  ];

  const UPCOMING = [
    { trip: "SEF-2025-0306", route: "İstanbul → Hamburg", vehicle: "34 TRK 7733", driver: "Sinan Kurt", date: "10.01.2025" },
    { trip: "SEF-2025-0312", route: "İstanbul → Trieste", vehicle: "34 TRK 4521", driver: "Osman Karaca", date: "12.01.2025" },
    { trip: "SEF-2025-0311", route: "İstanbul → Münih", vehicle: "34 TRK 3308", driver: "Recep Yaman", date: "11.01.2025" },
  ];

  return (
    <div className="overflow-y-auto h-full">
      <div className="p-6 space-y-6 max-w-[1400px]">

        {/* Welcome bar */}
        <motion.div initial={{ opacity: 0, y: -8 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}
          className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-bold text-gray-900">Günaydın, Bülent 👋</h2>
            <p className="text-sm text-gray-500 mt-0.5">08 Ocak 2025 · Bugün 7 aktif sefer var</p>
          </div>
          <div className="flex gap-2">
            <Btn variant="secondary" size="sm" onClick={() => onNavigate("quotes")}><Plus size={13} />Yeni Teklif</Btn>
            <Btn size="sm" onClick={() => onNavigate("trips")}><Truck size={13} />Sefer Ekle</Btn>
          </div>
        </motion.div>

        {/* KPI row */}
        <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3">
          {METRICS.map((m, i) => (
            <motion.div key={m.label}
              initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.25, delay: i * 0.05 }}>
              <MetricCard {...m} />
            </motion.div>
          ))}
        </div>

        {/* Charts row 1 */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">

          {/* Aylık Sevkiyat & Gelir - area chart */}
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.2 }}
            className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Aylık Sevkiyat & Gelir</h3>
                <p className="text-[11px] text-gray-400 mt-0.5">Son 6 ay · Adet ve bin EUR</p>
              </div>
              <div className="flex items-center gap-3 text-[11px]">
                <span className="flex items-center gap-1.5"><span className="w-3 h-0.5 bg-blue-500 rounded inline-block" />Sevkiyat</span>
                <span className="flex items-center gap-1.5"><span className="w-3 h-0.5 bg-emerald-500 rounded inline-block" />Gelir (K€)</span>
              </div>
            </div>
            <ResponsiveContainer width="100%" height={190}>
              <AreaChart data={MONTHLY_SHIPMENTS} margin={{ top: 4, right: 4, bottom: 0, left: -16 }}>
                <defs>
                  <linearGradient id="gradBlue" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#2563EB" stopOpacity={0.15} />
                    <stop offset="100%" stopColor="#2563EB" stopOpacity={0} />
                  </linearGradient>
                  <linearGradient id="gradGreen" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#10B981" stopOpacity={0.12} />
                    <stop offset="100%" stopColor="#10B981" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" vertical={false} />
                <XAxis dataKey="ay" tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <Tooltip {...CUSTOM_TOOLTIP_STYLE} />
                <Area type="monotone" dataKey="sevkiyat" stroke="#2563EB" strokeWidth={2} fill="url(#gradBlue)" dot={{ r: 3, fill: "#2563EB", strokeWidth: 0 }} activeDot={{ r: 5 }} name="Sevkiyat" />
                <Area type="monotone" dataKey="gelir" stroke="#10B981" strokeWidth={2} fill="url(#gradGreen)" dot={{ r: 3, fill: "#10B981", strokeWidth: 0 }} activeDot={{ r: 5 }} name="Gelir (K€)" />
              </AreaChart>
            </ResponsiveContainer>
          </motion.div>

          {/* Taşıma Tipi Dağılımı - donut */}
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.28 }}
            className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="mb-4">
              <h3 className="text-sm font-semibold text-gray-900">Taşıma Tipi Dağılımı</h3>
              <p className="text-[11px] text-gray-400 mt-0.5">Bu ay · Yük bazında</p>
            </div>
            <ResponsiveContainer width="100%" height={140}>
              <PieChart>
                <Pie data={LOAD_TYPE_DIST} cx="50%" cy="50%" innerRadius={42} outerRadius={62}
                  paddingAngle={3} dataKey="value" stroke="none">
                  {LOAD_TYPE_DIST.map((entry, i) => (
                    <Cell key={i} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip {...CUSTOM_TOOLTIP_STYLE} formatter={(v: number) => [`%${v}`, ""]} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 mt-2">
              {LOAD_TYPE_DIST.map(item => (
                <div key={item.name} className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: item.color }} />
                    <span className="text-[11px] text-gray-600">{item.name}</span>
                  </div>
                  <span className="text-[11px] font-semibold font-mono text-gray-800">%{item.value}</span>
                </div>
              ))}
            </div>
          </motion.div>
        </div>

        {/* Charts row 2 */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">

          {/* Haftalık Performans - bar chart */}
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.32 }}
            className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Haftalık Teslimat Performansı</h3>
                <p className="text-[11px] text-gray-400 mt-0.5">Bu hafta · Teslim / Gecikme</p>
              </div>
              <div className="flex items-center gap-3 text-[11px]">
                <span className="flex items-center gap-1.5"><span className="w-3 h-2.5 bg-blue-500 rounded inline-block" />Teslim</span>
                <span className="flex items-center gap-1.5"><span className="w-3 h-2.5 bg-red-400 rounded inline-block" />Gecikme</span>
              </div>
            </div>
            <ResponsiveContainer width="100%" height={175}>
              <BarChart data={WEEKLY_PERF} margin={{ top: 4, right: 4, bottom: 0, left: -16 }} barGap={3}>
                <CartesianGrid strokeDasharray="3 3" stroke="#F3F4F6" vertical={false} />
                <XAxis dataKey="gun" tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: "#9CA3AF" }} axisLine={false} tickLine={false} />
                <Tooltip {...CUSTOM_TOOLTIP_STYLE} />
                <Bar dataKey="teslim" fill="#2563EB" radius={[4, 4, 0, 0]} name="Teslim" maxBarSize={28} />
                <Bar dataKey="gecikme" fill="#FCA5A5" radius={[4, 4, 0, 0]} name="Gecikme" maxBarSize={28} />
              </BarChart>
            </ResponsiveContainer>
          </motion.div>

          {/* Son Aktiviteler */}
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.38 }}
            className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold text-gray-900">Son Aktiviteler</h3>
              <span className="text-[11px] text-blue-600 cursor-pointer">Tümü</span>
            </div>
            <div className="space-y-3">
              {RECENT_ACTIVITIES.map((act, i) => {
                const Icon = act.icon;
                return (
                  <motion.div key={i}
                    initial={{ opacity: 0, x: 8 }} animate={{ opacity: 1, x: 0 }}
                    transition={{ duration: 0.2, delay: 0.4 + i * 0.06 }}
                    className="flex items-start gap-2.5">
                    <div className={clsx("w-7 h-7 rounded-lg flex items-center justify-center shrink-0 mt-0.5", act.color)}>
                      <Icon size={12} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-[11px] font-medium text-gray-800 leading-snug">{act.text}</p>
                      <p className="text-[10px] text-gray-400 mt-0.5 truncate">{act.sub}</p>
                    </div>
                    <span className="text-[10px] text-gray-400 font-mono shrink-0">{act.time}</span>
                  </motion.div>
                );
              })}
            </div>
          </motion.div>
        </div>

        {/* Bottom row */}
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4 pb-2">

          {/* Quick access */}
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.42 }}
            className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="text-sm font-semibold text-gray-900 mb-3">Hızlı Erişim</h3>
            <div className="grid grid-cols-2 gap-2">
              {QUICK_LINKS.map(link => {
                const Icon = link.icon;
                return (
                  <button key={link.key} onClick={() => onNavigate(link.key)}
                    className="flex items-center gap-2.5 p-3 rounded-lg border border-gray-100 hover:border-blue-200 hover:bg-blue-50/30 transition-all group text-left">
                    <div className="w-7 h-7 rounded-lg bg-gray-100 group-hover:bg-blue-100 flex items-center justify-center transition-colors">
                      <Icon size={13} className="text-gray-500 group-hover:text-blue-600 transition-colors" />
                    </div>
                    <div>
                      <p className="text-xs font-semibold text-gray-800">{link.label}</p>
                      <p className="text-[10px] text-gray-400">{link.desc}</p>
                    </div>
                  </button>
                );
              })}
            </div>
          </motion.div>

          {/* Upcoming trips */}
          <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.46 }}
            className="xl:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-gray-900">Yaklaşan Seferler</h3>
              <button onClick={() => onNavigate("trips")}
                className="text-[11px] text-blue-600 hover:underline flex items-center gap-0.5">
                Tümünü gör <ChevronRight size={11} />
              </button>
            </div>
            <div className="space-y-2.5">
              {UPCOMING.map((t, i) => (
                <motion.div key={t.trip}
                  initial={{ opacity: 0, x: -8 }} animate={{ opacity: 1, x: 0 }}
                  transition={{ duration: 0.2, delay: 0.5 + i * 0.08 }}
                  className="flex items-center gap-3 p-3 rounded-lg border border-gray-100 hover:border-gray-200 hover:bg-gray-50/60 transition-all cursor-pointer"
                  onClick={() => onNavigate("trips")}>
                  <div className="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center shrink-0">
                    <Truck size={14} className="text-indigo-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-[11px] text-blue-600">{t.trip}</span>
                      <span className="text-[11px] text-gray-700 font-medium truncate">{t.route}</span>
                    </div>
                    <p className="text-[11px] text-gray-400 mt-0.5">{t.vehicle} · {t.driver}</p>
                  </div>
                  <div className="text-right shrink-0">
                    <p className="text-[11px] font-mono text-gray-500">{t.date}</p>
                    <Badge label="Beklemede" />
                  </div>
                </motion.div>
              ))}
            </div>
          </motion.div>
        </div>

      </div>
    </div>
  );
}

// ─────────────────────────────────────────────
// APP SHELL
// ─────────────────────────────────────────────
export default function App() {
  const [appState, setAppState] = useState<AppState>("login");
  const [active, setActive] = useState<ModuleKey>("dashboard");
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [toasts, setToasts] = useState<ToastData[]>([]);
  const toastId = useRef(0);

  const addToast = useCallback((message: string, type: ToastData["type"] = "success") => {
    const id = ++toastId.current;
    setToasts(prev => [...prev, { id, message, type }]);
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 3800);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  const moduleProps = { addToast };

  const MODULES: Record<ModuleKey, React.ReactNode> = {
    dashboard: <DashboardModule onNavigate={setActive} />,
    customers: <CustomersModule {...moduleProps} />,
    quotes: <QuotesModule {...moduleProps} />,
    loads: <LoadsModule {...moduleProps} />,
    trips: <TripsModule {...moduleProps} />,
    invoices: <InvoicesModule {...moduleProps} />,
    vehicles: <VehiclesModule {...moduleProps} />,
    users: <UsersModule {...moduleProps} />,
    support: <SupportModule {...moduleProps} />,
  };

  return (
    <div className="h-screen overflow-hidden" style={{ fontFamily: "'Inter', system-ui, sans-serif" }}>
      <AnimatePresence mode="wait">
        {appState === "login" ? (
          <motion.div key="login" className="h-full"
            initial={{ opacity: 1 }}
            exit={{ opacity: 0, scale: 1.02 }}
            transition={{ duration: 0.35, ease: "easeIn" }}>
            <LoginPage onLogin={() => setAppState("main")} />
          </motion.div>
        ) : (
          <motion.div key="main" className="flex h-full overflow-hidden"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.4, ease: "easeOut" }}
            style={{ backgroundColor: "#EEF1F6" }}>
            <Sidebar
              collapsed={collapsed}
              active={active}
              onSelect={k => setActive(k)}
              onToggle={() => setCollapsed(c => !c)}
              mobileOpen={mobileOpen}
              onMobileClose={() => setMobileOpen(false)}
            />

            <div className="flex flex-col flex-1 min-w-0 overflow-hidden">
              <TopBar active={active} onMenuToggle={() => setMobileOpen(true)} />

              <main className="flex-1 overflow-hidden">
                <AnimatePresence mode="wait">
                  <motion.div
                    key={active}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -4 }}
                    transition={{ duration: 0.18, ease: "easeOut" }}
                    className="h-full"
                  >
                    {MODULES[active]}
                  </motion.div>
                </AnimatePresence>
              </main>
            </div>

            <ToastContainer toasts={toasts} onRemove={removeToast} />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
