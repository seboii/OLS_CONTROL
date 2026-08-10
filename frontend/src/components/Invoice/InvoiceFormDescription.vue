<template>
  <div>
    <div v-if="page_loading" class="w-full min-h-64 flex justify-center items-center gap-4">
      <ProgressSpinner class="size-16!" />
    </div>
    <div v-else>
      <div>
        <div class="mt-6">
          <div class="text-lg font-medium text-gray-700">Yeni Madde Ekle</div>
          <div>
            <InputGroup>
              <InputText v-model="new_invoice_footer.value" class="w-full" />
              <InputGroupAddon v-pv-tooltip.top="`Ekle`">
                <Button @click="addFooterItem" severity="secondary">
                  <CheckmarkCircle02Icon class="w-5" />
                </Button>
              </InputGroupAddon>
            </InputGroup>
          </div>
        </div>
        <div class="mt-6">
          <div class="text-lg font-medium text-gray-700">Maddeler</div>
          <div class="space-y-2 mt-2">
            <div v-for="footer in invoice_footers" :key="footer.id">
              <div>
                <InputGroup>
                  <InputText v-model="footer.editable_value" class="w-full" :readonly="!footer.editable" />
                  <InputGroupAddon v-if="footer.editable" v-pv-tooltip.top="`Güncelle`">
                    <Button @click="updateFooter(footer.id, footer.editable_value)" severity="secondary">
                      <CheckmarkCircle02Icon class="w-5 text-green-500" />
                    </Button>
                  </InputGroupAddon>
                  <InputGroupAddon v-if="footer.editable" v-pv-tooltip.top="`Sil`">
                    <Button @click="deleteFooter(footer.id)" severity="secondary">
                      <Delete02Icon class="w-5 text-red-500" />
                    </Button>
                  </InputGroupAddon>
                  <InputGroupAddon v-pv-tooltip.top="footer.editable ? `Düzenleme modunu kapat` : `Düzenleme modunu aç`">
                    <Button @click="footerItemEdit(footer.id)" severity="secondary">
                      <Edit02Icon v-if="!footer.editable" class="w-5" />
                      <EditOffIcon v-else class="w-5" />
                    </Button>
                  </InputGroupAddon>
                </InputGroup>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref, reactive } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import { useConfirm } from "primevue/useconfirm";
import { toast } from "vue-sonner";
import { CheckmarkCircle02Icon, Delete02Icon, Edit02Icon, EditOffIcon, Invoice04Icon, PencilEdit02Icon, TextAlignLeftIcon } from "hugeicons-vue";
const props = defineProps(["invoiceId"]);
const DataStore = useDataStore();
const confirm = useConfirm();

const page_loading = ref(false);

const invoice_footers = ref([]);
const new_invoice_footer = reactive({
  value: "",
});

const updateFooter = async (id, value) => {
  confirm.require({
    header: "Fatura Ek Bilgisi Güncelleme",
    message: "Fatura ek bilgisini güncellemek için emin misiniz?",
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
      const res = await DataStore.UPDATE_INVOICE_FOOTER({
        data: {
          id: id,
          value: value,
        },
      });
      if (res) {
        let find_item = invoice_footers.value.find((footer) => footer.id == id);
        find_item.value = value;
        find_item.editable_value = value;
        find_item.editable = false;
        toast.success("Fatura ek bilgisi güncellendi.");
      }
    },
  });
};

const deleteFooter = async (id) => {
  confirm.require({
    header: "Fatura Ek Bilgisi Silme",
    message: "Fatura ek bilgisini silmek için emin misiniz?",
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
      const res = await DataStore.DELETE_INVOICE_FOOTER(id);
      if (res) {
        invoice_footers.value = invoice_footers.value.filter((footer) => footer.id != id);
        toast.success("Fatura ek bilgisi silindi.");
      }
    },
  });
};

const footerItemEdit = (id) => {
  let find_item = invoice_footers.value.find((footer) => footer.id == id);
  find_item.editable = !find_item.editable;
  find_item.editable_value = find_item.value;
};

const addFooterItem = async () => {
  if (!new_invoice_footer.value) {
    toast.error("Fatura madde bilgileri boş bırakılamaz.");
    return;
  }
  confirm.require({
    header: "Fatura Ek Bilgisi Ekleme",
    message: "Fatura ek bilgisinin ekleneceği bilgileri doğrulayınız.",
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
      const res = await DataStore.CREATE_INVOICE_FOOTER({
        data: {
          invoice_id: props.invoiceId,
          value: new_invoice_footer.value,
        },
      });
      if (res) {
        invoice_footers.value.push({
          ...res.data,
          editable: false,
          editable_value: res.data.value,
        });
        new_invoice_footer.value = "";
        toast.success("Fatura ek bilgisi eklendi.");
      }
    },
  });
};

onMounted(async () => {
  page_loading.value = true;

  const res = await DataStore.GET_INVOICE_FOOTERS({
    invoice_id: props.invoiceId,
  });
  if (res) {
    invoice_footers.value = res.data;
  }
  invoice_footers.value.map((footer) => {
    footer.editable = false;
    footer.editable_value = footer.value;
  });

  page_loading.value = false;
});
</script>
