<template>
  <Drawer v-model:visible="accountFormData.drawer_visible" header=" " position="bottom" class="min-h-svh" @show="onDrawerShow" @hide="onDrawerHide">
    <div
      :class="{
        'lg:mx-auto lg:max-w-(--breakpoint-xl)': activeTab == 3,
        'lg:mx-auto lg:max-w-(--breakpoint-sm)': activeTab != 3,
      }"
      class="transition-all duration-500"
    >
      <div class="flex justify-between gap-4">
        <div>
          <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">
            {{ accountFormData.form_type == "create" ? "Yeni Hesap Oluştur" : "Hesap Bilgileri" }}
          </div>
          <p class="text-sm text-gray-500">
            {{
              accountFormData.form_type == "create"
                ? "Bilgileri eksiksiz ve doğru doldurmaya özen gösteriniz."
                : "Hesap bilgilerini inceleyebilir ve güncelleme yapabilirsiniz."
            }}
          </p>
        </div>
      </div>
      <Tabs v-model:value="activeTab" class="mt-6!" lazy scrollable>
        <TabList>
          <Tab value="0">Genel Bilgiler</Tab>
          <Tab value="1">İletişim Bilgileri</Tab>
          <Tab value="2">Görevli</Tab>
          <Tab v-if="accountFormData?.active_data?.id" value="3">Faturalar</Tab>
        </TabList>
        <TabPanels class="px-0! pt-10!">
          <TabPanel value="0">
            <div class="flex items-center flex-col gap-4">
              <AvatarFile v-model="new_customer_data.avatar" v-model:image-remove="new_customer_data.avatar_remove" preview-path="/storage" size="128" />
              <div class="grid grid-cols-1 lg:grid-cols-2 gap-y-5 gap-x-5 mt-4 w-full">
                <div class="lg:col-span-2">
                  <FloatLabel variant="on" class="w-full">
                    <InputText v-model="new_customer_data.name" :invalid="v$.name.$error" autocomplete="off" fluid />
                    <label>Hesap Adı</label>
                  </FloatLabel>
                  <InputErrorMessage :errors="v$.name.$errors" />
                </div>
                <div class="lg:col-span-2">
                  <FloatLabel variant="on">
                    <MultiSelect
                      v-model="new_customer_data.account_type"
                      :invalid="v$.account_type.$error"
                      :options="define_datas.account.account_type"
                      display="chip"
                      optionLabel="name"
                      class="w-full"
                      dataKey="id"
                      filter
                      multiple
                    />
                    <SelectAjax
                      v-if="false"
                      v-model="new_customer_data.account_type"
                      :api="`/api/v1/account_type`"
                      :invalid="v$.account_type.$error"
                      optionLabel="name"
                      class="w-full"
                      dataKey="id"
                      filter
                      multiple
                    />
                    <label for="username">Hesap Türü</label>
                  </FloatLabel>
                  <InputErrorMessage :errors="v$.account_type.$errors" />
                </div>
                <div class="lg:col-span-2">
                  <FloatLabel variant="on">
                    <SelectAjax
                      v-model="new_customer_data.country"
                      :api="`/api/v1/country`"
                      :invalid="v$.country.$error"
                      optionLabel="name"
                      class="w-full"
                      filter
                      @change="countryOnChange"
                    />
                    <label for="username">Ülke</label>
                  </FloatLabel>
                  <InputErrorMessage :errors="v$.country.$errors" />
                </div>
                <div>
                  <FloatLabel variant="on">
                    <SelectAjax
                      v-model="new_customer_data.city"
                      :api="`/api/v1/city`"
                      :fetchParams="{
                        country_id: new_customer_data.country ? new_customer_data.country.id : null,
                      }"
                      :disabled="!new_customer_data.country"
                      optionLabel="name"
                      class="w-full"
                      filter
                      @change="cityOnChange"
                    />
                    <label for="username">Şehir</label>
                  </FloatLabel>
                </div>
                <div>
                  <FloatLabel variant="on">
                    <SelectAjax
                      v-model="new_customer_data.district"
                      :api="`/api/v1/district`"
                      :fetchParams="{
                        city_id: new_customer_data.city ? new_customer_data.city.id : null,
                      }"
                      :disabled="!new_customer_data.city"
                      optionLabel="name"
                      class="w-full"
                      filter
                    />
                    <label for="username">İlçe</label>
                  </FloatLabel>
                </div>
                <div>
                  <FloatLabel variant="on" class="w-full">
                    <InputText v-model="new_customer_data.accounting_code" autocomplete="off" fluid v-keyfilter.num readonly />
                    <label>Muhasebe Kodu</label>
                  </FloatLabel>
                </div>
                <FloatLabel variant="on" class="w-full">
                  <InputText v-model="new_customer_data.tax_number" autocomplete="off" fluid v-keyfilter.num />
                  <label>Vergi Numarası</label>
                </FloatLabel>
                <div>
                  <FloatLabel variant="on">
                    <SelectAjax v-model="new_customer_data.tax_office" :api="`/api/v1/tax_office`" optionLabel="name" class="w-full" filter>
                      <template #option="slotProps">
                        <div>
                          <div>{{ slotProps.option.name }}</div>
                          <div class="flex items-center gap-2 text-gray-500">
                            <div class="text-xs">{{ slotProps.option.city }} - {{ slotProps.option.special_code }}</div>
                          </div>
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Vergi Dairesi</label>
                  </FloatLabel>
                </div>
                <FloatLabel variant="on" class="w-full">
                  <InputText v-model="new_customer_data.discount" autocomplete="off" fluid v-keyfilter.money />
                  <label>İndirim Tutarı</label>
                </FloatLabel>
                <div class="lg:col-span-2">
                  <SelectButton
                    v-model="new_customer_data.individual_personal"
                    :options="individual_personal_options"
                    optionLabel="label"
                    :allowEmpty="false"
                    class="w-full [&>.p-togglebutton]:w-full"
                    fluid
                  />
                </div>
              </div>
            </div>
          </TabPanel>
          <TabPanel value="1">
            <div class="grid grid-cols-1 gap-y-5 gap-x-5 mt-4 w-full">
              <div>
                <FloatLabel variant="on" class="w-full">
                  <InputText v-model="new_customer_data.email" type="email" autocomplete="off" fluid />
                  <label>E-Posta</label>
                </FloatLabel>
              </div>
              <div class="flex items-center gap-2">
                <FloatLabel variant="on">
                  <SelectAjax
                    v-model="new_customer_data.phone_country"
                    :api="`/api/v1/country`"
                    :fetchParams="{
                      phone_code_status: 1,
                    }"
                    searchKey="phone_code"
                    :optionLabel="(e) => `+${e.phone_code}`"
                    class="w-full lg:w-40"
                    filter
                  >
                    <template #option="slotProps">
                      <div class="flex items-center gap-2">
                        <div>+{{ slotProps.option.phone_code }}</div>
                      </div>
                    </template>
                  </SelectAjax>
                  <label for="username">Ülke Kodu</label>
                </FloatLabel>
                <FloatLabel variant="on" class="w-full">
                  <InputText v-model="new_customer_data.phone" v-keyfilter.int fluid />
                  <label>Telefon Numarası</label>
                </FloatLabel>
              </div>
              <div>
                <FloatLabel variant="on">
                  <Select v-model="new_customer_data.contact_language" :options="define_datas.account.contact_language" optionLabel="name" class="w-full" />
                  <SelectAjax
                    v-if="false"
                    v-model="new_customer_data.contact_language"
                    :api="`/api/v1/country`"
                    searchKey="search"
                    optionLabel="name"
                    class="w-full"
                    filter
                    fluid
                  >
                    <template #option="slotProps">
                      <div class="flex items-center gap-2">
                        <div>{{ slotProps.option.name }}</div>
                      </div>
                    </template>
                  </SelectAjax>
                  <label for="username">İletişim Dili</label>
                </FloatLabel>
              </div>
              <FloatLabel variant="on" class="w-full">
                <Textarea v-model="new_customer_data.address" rows="5" cols="30" style="resize: none" fluid />
                <label>Adres</label>
              </FloatLabel>
              <div>
                <div>
                  <div class="flex justify-between items-center mb-6 py-3 px-6 bg-gray-50 rounded-lg">
                    <label>İlgili Kişiler ({{ new_customer_data.contact_persons.length }})</label>
                    <Button size="small" class="p-2!" outlined @click="addContactPerson" v-tooltip="'Yeni İlgili Kişi Ekle'">
                      <template #default> <Add01Icon size="14" /> </template>
                    </Button>
                  </div>
                  <div class="relative min-h-8">
                    <TransitionGroup
                      tag="ul"
                      enter-active-class="transition-all duration-[400ms]"
                      move-class="transition-all duration-[400ms]"
                      enter-from-class="opacity-0 scale-90"
                      enter-to-class="opacity-100"
                      mode="out-in"
                    >
                      <div v-for="(person, index) in new_customer_data.contact_persons" :key="person" class="w-full mb-3">
                        <div class="flex gap-3">
                          <FloatLabel variant="on" class="w-full">
                            <InputText v-model="person.name" autocomplete="off" fluid />
                            <label>İlgili Kişi</label>
                          </FloatLabel>
                          <FloatLabel variant="on" class="w-full">
                            <InputText v-model="person.email" type="email" autocomplete="off" fluid />
                            <label>E-posta</label>
                          </FloatLabel>
                          <Button label="Sil" size="small" severity="danger" class="p-3! aspect-square! shrink-0!" outlined @click="removeContactPerson(index)">
                            <template #default> <Delete02Icon size="18" /> </template>
                          </Button>
                        </div>
                      </div>
                    </TransitionGroup>
                  </div>
                </div>
              </div>
            </div>
          </TabPanel>
          <TabPanel value="2">
            <FloatLabel variant="on">
              <SelectAjax
                v-model="new_customer_data.charge_persons"
                multiple
                :api="`/api/v1/user`"
                :optionLabel="(e) => `${e.name} ${e.surname}`"
                class="w-full"
                dataKey="id"
                filter
              />
              <label>Satış Temsilcisi</label>
            </FloatLabel>
          </TabPanel>
          <TabPanel value="3">
            <div class="max-lg:hidden">
              <DataTable
                :value="accountFormData.active_data.invoice"
                paginator
                :rows="10"
                :rowsPerPageOptions="[5, 10, 20, 50]"
                class="w-full text-sm [&_.p-datatable-column-title]:text-nowrap!"
              >
                <Column field="invoice_id" header="Fatura No"></Column>
                <Column field="invoice_type.name" header="Fatura Tipi"></Column>
                <Column field="invoice_status.name" header="Fatura Durumu"></Column>
                <Column field="commercial_type" header="Ticari Tipi">
                  <template #body="{ data }">
                    <div>
                      <div>{{ invoice_commercial_type.find((item) => item.value == data.commercial_type)?.name }}</div>
                    </div>
                  </template>
                </Column>
                <Column field="box_type" header="Gelen/Giden Tipi">
                  <template #body="{ data }">
                    <div>
                      <div>{{ invoice_box_types.find((item) => item.value == data.box_type)?.name }}</div>
                    </div>
                  </template>
                </Column>
                <Column field="payable_amount" header="Ödenecek Tutar">
                  <template #body="{ data }">
                    <div>
                      <div class="text-nowrap">{{ useMoneyFormat(data.payable_amount) }} {{ data.document_currency_code }}</div>
                    </div>
                  </template>
                </Column>
                <Column field="tax_exclusive_amount" header="KDV Hariç Tutar">
                  <template #body="{ data }">
                    <div>
                      <div class="text-nowrap">{{ useMoneyFormat(data.tax_exclusive_amount) }} {{ data.document_currency_code }}</div>
                    </div>
                  </template>
                </Column>
                <Column field="tax_amount" header="KDV Tutarı">
                  <template #body="{ data }">
                    <div>
                      <div class="text-nowrap">{{ useMoneyFormat(data.tax_amount) }} {{ data.document_currency_code }}</div>
                    </div>
                  </template>
                </Column>
                <Column field="tax_rate" header="KDV Oranı">
                  <template #body="{ data }">
                    <div>
                      <div>{{ data.tax_rate }}%</div>
                    </div>
                  </template>
                </Column>
                <Column field="document_currency_code" header="Döviz Kodu">
                  <template #body="{ data }">
                    <div>
                      <div>{{ data.document_currency_code }}</div>
                    </div>
                  </template>
                </Column>
              </DataTable>
            </div>
            <div class="space-y-4 lg:hidden">
              <Card v-for="invoice in accountFormData.active_data.invoice" :key="invoice.id" class="border border-gray-100 shadow-sm">
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
                      <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-12">
                        <div class="text-sm font-medium text-gray-500">Fatura No</div>
                        <p class="text-sm font-medium">{{ invoice.invoice_id }}</p>
                      </div>
                      <div class="py-3 px-4 rounded-lg bg-gray-50 col-span-12 lg:col-span-12">
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
                        <p class="text-sm font-medium">
                          {{ useMoneyFormat(invoice.tax_amount) }} ({{ invoice.tax_rate }}%) {{ invoice.document_currency_code }}
                        </p>
                      </div>
                    </div>
                  </div>
                </template>
              </Card>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
      <div class="mt-6 flex lg:justify-between items-center gap-2">
        <div>
          <Button
            v-if="accountFormData.form_type == 'edit' && false"
            @click="handleDelete"
            class=""
            :disabled="new_customer_data.loading_status"
            label="Hesabı Sil"
            severity="danger"
            outlined
          />
        </div>
        <div>
          <Button
            @click="handleFormSubmit"
            :label="accountFormData.form_type == 'create' ? 'Oluştur' : 'Güncelle'"
            :loading="new_customer_data.loading_status"
            :disabled="new_customer_data.loading_status"
          />
        </div>
      </div>
    </div>
  </Drawer>
