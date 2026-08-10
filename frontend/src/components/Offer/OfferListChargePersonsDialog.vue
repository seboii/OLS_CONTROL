<template>
  <div>
    <Button
      :label="data.load_charge_person.length > 0 ? `${data.load_charge_person.length} Görevli` : 'Belirtilmedi'"
      @click="products_dialog_visible = true"
      class="text-nowrap"
      size="small"
      raised
      outlined
      rounded
    />
    <Dialog v-model:visible="products_dialog_visible" modal header="Görevliler" class="w-full lg:w-fit">
      <div class="my-6">
        <DataTable :value="data.load_charge_person" class="lg:min-w-[64rem] table-nowrap text-gray-300! text-sm!">
          <Column field="user_id" header="Görevli">
            <template #body="{ data }"> {{ data.user_id.name }} {{ data.user_id.surname }} </template></Column
          >
          <Column field="user_id" header="Görevi">
            <template #body="{ data }">
              {{ data.user_type == 1 ? "Operasyon Yetkilisi" : "Satış Temsilcisi" }}
            </template></Column
          >
        </DataTable>
      </div>
      <div class="flex justify-end gap-2">
        <Button size="small" type="button" label="Kapat" severity="secondary" @click="products_dialog_visible = false"></Button>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref } from "vue";
const props = defineProps(["data"]);
const products_dialog_visible = ref(false);
</script>
