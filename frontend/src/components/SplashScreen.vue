<template>
  <Transition name="fade">
    <div v-if="GeneralStore.splash_screen_status" class="fixed top-0 left-0 w-screen h-screen z-999999999999999 bg-white flex items-center justify-center">
      <span class="loader"></span>
    </div>
  </Transition>
</template>

<script setup>
import { useGeneralStore } from "@/stores/general_store";
const GeneralStore = useGeneralStore();
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: 200ms all;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
