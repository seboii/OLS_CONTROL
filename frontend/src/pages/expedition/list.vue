<template>
  <div>
    <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
      <div>
        <div class="mb-4 lg:mb-8 flex justify-between items-end gap-6">
          <div>
            <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Seferler</h2>
            <div class="text-gray-500 text-sm mt-1">Seferleri listeleyebilir ve yeni sefer oluşturabilirsiniz.</div>
          </div>
          <div>
            <Button @click="showExpeditionFormDrawer" label="Yeni Sefer Ekle" size="small" />
          </div>
        </div>
        <div>
          <Tabs v-model:value="active_table_tab" lazy scrollable>
            <TabList>
              <Tab value="0">Tümü</Tab>
              <Tab v-for="work_type in work_types" :key="work_type.id" :value="work_type.id">{{
                work_type.name.charAt(0).toUpperCase() + work_type.name.slice(1).toLowerCase()
              }}</Tab>
            </TabList>
            <TabPanels class="px-0!">
              <TabPanel value="0">
                <ExpeditionTable @onRowSelect="onRowSelect" />
              </TabPanel>
              <TabPanel v-for="work_type in work_types" :key="work_type.id" :value="work_type.id">
                <ExpeditionTable :typeId="work_type.id" @onRowSelect="onRowSelect" />
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>
      </div>
    </div>
    <ExpeditionFormDrawer
      v-model:visible="expedition_form_drawer_visible"
      v-model:expedition-id="expedition_id"
      v-model:general-data="expedition_drawer_props"
    />
  </div>
</template>

<script setup>
import { onMounted, ref, computed, reactive } from "vue";
import ExpeditionTable from "@/components/Expedition/ExpeditionTable.vue";
import { useDataStore } from "@/stores/data_store.js";
import ExpeditionFormDrawer from "@/components/Expedition/ExpeditionFormDrawer.vue";

const DataStore = useDataStore();

const expedition_form_drawer_visible = ref(false);
const expedition_id = ref(null);
const expedition_drawer_props = reactive({
  work_type: null,
});

const active_table_tab = ref("0");
const work_types = ref([]);
const expedition_form_route = computed(() => {
  return active_table_tab.value != 0 ? `/panel/expedition/form?work_type_id=${active_table_tab.value}` : "/panel/expedition/form";
});

const onRowSelect = (data) => {
  expedition_id.value = data.id;
  expedition_form_drawer_visible.value = true;
};

const showExpeditionFormDrawer = () => {
  expedition_drawer_props.work_type = active_table_tab.value != 0 ? active_table_tab.value : null;
  expedition_form_drawer_visible.value = true;
};

onMounted(async () => {
  const res = await DataStore.GET_WORK_TYPES();
  if (res) {
    work_types.value = res.data;
  }
});
</script>
