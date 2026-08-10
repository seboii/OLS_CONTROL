<template>
  <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
    <div class="mb-4 lg:mb-8 flex justify-between items-end gap-6">
      <div>
        <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Araçlar</h2>
        <div class="text-gray-500 text-sm mt-1">Araçları listeleyebilir ve yeni araç oluşturabilirsiniz.</div>
      </div>
      <div>
        <RouterLink to="/panel/car/form">
          <Button label="Yeni Araç Ekle" size="small" />
        </RouterLink>
      </div>
    </div>
    <Card>
      <template #content>
        <DatatableAjax :tableState="carTable" selectionMode="single" @rowSelect="redirectForm" removableSort search class="text-sm table-nowrap">
          <Column field="yuk_no" header="Plaka" class="text-nowrap">
            <template #body="{ data }">
              <RouterLink :to="`/panel/car/${data.id}`">{{ data.plate_number }}</RouterLink>
            </template>
          </Column>
          <Column field="car_type.name" header="Tipi" />
          <Column field="vehicle_status.name" header="Araç Durumu" />
          <Column field="vehicle_owner.name" header="Sahiplik Durumu" />
          <Column field="km" header="Kilometre" />
        </DatatableAjax>
      </template>
    </Card>
  </div>
</template>

<script setup>
import { onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";
import { useRouter } from "vue-router";

const Router = useRouter();
const carTable = useDatatable({
  getUrl: "/api/v1/car",
  deleteUrl: "/api/v1/car",
  perPage: 10,
});

const redirectForm = ({ data }) => {
  Router.push(`/panel/car/${data.id}`);
};

onMounted(async () => {
  carTable.refresh();
});
</script>
