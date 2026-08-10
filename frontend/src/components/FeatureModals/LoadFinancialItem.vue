<template>
  <div>
    <Dialog v-model:visible="FeatureStore.load_financial_items_modal_status" modal header="Yük Finansal Ürünleri" class="w-full lg:w-[900px]" @show="showModal">
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
          search
        >
          <Column field="name" header="Adı"></Column>
          <Column field="type" header="Tür">
            <template #body="slotProps">
              <div class="flex items-center gap-2">
                <div
                  :class="{
                    'bg-green-300': slotProps.data.type == 1,
                    'bg-red-300': slotProps.data.type == 2,
                  }"
                  class="size-2 rounded-full"
                ></div>
                <div>{{ getTypeName(slotProps.data.type) }}</div>
              </div>
            </template>
          </Column>
          <Column :rowEditor="true" style="width: 10%; min-width: 8rem" bodyStyle="text-align:center"></Column>
        </DatatableAjax>
      </div>
    </Dialog>
    <Dialog
      v-model:visible="type_data.modal_status"
      modal
      :header="type_data.data.id ? 'Yük Finansal Ürünü Düzenle' : 'Yük Finansal Ürünü Oluştur'"
      class="w-full lg:w-[500px]"
    >
      <div class="mt-8 mb-12 space-y-4">
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="type_data.data.name" fluid />
          <label>Adı </label>
        </FloatLabel>
        <div>
          <SelectButton
            v-model="type_data.data.type"
            :options="financial_item_type_options"
            optionLabel="name"
            optionValue="value"
            class="w-full [&>.p-togglebutton]:w-full"
          />
        </div>
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

const financial_item_type_options = [
  { name: "Alış", value: 1 },
  { name: "Satış", value: 2 },
];

const TypeDatatable = useDatatable({
  getUrl: "/api/v1/financial_item",
  deleteUrl: "/api/v1/financial_item",
  perPage: 5,
});

const FeatureStore = useFeatureStore();
const DataStore = useDataStore();

const type_data = reactive({
  modal_status: false,
  loading: false,
  data: {
    name: "",
    type: 1,
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
    type: 1,
  };
  type_data.modal_status = true;
};

const handleSubmit = async () => {
  type_data.loading = true;
  if (type_data.data.id) {
    await DataStore.UPDATE_LOAD_FINANCIAL_ITEM({ data: type_data.data });
    toast.success("Yük finansal ürünü başarıyla güncellendi.");
    TypeDatatable.refresh();
  } else {
    await DataStore.CREATE_LOAD_FINANCIAL_ITEM({ data: type_data.data });
    toast.success("Yük finansal ürünü başarıyla oluşturuldu.");
    TypeDatatable.fetchData({ page: 1 });
  }
  type_data.loading = false;
  type_data.modal_status = false;
};

const showModal = () => {
  TypeDatatable.clear();
  TypeDatatable.refresh();
};

const getTypeName = (type) => {
  switch (type) {
    case 1:
      return "Alış";
    case 2:
      return "Satış";
    default:
      return "";
  }
};
</script>