</template>

<script setup>
import { reactive, inject, watch, toRefs, ref } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import { Add01Icon, Delete02Icon, Menu01Icon } from "hugeicons-vue";
import { useVuelidate } from "@vuelidate/core";
import { required, email, helpers } from "@vuelidate/validators";
import { useMoneyFormat } from "@/composables/index.js";
import { define_datas, invoice_commercial_type, invoice_box_types } from "@/data/system_data.js";

const props = defineProps(["data", "accountFormData"]);

const emits = defineEmits(["formSubmit"]);
const DataStore = useDataStore();

const activeTab = ref("0");
const createInitialFormState = (data) => {
  return {
    name: data?.name ?? "",
    avatar: data?.avatar ? data.avatar : null,
    avatar_remove: false,
    account_type: data?.account_type_id ?? [],
    country: data?.country_id ?? null,
    city: data?.city_id ?? null,
    district: data?.district_id ?? null,
    phone_country: data?.phone_country_id ?? null,
    phone: data?.phone ?? "",
    address: data?.address ?? "",
    email: data?.email ?? "",
    tax_number: data?.tax_number ?? "",
    tax_office: data?.tax_office ?? "",
    contact_persons: data?.account_contact_person
      ? data.account_contact_person.map((person) => ({
          name: person.name,
          email: person.email,
        }))
      : [],
    discount: data?.discount ?? 0,
    charge_persons: data?.charge_person ?? [],
    accounting_code: data?.accounting_code ?? "",
    contact_language: data?.contact_language ? define_datas.account.contact_language.find((item) => item.id == data.contact_language.id) : null,
    individual_personal: data?.individual_personal ?? { label: "Tüzel", value: "T" },
    loading_status: false,
  };
};

