<template>
  <div>
    <div class="flex justify-end">
      <Button @click="financial_item_list_dialog_visible = true" label="Yeni Kalem Ekle" size="small" />
    </div>
    <div class="space-y-4 mt-4 relative">
      <TransitionGroup name="list1">
        <div v-for="item in invoiceItems" :key="item.id" class="w-full">
          <div class="rounded-xl p-5 lg:py-5 lg:px-8 border bg-white shadow-xs flex items-start lg:items-end justify-between max-lg:flex-col gap-4">
            <div class="w-full">
              <div class="flex items-center gap-2 mb-3">
                <div class="font-semibold">{{ item.item?.name }}</div>
              </div>
              <div class="flex flex-wrap gap-2 max-lg:flex-col">
                <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                  Adet : <span class="font-medium">{{ parseFloat(item.quantity) }}</span>
                </div>
                <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
                  Toplam Tutar : <span class="font-medium">{{ useMoneyFormat(item.total_price) }} {{ item.currency?.code }}</span>
                </div>
                <div v-if="item.load_transfer_einvoice" class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg flex items-center gap-2">
                  <div class="size-2 rounded-full bg-green-500"></div>
                  <div class="font-medium">Faturası Var</div>
                </div>
              </div>
            </div>
            <div class="flex items-center justify-end gap-2">
              <Button @click="emits('onInvoiceItemDelete', item)" severity="danger" outlined size="small" label="Sil" class="px-6!" />
            </div>
          </div>
        </div>
      </TransitionGroup>
    </div>
    <Dialog v-model:visible="financial_item_list_dialog_visible" modal header="Kalemler" class="w-full lg:w-7/8" @show="onShowFinancialItemListDialog">
      <div class="pt-2">
        <div class="flex justify-end gap-2 mb-8">
          <div v-if="false" class="w-full lg:w-40">
            <FloatLabel class="w-full" variant="on">
              <Select v-model="selected_buysell" :options="buysell_types" optionLabel="name" @change="onBuysellChange" showClear fluid />
              <label for="username">Durum</label>
            </FloatLabel>
          </div>
          <div>
            <FloatLabel variant="on">
              <SelectAjax
                v-model="selected_account"
                :api="`/api/v1/account`"
                optionLabel="name"
                class="w-full"
                filter
                @change="onAccountChange"
                @complete="onAccountChange"
              >
                <template #option="slotProps">
                  <div class="flex items-center gap-2">
                    <div class="size-8 rounded-lg overflow-hidden border shadow-xs bg-gray-100">
                      <img v-if="slotProps.option.avatar" :src="`/storage/${slotProps.option.avatar}`" class="w-full h-full object-cover" />
                    </div>
                    <div class="text-sm">{{ slotProps.option.name }}</div>
                  </div>
                </template>
              </SelectAjax>
              <label for="username">{{ financial_item_list_filter.buysell == 1 ? "Tedarikçiler" : "Müşteriler" }}</label>
            </FloatLabel>
          </div>
        </div>
        <DatatableAjax
          :tableState="financialItemTable"
          selectionMode="single"
          @rowSelect="onRowSelect"
          scrollable
          noAction
          removableSort
          search
          class="text-sm"
        >
          <Column header="Kalem">
            <template #body="{ data }">
              <div class="text-nowrap flex items-center gap-2">
                <div>{{ data.item?.name }}</div>
                <Badge v-if="invoiceItems.find((item) => item.id == data.id)" severity="success">Eklendi</Badge>
              </div>
              <div class="text-nowrap text-xs text-gray-500">{{ data.insert_name }}</div>
            </template>
          </Column>
          <Column header="Müşteri">
            <template #body="{ data }">
              <div>
                <div class="text-nowrap">{{ data.account_id?.name }}</div>
                <div class="text-nowrap text-xs text-gray-500">{{ data.account_id?.email }}</div>
              </div>
            </template>
          </Column>
          <Column header="Adet">
            <template #body="{ data }">
              <div>
                <div class="text-nowrap">{{ data.quantity }}</div>
              </div>
            </template>
          </Column>
          <Column header="Toplam Tutar">
            <template #body="{ data }">
              <div>
                <div class="text-nowrap">{{ useMoneyFormat(data.total_price) }} {{ data.currency?.code }}</div>
              </div>
            </template>
          </Column>
          <Column header="Net Tutar">
            <template #body="{ data }">
              <div>
                <div class="text-nowrap">{{ useMoneyFormat(data.net_price) }} {{ data.currency?.code }}</div>
              </div>
            </template>
          </Column>
          <Column header="Vergi Tutarı">
            <template #body="{ data }">
              <div>
                <div class="text-nowrap">{{ useMoneyFormat(data.tax_price) }} {{ data.currency?.code }}</div>
              </div>
            </template>
          </Column>
          <Column v-if="false" header="Vergi Oranı">
            <template #body="{ data }">
              <div>
                <div class="text-nowrap">{{ data.tax_rate }}</div>
              </div>
            </template>
          </Column>
        </DatatableAjax>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, reactive } from "vue";
import { useGetIsoDate, useDatatable, useMoneyFormat } from "@/composables/index.js";
import { buysell_types } from "@/data/system_data";
import { toast } from "vue-sonner";

const { invoiceItems, boxType = 0 } = defineProps(["invoiceItems", "boxType"]);
const emits = defineEmits(["onInvoiceItemSelect", "onInvoiceItemDelete"]);
const financial_item_list_dialog_visible = ref(false);
const selected_buysell = ref(null);
const selected_account = ref(null);
const financial_item_list_filter = reactive({
  status: "pending",
  account_id: "",
  buysell: boxType == 0 ? 1 : 2,
});
const financialItemTable = ref(
  useDatatable({
    getUrl: "/api/v1/load_transfer_invoice_item",
    deleteUrl: "/api/v1/load_transfer_invoice_item/delete",
    perPage: 10,
    defaultParams: financial_item_list_filter,
  })
);

const onShowFinancialItemListDialog = () => {
  financialItemTable.value.refresh();
};

const onAccountChange = (e) => {
  if (e.value && e.value.id) {
    financial_item_list_filter.account_id = e.value.id;
    financialItemTable.value = useDatatable({
      getUrl: "/api/v1/load_transfer_invoice_item",
      deleteUrl: "/api/v1/load_transfer_invoice_item/delete",
      perPage: 10,
      defaultParams: financial_item_list_filter,
    });
    financialItemTable.value.refresh();
  }
  if ((e.value && e.value == null) || e.value == "") {
    financial_item_list_filter.account_id = "";
    financialItemTable.value = useDatatable({
      getUrl: "/api/v1/load_transfer_invoice_item",
      deleteUrl: "/api/v1/load_transfer_invoice_item/delete",
      perPage: 10,
      defaultParams: financial_item_list_filter,
    });
    financialItemTable.value.refresh();
  }
};

const onRowSelect = (e) => {
  if (invoiceItems.find((item) => item.id == e.data.id)) {
    toast.error("Bu kalem zaten seçili.");
    return;
  }
  emits("onInvoiceItemSelect", e.data);
  toast.success("Kalem başarıyla eklendi.");
};
</script>
