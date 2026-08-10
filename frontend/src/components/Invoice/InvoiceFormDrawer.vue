<template>
  <Drawer v-model:visible="drawer_visible" header=" " position="bottom" class="min-h-svh" @show="onShowDrawer" @hide="onHideDrawer">
    <div v-if="page_loading" class="flex items-center justify-center max-lg:py-20 lg:min-h-full">
      <ProgressSpinner class="size-20!" />
    </div>
    <div v-else class="lg:mx-auto lg:max-w-(--breakpoint-sm)">
      <div>
        <div>
          <div class="mb-12">
            <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">{{ form_data.id ? "Fatura Bilgileri" : "Yeni Fatura Oluştur" }}</div>
            <p class="text-sm text-gray-500">Fatura bilgilerini eksiksiz doldurunuz.</p>
          </div>
          <Tabs v-model:value="active_tab" scrollable lazy>
            <TabList v-if="form_data.id">
              <Tab value="0">Bilgiler</Tab>
              <Tab value="1">Kalemler</Tab>
              <Tab value="2">Ek Bilgiler</Tab>
              <Tab v-if="form_data.id && form_data.document_id" value="3">Fatura Önizleme</Tab>
            </TabList>
            <TabPanels class="py-6! px-0!">
              <TabPanel value="0">
                <div>
                  <div v-if="form_data.id" class="grid grid-cols-2 gap-2 mb-6">
                    <div class="py-3 px-4 bg-gray-100 rounded-lg">
                      <div class="text-sm text-gray-800 font-medium">Fatura Numarası</div>
                      <div class="text-sm text-gray-800">{{ form_data.invoice_id ? form_data.invoice_id : "-" }}</div>
                    </div>
                    <div class="py-3 px-4 bg-gray-100 rounded-lg">
                      <div class="text-sm text-gray-800 font-medium">Fatura Durumu</div>
                      <div class="text-sm text-gray-800">{{ form_data.invoice_status?.name ? form_data.invoice_status?.name : "-" }}</div>
                    </div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-12 gap-y-6 gap-x-4">
                    <div class="lg:col-span-12">
                      <SelectButton
                        v-model="form_data.commercial_type"
                        :options="invoice_commercial_type.filter((item) => item.status)"
                        optionLabel="name"
                        :allowEmpty="false"
                        class="w-full [&>.p-togglebutton]:w-full"
                      />
                    </div>
                    <div v-if="false" class="lg:col-span-12">
                      <SelectButton
                        v-model="form_data.box_type"
                        :options="invoice_box_type"
                        optionLabel="name"
                        optionValue="value"
                        :allowEmpty="false"
                        class="w-full [&>.p-togglebutton]:w-full"
                      />
                    </div>
                    <div v-if="false" class="lg:col-span-12">
                      <FloatLabel variant="on">
                        <InputText v-model="form_data.invoice_id" class="w-full" disabled />
                        <label>Fatura Numarası</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-12">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.account" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter></SelectAjax>
                        <label>Cari</label>
                      </FloatLabel>
                    </div>
                    <div v-if="false" class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <SelectAjax
                          v-model="form_data.invoice_status"
                          :api="`/api/v1/invoice_status`"
                          optionLabel="name"
                          class="w-full"
                          :disabled="default_data?.created_by_integration"
                          filter
                        ></SelectAjax>
                        <label>Fatura Durumu</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-12">
                      <FloatLabel variant="on">
                        <SelectAjax v-model="form_data.invoice_type" :api="`/api/v1/invoice_type`" optionLabel="name" class="w-full" filter></SelectAjax>
                        <label>Fatura Tipi</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <DatePicker v-model="form_data.invoice_create_date" class="w-full" />
                        <label>Fatura Oluşturulma Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-6">
                      <FloatLabel variant="on">
                        <DatePicker v-model="form_data.invoice_execution_date" class="w-full" />
                        <label>Fatura İşlem Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-12">
                      <FloatLabel variant="on">
                        <InputText v-model="form_data.order_document_id" class="w-full" />
                        <label>Sipariş Belge Numarası</label>
                      </FloatLabel>
                    </div>
                    <div class="lg:col-span-12">
                      <FloatLabel variant="on">
                        <Textarea v-model="form_data.message" autoResize class="w-full" rows="3"></Textarea>
                        <label>Mesaj</label>
                      </FloatLabel>
                    </div>
                  </div>
                  <div class="flex justify-between mt-6">
                    <div>
                      <Button
                        v-if="form_data.id"
                        size="small"
                        severity="danger"
                        @click="handleDelete"
                        :disabled="delete_loading_status"
                        :loading="delete_loading_status"
                        label="Kaydı Sil"
                      />
                    </div>
                    <div class="flex items-center gap-2">
                      <TriggerPopover v-if="form_data.id">
                        <template #trigger>
                          <Button label="Detaylar" size="small" severity="secondary" />
                        </template>
                        <template #content>
                          <div class="grid grid-cols-1 gap-2">
                            <Button
                              v-if="form_data.invoice_status?.id == define_datas.invoice_pending_approval_status_id"
                              @click="accept_reject_dialog_visible = true"
                              :disabled="accept_reject_loading_status"
                              :loading="accept_reject_loading_status"
                              label="Fatura Durumu Değiştir"
                              size="small"
                              severity="secondary"
                            />
                            <Button @click="draft_dialog_visible = true" label="Taslak Oluştur" size="small" severity="secondary" />
                          </div>
                        </template>
                      </TriggerPopover>
                      <Button @click="handleForm" :disabled="save_loading_status" :loading="save_loading_status" label="Kaydet" size="small" />
                    </div>
                  </div>
                </div>
              </TabPanel>
              <TabPanel value="1">
                <InvoiceFormInvoiceItems
                  :invoice-items="form_data.invoice_items"
                  :box-type="form_data.box_type"
                  @on-invoice-item-select="onInvoiceItemSelect"
                  @on-invoice-item-delete="onInvoiceItemDelete"
                />
              </TabPanel>
              <TabPanel value="2">
                <InvoiceFormDescription
                  :invoice-id="invoice_id"
                  :invoice-number="form_data.invoice_id"
                  :box-type="form_data.box_type"
                  @change-invoice-number="onChangeFormDocumentInvoiceNumber"
                />
              </TabPanel>
              <TabPanel value="3">
                <InvoiceFormDocumentViewer
                  :invoice-id="invoice_id"
                  :invoice-number="form_data.invoice_id"
                  :invoice-document-id="form_data.document_id"
                  :box-type="form_data.box_type"
                  @change-invoice-number="onChangeFormDocumentInvoiceNumber"
                  @change-invoice-document-id="onChangeFormDocumentInvoiceId"
                />
              </TabPanel>
            </TabPanels>
          </Tabs>
        </div>
      </div>
    </div>
    <Dialog
      v-model:visible="accept_reject_dialog_visible"
      header="Fatura Durumu"
      modal
      @show="onShowAcceptRejectDialog"
      @hide="onHideAcceptRejectDialog"
      class="w-full lg:w-100"
    >
      <div>
        <div>
          <div class="text-sm">Faturanın durumunu seçiniz. Lütfen işlemi yaparken dikkatli olunuz.</div>
          <div class="mt-4">
            <SelectButton
              v-model="accept_reject_status"
              :options="accept_reject_status_options"
              optionLabel="name"
              :allowEmpty="false"
              class="w-full [&>.p-togglebutton]:w-full [&_.p-togglebutton-label]:text-sm"
            />
          </div>
          <Transition
            enter-active-class="transition-all duration-500"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            mode="out-in"
          >
            <div v-if="accept_reject_status?.value == 'Declined'" class="mt-4">
              <FloatLabel variant="in">
                <label>Sebebi</label>
                <Textarea v-model="accept_reject_reason" autoResize class="w-full" rows="3"></Textarea>
              </FloatLabel>
            </div>
          </Transition>
        </div>
        <div class="w-full mt-4">
          <Button
            @click="handleAcceptReject"
            :disabled="accept_reject_loading_status"
            :loading="accept_reject_loading_status"
            label="Fatura Durumu Değiştir"
            size="small"
            fluid
          />
        </div>
      </div>
    </Dialog>
    <Dialog v-model:visible="draft_dialog_visible" header="Fatura Durumu" modal class="w-full lg:w-100">
      <div>
        <div>
          <div class="text-sm">Faturanın taslağını oluşturmak için gerekli alanları eksiksiz doldurunuz.</div>
          <div class="space-y-4 my-8">
            <FloatLabel variant="on">
              <SelectAjax
                v-model="draft_data.currency"
                :api="`/api/v1/currency`"
                optionLabel="name"
                class="w-full"
                filter
                @change="getExchangeRate"
              ></SelectAjax>
              <label>Para Birimi</label>
            </FloatLabel>
            <FloatLabel variant="on">
              <DatePicker v-model="draft_data.exchange_date" class="w-full" @date-select="getExchangeRate" />
              <label>Döviz Kuru Tarihi</label>
            </FloatLabel>
            <FloatLabel variant="on">
              <InputNumber v-model="draft_data.exchange_price" :minFractionDigits="5" :maxFractionDigits="5" class="w-full" />
              <label>Döviz Kuru Fiyatı</label>
            </FloatLabel>
          </div>
        </div>
        <div class="w-full mt-4">
          <Button @click="createDraftInvoice" :disabled="draft_loading_status" :loading="draft_loading_status" label="Taslak Oluştur" size="small" fluid />
        </div>
      </div>
    </Dialog>
  </Drawer>