const individual_personal_options = [
  { label: "Tüzel", value: "T" },
  { label: "Şahıs", value: "S" },
];

// Reactive form state
const new_customer_data = reactive(createInitialFormState(props.accountFormData.active_data));

const tabs = [
  {
    name: "Genel Bilgiler",
    value: "0",
    rules: {
      name: {
        required: helpers.withMessage("Hesap adı zorunludur.", required),
      },
      account_type: { required: helpers.withMessage("Hesap türü zorunludur.", required) },
      country: { required: helpers.withMessage("Ülke zorunludur.", required) },
    },
  },
  {
    name: "İletişim Bilgileri",
    value: "1",
    rules: {},
  },
];
let rules = {};
tabs.forEach((tab) => {
  rules = { ...rules, ...tab.rules };
});

const v$ = useVuelidate(rules, new_customer_data);

const handleFormSubmit = async () => {
  const validate_status = await v$.value.$validate();

  if (!validate_status) {
    let error_key = v$.value.$errors[0].$property;
    const errorTab = tabs.find((tab) => Object.keys(tab.rules).includes(error_key));
    if (errorTab) {
      activeTab.value = errorTab.value;
    }
    return;
  }

  new_customer_data.loading_status = true;

  const form_data = new FormData();
  if (new_customer_data.account_type instanceof Array) {
    new_customer_data.account_type.forEach((item, index) => {
      form_data.append(`account_type_mapping[]`, item.id);
    });
  }
  form_data.append("name", new_customer_data.name);
  form_data.append("country_id", new_customer_data.country.id);
  if (new_customer_data.city) {
    form_data.append("city_id", new_customer_data.city.id);
  }
  if (new_customer_data.district) {
    form_data.append("district_id", new_customer_data.district.id);
  }
  if (new_customer_data.phone_country) {
    form_data.append("phone_country_id", new_customer_data?.phone_country?.id ?? "");
  }
  if (new_customer_data.phone) {
    form_data.append("phone", new_customer_data.phone);
  }
  if (new_customer_data.address) {
    form_data.append("address", new_customer_data.address);
  }
  if (new_customer_data.email) {
    form_data.append("email", new_customer_data.email);
  }
  form_data.append("tax_number", new_customer_data.tax_number);
  form_data.append("tax_office", new_customer_data.tax_office?.id ?? "");
  form_data.append("discount", new_customer_data.discount);
  form_data.append("individual_personal", new_customer_data.individual_personal?.value ?? "");

  if (new_customer_data.contact_persons.length > 0) {
    new_customer_data.contact_persons.forEach((person, index) => {
      form_data.append(`contact_persons[${index}][name]`, person.name);
      form_data.append(`contact_persons[${index}][email]`, person.email);
    });
  }

  new_customer_data.charge_persons.forEach((item, index) => {
    form_data.append(`account_charge_person[]`, item.id);
  });

  if (new_customer_data.avatar && new_customer_data.avatar instanceof File) {
    form_data.append("avatar", new_customer_data.avatar);
  }
  if (new_customer_data.avatar_remove) {
    form_data.append("avatar_remove", 1);
  }

  if (new_customer_data.contact_language) {
    form_data.append("contact_language", new_customer_data.contact_language.id);
  }

  let res;
  if (props.accountFormData.form_type == "edit") {
    form_data.append("id", props.accountFormData.active_data.id);
    res = await DataStore.UPDATE_CUSTOMER({ data: form_data });
  } else {
    res = await DataStore.CREATE_CUSTOMER({ data: form_data });
  }
  if (res) {
    props.accountFormData.drawer_visible = false;
    emits("formSubmit", {
      account_type: new_customer_data.account_type,
    });
  }

  new_customer_data.loading_status = false;
};
const handleDelete = async () => {
  new_customer_data.loading_status = true;
  const res = await DataStore.DELETE_CUSTOMER({ id_list: [props.accountFormData.active_data.id] });
  if (res) {
    new_customer_data.loading_status = false;
    props.accountFormData.drawer_visible = false;
    emits("formSubmit", {
      account_type: new_customer_data.account_type,
    });
  }
};

