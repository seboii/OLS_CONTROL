<template>
  <Drawer v-model:visible="drawer_visible" header=" " position="bottom" class="min-h-svh" @show="onShowDrawer" @hide="onHideDrawer">
    <div v-if="page_loading" class="flex items-center justify-center max-lg:py-20 lg:min-h-full">
      <ProgressSpinner class="size-20!" />
    </div>
    <div v-else class="lg:mx-auto lg:max-w-(--breakpoint-md)">
      <div>
        <div>
          <div class="mb-12">
            <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">{{ form_data.id ? "Sefer Bilgileri" : "Yeni Sefer Oluştur" }}</div>
            <p class="text-sm text-gray-500">Sefer bilgilerini eksiksiz doldurunuz.</p>
          </div>
          <Tabs value="0" scrollable lazy>
            <TabList v-if="form_data.id">
              <Tab value="0">Bilgiler</Tab>
              <Tab v-if="form_data.id" value="1">Sefer İçeriği</Tab>
              <Tab v-if="form_data.id" value="2">Hareketler</Tab>
            </TabList>
            <TabPanels class="py-6! px-0!">
              <TabPanel value="0">
                <div>
                  <div class="grid grid-cols-1 lg:grid-cols-12 gap-y-6 gap-x-4">
                    <div v-if="form_data.id" class="lg:col-span-12">
                      <div class="text-sm text-gray-500">Sefer Numarası</div>
                      <div class="text-gray-800 font-medium text-lg">{{ form_data.expedition_number }}</div>
                    </div>
                    <hr v-if="form_data.id" class="lg:col-span-12" />
                    <div :class="form_data.id ? 'lg:col-span-12' : 'lg:col-span-6'">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.romork" :api="`/api/v1/car`" optionLabel="plate_number" class="w-full" filter>
                          <template v-if="usePermissionStatus('load_transfer_type_management').read" #footer>
                            <div class="p-2">
                              <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label>Römork</label>
                      </FloatLabel>
                    </div>
                    <div v-if="form_data.id" class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.expedition_status" :api="`/api/v1/expedition_status`" optionLabel="name" class="w-full" filter>
                          <template v-if="usePermissionStatus('load_transfer_type_management').read" #footer>
                            <div class="p-2">
                              <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label>Sefer Durumu</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.expedition_type" :api="`/api/v1/expedition_type`" optionLabel="name" class="w-full" filter>
                          <template v-if="usePermissionStatus('load_transfer_type_management').read" #footer>
                            <div class="p-2">
                              <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label>Sefer Tipi</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.work_type" :api="`/api/v1/work_type`" optionLabel="name" class="w-full" filter>
                          <template v-if="usePermissionStatus('load_transfer_type_management').read" #footer>
                            <div class="p-2">
                              <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label>Çalışma Tipi</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.department" :api="`/api/v1/department`" optionLabel="name" class="w-full" filter>
                          <template v-if="usePermissionStatus('load_transfer_type_management').read" #footer>
                            <div class="p-2">
                              <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label>Departman</label>
                      </FloatLabel>
                    </div>
                    <div v-if="form_data.id" class="lg:col-span-12">
                      <FloatLabel variant="on">
                        <DatePicker v-model="form_data.car_exit_date" class="w-full" />
                        <label>Araç Çıkış Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-12">
                      <div class="font-medium mb-4">Başlangıç</div>
                      <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
                        <div>
                          <FloatLabel variant="on">
                            <DatePicker v-model="form_data.release_date" class="w-full" />
                            <label>Başlangıç Tarihi</label>
                          </FloatLabel>
                        </div>
                        <div>
                          <FloatLabel variant="on">
                            <SelectAjax v-model="form_data.start_city" :api="`/api/v1/city`" optionLabel="name" class="w-full" filter />
                            <label for="username">Başlangıç Şehri</label>
                          </FloatLabel>
                        </div>
                      </div>
                    </div>
                    <div class="lg:col-span-12">
                      <div class="font-medium mb-4">Yüklenme</div>
                      <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
                        <div>
                          <FloatLabel variant="on">
                            <DatePicker v-model="form_data.loading_date" class="w-full" />
                            <label>Yüklenme Tarihi</label>
                          </FloatLabel>
                        </div>
                        <div>
                          <FloatLabel variant="on">
                            <SelectAjax v-model="form_data.load_city" :api="`/api/v1/city`" optionLabel="name" class="w-full" filter />
                            <label for="username">Yüklenme Şehri</label>
                          </FloatLabel>
                        </div>
                      </div>
                    </div>
                    <div class="lg:col-span-12">
                      <div class="font-medium mb-4">Bitiş</div>
                      <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
                        <div>
                          <FloatLabel variant="on">
                            <DatePicker v-model="form_data.return_date" class="w-full" />
                            <label>Bitiş Tarihi</label>
                          </FloatLabel>
                        </div>
                        <div>
                          <FloatLabel variant="on">
                            <SelectAjax v-model="form_data.end_city" :api="`/api/v1/city`" optionLabel="name" class="w-full" filter />
                            <label for="username">Bitiş Şehri</label>
                          </FloatLabel>
                        </div>
                      </div>
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
              </TabPanel>
              <TabPanel value="1">
                <ExpeditionLoad :expedition-id="form_data.id" />
              </TabPanel>
              <TabPanel value="2">
                <ExpeditionFormMovements :expedition-id="form_data.id" />
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>
      </div>
    </div>
  </Drawer>
