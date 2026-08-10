<template>
  <div>
    <div class="flex justify-end mb-4">
      <Button @click="expedition_load_modal_status = true" label="Yeni Ekle" size="small" />
    </div>
    <div>
      <div>
        <div class="text-lg font-medium text-black">Yükler {{ expedition_load_data.length > 0 ? `(${expedition_load_data.length})` : "" }}</div>
      </div>
      <div v-if="expedition_load_loading" class="space-y-2 mt-4">
        <Skeleton v-for="i in 5" :key="i" class="w-full h-16!" />
      </div>
      <div v-else class="mt-4">
        <div v-if="expedition_load_data.length > 0">
          <div class="bg-gray-50 p-4 rounded-lg border mb-4">
            <div class="text-lg font-medium text-gray-800 mb-3">Toplam Değerler</div>
            <div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-3">
              <div class="bg-white p-3 rounded-md border">
                <div class="text-sm text-gray-500">Toplam Adet</div>
                <div class="text-lg font-semibold">{{ expedition_load_total_values.total_quantity }}</div>
              </div>
              <div class="bg-white p-3 rounded-md border">
                <div class="text-sm text-gray-500">Brüt Ağırlık</div>
                <div class="text-lg font-semibold">{{ expedition_load_total_values.total_gross_weight }} kg</div>
              </div>
              <div class="bg-white p-3 rounded-md border">
                <div class="text-sm text-gray-500">Net Ağırlık</div>
                <div class="text-lg font-semibold">{{ expedition_load_total_values.total_net_weight }} kg</div>
              </div>
              <div class="bg-white p-3 rounded-md border">
                <div class="text-sm text-gray-500">Lademetre</div>
                <div class="text-lg font-semibold">{{ expedition_load_total_values.total_lademeter }}</div>
              </div>
              <div class="bg-white p-3 rounded-md border">
                <div class="text-sm text-gray-500">Hacim</div>
                <div class="text-lg font-semibold">{{ expedition_load_total_values.total_volume }} m³</div>
              </div>
            </div>
          </div>
          <div class="space-y-4">
            <div v-for="item in expedition_load_data" :key="item.id" class="pt-4 px-5 border rounded-lg shadow-xs">
              <div class="w-full flex justify-between">
                <div>
                  <div class="space-y-1">
                    <div class="flex items-center gap-2">
                      <div class="text-sm text-gray-700">
                        Yük Numarası : <span class="font-medium">{{ item.load_transfer_id.load_number_work_type }}</span>
                      </div>
                      <a :href="`/panel/real-load?load_id=${item.load_transfer_id.id}`" target="_blank" v-tooltip.top="'Yük detayları'" class="cursor-pointer">
                        <LinkForwardIcon class="w-4 h-4 text-gray-700" />
                      </a>
                    </div>
                    <div class="flex items-center gap-2">
                      <div class="text-sm text-gray-700">
                        Müşteri : <span class="font-medium">{{ item.load_transfer_id.customer_id.name }}</span>
                      </div>
                    </div>
                    <div class="flex items-center gap-2">
                      <div target="_blank" class="block text-sm text-gray-700">
                        Römork : <span class="font-medium">{{ item.romork_id.plate_number }}</span>
                      </div>
                      <a :href="`/panel/car/${item.romork_id.id}`" target="_blank" v-tooltip.top="'Römork detayları'" class="cursor-pointer">
                        <LinkForwardIcon class="w-4 h-4 text-gray-700" />
                      </a>
                    </div>
                  </div>
                </div>
                <div>
                  <Button @click="deleteExpeditionLoad(item)" label="Sil" size="small" severity="danger" outlined />
                </div>
              </div>
              <div class="mt-4">
                <div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-2">
                  <div class="bg-gray-50 p-2 rounded-md">
                    <div class="text-xs text-gray-500">Toplam Adet</div>
                    <div class="text-sm font-semibold">{{ item.total_values.total_quantity }}</div>
                  </div>
                  <div class="bg-gray-50 p-2 rounded-md">
                    <div class="text-xs text-gray-500">Hacim</div>
                    <div class="text-sm font-semibold">{{ item.total_values.total_volume }} m³</div>
                  </div>
                  <div class="bg-gray-50 p-2 rounded-md">
                    <div class="text-xs text-gray-500">Brüt Ağırlık</div>
                    <div class="text-sm font-semibold">{{ item.total_values.total_gross_weight }} kg</div>
                  </div>
                  <div class="bg-gray-50 p-2 rounded-md">
                    <div class="text-xs text-gray-500">Net Ağırlık</div>
                    <div class="text-sm font-semibold">{{ item.total_values.total_net_weight }} kg</div>
                  </div>
                  <div class="bg-gray-50 p-2 rounded-md">
                    <div class="text-xs text-gray-500">Lademetre</div>
                    <div class="text-sm font-semibold">{{ item.total_values.total_lademeter }}</div>
                  </div>
                </div>
              </div>
              <div>
                <Accordion class="w-full">
                  <AccordionPanel value="0" class="border-b-0!">
                    <AccordionHeader class="px-0!">Detaylar</AccordionHeader>
                    <AccordionContent class="[&_.p-accordioncontent-content]:px-0!">
                      <div v-for="(item, index) in item.load_transfer_id.load_transfer_package" :key="index">
                        <div class="rounded-xl p-5 border bg-white shadow-xs flex items-start lg:items-end justify-between max-lg:flex-col gap-4">
                          <div class="w-full">
                            <div class="flex items-center gap-2 mb-3">
                              <div class="font-semibold">{{ index + 1 }}.Ürün</div>
                            </div>
                            <div class="flex flex-wrap gap-2 max-lg:flex-col">
                              <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                                Mal Cinsi : <span class="font-medium">{{ item.product_type_id?.name }}</span>
                              </div>
                              <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                                Adet : <span class="font-medium">{{ parseFloat(item.quantity) }}</span>
                              </div>
                              <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                                Hacim : <span class="font-medium">{{ item.volume }}m³</span>
                              </div>
                              <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                                Brüt Ağırlık : <span class="font-medium">{{ item.gross_weight }} kg</span>
                              </div>
                              <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                                Net Ağırlık : <span class="font-medium">{{ item.net_weight }} kg</span>
                              </div>
                              <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                                Lademetre : <span class="font-medium">{{ item.lademeter }}</span>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </AccordionContent>
                  </AccordionPanel>
                </Accordion>
              </div>
            </div>
          </div>
        </div>
        <div v-else class="flex justify-center items-center py-4">
          <div class="py-2 px-4 rounded-lg bg-gray-100 w-fit text-sm">Henüz yük eklenmemiş</div>
        </div>
      </div>
    </div>
    <Dialog
      v-model:visible="expedition_load_modal_status"
      modal
      :header="'Yükler'"
      class="w-full lg:max-w-(--breakpoint-2xl)"
      @show="onExpeditionLoadModalShow"
    >
      <div>
        <DatatableAjax :tableState="expeditionTable" selectionMode="single" @rowSelect="onRowSelect" removableSort search class="text-sm table-nowrap">
          <Column field="load_number_work_type" header="Yük Numarası" />
          <Column field="customer_id.name" header="Müşteri" class="text-nowrap" />
          <Column field="sender_id.name" header="Gönderici" />
          <Column field="load_status_id.name" header="Durum" />
        </DatatableAjax>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";