</template>

<script setup>
import { watch, ref, reactive, onMounted, nextTick } from "vue";
import { toast } from "vue-sonner";
import { usePermissionStatus, useGetIsoDate } from "@/composables/index.js";
import { useFeatureStore } from "@/stores/general_store.js";
import { useDataStore } from "@/stores/data_store.js";
import { useRoute, useRouter } from "vue-router";
import { define_datas, buysell_types, invoice_commercial_type, invoice_box_type } from "@/data/system_data";
import InvoiceFormInvoiceItems from "@/components/Invoice/InvoiceFormInvoiceItems.vue";
import InvoiceFormDocumentViewer from "@/components/Invoice/InvoiceFormDocumentViewer.vue";
import InvoiceFormDescription from "@/components/Invoice/InvoiceFormDescription.vue";
import { useConfirm } from "primevue/useconfirm";
const Route = useRoute();
const Router = useRouter();
const emits = defineEmits(["hide"]);
const props = defineProps(["boxType"]);

const DataStore = useDataStore();
const FeatureStore = useFeatureStore();
const confirm = useConfirm();
const general_data = defineModel("general-data");
const drawer_visible = defineModel("visible");
const invoice_id = defineModel("invoice-id");
const active_tab = ref("0");
const accept_reject_dialog_visible = ref(false);
const accept_reject_reason = ref("");
const accept_reject_status = ref("");
const accept_reject_status_options = [
  {
    value: "Approved",
    name: "Onayla",
  },
  {
    value: "Declined",
    name: "Reddet",
  },
];

