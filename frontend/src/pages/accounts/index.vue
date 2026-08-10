<template>
  <div>
    <div v-if="usePermissionStatus(page_permission_slug).read">
      <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
        <div>
          <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
            <div>
              <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Hesaplar</h2>
              <div class="text-gray-500 text-sm mt-1">Hesaplarınızı yönetebilir ve detaylarını görüntüleyebilirsiniz.</div>
            </div>
            <Button @click="newCustomer" size="small" label="Yeni Hesap Oluştur" />
          </div>
          <Tabs v-model:value="active_table_tab" lazy scrollable>
            <TabList
              :pt="{
                tablist: {
                  class: 'bg-transparent!',
                },
              }"
            >
              <Tab value="0">Tümü</Tab>
              <Tab value="1">Müşteriler</Tab>
              <Tab value="2">Tedarikçiler</Tab>
              <Tab value="3">Alıcılar</Tab>
              <Tab value="4">Göndericiler</Tab>
              <Tab value="5">Acenteler</Tab>
            </TabList>
            <TabPanels
              :pt="{
                root: {
                  class: 'bg-transparent! px-0!',
                },
              }"
            >
              <TabPanel value="0">
                <AccountTable @setActiveCustomer="setActiveCustomer" />
              </TabPanel>
              <TabPanel value="1">
                <AccountTable ref="type_1_table" typeId="1" @setActiveCustomer="setActiveCustomer" />
              </TabPanel>
              <TabPanel value="2">
                <AccountTable ref="type_2_table" typeId="2" @setActiveCustomer="setActiveCustomer" />
              </TabPanel>
              <TabPanel value="3">
                <AccountTable ref="type_3_table" typeId="3" @setActiveCustomer="setActiveCustomer" />
              </TabPanel>
              <TabPanel value="4">
                <AccountTable ref="type_4_table" typeId="4" @setActiveCustomer="setActiveCustomer" />
              </TabPanel>
              <TabPanel value="5">
                <AccountTable ref="type_5_table" typeId="5" @setActiveCustomer="setActiveCustomer" />
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>
      </div>
      <AccountFormDrawer :accountFormData="account_form_data" @formSubmit="refreshDatatable" />
    </div>
    <NoPermissionPage v-else />
  </div>
</template>

<script setup>
import { ref, reactive, provide, inject } from "vue";
import { UserAdd01Icon } from "hugeicons-vue";
import AccountFormDrawer from "@/components/Accounts/AccountFormDrawer.vue";
import AccountTable from "@/components/Accounts/AccountTable.vue";
import { useDataStore } from "@/stores/data_store.js";
import NoPermissionPage from "@/components/Errors/401.vue";
import { usePermissionStatus } from "@/composables/index.js";

const page_permission_slug = "account_management";
const DataStore = useDataStore();
const progress_overlay_status = inject("progress_overlay_status");

const active_table_tab = ref("0");
const type_1_table = ref(null);
const type_2_table = ref(null);
const type_3_table = ref(null);
const type_4_table = ref(null);
const type_5_table = ref(null);

const account_form_data = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
});

const newCustomer = () => {
  account_form_data.active_data = null;
  account_form_data.form_type = "create";
  account_form_data.drawer_visible = true;
};
const setActiveCustomer = async (data) => {
  progress_overlay_status.value = true;
  const res = await DataStore.GET_ACCOUNT(data.id);
  progress_overlay_status.value = false;
  account_form_data.active_data = res;
  account_form_data.form_type = "edit";
  account_form_data.drawer_visible = true;
};

const refreshDatatable = (data) => {
  const { account_type } = data;
  if (account_type.id == 1 || active_table_tab.value == "1") {
    type_1_table.value.refresh();
  }
  if (account_type.id == 2 || active_table_tab.value == "2") {
    type_2_table.value.refresh();
  }
  if (account_type.id == 3 || active_table_tab.value == "3") {
    type_3_table.value.refresh();
  }
  if (account_type.id == 4 || active_table_tab.value == "4") {
    type_4_table.value.refresh();
  }
  if (account_type.id == 5 || active_table_tab.value == "5") {
    type_5_table.value.refresh();
  }
};

provide("account_form_data", account_form_data);
</script>
