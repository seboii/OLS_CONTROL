<template>
  <div>
    <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
      <div>
        <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
          <div>
            <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Destek Talepleri</h2>
            <div class="text-gray-500 text-sm mt-1">Websiteden oluşturulmuş destek taleplerini inceleyebilir ve detaylarını görüntüleyebilirsiniz.</div>
          </div>
        </div>
        <FormsTable ref="contactFormsTableRef" @setActiveCustomer="setActiveCustomer" />
      </div>
    </div>
    <FormDetailDrawer :userState="user_state" @formSubmit="handleRefreshTable" />
  </div>
</template>

<script setup>
import { ref, reactive, provide } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import FormsTable from "@/components/Website/Contact/FormsTable.vue";
import FormDetailDrawer from "@/components/Website/Contact/FormDetailDrawer.vue";

const DataStore = useDataStore();
const user_state = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
  loading_status: false,
});
const contactFormsTableRef = ref(null);

const newUser = () => {
  user_state.active_data = null;
  user_state.form_type = "create";
  user_state.drawer_visible = true;
};
const setActiveCustomer = async (data) => {
  user_state.form_type = "edit";
  user_state.active_data = null;
  user_state.loading_status = true;

  const res = await DataStore.GET_WEBSITE_CONTACT_FORM(data.id);
  user_state.drawer_visible = true;
  user_state.loading_status = false;
  user_state.active_data = res;
};
const handleRefreshTable = () => {
  console.log(contactFormsTableRef);
  contactFormsTableRef.value.refresh();
};
</script>