</template>

<script setup>
import { watch, ref, reactive, onMounted } from "vue";
import { toast } from "vue-sonner";
import { usePermissionStatus, useGetIsoDate, useDatatable } from "@/composables/index.js";
import { useFeatureStore } from "@/stores/general_store.js";
import { useDataStore } from "@/stores/data_store.js";
import { useRoute, useRouter } from "vue-router";
import ExpeditionLoad from "@/components/Expedition/ExpeditionLoad.vue";
import ExpeditionFormMovements from "@/components/Expedition/ExpeditionFormMovements.vue";

const Route = useRoute();
const Router = useRouter();
const emits = defineEmits(["hide"]);

const DataStore = useDataStore();
const FeatureStore = useFeatureStore();

const general_data = defineModel("general-data");
const drawer_visible = defineModel("visible");
const expedition_id = defineModel("expedition-id");

const form_data = reactive({
  id: "",
  expedition_id: "",
  expedition_number: "",
  expedition_type: "",
  sefer_id: "",
  department: "",
  expedition_status: "",
  work_type: "",
  romork: "",
  start_city: "",
  load_city: "",
  end_city: "",
  loading_date: "",
  release_date: "",
  return_date: "",
  car_exit_date: "",
});
const page_loading = ref(false);
const delete_loading_status = ref(false);
const save_loading_status = ref(false);

const handleForm = async () => {
  if (!form_data.expedition_type || !form_data.department || !form_data.work_type || !form_data.romork) {
    toast.error("Lütfen tüm alanları doldurunuz.");
    return;
  }
  save_loading_status.value = true;
  try {
    const send_data = new FormData();

    if (form_data.id) {
      send_data.append("id", form_data.id);
    }
    send_data.append("expedition_type", form_data.expedition_type?.id);
    send_data.append("department_id", form_data.department?.id);
    if (form_data.id) {
      send_data.append("expedition_status_id", form_data.expedition_status?.id);
      send_data.append("car_exit_date", form_data.car_exit_date ? useGetIsoDate(form_data.car_exit_date) : "");
    }
    send_data.append("work_type", form_data.work_type?.id);
    send_data.append("romork_id", form_data.romork?.id);

    send_data.append("loading_date", form_data.loading_date ? useGetIsoDate(form_data.loading_date) : "");
    send_data.append("release_date", form_data.release_date ? useGetIsoDate(form_data.release_date) : "");
    send_data.append("return_date", form_data.return_date ? useGetIsoDate(form_data.return_date) : "");
    send_data.append("load_city_id", form_data.load_city ? form_data.load_city?.id : "");
    send_data.append("end_city_id", form_data.end_city ? form_data.end_city?.id : "");
    send_data.append("start_city_id", form_data.start_city ? form_data.start_city?.id : "");

    if (form_data.id) {
      const req_body = {};
      for (let [key, value] of send_data.entries()) {
        req_body[key] = value;
      }
      const res = await DataStore.UPDATE_EXPEDITION({
        data: req_body,
      });
      if (res) {
        toast.success("Sefer güncellendi.");
      }
    } else {
      const res = await DataStore.CREATE_EXPEDITION({
        data: send_data,
      });
      if (res) {
        toast.success("Yeni sefer oluşturuldu.");
        drawer_visible.value = false;
      }
    }
  } catch (error) {
    console.log(error);
    toast.error("Bir hata oluştu.");
  }
  save_loading_status.value = false;
};

