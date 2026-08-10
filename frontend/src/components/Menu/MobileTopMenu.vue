<template>
  <div class="sticky top-0 left-0 z-101">
    <div class="w-full h-14 bg-white border-b shadow-xs lg:hidden">
      <div class="h-full w-full flex items-center justify-between gap-2 container">
        <router-link to="/panel" class="h-full flex items-center justify-center">
          <img src="/assets/media/logo/ols-icon-primary.svg" class="w-7" />
        </router-link>
        <div class="flex items-center justify-end gap-4">
          <button v-if="showInstallButton" @click="installApp">
            <InboxDownloadIcon size="20" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { InboxDownloadIcon } from "hugeicons-vue";
import { useGeneralStore } from "@/stores/general_store.js";

const GeneralStore = useGeneralStore();
const router = useRouter();
const deferredPrompt = ref(null); // Olayı saklayacağımız değişken
const showInstallButton = ref(false); // Butonun görünürlüğünü kontrol eden değişken

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
