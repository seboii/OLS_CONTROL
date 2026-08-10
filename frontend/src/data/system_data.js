import { reactive } from "vue";
import { ref } from "vue";
import {
  UserGroupIcon,
  PackageMovingIcon,
  UserListIcon,
  CustomerSupportIcon,
  Car01Icon,
  Route03Icon,
  CodesandboxIcon,
  UserIcon,
  Settings02Icon,
  Invoice01Icon,
} from "hugeicons-vue";

// Yalnızca 8 kapsam-içi modül + Ayarlar (Hesabım). Kapsam dışı dallar
// (Anasayfa/Dashboard, Raporlar, Çalışan Takibi, 3D Yük Önizlemesi, Gümrük
// Yönetimi, Sistem Ayarları, Günlük Değerlendirme) olsnew'de mevcuttu ama
// bilinçli olarak kaldırıldı — bkz. docs/SECILI-MODUL-PARITE-MATRISI.md.
export const app_menu = ref([
  {
    id: 6,
    name: "Müşteriler",
    icon: UserGroupIcon,
    path: "/panel/accounts",
    permission_slug: "account_management",
  },
  {
    id: 4,
    name: "Teklifler",
    key: "offers",
    icon: PackageMovingIcon,
    path: "/panel/offer",
    permission_slug: "load_management",
    count: "",
  },
  {
    id: 8,
    name: "Yükler",
    key: "loads",
    icon: CodesandboxIcon,
    path: "/panel/real-load",
    count: "",
  },
  {
    id: 8,
    name: "Seferler",
    icon: Route03Icon,
    path: "/panel/expedition",
  },
  {
    id: 8,
    name: "Faturalar",
    icon: Invoice01Icon,
    path: "/panel/invoices",
    children: [
      {
        id: 81,
        name: "Gider Faturalar",
        icon: Invoice01Icon,
        path: "/panel/invoices/incoming",
      },
      {
        id: 82,
        name: "Gelir Faturalar",
        icon: Invoice01Icon,
        path: "/panel/invoices/outgoing",
      },
      {
        id: 83,
        name: "Onay Bekleyen Faturalar",
        icon: Invoice01Icon,
        path: "/panel/invoices/pending",
      },
    ],
  },
  {
    id: 8,
    name: "Araçlar",
    icon: Car01Icon,
    path: "/panel/car",
  },
  {
    id: 7,
    name: "Kullanıcılar",
    icon: UserListIcon,
    path: "/panel/user/list",
  },
  {
    id: 7,
    name: "Destek Talepleri",
    icon: CustomerSupportIcon,
    path: "/panel/website/contact/form",
  },
  {
    id: 7,
    name: "Ayarlar",
    icon: Settings02Icon,
    children: [
      {
        name: "Hesabım",
        icon: UserIcon,
        path: "/panel/account",
      },
    ],
  },
]);

export const invoice_commercial_type = [
  {
    value: 0,
    name: "Temel Fatura",
    color: "bg-blue-500",
    status: true,
  },
  {
    value: 1,
    name: "Ticari Fatura",
    color: "bg-orange-500",
    status: true,
  },
  {
    value: 4,
    name: "E-Arşiv",
    color: "bg-green-500",
    status: false,
  },
];

export const invoice_box_type = [
  {
    value: 0,
    name: "Alış",
    status: true,
  },
  {
    value: 1,
    name: "Satış",
    status: true,
  },
];

export const invoice_box_types = [
  {
    value: 0,
    name: "Gelen Fatura",
  },
  {
    value: 1,
    name: "Giden Fatura",
  },
];

