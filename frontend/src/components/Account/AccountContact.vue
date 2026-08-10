<template>
  <div>
    <div class="h-full border lg:rounded-xl shadow-xs bg-white">
      <div class="py-6 max-lg:container lg:px-8 flex justify-between gap-4 border-b">
        <div>
          <h3 class="font-medium text-xl">İletişim Bilgileri</h3>
          <p class="text-xs text-gray-500 mt-1 leading-4 min-h-8">İletişim bilgilerinizi doğru ve eksiksiz girmeye özen gösteriniz.</p>
        </div>
        <div>
          <Button @click="contact_form_modal_status = true" size="small" severity="secondary" outlined rounded>Düzenle</Button>
        </div>
      </div>
      <div class="py-6 max-lg:container lg:p-8 space-y-4 lg:space-y-8">
        <div>
          <div class="font-semibold text-gray-800 mb-1">Telefon Numarası</div>
          <div class="flex items-center gap-1">
            <div v-if="DataStore.user.data?.phone_country_id" class="flex items-center gap-2">
              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden">
                <img
                  :src="`https://flagcdn.com/w160/${DataStore.user.data?.phone_country_id?.country_code.toLowerCase()}.png`"
                  class="w-full h-full object-cover"
                />
              </div>
              <p class="text-gray-600 text-sm">+{{ DataStore.user.data?.phone_country_id?.phone_code }} {{ DataStore.user.data?.phone_country }}</p>
            </div>
            <p class="text-gray-600 text-sm">{{ DataStore.user.data?.phone }}</p>
          </div>
        </div>
        <div>
          <div class="font-semibold text-gray-800 mb-1">E-Posta Adresi</div>
          <div class="flex items-center gap-1">
            <p class="text-gray-600 text-sm">{{ DataStore.user.data?.email }}</p>
          </div>
        </div>
      </div>
    </div>
    <AccountContactFormModal :modalStatus="contact_form_modal_status" @formSubmit="getProfileData" />
  </div>
</template>

<script setup>
import { ref, provide } from "vue";
import { useDataStore } from "@/stores/data_store";
import AccountContactFormModal from "@/components/Account/AccountContactFormModal.vue";

const DataStore = useDataStore();
const contact_form_modal_status = ref(false);

const getProfileData = async () => {
  await DataStore.GET_PROFILE();
};

provide("contact_form_modal_status", contact_form_modal_status);
</script>
