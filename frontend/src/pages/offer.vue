<template>
  <div v-if="usePermissionStatus(page_permission_slug).read" class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
    <Offer />
  </div>
  <div v-else>
    <NoPermissionPage />
  </div>
</template>

<script setup>
import Offer from "@/components/Offer/Offer.vue";
import NoPermissionPage from "@/components/Errors/401.vue";
import { usePermissionStatus } from "@/composables/index.js";
const page_permission_slug = "load_management";
</script>
