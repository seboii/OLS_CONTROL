<template>
  <div>
    <Dialog v-model:visible="FeatureStore.destinations_modal_status" modal header="Konumlar" class="w-full lg:w-[800px]" @show="showModal">
      <div class="flex justify-end">
        <Button type="button" label="Yeni Ekle" size="small" @click="setCreate"></Button>
      </div>
      <div class="mt-6">
        <DatatableAjax
          :tableState="DestinationDatatable"
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
            <template #body="{ data }">
              <div class="flex items-center gap-2">
                <div :class="data.status ? 'bg-green-500' : 'bg-red-500'" class="w-2 h-2 p-0 rounded-full" />
                <span>{{ data.name }}</span>
              </div>
            </template>
          </Column>
          <Column field="country.name" header="Ülke"></Column>
          <Column field="city.name" header="Şehir"></Column>
          <Column field="district.name" header="İlçe"></Column>
          <Column :rowEditor="true" style="width: 10%; min-width: 8rem" bodyStyle="text-align:center"></Column>
        </DatatableAjax>
      </div>
    </Dialog>
    <Dialog
      v-model:visible="destination_data.modal_status"
      modal
      :header="destination_data.data.id ? 'Konum Düzenle' : 'Konum Oluştur'"
      class="w-full lg:w-[500px]"
    >
      <div class="my-4 space-y-4">
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="destination_data.data.name" fluid />
          <label>Adı</label>
        </FloatLabel>

        <FloatLabel class="w-full" variant="on">
          <SelectAjax v-model="destination_data.data.country" :api="`/api/v1/country`" optionLabel="name" class="w-full" filter @change="onCountryChange" />
          <label>Ülke</label>
        </FloatLabel>

        <FloatLabel class="w-full" variant="on">
          <SelectAjax
            v-model="destination_data.data.city"
            :api="`/api/v1/city`"
            :fetchParams="{
              country_id: destination_data.data.country ? destination_data.data.country.id : null,
            }"
            :disabled="!destination_data.data.country"
            optionLabel="name"
            class="w-full"
            filter
            @change="onCityChange"
          />
          <label>Şehir</label>
        </FloatLabel>

        <FloatLabel class="w-full" variant="on">
          <SelectAjax
            v-model="destination_data.data.district"
            :api="`/api/v1/district`"
            :fetchParams="{
              city_id: destination_data.data.city ? destination_data.data.city.id : null,
            }"
            :disabled="!destination_data.data.city"
            optionLabel="name"
            class="w-full"
            filter
          />
          <label>İlçe</label>
        </FloatLabel>

        <FloatLabel class="w-full" variant="on">
          <Textarea v-model="destination_data.data.address" autoResize class="w-full" />
          <label>Adres</label>
        </FloatLabel>

        <div
          @click="destination_data.data.status = !destination_data.data.status"
          :class="{
            'border-primary-500': destination_data.data.status,
          }"
          class="py-3 px-5 rounded-lg bg-white shadow-xs border flex items-center gap-1"
        >
          <Checkbox @click.stop v-model="destination_data.data.status" :binary="true" />
          <label class="ml-2">Aktif</label>
        </div>
      </div>
      <div class="grid grid-cols-2 gap-2">
        <Button label="İptal" severity="secondary" @click="destination_data.modal_status = false"></Button>
        <Button label="Kaydet" :disabled="destination_data.loading" :loading="destination_data.loading" @click="handleSubmit"></Button>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from "vue";
import { useFeatureStore } from "@/stores/general_store.js";
import { useDataStore } from "@/stores/data_store.js";
import { useDatatable } from "@/composables/index.js";
import { toast } from "vue-sonner";

const DestinationDatatable = useDatatable({
  getUrl: "/api/v1/destination",
  deleteUrl: "/api/v1/destination",
  perPage: 5,
});

const FeatureStore = useFeatureStore();
const DataStore = useDataStore();

const destination_data = reactive({
  modal_status: false,
  loading: false,
  data: {
    name: "",
    country: null,
    city: null,
    district: null,
    address: "",
    status: true,
  },
});

const onRowSelect = (e) => {
  let active_item_data = { ...e.data };

  // Obje referanslarını düzgün şekilde ayarla
  destination_data.data = {
    id: active_item_data.id,
    name: active_item_data.name,
    country: active_item_data.country,
    city: active_item_data.city,
    district: active_item_data.district,
    address: active_item_data.address,
    status: active_item_data.status,
  };

  destination_data.modal_status = true;
};

const setCreate = () => {
  destination_data.data = {
    name: "",
    country: null,
    city: null,
    district: null,
    address: "",
    status: true,
  };
  destination_data.modal_status = true;
};

const handleSubmit = async () => {
  if (!destination_data.data.name) {
    toast.error("Lütfen varış noktası adını giriniz.");
    return;
  }

  if (!destination_data.data.country) {
    toast.error("Lütfen ülke seçiniz.");
    return;
  }

  if (!destination_data.data.city) {
    toast.error("Lütfen şehir seçiniz.");
    return;
  }

  // API'ye gönderilecek veriyi hazırla
  const submitData = {
    id: destination_data.data.id,
    name: destination_data.data.name,
    country_id: destination_data.data.country.id,
    city_id: destination_data.data.city.id,
    district_id: destination_data.data.district ? destination_data.data.district.id : null,
    address: destination_data.data.address,
    status: destination_data.data.status,
  };

  destination_data.loading = true;
  if (destination_data.data.id) {
    await DataStore.UPDATE_DESTINATION({ data: submitData });
    toast.success("Varış noktası başarıyla güncellendi.");
    DestinationDatatable.refresh();
  } else {
    await DataStore.CREATE_DESTINATION({ data: submitData });
    toast.success("Varış noktası başarıyla oluşturuldu.");
    DestinationDatatable.fetchData({ page: 1 });
  }
  destination_data.loading = false;
  destination_data.modal_status = false;
};

const showModal = async () => {
  DestinationDatatable.refresh();
};

const onCountryChange = () => {
  destination_data.data.city = null;
  destination_data.data.district = null;
};

const onCityChange = () => {
  destination_data.data.district = null;
};

onMounted(async () => {
  // SelectAjax bileşeni otomatik olarak verileri yükleyecek
});
</script>
