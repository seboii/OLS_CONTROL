<template>
  <div>
    <SplashScreen />
    <MobileTopMenu />
    <MobileBottomMenu />
    <FeatureModals />

    <div class="flex lg:max-h-screen lg:overflow-hidden max-lg:pb-16 bg-gray-100">
      <MenuLeft />
      <div class="flex flex-col gap-16 lg:overflow-auto w-full min-h-screen max-md:container py-8 px-8">
        <Header />
        <router-view></router-view>
      </div>
    </div>
  </div>
</template>

<script setup>
// Mesajlaşma (MessageDrawer/NotificationDrawer, MongoDB+Socket.IO) ve
// QuickLoad (tetikleyicisi olmayan ölü UI) kapsam dışı bırakıldı — bkz.
// docs/SECILI-MODUL-PARITE-MATRISI.md. Menüdeki teklif/yük sayaç rozetleri de
// kaynağı Dashboard olduğu için (kapsam dışı) kaldırıldı.
import { defineAsyncComponent } from "vue";
const MobileTopMenu = defineAsyncComponent(() => import("@/components/Menu/MobileTopMenu.vue"));
const MobileBottomMenu = defineAsyncComponent(() => import("@/components/Menu/MobileBottomMenu.vue"));
const MenuLeft = defineAsyncComponent(() => import("@/components/Menu/MenuLeft.vue"));
const FeatureModals = defineAsyncComponent(() => import("@/components/FeatureModals/FeatureModals.vue"));
import SplashScreen from "@/components/SplashScreen.vue";
import Header from "@/components/Header/Header.vue";
</script>
