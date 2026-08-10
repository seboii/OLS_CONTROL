<template>
  <div class="mt-4">
    <div class="space-y-4">
      <Card v-for="{ invoice } in loadData.load_transfer_invoice_maps" :key="invoice.id" class="border border-gray-100 shadow-sm">
        <template #content>
          <div>
            <div class="grid grid-cols-1 lg:grid-cols-12 gap-2">
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-6">
                <div class="text-sm font-medium text-gray-500">Fatura Ticareti Tipi</div>
                <p class="text-sm font-medium">{{ invoice_commercial_type.find((item) => item.value == invoice.commercial_type)?.name }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-6">
                <div class="text-sm font-medium text-gray-500">Fatura Gelen/Giden Durumu</div>
                <p class="text-sm font-medium">{{ invoice_box_types.find((item) => item.value == invoice.box_type)?.name }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-6">
                <div class="text-sm font-medium text-gray-500">Fatura Durumu</div>
                <p class="text-sm font-medium">{{ invoice.invoice_status?.name }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-6">
                <div class="text-sm font-medium text-gray-500">Fatura Tipi</div>
                <p class="text-sm font-medium">{{ invoice.invoice_type?.name }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-6">
                <div class="text-sm font-medium text-gray-500">Fatura No</div>
                <p class="text-sm font-medium">{{ invoice.invoice_id }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-6">
                <div class="text-sm font-medium text-gray-500">Alıcı</div>
                <p class="text-sm font-medium">{{ invoice.target_title }}</p>
                <p class="text-xs text-gray-500">{{ invoice.target_identity_no }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-4">
                <div class="text-sm font-medium text-gray-500">Tutar</div>
                <p class="text-sm font-medium">{{ useMoneyFormat(invoice.payable_amount) }} {{ invoice.document_currency_code }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-4">
                <div class="text-sm font-medium text-gray-500">KDV Hariç</div>
                <p class="text-sm font-medium">{{ useMoneyFormat(invoice.tax_exclusive_amount) }} {{ invoice.document_currency_code }}</p>
              </div>
              <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-4">
                <div class="text-sm font-medium text-gray-500">KDV</div>
                <p class="text-sm font-medium">{{ useMoneyFormat(invoice.tax_amount) }} ({{ invoice.tax_rate }}%) {{ invoice.document_currency_code }}</p>
              </div>
            </div>
          </div>
        </template>
      </Card>
    </div>
  </div>
</template>

<script setup>
import { useMoneyFormat } from "@/composables/index.js";
import { invoice_commercial_type, invoice_box_types } from "@/data/system_data.js";
const props = defineProps({
  loadData: {
    type: Object,
    required: true,
  },
});
</script>
