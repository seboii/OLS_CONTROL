<template>
  <div>
    <div class="h-full border lg:rounded-xl shadow-xs bg-white">
      <div class="py-6 max-lg:container lg:px-8 flex justify-between gap-4 border-b">
        <div>
          <h3 class="font-medium text-xl">Güvenlik</h3>
          <p class="text-xs text-gray-500 mt-1 leading-4 min-h-8">Şifrenizi ve güvenlik araçlarınızı yönetebilirsiniz.</p>
        </div>
        <div>
          <Button @click="security_form_modal_status = true" size="small" severity="secondary" outlined rounded>Düzenle</Button>
        </div>
      </div>
      <div class="py-6 max-lg:container lg:p-8 space-y-4 lg:space-y-8">
        <div>
          <div class="font-semibold text-gray-800 mb-1">Şifre</div>
          <div class="flex items-center justify-between gap-4">
            <p class="text-gray-600 text-sm">**********</p>
          </div>
        </div>
      </div>
    </div>
    <AccountSecurityFormModal :modalStatus="security_form_modal_status" @formSubmit="getProfileData" />
  </div>
</template>

<script setup>
import { ref, provide } from "vue";
import { useDataStore } from "@/stores/data_store";
import AccountSecurityFormModal from "@/components/Account/AccountSecurityFormModal.vue";

const DataStore = useDataStore();
const security_form_modal_status = ref(false);

const getProfileData = async () => {
  await DataStore.GET_PROFILE();
};

provide("security_form_modal_status", security_form_modal_status);
</script>