import { useDataStore } from "@/stores/data_store.js";
import { toast } from "vue-sonner";
import { useConfirm } from "primevue/useconfirm";
import { LinkForwardIcon } from "hugeicons-vue";

const props = defineProps(["expeditionId"]);

const DataStore = useDataStore();
const confirm = useConfirm();

const expedition_load_data = ref({});
const expedition_load_total_values = ref({});
const expedition_load_modal_status = ref(false);
const expedition_load_loading = ref(true);
const expedition_load_form_loading = ref(false);

const expeditionTable = useDatatable({
  getUrl: "/api/v1/expedition_load_mapping",
  deleteUrl: "/api/v1/expedition_load_mapping",
  perPage: 10,
});

const onRowSelect = async (e) => {
  const res = await DataStore.SAVE_EXPEDITION_LOAD({ data: { expedition_id: props.expeditionId, load_transfer_id: e.data.id } });
  if (res) {
    toast.success("Başarıyla eklendi");
    expedition_load_modal_status.value = false;
    getExpeditionLoadData();
  }
};

const handleExpeditionLoadForm = () => {
  console.log(expedition_load_data.value);
};

const onExpeditionLoadModalShow = () => {
  expeditionTable.refresh();
};

const getExpeditionLoadData = async () => {
  const res = await DataStore.GET_EXPEDITION_LOADS(props.expeditionId);
  if (res) {
    expedition_load_data.value = res.data;
    expedition_load_total_values.value = res.total_expedition_values;
  }
  expedition_load_loading.value = false;
};

const deleteExpeditionLoad = (item) => {
  confirm.require({
    message: `${item.load_transfer_id.load_number_work_type} numaralı yükü silmek istediğinize emin misiniz?`,
    header: "Uyarı",
    acceptProps: {
      severity: "danger",
      label: "Sil",
      size: "small",
    },
    rejectProps: {
      severity: "secondary",
      label: "İptal",
      size: "small",
    },
    accept: async () => {
      const res = await DataStore.DELETE_EXPEDITION_LOAD(item.id);
      if (res) {
        toast.success("Başarıyla silindi");
        getExpeditionLoadData();
      } else {
        toast.error("Bir hata oluştu");
      }
    },
  });
};

onMounted(async () => {
  getExpeditionLoadData();
});
</script>
