<template>
  <div>
    <div class="w-full lg:max-w-(--breakpoint-xl) lg:mx-auto">
      <div>
        <div class="mb-4 lg:mb-8 flex justify-between items-center gap-6">
          <div>
            <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Faturalar</h2>
            <div class="text-gray-500 text-sm mt-1">Faturalarınızı görüntüleyebilirsiniz.</div>
          </div>
          <Button label="Yeni Fatura Oluştur" @click="invoice_form_drawer_visible = true" size="small" />
        </div>
        <div>
          <Tabs v-model:value="active_tab" lazy scrollable>
            <TabList>
              <Tab value="0">Gelen Faturalar</Tab>
              <Tab value="1">Giden Faturalar</Tab>
              <Tab value="2">Onay Bekleyen Faturalar</Tab>
            </TabList>
            <TabPanels class="px-0!">
              <TabPanel value="0">
                <Card>
                  <template #content>
                    <InvoiceTable ref="inbox_invoice_table" :invoice-type="0" @on-row-select="onRowSelect" />
                  </template>
                </Card>
              </TabPanel>
              <TabPanel value="1">
                <Card>
                  <template #content>
                    <InvoiceTable ref="outbox_invoice_table" :invoice-type="1" @on-row-select="onRowSelect" />
                  </template>
                </Card>
              </TabPanel>
              <TabPanel value="2">
                <Card>
                  <template #content>
                    <InvoiceTable
                      ref="pending_approval_invoice_table"
                      :invoice-type="0"
                      :invoice-status="define_datas.invoice_pending_approval_status_id"
                      @on-row-select="onRowSelect"
                    />
                  </template>
                </Card>
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>
      </div>
    </div>
    <InvoiceFormDrawer v-model:visible="invoice_form_drawer_visible" v-model:invoice-id="invoice_id" @hide="onHideInvoiceFormDrawer" />
  </div>
</template>

<script setup>
import { ref, useTemplateRef } from "vue";
import { define_datas } from "@/data/system_data";
import InvoiceTable from "@/components/Invoice/InvoiceTable.vue";
import InvoiceFormDrawer from "@/components/Invoice/InvoiceFormDrawer.vue";

const inbox_invoice_table = useTemplateRef("inbox_invoice_table");
const outbox_invoice_table = useTemplateRef("outbox_invoice_table");
const pending_approval_invoice_table = useTemplateRef("pending_approval_invoice_table");

const active_tab = ref("0");
const invoice_form_drawer_visible = ref(false);
const invoice_id = ref(null);
const onHideInvoiceFormDrawer = () => {
  invoice_id.value = null;
  if (active_tab.value == "0") {
    inbox_invoice_table.value.refresh();
  } else if (active_tab.value == "1") {
    outbox_invoice_table.value.refresh();
  } else if (active_tab.value == "2") {
    pending_approval_invoice_table.value.refresh();
  }
};

const onRowSelect = (data) => {
  invoice_id.value = data.id;
  invoice_form_drawer_visible.value = true;
};
</script>