const draft_data = reactive({
  currency: "",
  exchange_date: "",
  exchange_price: "",
});
const draft_dialog_visible = ref(false);
const draft_loading_status = ref(false);
const createDraftInvoice = async () => {
  if (!draft_data.currency) {
    toast.error("Lütfen tüm alanları doldurunuz.");
    return;
  }
  draft_loading_status.value = true;
  confirm.require({
    header: "Fatura Taslak Oluşturma",
    message: "Faturanın taslak haline getirilmesi için emin misiniz?",
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
      let request_body = {
        invoice_id: invoice_id.value,
      };
      let currency_code = draft_data.currency.code;
      if (currency_code == "TL") {
        request_body.currency_code = "TRY";
      } else {
        request_body.currency_code = currency_code;
      }
      if (draft_data.exchange_date) {
        request_body.exchange_date = useGetIsoDate(draft_data.exchange_date);
      }
      if (draft_data.exchange_price) {
        request_body.exchange_price = draft_data.exchange_price;
      }
      const res = await DataStore.CREATE_DRAFT_INVOICE(request_body);
      if (res) {
        toast.success("Fatura taslak oluşturuldu.");
        draft_dialog_visible.value = false;
        form_data.document_id = res.data.document_id;
      }
    },
  });
  draft_loading_status.value = false;
};

