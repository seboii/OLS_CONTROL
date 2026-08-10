<template>
  <div class="flex items-center justify-between gap-4 max-lg:hidden">
    <div>
      <slot name="title"></slot>
    </div>
    <div class="flex items-center justify-end gap-2">
      <div v-if="showInstallButton" @click="installApp" role="button" class="size-14 rounded-full bg-white border shadow-xs flex items-center justify-center">
        <InboxDownloadIcon size="20" class="text-gray-600" />
      </div>
      <div
        role="button"
        @click="accountPopoverToggle"
        class="h-14 lg:min-w-64 pl-3 pr-8 rounded-full bg-white border shadow-xs flex items-center gap-2"
        v-ripple
      >
        <div class="size-9 rounded-full border overflow-hidden bg-gray-100">
          <img v-if="DataStore.user?.data?.avatar" :src="`/storage/${DataStore.user.data.avatar}`" class="w-full h-full object-cover" />
        </div>
        <div>
          <div class="font-medium text-gray-700 text-sm">{{ DataStore.user?.data?.name }} {{ DataStore.user?.data?.surname }}</div>
          <div class="text-xs text-gray-500 line-clamp-1">{{ DataStore.user?.data?.email }}</div>
          <div v-if="false" class="text-xs text-gray-500">Operasyon Yöneticisi</div>
        </div>
      </div>
      <Popover ref="account_popover" class="lg:min-w-64">
        <div class="space-y-2">
          <Button @click="() => $router.push({ path: '/panel/account' })" size="small" severity="secondary" label="Hesabım" fluid />
          <Button @click="logout" size="small" severity="secondary" label="Çıkış Yap" fluid />
        </div>
      </Popover>
    </div>
  </div>
</template>

<script setup>
import axios from "axios";
import { ref, watch, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useGeneralStore } from "@/stores/general_store";
import { useDataStore } from "@/stores/data_store";
import { Analytics01Icon, ChartHistogramIcon, UserStoryIcon, UserMultiple02Icon, InboxDownloadIcon } from "hugeicons-vue";
import Cookies from "js-cookie";

const router = useRouter();
const GeneralStore = useGeneralStore();
const DataStore = useDataStore();

const deferredPrompt = ref(null); // Olayı saklayacağımız değişken
const showInstallButton = ref(false); // Butonun görünürlüğünü kontrol eden değişken
const account_popover = ref(null);
const accountPopoverToggle = (event) => {
  account_popover.value.toggle(event);
};

const logout = async () => {
  await GeneralStore.LOGOUT();
};

const installApp = () => {
  if (deferredPrompt.value) {
    // Yükleme ekranını tetikle
    deferredPrompt.value.prompt();

    // Kullanıcının cevabını kontrol et
    deferredPrompt.value.userChoice.then((choiceResult) => {
      if (choiceResult.outcome === "accepted") {
        console.log("Kullanıcı PWA uygulamasını yükledi");
      } else {
        console.log("Kullanıcı PWA uygulamasını reddetti");
      }
      // `deferredPrompt` null yap, tekrar tetiklenmesin
      deferredPrompt.value = null;
      showInstallButton.value = false;
    });
  }
};

onMounted(() => {
  window.addEventListener("beforeinstallprompt", (event) => {
    // Tarayıcının varsayılan "PWA Yükle" davranışını durdur
    event.preventDefault();
    // Olayı sakla, daha sonra tetiklemek için
    deferredPrompt.value = event;
    // Butonu göstermek için görünürlüğü ayarla
    showInstallButton.value = true;
  });
});
</script>
