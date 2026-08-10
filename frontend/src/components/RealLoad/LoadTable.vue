<template>
  <Card>
    <template #content>
      <DatatableAjax :tableState="realLoadTable" selectionMode="single" @rowSelect="onRowSelect" removableSort search class="text-sm table-nowrap">
        <Column field="load_number_work_type" header="Yük Numarası" />
        <Column field="customer_id.name" header="Müşteri" c>
          <template #body="slotProps">
            <div :title="slotProps.data.customer_id.name" class="line-clamp-1">{{ slotProps.data.customer_id.name }}</div>
          </template>
        </Column>
        <Column field="sender_id.name" header="Gönderici" c>
          <template #body="slotProps">
            <div :title="slotProps.data.sender_id.name" class="line-clamp-1">{{ slotProps.data.sender_id.name }}</div>
          </template>
        </Column>
        <Column field="load_status_id.name" header="Durum" />
      </DatatableAjax>
    </template>
  </Card>
</template>

<script setup>
import { ref, reactive, provide, onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";

const props = defineProps(["typeId"]);
const emits = defineEmits(["setActiveLoad"]);

let table_params = {};
if (props.typeId) {
  table_params.work_type_id = props.typeId;
}

const realLoadTable = useDatatable({
  getUrl: "/api/v1/load_transfer",
  defaultParams: table_params,
  deleteUrl: "/api/v1/load_transfer",
  perPage: 10,
});

const onRowSelect = (e) => {
  emits("setActiveLoad", e.data);
};

defineExpose({
  refresh: realLoadTable.refresh,
});

onMounted(() => {
  realLoadTable.refresh();
});
</script>
