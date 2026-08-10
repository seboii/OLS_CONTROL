<template>
  <div>
    <SplashScreen />
    <router-view></router-view>
  </div>
</template>

<script setup>
import { useGeneralStore } from "@/stores/general_store";
import SplashScreen from "@/components/SplashScreen.vue";

const GeneralStore = useGeneralStore();
</script>
