<template>
  <div>
    <div class="h-full border lg:rounded-xl shadow-xs bg-white">
      <div class="py-6 max-lg:container lg:px-8 flex justify-between gap-4 border-b">
        <div>
          <h3 class="font-medium text-xl">Profil</h3>
          <p class="text-xs text-gray-500 mt-1 leading-4 min-h-8">Kişisel bilgilerinize ait detayları görüntüleyebilir ve değiştirebilirsiniz.</p>
        </div>
        <div>
          <Button @click="general_form_modal_status = true" size="small" severity="secondary" outlined rounded>Düzenle</Button>
        </div>
      </div>
      <div class="py-6 max-lg:container lg:p-8 space-y-4 lg:space-y-8">
        <div>
          <div class="font-semibold text-gray-800">Ad Soyad</div>
          <div>
            <p class="text-gray-600 text-sm">{{ DataStore.user?.data?.name }} {{ DataStore.user?.data?.surname }}</p>
          </div>
        </div>
        <div>
          <div class="font-semibold text-gray-800">Ülke</div>
          <div v-if="DataStore.user?.data?.country_id">
            <div class="text-gray-600 text-sm flex items-center gap-2">
              <div class="w-6 aspect-4/3 rounded-sm overflow-hidden">
                <img :src="`https://flagcdn.com/w160/${DataStore.user.data?.country_id?.country_code.toLowerCase()}.png`" class="w-full h-full object-cover" />
              </div>
              <div>{{ DataStore.user?.data?.country_id?.name }}</div>
            </div>
          </div>
          <div v-else>-</div>
        </div>
      </div>
    </div>
    <AccountGeneralFormModal :modalStatus="general_form_modal_status" @formSubmit="getProfileData" />
  </div>
</template>

<script setup>
import { ref, provide } from "vue";
import { useDataStore } from "@/stores/data_store";
import AccountGeneralFormModal from "@/components/Account/AccountGeneralFormModal.vue";

const DataStore = useDataStore();
const general_form_modal_status = ref(false);

const getProfileData = async () => {
  await DataStore.GET_PROFILE();
};

provide("general_form_modal_status", general_form_modal_status);
</script>
