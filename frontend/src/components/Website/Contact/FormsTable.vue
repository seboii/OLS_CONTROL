<template>
  <div>
    <Card>
      <template #content>
        <DatatableAjax :tableState="userTable" selectionMode="single" @rowSelect="onRowSelect" class="text-sm" noAction removableSort search>
          <Column header="Kullanıcı">
            <template #body="{ data }">
              <div class="font-medium text-nowrap py-2">{{ data.first_name }} {{ data.last_name }}</div>
            </template>
          </Column>
          <Column field="is_read" header="Tarih">
            <template #body="{ data }">
              <div class="font-medium text-nowrap">{{ new Date(data.created_at).toLocaleString() }}</div>
            </template>
          </Column>
          <Column header="Telefon">
            <template #body="{ data }">
              <div class="text-nowrap">{{ data.phone }}</div>
            </template>
          </Column>
          <Column field="email" header="E-Posta"></Column>
          <Column field="is_read" header="Okunma Durumu">
            <template #body="{ data }">
              <div class="flex items-center gap-2">
                <div
                  :class="{
                    'bg-green-300': data.is_read,
                    'bg-red-300': !data.is_read,
                  }"
                  class="size-2 rounded-full"
                ></div>
                <div class="font-medium text-nowrap">{{ data.is_read ? "Okundu" : "Okunmadı" }}</div>
              </div>
            </template>
          </Column>
          <Column field="is_answered" header="Yanıtlanma Durumu">
            <template #body="{ data }">
              <div class="flex items-center gap-2">
                <div
                  :class="{
                    'bg-green-300': data.is_answered,
                    'bg-red-300': !data.is_answered,
                  }"
                  class="size-2 rounded-full"
                ></div>
                <div class="font-medium text-nowrap">{{ data.is_answered ? "Yanıtlandı" : "Yanıtlanmadı" }}</div>
              </div>
            </template>
          </Column>
        </DatatableAjax>
      </template>
    </Card>
  </div>
</template>

<script setup>
import { ref, reactive, provide, onMounted } from "vue";
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

const emits = defineEmits(["setActiveCustomer"]);

const user_state = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
});

const userTable = useDatatable({
  getUrl: "/api/website/contact/form",
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
