<template>
  <div>
    <Card>
      <template #content>
        <DatatableAjax :tableState="expeditionTable" selectionMode="single" @rowSelect="onRowSelect" removableSort search class="text-sm table-nowrap">
          <Column field="expedition_number" header="Sefer Numarası" class="text-nowrap" />
          <Column field="expedition_type_id.name" header="Tipi" />
          <Column field="work_type.name" header="Yük Türü" />
          <Column field="romork_id.plate_number" header="Plaka" />
          <Column :field="(e) => `${e.start_city_id ? e.start_city_id.name : ''} - ${e.end_city_id ? e.end_city_id.name : ''}`" header="Başlangıç - Bitiş">
            <template #body="{ data }">
              <div class="flex items-center gap-1.5">
                <p class="max-lg:text-nowrap">{{ data.start_city_id ? data.start_city_id.name : "" }} - {{ data.end_city_id ? data.end_city_id.name : "" }}</p>
              </div>
            </template>
          </Column>
          <Column field="department_id.name" header="Departman" />
          <Column field="status_id.name" header="Durum" />
        </DatatableAjax>
      </template>
    </Card>
  </div>
</template>

<script setup>
import { onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";

const props = defineProps(["typeId"]);
const emits = defineEmits(["onRowSelect"]);

const onRowSelect = ({ data }) => {
  emits("onRowSelect", data);
};

let table_params = {};
if (props.typeId) {
  table_params.work_type_id = props.typeId;
}

const expeditionTable = useDatatable({
  getUrl: "/api/v1/expedition",
  defaultParams: table_params,
  deleteUrl: "/api/v1/expedition",
  perPage: 10,
});

onMounted(() => {
  expeditionTable.refresh();
});
</script>
