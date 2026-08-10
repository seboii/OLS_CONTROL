<template>
  <div>
    <div class="text-nowrap">{{ data.load_content_count }} adet ürün</div>
    <button v-if="false" @click="products_dialog_visible = true" type="button" class="py-1.5 px-4 bg-white border shadow-xs rounded-xl w-fit text-sm"></button>
    <Dialog v-model:visible="products_dialog_visible" modal header=" " class="w-full lg:w-fit">
      <div>
        <div class="text-xl lg:text-3xl font-medium mb-1 text-gray-900">Ürünler</div>
        <div class="text-sm text-gray-500">{{ data.id }} numaralı teklifin ürün bilgileri aşağıda listelenmiştir.</div>
      </div>
      <div class="my-6">
        <DataTable :value="data.products" stripedRows class="lg:min-w-[64rem] table-nowrap">
          <Column field="quantity" header="Adet"></Column>
          <Column field="weight" header="Ağırlık"></Column>
          <Column field="width" header="Boy"></Column>
          <Column field="length" header="En"></Column>
          <Column field="height" header="Yükseklik"></Column>
          <Column field="height" header="Ürün Tipi">
            <template #body="{ data }">
              {{ data.product_type.name }}
            </template>
          </Column>
          <Column field="is_hoard" header="İstiflenebilir">
            <template #body="{ data }">
              {{ data.is_hoard ? "Evet" : "Hayır" }}
            </template>
          </Column>
        </DataTable>
      </div>
      <div class="flex justify-end gap-2">
        <Button type="button" label="Kapat" severity="secondary" @click="products_dialog_visible = false"></Button>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref } from "vue";
const props = defineProps(["data"]);
const products_dialog_visible = ref(false);
</script>
