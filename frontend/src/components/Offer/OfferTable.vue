<template>
  <DatatableAjax :tableState="loadTable" selectionMode="single" @rowSelect="onRowSelect" size="large" class="table-nowrap text-sm" removableSort search>
    <Column v-if="false" header="">
      <template #body="{ data }">
        <div class="flex items-center justify-center">
          <ArtificialIntelligence04Icon v-if="data.mail_id" class="text-primary" v-tooltip="{ content: `Yapay zeka ile oluşturuldu.`, delay: 0 }" />
        </div>
      </template>
    </Column>
    <Column header="Yük No">
      <template #body="{ data }">
        <div class="flex items-center gap-1.5">
          <p class="max-lg:text-nowrap">{{ data.load_number }}</p>
        </div>
      </template>
    </Column>
    <Column header="Rezervasyon No">
      <template #body="{ data }">
        <div class="flex items-center gap-1.5">
          <p class="max-lg:text-nowrap">{{ data.reservation_number }}</p>
        </div>
      </template>
    </Column>
    <Column header="Yük Tipi">
      <template #body="{ data }">
        <div class="flex items-center gap-1.5">
          <p class="max-lg:text-nowrap">{{ data.loading_type_id?.name }}</p>
        </div>
      </template>
    </Column>
    <Column header="Yük Türü">
      <template #body="{ data }">
        <div class="flex items-center gap-1.5">
          <p class="max-lg:text-nowrap">
            {{ data.work_type_id?.name }}
          </p>
        </div>
      </template>
    </Column>
    <Column header="Müşteri">
      <template #body="{ data }">
        <div class="flex gap-3">
          <div v-if="data.customer_id?.avatar && false" class="bg-white size-10 rounded-lg overflow-hidden shrink-0 border">
            <img :src="`/storage/${data.customer_id.avatar}`" class="w-full h-full object-contain" />
          </div>
          <div>
            <div class="font-medium text-wrap max-w-64 max-lg:min-w-64 line-clamp-1" :title="`${data.customer_id?.name}`">
              {{ data.customer_id?.name }}
            </div>
            <div v-if="data.customer_id?.country_id" class="text-gray-600 text-xs flex items-center gap-2 mt-1">
              <div v-if="false" class="w-5 aspect-4/3 rounded-sm overflow-hidden">
                <img :src="`https://flagcdn.com/w160/${data.customer_id?.country_id?.country_code.toLowerCase()}.png`" class="w-full h-full object-cover" />
              </div>
              <div>{{ data.customer_id?.country_id?.name }}</div>
            </div>
          </div>
        </div>
      </template>
    </Column>
    <Column header="Ürünler">
      <template #body="{ data }">
        <OfferListProductsDialog :data="data" />
      </template>
    </Column>
    <Column v-if="false" header="Fiyat">
      <template #body="{ data }">
        <div>{{ new Intl.NumberFormat("tr-TR").format(data.price.value) }} {{ data.price.currency }}</div>
      </template>
    </Column>
    <Column header="Görevli">
      <template #body="{ data }">
        <OfferListChargePersonsDialog :data="data" />
        <div>{{ data.employee }}</div>
      </template>
    </Column>
    <Column header="Teklif Tarihi">
      <template #body="{ data }">
        <div class="text-nowrap">{{ new Date(data.offer_date).toLocaleDateString("tr-TR", { day: "numeric", month: "long", year: "numeric" }) }}</div>
      </template>
    </Column>
  </DatatableAjax>
</template>

<script setup>
import { onMounted } from "vue";
import { useDatatable } from "@/composables/index.js";
import OfferListProductsDialog from "@/components/Offer/OfferListProductsDialog.vue";
import OfferListChargePersonsDialog from "@/components/Offer/OfferListChargePersonsDialog.vue";
import { ArtificialIntelligence04Icon } from "hugeicons-vue";

const props = defineProps(["statusType", "timeout"]);
const emits = defineEmits("onRowSelect");

let params = {};
if (props.timeout) {
  params = {
    timeout: true,
  };
} else {
  params = {
    status_type_id: props.statusType,
  };
}
const loadTable = useDatatable({
  getUrl: "/api/v1/load",
  defaultParams: params,
  deleteUrl: "/api/v1/load",
  perPage: 10,
});

const onRowSelect = (data) => {
  emits("onRowSelect", data);
};

defineExpose({
  refresh: loadTable.refresh,
});

onMounted(() => {
  loadTable.refresh();
});
</script>
