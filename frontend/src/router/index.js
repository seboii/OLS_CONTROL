import { createRouter, createWebHistory } from "vue-router";
import { useGeneralStore } from "@/stores/general_store.js";
import { useDataStore } from "@/stores/data_store.js";
import Cookies from "js-cookie";

import DefaultLayout from "@/layouts/Default.vue";
import GuestLayout from "@/layouts/Guest.vue";
import Error404 from "@/components/Errors/404.vue";

const authUser = async (to, from, next) => {
  try {
    const GeneralStore = useGeneralStore();
    const DataStore = useDataStore();
    const res = await GeneralStore.GET_AUTH();
    DataStore.user.data = GeneralStore.user_data;
    if (res) {
      await DataStore.GET_USER_ROLE(DataStore.user.data?.id);
    }
    if (to.meta.auth_type == "guest") {
      if (!res) {
        next();
      } else {
        await router.push({ path: "/panel" });
      }
    } else {
      next();
    }
    if (to.meta.auth_type == "user") {
      let token = Cookies.get("token");
      if (!token) {
        await router.push({ path: "/login" });
        return false;
      }
      if (!res) {
        await router.push({ path: "/login" });
      }
    }
    GeneralStore.SET_SPLASH_SCREEN_STATUS(false);
  } catch (error) {
    window.location.href = "/404";
  }
};

// Yalnızca 8 kapsam-içi modül (Müşteri, Teklif, Yük, Sefer, Fatura, Araç,
// Kullanıcılar, Destek Talebi) + zorunlu ortak kabuk (giriş, profil, 404).
// Kapsam dışı rotalar (dashboard, mail, calendar, working-tracking, goals,
// customs/ordino/authorization-letter, excel-management, reports,
// potantiel-jobs, customs-document-scan, container/3D, system ayarları)
// olsnew'de mevcut ama bilinçli olarak buraya taşınmadı — bkz.
// docs/SECILI-MODUL-PARITE-MATRISI.md.
//
// "/panel" (Dashboard) kapsam dışı bırakıldığı için giriş sonrası hedef
// doğrudan Müşteriler ekranına yönlendirilir; yeni bir ana sayfa
// tasarlanmadı (kapsamı genişletmemek için).
const routes = [
  { path: "/:pathMatch(.*)*", component: Error404 },
  { path: "/", redirect: "/panel" },
  {
    path: "/login",
    component: GuestLayout,
    meta: { auth_type: "guest" },
    beforeEnter: authUser,
    children: [
      {
        path: "",
        component: () => import("@/pages/guest/login.vue"),
      },
    ],
  },
  {
    path: "/panel",
    component: DefaultLayout,
    meta: { auth_type: "user" },
    beforeEnter: authUser,
    children: [
      {
        path: "",
        redirect: "/panel/accounts",
      },
      {
        path: "accounts",
        component: () => import("@/pages/accounts/index.vue"),
      },
      {
        path: "user",
        children: [
          {
            path: "list",
            component: () => import("@/pages/user/list.vue"),
          },
        ],
      },
      {
        path: "account",
        children: [
          {
            path: "",
            component: () => import("@/pages/account.vue"),
          },
        ],
      },
      {
        path: "offer",
        children: [
          {
            path: "",
            component: () => import("@/pages/offer.vue"),
          },
        ],
      },
      {
        path: "jobs",
        children: [
          {
            path: "",
            component: () => import("@/pages/offer.vue"),
          },
        ],
      },
      {
        path: "car",
        children: [
          {
            path: "",
            component: () => import("@/pages/car/list.vue"),
          },
          {
            path: "form",
            component: () => import("@/pages/car/form.vue"),
          },
          {
            path: ":id",
            component: () => import("@/pages/car/form.vue"),
          },
        ],
      },
      {
        path: "invoices",
        children: [
          {
            path: "",
            component: () => import("@/pages/invoices.vue"),
          },
          {
            path: "incoming",
            component: () => import("@/pages/invoices/incoming.vue"),
          },
          {
            path: "outgoing",
            component: () => import("@/pages/invoices/outgoing.vue"),
          },
          {
            path: "pending",
            component: () => import("@/pages/invoices/pending.vue"),
          },
        ],
      },
      {
        path: "expedition",
        children: [
          {
            path: "",
            component: () => import("@/pages/expedition/list.vue"),
          },
          {
            path: "form",
            component: () => import("@/pages/expedition/form.vue"),
          },
          {
            path: ":id",
            component: () => import("@/pages/expedition/form.vue"),
          },
        ],
      },
      {
        path: "real-load",
        children: [
          {
            path: "",
            component: () => import("@/pages/real-load/list.vue"),
          },
        ],
      },
      {
        path: "website",
        children: [
          {
            path: "contact",
            children: [
              {
                path: "form",
                children: [
                  {
                    path: "",
                    component: () => import("@/pages/website/contact/forms.vue"),
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
