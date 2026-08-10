<template>
  <div>
    <Card>
      <template #content>
        <DatatableAjax :tableState="userTable" selectionMode="single" @rowSelect="onRowSelect" class="text-sm" removableSort search>
          <Column header="Kullanıcı">
            <template #body="{ data }">
              <div class="flex items-center gap-3">
                <div class="bg-gray-100 size-8 lg:size-10 rounded-lg overflow-hidden shrink-0 border">
                  <img v-if="data.avatar" :src="`/storage/${data.avatar}`" class="w-full h-full object-cover" />
                </div>
                <div v-if="false" class="bg-gray-100 size-10 lg:size-12 rounded-lg overflow-hidden shrink-0 border flex items-center justify-center">
                  <img v-if="data?.avatar" :src="`/storage/${data?.avatar}`" class="w-full h-full object-cover" />
                  <span v-else class="text-lg text-gray-400">{{ data?.name?.charAt(0).toUpperCase() }}</span>
                </div>
                <div>
                  <div class="text-nowrap">{{ data.name }} {{ data.surname }}</div>
                  <div class="text-xs text-gray-500 text-nowrap">{{ data.title }}</div>
                </div>
              </div>
            </template>
          </Column>
          <Column header="Telefon">
            <template #body="{ data }">
              <div class="text-nowrap">{{ data.phone }}</div>
            </template>
          </Column>
          <Column field="email" header="E-Posta"></Column>
        </DatatableAjax>
      </template>
    </Card>
  </div>
</template>

<script setup>
import { ref, reactive, provide, onMounted, defineComponent } from "vue";
import {
  LinkSquare02Icon,
  ImageAdd02Icon,
  TruckDeliveryIcon,
  Share01Icon,
  Pdf01Icon,
  Download04Icon,
  CargoShipIcon,
  UserAdd01Icon,
  UserIdVerificationIcon,
  UserRemove01Icon,
  UserEdit01Icon,
} from "hugeicons-vue";
import { useDatatable } from "@/composables/index.js";

const todos = ref([
  { id: 1, name: "deneme" },
  { id: 2, name: "deneme" },
  { id: 3, name: "deneme" },
  { id: 4, name: "deneme" },
  { id: 5, name: "deneme" },
]);

const emits = defineEmits(["setActiveCustomer"]);

const user_state = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
});

const userTable = useDatatable({
  getUrl: "/api/v1/user",
  deleteUrl: "/api/v1/user",
  perPage: 10,
});

const onRowSelect = async (e) => {
  emits("setActiveCustomer", e.data);
};

defineExpose({
  refresh: userTable.refresh,
});
onMounted(() => {
  userTable.refresh();
});
</script>
