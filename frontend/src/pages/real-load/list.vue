<template>
  <div>
    <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
      <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
        <div>
          <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Yükler</h2>
          <div class="text-gray-500 text-sm mt-1">Yükleri listeleyebilir ve güncelleme yapabilirsiniz..</div>
        </div>
      </div>
      <Tabs :value="active_table_tab" lazy scrollable>
        <TabList>
          <Tab value="0">Tümü</Tab>
          <Tab v-for="work_type in work_types" :key="work_type.id" :value="work_type.id">{{
            work_type.name.charAt(0).toUpperCase() + work_type.name.slice(1).toLowerCase()
          }}</Tab>
        </TabList>
        <TabPanels class="px-0!">
          <TabPanel value="0">
            <LoadTable @setActiveLoad="setActiveLoad" />
          </TabPanel>
          <TabPanel v-for="work_type in work_types" :key="work_type.id" :value="work_type.id">
            <LoadTable ref="work_type_tables" :typeId="work_type.id" @setActiveLoad="setActiveLoad" />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
    <LoadFormDrawer v-model:visible="load_drawer_status" v-model:load-id="selected_load_id" @formSubmit="refreshDatatable" @onHideDrawer="onHideDrawer" />
  </div>
</template>

<script setup>
import { ref, onMounted, inject, watch } from "vue";
import { useDatatable } from "@/composables/index.js";
import LoadFormDrawer from "@/components/RealLoad/LoadFormDrawer.vue";
import { useDataStore } from "@/stores/data_store.js";
import LoadTable from "@/components/RealLoad/LoadTable.vue";
import { useRouter } from "vue-router";

const Router = useRouter();
const DataStore = useDataStore();

const active_table_tab = ref("0");
const work_types = ref([]);
const work_type_tables = ref([]);

const realLoadTable = useDatatable({
  getUrl: "/api/v1/load_transfer",
  deleteUrl: "/api/v1/load_transfer",
  perPage: 10,
});

const selected_load_id = ref();
const load_drawer_status = ref(false);

const setActiveLoad = async (data) => {
  load_drawer_status.value = true;
  selected_load_id.value = data.id;
  Router.push({
    query: {
      load_id: data.id,
    },
  });
};

const refreshDatatable = (data) => {
  const work_type_id = data.load_data?.work_type?.id;
  work_type_tables.value.forEach((table, index) => {
    if (work_type_id == table?.typeId || active_table_tab.value == String(index + 1)) {
      table?.refresh();
    }
  });
};

const onHideDrawer = () => {};

onMounted(async () => {
  const res = await DataStore.GET_WORK_TYPES();
  if (res) {
    work_types.value = res.data;
  }
  realLoadTable.refresh();
});
</script>
