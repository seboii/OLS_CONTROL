<template>
  <Teleport to="body">
    <Transition name="fade">
      <div v-if="GeneralStore.mobile_bottom_menu.visible" class="fixed top-0 h-[calc(100vh-4rem)] w-full bg-white z-102 overflow-auto">
        <div class="py-4 transition-all">
          <MobileMenuItem v-for="item in app_menu" :key="item.id" :item="item" :depth="0" @menu-click="handleMenuClick" />
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { watch } from "vue";
import { useRoute } from "vue-router";
import { useGeneralStore } from "@/stores/general_store.js";
import { app_menu } from "@/data/system_data.js";
import MobileMenuItem from "@/components/Menu/MobileMenuItem.vue";

const GeneralStore = useGeneralStore();
const route = useRoute();

watch(
  () => route.fullPath,
  () => {
    GeneralStore.mobile_bottom_menu.visible = false;
  }
);

watch(
  () => GeneralStore.mobile_bottom_menu.visible,
  (value) => {
    document.body.style.overflow = value ? "hidden" : "auto";
  }
);
const handleMenuClick = (item) => {
  if (item.path) {
    GeneralStore.mobile_bottom_menu.visible = false;
  }
};
</script>
<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: 250ms all;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(20px);
}
</style>
