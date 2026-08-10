<template>
  <div>
    <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
      <div>
        <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
          <div>
            <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Gider Faturalar</h2>
            <div class="text-gray-500 text-sm mt-1">Gider faturalarınızı görüntüleyebilirsiniz.</div>
          </div>
          <Button label="Yeni Fatura Oluştur" @click="invoice_form_drawer_visible = true" size="small" />
        </div>
        <div>
          <Card>
            <template #content>
              <InvoiceTable ref="inbox_invoice_table" :invoice-type="0" @on-row-select="onRowSelect" />
            </template>
          </Card>
        </div>
      </div>
    </div>
    <InvoiceFormDrawer v-model:visible="invoice_form_drawer_visible" v-model:invoice-id="invoice_id" :box-type="0" @hide="onHideInvoiceFormDrawer" />
  </div>
</template>

<script setup>
import { ref, useTemplateRef } from "vue";
import InvoiceTable from "@/components/Invoice/InvoiceTable.vue";
import InvoiceFormDrawer from "@/components/Invoice/InvoiceFormDrawer.vue";

const inbox_invoice_table = useTemplateRef("inbox_invoice_table");

const invoice_form_drawer_visible = ref(false);
const invoice_id = ref(null);
const onHideInvoiceFormDrawer = () => {
  invoice_id.value = null;
  inbox_invoice_table.value.refresh();
};

const onRowSelect = (data) => {
  invoice_id.value = data.id;
  invoice_form_drawer_visible.value = true;
};
</script>