export const define_datas = reactive({
  invoice_pending_approval_status_id: 7,
  account: {
    account_type: [
      {
        id: 1,
        name: "Müşteri",
        siber_id: null,
        created_at: "2025-01-03T13:11:35.000000Z",
        updated_at: "2025-01-03T13:11:35.000000Z",
      },
      {
        id: 2,
        name: "Tedarikçi",
        siber_id: null,
        created_at: "2025-01-03T13:11:35.000000Z",
        updated_at: "2025-01-03T13:11:35.000000Z",
      },
      {
        id: 3,
        name: "Alıcı",
        siber_id: null,
        created_at: "2025-01-03T13:11:35.000000Z",
        updated_at: "2025-01-03T13:11:35.000000Z",
      },
      {
        id: 4,
        name: "Gönderici",
        siber_id: null,
        created_at: "2025-01-03T13:11:35.000000Z",
        updated_at: "2025-01-03T13:11:35.000000Z",
      },
      {
        id: 5,
        name: "Acente",
        siber_id: null,
        created_at: "2025-01-03T13:11:35.000000Z",
        updated_at: "2025-01-03T13:11:35.000000Z",
      },
    ],
    contact_language: [
      {
        id: "6146f406-d738-4707-a66c-ed542e342b4a",
        name: "İngilizce",
        country_code: "GB",
        flag: null,
        phone_code: "44",
        slug: null,
        siber_id: "6146F406-D738-4707-A66C-ED542E342B4A",
        created_at: "2025-01-03T13:22:06.000000Z",
        updated_at: "2025-01-03T13:22:06.000000Z",
      },
      {
        id: "14054599-5c16-4c40-ace5-14d880bde59e",
        name: "Rusça",
        country_code: "SU",
        flag: null,
        phone_code: "7",
        slug: null,
        siber_id: "14054599-5C16-4C40-ACE5-14D880BDE59E",
        created_at: "2025-01-03T13:22:05.000000Z",
        updated_at: "2025-01-03T13:22:05.000000Z",
      },
      {
        id: "eb3f2dbe-96fe-4b17-9947-c0ad63af76ca",
        name: "Türkçe",
        country_code: "TR",
        flag: null,
        phone_code: "90",
        slug: null,
        siber_id: "EB3F2DBE-96FE-4B17-9947-C0AD63AF76CA",
        created_at: "2025-01-03T13:22:06.000000Z",
        updated_at: "2025-01-03T13:22:06.000000Z",
      },
    ],
  },
  offer: {
    transport_type: [
      {
        id: 1,
        name: "RO-RO",
        code: "1",
        group_code: "REZERVASYONTASIMASEKLI",
        siber_id: "9E45ED23-EF9F-45E4-9530-0FA9F2D6C51C",
        created_at: "2025-01-03T13:17:10.000000Z",
        updated_at: "2025-01-03T13:17:10.000000Z",
      },
      {
        id: 2,
        name: "TREN",
        code: "2",
        group_code: "REZERVASYONTASIMASEKLI",
        siber_id: "E0ADF7B0-6711-48ED-B2F5-FFBDEBD405A2",
        created_at: "2025-01-03T13:17:10.000000Z",
        updated_at: "2025-01-03T13:17:10.000000Z",
      },
      {
        id: 3,
        name: "KARA",
        code: "3",
        group_code: "REZERVASYONTASIMASEKLI",
        siber_id: "B84B6983-7328-469C-8CBE-58E4AB2B3DB4",
        created_at: "2025-01-03T13:17:10.000000Z",
        updated_at: "2025-01-03T13:17:10.000000Z",
      },
    ],
  },
  currency: [
    {
      id: 98,
      name: "ABD DOLARI",
      symbol: "$",
      code: "USD",
      siber_id: "98A1F932-AD6C-4A57-881D-DFE19362FA2C",
      created_at: "2025-01-03T13:28:31.000000Z",
      updated_at: "2025-01-03T13:28:31.000000Z",
    },
    {
      id: 93,
      name: "EURO",
      symbol: "€",
      code: "EUR",
      siber_id: "32D6C8A5-2923-4E1F-8021-D684CABC9648",
      created_at: "2025-01-03T13:28:31.000000Z",
      updated_at: "2025-01-03T13:28:31.000000Z",
    },
    {
      id: 73,
      name: "TÜRK LİRASI",
      symbol: "₺",
      code: "TL",
      siber_id: "6B0FCB81-8CCA-4324-81F1-ABB8A7C36C42",
      created_at: "2025-01-03T13:28:31.000000Z",
      updated_at: "2025-01-03T13:28:31.000000Z",
    },
    {
      id: 69,
      name: "RUS RUBLESİ",
      symbol: "₽",
      code: "RUB",
      siber_id: "A5B51807-8E2D-4678-8BCF-99A4067EC636",
      created_at: "2025-01-03T13:28:31.000000Z",
      updated_at: "2025-01-03T13:28:31.000000Z",
    },
    {
      id: 102,
      name: "ÇİN YUANI",
      symbol: "¥",
      code: "CNY",
      siber_id: "5C0D355F-D2C4-460E-980B-EE367DF0A62F",
      created_at: "2025-01-03T13:28:31.000000Z",
      updated_at: "2025-01-03T13:28:31.000000Z",
    },
    {
      id: 53,
      name: "İNGİLİZ STERLİNİ",
      symbol: "£",
      code: "GBP",
      siber_id: "E5D17FFB-E0A8-44B8-8E6F-77259F73E45C",
      created_at: "2025-01-03T13:28:31.000000Z",
      updated_at: "2025-01-03T13:28:31.000000Z",
    },
  ],
});
export const yes_no_options = [
  {
    label: "Evet",
    value: 1,
  },
  {
    label: "Hayır",
    value: 0,
  },
];

export const buysell_types = [
  {
    name: "Alış",
    value: 1,
  },
  {
    name: "Satış",
    value: 2,
  },
];
export const financial_item_status_type = [
  { id: "pending", name: "Bekleniyor" },
  { id: "invoice_received", name: "Faturası Geldi" },
  { id: "invoice_issued", name: "Faturası Kesildi" },
];
export const product_stackable_types = [
  {
    id: 1,
    name: "İstiflenebilir",
  },
  {
    id: 0,
    name: "İstiflenemez",
  },
];