const getExchangeRate = async () => {
  if (!draft_data.currency || !draft_data.currency.code || !draft_data.exchange_date) {
    return;
  }
  let currency_code = draft_data.currency.code;
  let date = useGetIsoDate(draft_data.exchange_date);
  const res = await DataStore.GET_EXCHANGE_RATE({ currency_code, date });
  if (res) {
    draft_data.exchange_price = res.data.forex_buying;
  }
};

const default_data = ref();
const form_data = reactive({
  id: "",
  invoice_id: "",
  commercial_type: invoice_commercial_type[0],
  box_type: invoice_box_type[0].value,
  account: "",
  invoice_status: "",
  invoice_type: "",
  invoice_create_date: "",
  invoice_execution_date: "",
  order_document_id: "",
  message: "",
  invoice_items: [],
  document_id: "",
});
const page_loading = ref(false);
const delete_loading_status = ref(false);
const save_loading_status = ref(false);
const accept_reject_loading_status = ref(false);
const handleForm = async () => {
  if (!form_data.commercial_type || !form_data.account || !form_data.invoice_type) {
    toast.error("Lütfen tüm alanları doldurunuz.");
    return;
  }
  save_loading_status.value = true;
  try {
    const send_data = new FormData();

    if (form_data.id) {
      send_data.append("id", form_data.id);
    }
    send_data.append("commercial_type", form_data.commercial_type?.value);
    send_data.append("box_type", form_data.box_type);
    send_data.append("invoice_id", form_data.invoice_id);
    send_data.append("account_id", form_data.account?.id);
    //send_data.append("invoice_status_id", form_data.invoice_status?.id);
    send_data.append("invoice_type_id", form_data.invoice_type?.id);
    send_data.append("invoice_create_date", form_data.invoice_create_date ? useGetIsoDate(form_data.invoice_create_date) : "");
    send_data.append("invoice_execution_date", form_data.invoice_execution_date ? useGetIsoDate(form_data.invoice_execution_date) : "");
    send_data.append("order_document_id", form_data.order_document_id ? form_data.order_document_id : "");
    send_data.append("message", form_data.message ? form_data.message : "");

    if (form_data.invoice_items.length > 0) {
      form_data.invoice_items.forEach((item, index) => {
        send_data.append(`load_transfer_invoice_maps[${index}][load_transfer_id]`, item.load_transfer?.id);
        send_data.append(`load_transfer_invoice_maps[${index}][invoice_item_id]`, item.id);
      });
    }

    if (form_data.id) {
      const res = await DataStore.UPDATE_INVOICE({
        data: send_data,
      });
      if (res) {
        toast.success("Fatura güncellendi.");
      }
    } else {
      const res = await DataStore.CREATE_INVOICE({
        data: send_data,
      });
      if (res) {
        invoice_id.value = res.data.id;
        nextTick(() => {
          onShowDrawer();
        });
        toast.success("Yeni fatura oluşturuldu.");
      }
    }
  } catch (error) {
    console.log(error);
    toast.error("Bir hata oluştu.");
  }
  save_loading_status.value = false;
};

const handleDelete = async () => {
  delete_loading_status.value = true;
  try {
    const res = await DataStore.DELETE_INVOICE(form_data.id);
    if (res) {
      toast.success("Fatura silindi.");
      drawer_visible.value = false;
    }
  } catch (error) {
    console.log(error);
  }
  delete_loading_status.value = false;
};

