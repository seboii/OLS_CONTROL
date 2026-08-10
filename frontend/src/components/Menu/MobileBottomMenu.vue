<template>
  <div class="lg:hidden">
    <div class="fixed bottom-0 left-0 w-full h-20 bg-white border-t z-103 flex items-center py-2">
      <div class="container w-full h-full grid grid-cols-5 gap-2 pb-4">
        <button @click="menu_data.home.click" class="h-full bg-white rounded-xl flex items-center justify-center">
          <Home09Icon
            size="24"
            :class="{
              'text-primary': menu_data.home.active,
            }"
          />
        </button>
        <button @click="menu_data.loads.click" class="h-full bg-white rounded-xl flex items-center justify-center">
          <ProductLoadingIcon
            size="24"
            :class="{
              'text-primary': menu_data.loads.active,
            }"
          />
        </button>
        <button @click="menu_data.new_load.click" class="h-full bg-primary rounded-full flex items-center justify-center">
          <AddCircleIcon size="30" class="text-white" />
        </button>
        <button @click="menu_data.mail.click" class="h-full bg-white rounded-xl flex items-center justify-center">
          <Mail01Icon
            size="24"
            :class="{
              'text-primary': menu_data.mail.active,
            }"
          />
        </button>
        <button @click="menu_data.menu.click" class="h-full bg-white rounded-xl flex items-center justify-center">
          <Menu01Icon
            size="24"
            :class="{
              'text-primary': menu_data.menu.active,
            }"
          />
          <MobilMenu />
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, defineAsyncComponent, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { Home09Icon, ProductLoadingIcon, Mail01Icon, Menu01Icon, AddCircleIcon } from "hugeicons-vue";
import { useGeneralStore } from "@/stores/general_store.js";
const MobilMenu = defineAsyncComponent(() => import("@/components/Menu/MobileMenu.vue"));

const GeneralStore = useGeneralStore();
const active_menu = ref("home");
const route = useRoute();
const router = useRouter();

const menu_data = reactive({
  home: {
    name: "home",
    icon: Home09Icon,
    click: () => {
      GeneralStore.SET_MOBILE_BOTTOM_MENU_STATUS(false);
      router.push({ path: "/panel" });
    },
    active: computed(() => route.path === "/panel" || route.path === "/panel/"),
  },
  loads: {
    name: "loads",
    icon: ProductLoadingIcon,
    click: () => {
      GeneralStore.SET_MOBILE_BOTTOM_MENU_STATUS(false);
      router.push({ path: "/panel/offer" });
    },
    active: computed(() => route.path === "/panel/offer"),
  },
  new_load: {
    name: "new_load",
    icon: AddCircleIcon,
    click: () => {
      router.push("/panel/offer");
      return true;
      GeneralStore.SET_MOBILE_BOTTOM_MENU_STATUS(false);
      GeneralStore.SET_NEW_QUICK_LOAD_DRAWER_STATUS(true);
    },
  },
  mail: {
    name: "mail",
    icon: Mail01Icon,
    click: () => {
      GeneralStore.SET_MOBILE_BOTTOM_MENU_STATUS(false);
      router.push({ path: "/panel/mail" });
    },
    active: computed(() => route.path === "/panel/mail"),
  },
  menu: {
    name: "menu",
    icon: Menu01Icon,
    click: () => {
      GeneralStore.SET_MOBILE_BOTTOM_MENU_TOGGLE();
    },
    active: computed(() => GeneralStore.mobile_bottom_menu.visible == true),
  },
});
</script>
