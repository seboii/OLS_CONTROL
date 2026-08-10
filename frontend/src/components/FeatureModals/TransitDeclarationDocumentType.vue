<template>
  <div>
    <Dialog
      v-model:visible="FeatureStore.transit_declaration_document_types_modal_status"
      modal
      header="Transit Beyannamesi Döküman Tipleri"
      class="w-full lg:w-[600px]"
      @show="showModal"
    >
      <div class="flex justify-end">
        <Button type="button" label="Yeni Ekle" size="small" @click="setCreate"></Button>
      </div>
      <div class="mt-6">
        <DatatableAjax
          :tableState="TypeDatatable"
          selectionMode="single"
          @rowSelect="onRowSelect"
          :pt="{
            tbody: {
              class: 'text-sm!',
            },
          }"
          class="table-nowrap"
          removableSort
        >
          <Column field="name" header="Adı">
            <template #editor="{ data, field }">
              <InputText v-model="data[field]" fluid />
            </template>
          </Column>
          <Column :rowEditor="true" style="width: 10%; min-width: 8rem" bodyStyle="text-align:center"></Column>
        </DatatableAjax>
      </div>
    </Dialog>
    <Dialog
      v-model:visible="type_data.modal_status"
      modal
      :header="type_data.data.id ? 'Transit Beyannamesi Döküman Tipi Düzenle' : 'Transit Beyannamesi Döküman Tipi Oluştur'"
      class="w-full lg:w-[500px]"
    >
      <div class="my-8">
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="type_data.data.name" fluid />
          <label>Adı </label>
        </FloatLabel>
      </div>
      <div class="grid grid-cols-2 gap-2">
        <Button label="İptal" severity="secondary" @click="type_data.modal_status = false"></Button>
        <Button label="Kaydet" :disabled="type_data.loading" :loading="type_data.loading" @click="handleSubmit"></Button>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from "vue";
import { useFeatureStore } from "@/stores/general_store.js";
import { useDataStore } from "@/stores/data_store.js";
import { useDatatable } from "@/composables/index.js";
import { toast } from "vue-sonner";

const TypeDatatable = useDatatable({
  getUrl: "/api/v1/transit-declaration/document/type/list",
  deleteUrl: "/api/v1/transit-declaration/document/type/delete",
  perPage: 5,
});

const FeatureStore = useFeatureStore();
const DataStore = useDataStore();

const type_data = reactive({
  modal_status: false,
  loading: false,
  data: {
    name: "",
    status: 1,
  },
});
const onRowSelect = (e) => {
  let active_item_data = { ...e.data };
  type_data.data = active_item_data;
  type_data.modal_status = true;
};

const setCreate = () => {
  type_data.data = {
    name: "",
    status: 1,
  };
  type_data.modal_status = true;
};

const handleSubmit = async () => {
  type_data.loading = true;
  console.log(type_data.data);
  if (type_data.data.id) {
    await DataStore.UPDATE_TRANSIT_DECLARATION_DOCUMENT_TYPE({ data: type_data.data });
    toast.success("Transit beyannamesi dosya tipi başarıyla güncellendi.");
    TypeDatatable.refresh();
  } else {
    await DataStore.CREATE_TRANSIT_DECLARATION_DOCUMENT_TYPE({ data: type_data.data });
    toast.success("Transit beyannamesi dosya tipi başarıyla oluşturuldu.");
    TypeDatatable.fetchData({ page: 1 });
  }
  type_data.loading = false;
  type_data.modal_status = false;
};

const showModal = () => {
  TypeDatatable.refresh();
};
</script>
