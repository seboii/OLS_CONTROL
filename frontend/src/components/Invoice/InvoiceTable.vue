<template>
  <div>
    <DatatableAjax :tableState="invoiceTable" selectionMode="single" @rowSelect="onRowSelect" scrollable removableSort search class="text-sm">
      <Column header="Fatura Numarası" frozen>
        <template #body="{ data }">
          <div class="text-nowrap">{{ data.invoice_id ? data.invoice_id : "-" }}</div>
        </template>
      </Column>
      <Column header="Senaryo Tipi">
        <template #body="{ data }">
          <div
            v-if="invoice_commercial_type.find((item) => item.value == data.commercial_type)"
            class="rounded-full border shadow-xs w-fit py-2 px-3 flex items-center justify-start gap-2 text-nowrap"
          >
            <div
              :class="{
                [invoice_commercial_type.find((item) => item.value == data.commercial_type)?.color]: true,
              }"
              class="w-1.5 aspect-square rounded-full overflow-hidden"
            ></div>
            <div class="text-xs">{{ invoice_commercial_type.find((item) => item.value == data.commercial_type)?.name }}</div>
          </div>
        </template>
      </Column>
      <Column header="Fatura Durumu">
        <template #body="{ data }">
          <div class="text-nowrap">{{ data.invoice_status?.name }}</div>
        </template>
      </Column>
      <Column header="Firma Adı">
        <template #body="{ data }">
          <div class="text-nowrap">{{ data.target_title }}</div>
        </template>
      </Column>
      <Column header="Vergi Kimlik No">
        <template #body="{ data }">
          <div class="text-nowrap">{{ data.target_identity_no }}</div>
        </template>
      </Column>
      <Column header="Fatura Tutarı (TRY)">
        <template #body="{ data }">
          <div class="text-nowrap">{{ data.payable_amount ? parseFloat(data.payable_amount).toFixed(2) : "-" }} {{ data.document_currency_code }}</div>
        </template>
      </Column>
      <Column header="Fatura Oluşturulma Tarihi">
        <template #body="{ data }">
          <div class="text-nowrap">{{ new Date(data.invoice_create_date).toLocaleString() }}</div>
        </template>
      </Column>
      <Column header="Fatura Gerçekleşme Tarihi">
        <template #body="{ data }">
          <div class="text-nowrap">{{ new Date(data.invoice_execution_date).toLocaleString() }}</div>
        </template>
      </Column>
    </DatatableAjax>
  </div>
</template>

<script setup>
import { onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";
import { invoice_commercial_type, invoice_box_types } from "@/data/system_data.js";

const props = defineProps(["invoiceType", "invoiceStatus"]);
const emits = defineEmits(["onRowSelect"]);
let default_params = {};
if (props.invoiceStatus) {
  default_params = {
    ...default_params,
    invoice_status_id: props.invoiceStatus,
  };
}
if (props.invoiceType !== undefined && props.invoiceType !== null) {
  default_params = {
    ...default_params,
    box_type: props.invoiceType,
  };
}
const invoiceTable = useDatatable({
  getUrl: "/api/v1/invoice",
  deleteUrl: "/api/v1/invoice/delete",
  perPage: 10,
  defaultParams: default_params,
});

const onRowSelect = (data) => {
  emits("onRowSelect", data.data);
};

defineExpose({
  refresh: () => {
    invoiceTable.refresh();
  },
});

onMounted(async () => {
  invoiceTable.refresh();
});
</script>