const handleDelete = async () => {
  delete_loading_status.value = true;
  try {
    const res = await DataStore.DELETE_EXPEDITION(form_data.id);
    if (res) {
      toast.success("Sefer silindi.");
      drawer_visible.value = false;
    }
  } catch (error) {
    console.log(error);
  }
  delete_loading_status.value = false;
};

const getExpedition = async () => {
  const res = await DataStore.GET_EXPEDITION(expedition_id.value);
  if (res) {
    const data = res.data;
    form_data.id = data.id;
    form_data.expedition_id = data.expedition_id;
    form_data.expedition_number = data.expedition_number;
    form_data.expedition_type = data.expedition_type_id;
    form_data.sefer_id = data.sefer_id;
    form_data.department = data.department_id;
    form_data.expedition_status = data.status_id;
    form_data.work_type = data.work_type;
    form_data.romork = data.romork_id;
    form_data.start_city = data.start_city_id;
    form_data.end_city = data.end_city_id;
    form_data.load_city = data.load_city_id;

    if (res.data.car_exit_date) {
      form_data.car_exit_date = new Date(res.data.car_exit_date);
    }
    if (data.loading_date) {
      form_data.loading_date = new Date(data.loading_date);
    }
    if (data.release_date) {
      form_data.release_date = new Date(data.release_date);
    }
    if (data.return_date) {
      form_data.return_date = new Date(data.return_date);
    }
  }
};

const onShowDrawer = async (event) => {
  if (expedition_id.value) {
    Router.push({
      query: {
        ...Route.query,
        "expedition-id": expedition_id.value,
      },
    });
    page_loading.value = true;
    await getExpedition();
    page_loading.value = false;
  }

  if (general_data.value?.work_type) {
    const res = await DataStore.GET_WORK_TYPE(general_data.value.work_type);
    if (res) {
      form_data.work_type = res.data;
    }
  }
};
const onHideDrawer = () => {
  let new_query = { ...Route.query };
  delete new_query["expedition-id"];
  Router.push({
    query: new_query,
  });
  expedition_id.value = null;

  form_data.id = "";
  form_data.expedition_id = "";
  form_data.expedition_number = "";
  form_data.expedition_type = "";
  form_data.sefer_id = "";
  form_data.department = "";
  form_data.expedition_status = "";
  form_data.work_type = "";
  form_data.romork = "";
  form_data.start_city = "";
  form_data.load_city = "";
  form_data.end_city = "";
  form_data.loading_date = "";
  form_data.release_date = "";
  form_data.return_date = "";
  form_data.car_exit_date = "";
  emits("hide");
};

watch(
  () => Route.query["expedition-id"],
  (newVal) => {
    if (!newVal) {
      expedition_id.value = null;
      drawer_visible.value = false;
    }
  }
);

onMounted(() => {
  if (Route.query["expedition-id"]) {
    expedition_id.value = Route.query["expedition-id"];
    drawer_visible.value = true;
  }
});
</script>
