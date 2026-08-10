<template>
  <div>
    <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
      <div>
        <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
          <div>
            <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Kullanıcılar</h2>
            <div class="text-gray-500 text-sm mt-1">Kullanıcılarınızı yönetebilir ve detaylarını görüntüleyebilirsiniz.</div>
          </div>
          <Button @click="newUser" label="Yeni Hesap Oluştur" size="small" />
        </div>
        <UserTable ref="userTableRef" @setActiveCustomer="setActiveCustomer" />
      </div>
    </div>
    <UserFormDrawer :userState="user_state" @formSubmit="handleRefreshTable" />
  </div>
</template>

<script setup>
import { ref, reactive, inject } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import UserTable from "@/components/User/UserTable.vue";
import UserFormDrawer from "@/components/User/UserFormDrawer.vue";
import { UserAdd01Icon } from "hugeicons-vue";

const DataStore = useDataStore();
const progress_overlay_status = inject("progress_overlay_status");

const user_state = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
  loading_status: false,
});
const userTableRef = ref(null);

const newUser = () => {
  user_state.active_data = null;
  user_state.form_type = "create";
  user_state.drawer_visible = true;
};
const setActiveCustomer = async (data) => {
  user_state.form_type = "edit";
  user_state.active_data = null;
  user_state.loading_status = true;

  progress_overlay_status.value = true;
  const res = await DataStore.GET_USER_DATA(data.id);
  progress_overlay_status.value = false;

  user_state.drawer_visible = true;
  user_state.loading_status = false;
  user_state.active_data = res;
};
const handleRefreshTable = () => {
  console.log(userTableRef);
  userTableRef.value.refresh();
};
</script>
