<template>
  <div>
    <div class="h-full border lg:rounded-xl shadow-xs bg-white">
      <div class="py-6 max-lg:container lg:px-8 flex justify-between gap-4 border-b">
        <div>
          <h3 class="font-medium text-xl">Arayüz</h3>
          <p class="text-xs text-gray-500 mt-1 leading-4 min-h-8">Uygulamanın arayüz ayarlarını düzenleyebilirsiniz.</p>
        </div>
      </div>
      <div class="py-6 max-lg:container lg:p-8 space-y-4 lg:space-y-8">
        <div>
          <div class="font-semibold text-gray-800 mb-1">Renk</div>
          <div class="flex flex-wrap gap-2 mt-4">
            <div role="button" v-for="color in primary_colors" :key="color" @click="setThemeColor(color.name)" v-tooltip="color.name">
              <div
                :style="{
                  backgroundColor: color.colors.default,
                }"
                :class="{
                  'ring-2': activeColors.default == color.colors.default,
                }"
                class="size-5 rounded-full"
              ></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, provide } from "vue";
import { useDataStore } from "@/stores/data_store";
import { primary_colors, setThemeColor, activeColors } from "@/config/theme-config.js";
import JsCookie from "js-cookie";

const themeCookie = JsCookie.get("theme-color");
const DataStore = useDataStore();
const security_form_modal_status = ref(false);

const getProfileData = async () => {
  await DataStore.GET_PROFILE();
};

provide("security_form_modal_status", security_form_modal_status);
</script>
