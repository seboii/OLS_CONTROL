<template>
  <div>
    <div v-if="pdf_loading" class="w-full min-h-64 flex justify-center items-center gap-4">
      <ProgressSpinner class="size-16!" />
    </div>
    <div v-else>
      <div v-if="!invoiceNumber && base64_data" class="mb-8">
        <Card>
          <template #title>
            <div class="flex items-center gap-4">
              <div>
                <Invoice04Icon class="text-primary w-6" />
              </div>
              <div>Fatura Taslak Onayı</div>
            </div>
          </template>
          <template #content>
            <div class="mt-4">
              <div class="text-sm mb-4">Fatura taslağı oluşturulmuştur. Doğruluğunu teyit ettikten sonra onaylayınız.</div>
              <div class="grid grid-cols-2 gap-4">
                <Button @click="approveDraftInvoice" :disabled="draft_invoice_form_loading" :loading="draft_invoice_form_loading" label="Onayla" size="small" />
                <Button
                  @click="rejectDraftInvoice"
                  :disabled="draft_invoice_form_loading"
                  :loading="draft_invoice_form_loading"
                  label="Reddet"
                  size="small"
                  severity="secondary"
                />
              </div>
            </div>
          </template>
        </Card>
      </div>
      <iframe id="pdfViewer" :src="base64_data" class="w-full aspect-[9/16]"></iframe>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref, reactive } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import { useConfirm } from "primevue/useconfirm";
import { toast } from "vue-sonner";
import { CheckmarkCircle02Icon, Delete02Icon, Edit02Icon, EditOffIcon, Invoice04Icon, PencilEdit02Icon, TextAlignLeftIcon } from "hugeicons-vue";
const props = defineProps(["invoiceId", "invoiceNumber", "boxType", "invoiceDocumentId"]);
const DataStore = useDataStore();
const base64_data = ref("");
const pdf_loading = ref(false);

const confirm = useConfirm();

const emits = defineEmits(["changeInvoiceNumber", "changeInvoiceDocumentId"]);

const draft_invoice_form_loading = ref(false);
const approveDraftInvoice = async () => {
  draft_invoice_form_loading.value = true;
  confirm.require({
    header: "Fatura Taslak Onayı",
    message: "Faturanın taslağının onaylanması için emin misiniz?",
    acceptProps: {
      label: "Evet",
      size: "small",
    },
    rejectProps: {
      label: "Hayır",
      severity: "secondary",
      size: "small",
    },
    accept: async () => {
      const res = await DataStore.INVOICE_DRAFT_APPROVE({
        invoice_id: props.invoiceId,
      });
      if (res) {
        emits("changeInvoiceNumber", res.invoice_number);
        toast.success("Fatura taslağı onaylandı.");
      }
    },
  });
  draft_invoice_form_loading.value = false;
};
const rejectDraftInvoice = async () => {
  draft_invoice_form_loading.value = true;
  confirm.require({
    header: "Fatura Taslak Reddetme",
    message: "Faturanın taslağının reddedilmesi için emin misiniz?",
    acceptProps: {
      label: "Evet",
      size: "small",
    },
    rejectProps: {
      label: "Hayır",
      severity: "secondary",
      size: "small",
    },
    accept: async () => {
      const res = await DataStore.INVOICE_DRAFT_REJECT({
        invoice_id: props.invoiceId,
      });
      if (res) {
        emits("changeInvoiceNumber", res.invoice_number);
        emits("changeInvoiceDocumentId", res.data.document_id);
        toast.success("Fatura taslağı reddedildi.");
      }
    },
  });
  draft_invoice_form_loading.value = false;
};

onMounted(async () => {
  pdf_loading.value = true;

  const pdfPromise =
    props.boxType == 0 ? DataStore.GET_INVOICE_INBOX_PDF_VIEW({ id: props.invoiceId }) : DataStore.GET_INVOICE_OUTBOX_PDF_VIEW({ id: props.invoiceId });

  const [pdfResult] = await Promise.all([pdfPromise]);
  if (pdfResult) {
    base64_data.value = `data:application/pdf;base64,${pdfResult.data}`;
  }

  pdf_loading.value = false;
});
</script>