const countryOnChange = () => {
  new_customer_data.city = null;
  new_customer_data.district = null;
};

const cityOnChange = () => {
  new_customer_data.district = null;
};

// Yeni fonksiyonları ekle:
const addContactPerson = () => {
  new_customer_data.contact_persons.push({
    name: "",
    email: "",
  });
};

const removeContactPerson = (index) => {
  new_customer_data.contact_persons.splice(index, 1);
};

const onDrawerShow = () => {
  activeTab.value = "0";
  if (props.accountFormData.form_type == "edit") {
    Object.assign(new_customer_data, createInitialFormState(props.accountFormData.active_data));
    if (props.accountFormData.active_data.user_account_mapping && props.accountFormData.active_data.user_account_mapping.length > 0) {
      props.accountFormData.active_data.user_account_mapping.forEach((item) => {
        new_customer_data.charge_persons.push(item.user_id);
      });
    }
    if (props.accountFormData.active_data.account_type_mapping_id && props.accountFormData.active_data.account_type_mapping_id.length > 0) {
      props.accountFormData.active_data.account_type_mapping_id.forEach((item) => {
        new_customer_data.account_type.push(item.account_type_id);
      });
    }
    if (props.accountFormData.active_data.account_type_mapping_id && props.accountFormData.active_data.individual_personal) {
      new_customer_data.individual_personal = individual_personal_options.find((item) => item.value == props.accountFormData.active_data.individual_personal);
    }
  }
};

const onDrawerHide = () => {
  Object.assign(new_customer_data, createInitialFormState(props.accountFormData));
  v$.value.$reset();
};
</script>
