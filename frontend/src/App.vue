<template>
  <div>
    <ConfirmDialog :draggable="false" class="w-full lg:w-auto lg:min-w-[500px]" />
    <Toaster position="top-center" closeButton />
    <ProgressOverlay :show="progress_overlay_status" />
    <router-view v-if="!GeneralStore.splash_screen_status" />
  </div>
</template>

<script setup>
import { ref, provide, onMounted } from "vue";
import { Toaster } from "vue-sonner";
import { useDataStore } from "@/stores/data_store.js";
import { useGeneralStore } from "@/stores/general_store.js";
import ProgressOverlay from "@/components/ProgressOverlay.vue";
const base_url = "http://localhost:8000/api/v1";
const DataStore = useDataStore();
const GeneralStore = useGeneralStore();

const progress_overlay_status = ref(false);

provide("base_url", base_url);
provide("progress_overlay_status", progress_overlay_status);
</script>
