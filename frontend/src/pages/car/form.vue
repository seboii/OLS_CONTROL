<template>
  <div class="w-full lg:max-w-(--breakpoint-md) lg:mx-auto">
    <div>
      <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
        <div>
          <div class="mb-4 lg:mb-8">
            <RouterLink to="/panel/car">
              <Button label="Geri Dön" size="small" class="bg-white!" outlined raised />
            </RouterLink>
          </div>
          <h2 class="text-gray-800 font-medium text-4xl tracking-tight">
            {{ route.params.id ? "Araç Düzenleme" : "Yeni Araç Ekle" }}
          </h2>
          <div class="text-gray-500 text-sm mt-1">
            {{
              route.params.id
                ? "Araç bilgilerini düzenlemek için gerekli alanları eksiksiz doldurunuz."
                : "Yeni araç eklemek için gerekli alanları eksiksiz doldurunuz."
            }}
          </div>
        </div>
      </div>
      <div>
        <Card>
          <template #content>
            <div v-if="page_loading_status" class="space-y-4">
              <Skeleton class="h-14!" />
              <Skeleton class="h-14!" />
              <Skeleton class="h-14!" />
              <Skeleton class="h-14!" />
              <Skeleton class="h-14!" />
            </div>
            <div v-else>
              <div class="grid grid-cols-1 lg:grid-cols-12 gap-y-6 gap-x-4">
                <div class="lg:col-span-12">
                  <FloatLabel variant="on">
                    <InputText v-model="form_data.plate" fluid />
                    <label>Plaka</label>
                  </FloatLabel>
                  <div v-if="form_data.id" class="mt-2">
                    <div class="flex items-center gap-2">
                      <AlertCircleIcon size="16" class="text-red-500" />
                      <div class="text-xs text-gray-500">Kullanımda olan aracın plakasını değiştirilemez.</div>
                    </div>
                  </div>
                </div>
                <div class="lg:col-span-12">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="form_data.romork_type" :api="`/api/v1/romork_type`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label>İstenilen Römork Cinsi</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-12">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="form_data.customer" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label>Kiralanan Firma</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="form_data.car_type" :api="`/api/v1/car_type`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label>Araç Tipi</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="form_data.car_owner" :api="`/api/v1/car_owner`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label>Araç Sahibi</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="form_data.car_status" :api="`/api/v1/car_status`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label>Araç Durumu</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="form_data.width" min="0" fluid v-keyfilter.num />
                    <label> En (cm)</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="form_data.length" min="0" fluid v-keyfilter.num />
                    <label> Boy (cm)</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="form_data.height" min="0" fluid v-keyfilter.num />
                    <label> Yükseklik (cm)</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel variant="on">
                    <InputNumber v-model="form_data.km" :maxFractionDigits="2" fluid />
                    <label>Kilometre</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-6">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="form_data.capacity" fluid v-keyfilter.num />
                    <label>Kapasite</label>
                  </FloatLabel>
                </div>
              </div>
              <div class="flex justify-between mt-6">
                <div>
                  <Button
                    v-if="form_data.id"
                    size="small"
                    severity="danger"
                    @click="handleDelete"
                    :disabled="delete_loading_status"
                    :loading="delete_loading_status"
                    label="Kaydı Sil"
                  />
                </div>
                <Button @click="handleForm" :disabled="save_loading_status" :loading="save_loading_status" label="Kaydet" size="small" />
              </div>
            </div>
          </template>
        </Card>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from "vue";
import { toast } from "vue-sonner";
import { usePermissionStatus } from "@/composables/index.js";
import { useFeatureStore } from "@/stores/general_store.js";
import { useDataStore } from "@/stores/data_store.js";
import { useRoute, useRouter } from "vue-router";
import { AlertCircleIcon } from "hugeicons-vue";

const DataStore = useDataStore();
const FeatureStore = useFeatureStore();
const route = useRoute();
const router = useRouter();

const international_status_options = [
  { name: "Evet", value: 1 },
  { name: "Hayır", value: 0 },
];
const in_country_status_options = [
  { name: "Evet", value: 1 },
  { name: "Hayır", value: 0 },
];

const form_data = reactive({
  id: "",
  plate: "",
  car_type: "",
  romork_type: "",
  car_owner: "",
  car_status: "",
  customer: "",
  width: 0,
  length: 0,
  height: 0,
  km: 0,
  capacity: 0,
});
const page_loading_status = ref(route.params.id ? true : false);
const delete_loading_status = ref(false);
const save_loading_status = ref(false);

const handleForm = async () => {
  save_loading_status.value = true;
  try {
    const send_data = new FormData();

    if (form_data.id) {
      send_data.append("id", form_data.id);
    }
    send_data.append("plate_number", form_data.plate);
    send_data.append("car_type", form_data.car_type?.id);
    send_data.append("romork_type", form_data.romork_type?.id);
    send_data.append("vehicle_owner", form_data.car_owner?.id);
    send_data.append("vehicle_status", form_data.car_status?.id);
    send_data.append("customer_id", form_data.customer?.id);
    send_data.append("width", form_data.width);
    send_data.append("length", form_data.length);
    send_data.append("height", form_data.height);
    send_data.append("km", form_data.km);
    send_data.append("capacity", form_data.capacity);

    if (form_data.id) {
      const req_body = {};
      for (let [key, value] of send_data.entries()) {
        req_body[key] = value;
      }
      const res = await DataStore.UPDATE_CAR({
        data: req_body,
      });
      if (res) {
        toast.success("Araç güncellendi.");
      }
    } else {
      const res = await DataStore.CREATE_CAR({
        data: send_data,
      });
      if (res) {
        toast.success("Yeni araç oluşturuldu.");
        router.push("/panel/car");
      }
    }
  } catch (error) {
    console.log(error);
  }
  save_loading_status.value = false;
};

const handleDelete = async () => {
  delete_loading_status.value = true;
  try {
    const res = await DataStore.DELETE_CAR(form_data.id);
    if (res) {
      toast.success("Araç silindi.");
      router.push("/panel/car");
    }
  } catch (error) {
    console.log(error);
  }
  delete_loading_status.value = false;
};

onMounted(async () => {
  if (route.params.id) {
    console.log(route.params);
    page_loading_status.value = true;
    const res = await DataStore.GET_CAR(route.params.id);
    if (res) {
      const data = res.data;
      form_data.id = data.id;
      form_data.plate = data.plate_number;
      form_data.car_type = data.car_type;
      form_data.romork_type = data.romork_type;
      form_data.car_owner = data.vehicle_owner;
      form_data.car_status = data.vehicle_status;
      form_data.customer = data.customer;
      form_data.width = data.width;
      form_data.length = data.length;
      form_data.height = data.height;
      form_data.km = data.km;
      form_data.capacity = data.capacity;
    }
    page_loading_status.value = false;
  }
});
</script>
