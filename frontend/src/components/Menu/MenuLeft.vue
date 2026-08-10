<template>
  <div
    :class="{
      'w-20 bg-primary': !big_menu_status,
      'w-64 bg-black/95': big_menu_status,
    }"
    class="relative shrink-0 h-screen transition-all duration-[350ms] max-lg:hidden z-1002"
  >
    <button
      @click="bigMenuStatus(big_menu_status ? false : true)"
      class="absolute top-1/2 left-full -translate-y-1/2 translate-x-2 size-8 rounded-full bg-white border shadow-sm z-2 flex items-center justify-center"
    >
      <ArrowRight01Icon
        :class="{
          'transform rotate-180': big_menu_status,
        }"
        class="text-primary transition-all"
        size="20"
      />
    </button>
    <div class="w-full h-full flex flex-col items-center py-4">
      <div class="w-full h-full flex flex-col items-center gap-8 lg:gap-14">
        <div class="w-full">
          <router-link to="/panel" class="w-full h-full flex items-center justify-center">
            <div class="w-full flex items-center justify-center transition-all">
              <img src="/assets/media/logo/ols-icon-white.svg" class="w-11" />
            </div>
          </router-link>
        </div>
        <div class="w-full overflow-auto thin-scrollbar pr-2">
          <MenuItem v-for="item in app_menu" :key="item.id" :item="item" :big-menu-status="big_menu_status" :current-route="route.fullPath" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { useGeneralStore } from "@/stores/general_store";
import MenuItem from "@/components/Menu/MenuItem.vue";
import { ArrowRight01Icon } from "hugeicons-vue";
import { app_menu } from "@/data/system_data.js";

const route = useRoute();
const GeneralStore = useGeneralStore();
const big_menu_status = ref(true);
let route_path = ref(null);

const bigMenuStatus = (status) => {
  big_menu_status.value = status;
};

onMounted(() => {
  route_path.value = window.location.pathname;
});
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: 500ms all;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  max-width: 0px;
  overflow: hidden;
}
</style>