const getInvoice = async () => {
  page_loading.value = true;
  const res = await DataStore.GET_INVOICE(invoice_id.value);
  if (res) {
    default_data.value = res.data;
    const data = res.data;
    form_data.id = data.id;
    form_data.invoice_id = data.invoice_id;
    form_data.commercial_type = invoice_commercial_type.find((item) => item.value == data.commercial_type);
    form_data.account = data.invoice_account;
    form_data.box_type = data.box_type;
    form_data.invoice_status = data.invoice_status;
    form_data.invoice_type = data.invoice_type;
    form_data.invoice_create_date = data.invoice_create_date ? new Date(data.invoice_create_date) : "";
    form_data.invoice_execution_date = data.invoice_execution_date ? new Date(data.invoice_execution_date) : "";
    form_data.order_document_id = data.order_document_id;
    form_data.message = data.message;
    form_data.document_id = data.document_id;

    if (data.load_transfer_invoice_maps.length > 0) {
      data.load_transfer_invoice_maps.forEach((item) => {
        form_data.invoice_items.push(item.load_transfer_invoice_item);
      });
    }
  }
  page_loading.value = false;
};

const onShowDrawer = async (event) => {
  if (invoice_id.value) {
    Router.push({
      query: {
        ...Route.query,
        "invoice-id": invoice_id.value,
      },
    });
    await getInvoice();
  }
};
const onHideDrawer = () => {
  let new_query = { ...Route.query };
  delete new_query["invoice-id"];
  Router.push({
    query: new_query,
  });
  invoice_id.value = null;

  default_data.value = null;
  form_data.id = "";
  form_data.invoice_id = "";
  form_data.commercial_type = "";
  form_data.account = "";
  form_data.invoice_status = "";
  form_data.invoice_type = "";
  form_data.invoice_create_date = "";
  form_data.invoice_execution_date = "";
  form_data.order_document_id = "";
  form_data.message = "";
  form_data.invoice_items = [];
  form_data.document_id = "";
  emits("hide");
};

watch(
  () => Route.query["invoice-id"],
  (newVal) => {
    if (!newVal) {
      invoice_id.value = null;
      drawer_visible.value = false;
    }
  }
);

const handleAcceptReject = async () => {
  if (!accept_reject_status.value) {
    toast.error("Lütfen bir durum seçiniz.");
    return;
  }
  accept_reject_loading_status.value = true;
  let req_body = {
    invoice_id: invoice_id.value,
    status: accept_reject_status.value?.value,
  };
  if (accept_reject_status.value?.value == "declined") {
    req_body.reason = accept_reject_reason.value;
  }
  const res = await DataStore.INVOICE_ACCEPT_REJECT(req_body);
  if (res) {
    toast.success("Fatura durumu değiştirildi.");
    accept_reject_dialog_visible.value = false;
    accept_reject_status.value = "";
    accept_reject_reason.value = "";
  }
  accept_reject_loading_status.value = false;
};

const onShowAcceptRejectDialog = () => {
  accept_reject_status.value = "";
  accept_reject_reason.value = "";
};

const onInvoiceItemSelect = (item) => {
  form_data.invoice_items = [...form_data.invoice_items, item];
};

const onInvoiceItemDelete = (delete_item) => {
  form_data.invoice_items = form_data.invoice_items.filter((item) => item.id !== delete_item.id);
};

const onChangeFormDocumentInvoiceNumber = (invoice_number) => {
  form_data.invoice_id = invoice_number;
};

const onChangeFormDocumentInvoiceId = (invoice_document_id) => {
  form_data.document_id = invoice_document_id;
  active_tab.value = "0";
};

onMounted(() => {
  if (props.boxType) {
    form_data.box_type = props.boxType;
  }
  if (Route.query["invoice-id"]) {
    invoice_id.value = Route.query["invoice-id"];
    drawer_visible.value = true;
  }
});
</script>
