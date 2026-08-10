<template>
  <Card>
    <template #content>
      <DatatableAjax :tableState="customerTable" class="text-sm" selectionMode="single" @rowSelect="onRowSelect" removableSort search>
        <Column field="name" sortable header="Adı">
          <template #body="{ data }">
            <div class="flex items-center gap-3">
              <div class="bg-gray-100 size-10 lg:size-12 rounded-lg overflow-hidden shrink-0 border flex items-center justify-center">
                <img v-if="data?.avatar" :src="`/storage/${data?.avatar}`" class="w-full h-full object-cover" />
                <span v-else class="text-lg text-gray-400">{{ data?.name?.charAt(0).toUpperCase() }}</span>
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <div class="max-lg:min-w-64 font-medium">{{ data?.name }}</div>
                  <div
                    v-if="data?.country_id && false"
                    class="w-5 aspect-4/3 rounded-sm overflow-hidden shrink-0"
                    v-tooltip="{ content: data?.country_id?.name, delay: 0 }"
                  >
                    <img :src="`https://flagcdn.com/w320/${data?.country_id?.country_code?.toLowerCase()}.png`" class="w-full h-full object-cover" />
                  </div>
                </div>
                <div v-if="data?.country_id" class="text-xs text-gray-500">{{ data?.country_id?.name }}</div>
                <div v-if="false && data?.country_id" class="flex items-center gap-1.5 mt-1">
                  <div v-if="data?.country_id" class="w-4 aspect-square rounded-full overflow-hidden shrink-0">
                    <img :src="`https://flagcdn.com/w320/${data?.country_id?.country_code?.toLowerCase()}.png`" class="w-full h-full object-cover" />
                  </div>
                  <div class="text-center text-xs text-nowrap">
                    {{ data?.country_id?.name }}
                  </div>
                </div>
              </div>
            </div>
          </template>
        </Column>
        <Column header="Telefon">
          <template #body="{ data }">
            <div v-if="data?.phone && data?.phone.length > 2" class="flex items-center gap-2">
              <div v-if="data?.phone_country_id && false" class="w-6 aspect-4/3 rounded-sm overflow-hidden">
                <img :src="`https://flagcdn.com/w320/${data?.phone_country_id?.country_code?.toLowerCase()}.png`" class="w-full h-full object-cover" />
              </div>
              <div v-if="data?.phone" class="text-center text-nowrap">
                <span v-if="data?.phone_country_id?.phone_code">+{{ data?.phone_country_id?.phone_code }}</span>
                <span>{{ data?.phone }}</span>
              </div>
            </div>
          </template>
        </Column>
        <Column field="email" header="E-Posta"></Column>
      </DatatableAjax>
    </template>
  </Card>
</template>

<script setup>
import { ref, reactive, provide, onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";

const props = defineProps(["typeId"]);
const emits = defineEmits(["setActiveCustomer"]);
const account_type_list = [
  { id: 1, name: "Müşteri" },
  { id: 2, name: "Tedarikçi" },
  { id: 3, name: "Alıcı" },
  { id: 4, name: "Gönderici" },
  { id: 5, name: "Acente" },
];

const customer_form_data = reactive({
  drawer_visible: false,
  form_type: "create",
});
const active_customer = ref(null);

const customerTable = useDatatable({
  getUrl: "/api/v1/account",
  defaultParams: {
    account_type_id: props.typeId ? account_type_list.find((item) => item.id == props.typeId).id : "",
  },
  deleteUrl: "/api/v1/account",
  perPage: 10,
});

const onRowSelect = (e) => {
  emits("setActiveCustomer", e.data);
};

provide("page_provide", {
  customer_form_data: customer_form_data,
});

defineExpose({
  refresh: customerTable.refresh,
});

onMounted(() => {
  customerTable.refresh();
});
</script>
