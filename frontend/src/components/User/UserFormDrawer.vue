<template>
  <Drawer v-model:visible="userState.drawer_visible" header=" " position="bottom" class="min-h-svh" @show="onDrawerShow" @hide="onDrawerHide">
    <div class="lg:mx-auto lg:max-w-(--breakpoint-sm)">
      <div class="flex justify-between gap-4">
        <div>
          <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">
            {{ userState.form_type == "create" ? "Yeni Kullanıcı Oluştur" : "Kullanıcı Bilgileri" }}
          </div>
          <p class="text-sm text-gray-500">
            {{
              userState.form_type == "create"
                ? "Bilgileri eksiksiz ve doğru doldurmaya özen gösteriniz."
                : "Kullanıcı bilgilerini inceleyebilir ve güncelleme yapabilirsiniz."
            }}
          </p>
        </div>
      </div>
      <div v-if="userState.loading_status" class="flex items-center justify-center pt-20">
        <ProgressSpinner class="size-14!" />
      </div>
      <div v-else>
        <div>
          <Tabs v-model:value="activeTab" class="mt-6!" lazy scrollable>
            <TabList>
              <Tab value="0">Genel Bilgiler</Tab>
              <Tab value="1">İletişim Bilgileri</Tab>
              <Tab v-if="props.userState.form_type == 'edit'" value="2">Rol Ayarları</Tab>
            </TabList>
            <TabPanels class="px-0! pt-10!">
              <TabPanel value="0">
                <div class="flex items-center flex-col gap-4">
                  <AvatarFile v-model="page_data.avatar" v-model:image-remove="page_data.avatar_remove" preview-path="/storage" size="128" />
                  <div class="grid lg:grid-cols-2 gap-y-5 gap-x-5 mt-4 w-full">
                    <div>
                      <FloatLabel variant="on" class="w-full">
                        <InputText v-model="page_data.name" :invalid="v$.name.$error" autocomplete="off" fluid />
                        <label>Adı</label>
                      </FloatLabel>
                      <InputErrorMessage :errors="v$.name.$errors" />
                    </div>
                    <div>
                      <FloatLabel variant="on" class="w-full">
                        <InputText v-model="page_data.surname" :invalid="v$.surname.$error" autocomplete="off" fluid />
                        <label>Soyadı</label>
                      </FloatLabel>
                      <InputErrorMessage :errors="v$.surname.$errors" />
                    </div>
                    <div>
                      <FloatLabel variant="on" class="w-full">
                        <Password v-model="page_data.password" :invalid="v$.password?.$error" autocomplete="off" fluid />
                        <label>Şifre</label>
                      </FloatLabel>
                      <InputErrorMessage v-if="v$.password" :errors="v$.password.$errors" />
                    </div>
                    <div>
                      <FloatLabel variant="on" class="w-full">
                        <Password
                          v-model="page_data.password_confirmation"
                          :invalid="v$.password_confirmation?.$error"
                          autocomplete="off"
                          :feedback="false"
                          fluid
                        />
                        <label>Şifre Tekrar</label>
                      </FloatLabel>
                      <InputErrorMessage v-if="v$.password_confirmation" :errors="v$.password_confirmation.$errors" />
                    </div>
                    <div>
                      <FloatLabel variant="on" class="w-full">
                        <InputText v-model="page_data.pkds_id" autocomplete="off" fluid />
                        <label>PDKS Numarası</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
              </TabPanel>
              <TabPanel value="1">
                <div class="grid grid-cols-1 gap-y-5 gap-x-5 mt-4 w-full">
                  <div>
                    <FloatLabel variant="on" class="w-full">
                      <InputText v-model="page_data.email" :invalid="v$.email.$error" type="email" autocomplete="off" fluid />
                      <label>E-Posta</label>
                    </FloatLabel>
                    <InputErrorMessage :errors="v$.email.$errors" />
                  </div>
                  <div class="flex items-center gap-2">
                    <FloatLabel variant="on">
                      <SelectAjax
                        v-model="page_data.phone_country"
                        :api="`/api/v1/country`"
                        searchKey="phone_code"
                        optionLabel="phone_code"
                        class="w-full lg:w-40"
                        filter
                      >
                        <template #value="slotProps">
                          <div v-if="slotProps.value" class="flex items-center gap-2">
                            <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                              <img
                                v-if="slotProps.value.country_code"
                                :src="`https://flagcdn.com/w160/${slotProps.value.country_code.toLowerCase()}.png`"
                                class="w-full h-full object-cover"
                              />
                            </div>
                            <div>+{{ slotProps.value.phone_code }}</div>
                          </div>
                          <span v-else> {{ slotProps.placeholder }} </span>
                        </template>
                        <template #option="slotProps">
                          <div class="flex items-center gap-2">
                            <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                              <img
                                v-if="slotProps.option.country_code"
                                :src="`https://flagcdn.com/w160/${slotProps.option.country_code.toLowerCase()}.png`"
                                class="w-full h-full object-cover"
                              />
                            </div>
                            <div>+{{ slotProps.option.phone_code }}</div>
                          </div>
                        </template>
                      </SelectAjax>
                      <label for="username">Ülke Kodu</label>
                    </FloatLabel>
                    <FloatLabel variant="on" class="w-full">
                      <InputText v-model="page_data.phone" autocomplete="off" fluid />
                      <label>Telefon Numarası</label>
                    </FloatLabel>
                  </div>
                </div>
              </TabPanel>
              <TabPanel value="2">
                <UserRole
                  v-if="usePermissionStatus('role_management').read || userState.active_data.id == DataStore.user.data.id"
                  :user-id="userState.active_data.id"
                />
                <NoPermissionPage v-else size="small" />
              </TabPanel>
            </TabPanels>
          </Tabs>
          <div v-if="activeTab == '0' || activeTab == '1'" class="mt-6 flex lg:justify-between items-center gap-2">
            <Button
              v-if="userState.form_type == 'edit'"
              @click="handleDelete"
              size="small"
              :loading="page_data.loading_status"
              :disabled="page_data.loading_status"
              label="Hesabı Sil"
              severity="danger"
              outlined
            />
            <Button @click="handleFormSubmit" size="small" :loading="page_data.loading_status" :disabled="page_data.loading_status">
              {{ userState.form_type == "create" ? "Oluştur" : "Güncelle" }}
            </Button>
          </div>
        </div>
      </div>
    </div>
  </Drawer>
</template>

<script setup>
import { reactive, inject, watch, toRefs, ref } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import { Menu01Icon } from "hugeicons-vue";
import { useVuelidate } from "@vuelidate/core";
import { required, email, minLength, helpers } from "@vuelidate/validators";
import { usePermissionStatus } from "@/composables/index.js";

import NoPermissionPage from "@/components/Errors/401.vue";
import UserRole from "@/components/User/UserRole.vue";

const props = defineProps(["data", "userState"]);

const emits = defineEmits(["formSubmit"]);
const DataStore = useDataStore();
const { base_url } = inject("base_url");

const activeTab = ref("0");

const page_data = reactive({
  name: "",
  surname: "",
  email: "",
  phone_country: null,
  phone: "",
  password: "",
  password_confirmation: "",
  avatar: null,
  avatar_remove: false,
  loading_status: false,
  pkds_id: null,
});

let rules = {
  name: {
    required: helpers.withMessage("Ad zorunludur.", required),
  },
  surname: {
    required: helpers.withMessage("Soyad zorunludur.", required),
  },
  email: {
    required: helpers.withMessage("E-posta zorunludur.", required),
    email: helpers.withMessage("Geçerli bir e-posta adresi giriniz.", email),
  },
};

const v$ = useVuelidate(rules, page_data);

const handleFormSubmit = async () => {
  const validate_status = await v$.value.$validate();

  if (!validate_status) {
    return;
  }

  const form_data = new FormData();
  form_data.append("name", page_data.name);
  form_data.append("surname", page_data.surname);
  form_data.append("email", page_data.email);
  form_data.append("phone", page_data.phone);
  if (page_data.phone_country) {
    form_data.append("phone_country_id", page_data.phone_country.id);
  }
  if (page_data.password) {
    form_data.append("password", page_data.password);
    form_data.append("password_confirmation", page_data.password_confirmation);
  }

  if (page_data.avatar && page_data.avatar instanceof File) {
    form_data.append("avatar", page_data.avatar);
  }
  if (page_data.avatar_remove) {
    form_data.append("avatar_remove", 1);
  }
  if (page_data.pkds_id) {
    form_data.append("pkds_id", page_data.pkds_id);
  }

  let res;
  if (props.userState.form_type == "edit") {
    form_data.append("id", props.userState.active_data.id);
    res = await DataStore.UPDATE_USER({ data: form_data });
    if (res && props.userState.active_data.id == DataStore.user.data.id) {
      DataStore.user.data = res.data;
    }
  } else {
    res = await DataStore.CREATE_USER({ data: form_data });
  }
  if (res) {
    page_data.loading_status = false;
    props.userState.drawer_visible = false;
    emits("formSubmit");
  }
};
const handleDelete = async () => {
  const res = await DataStore.DELETE_USER({ id_list: [props.userState.active_data.id] });
  if (res) {
    page_data.loading_status = false;
    props.userState.drawer_visible = false;
    emits("formSubmit", {
      account_type: page_data.account_type,
    });
  }
};

const onDrawerShow = () => {
  if (props.userState.form_type == "edit") {
    page_data.name = props.userState.active_data.name;
    page_data.surname = props.userState.active_data.surname;
    page_data.email = props.userState.active_data.email;
    page_data.phone_country = props.userState.active_data.phone_country_id;
    page_data.phone = props.userState.active_data.phone;
    page_data.avatar = props.userState.active_data.avatar;
    page_data.avatar_remove = false;
    page_data.pkds_id = props.userState.active_data.pkds_id;
    rules.password = { required: helpers.withMessage("Şifre zorunludur.", required) };
    rules.password_confirmation = { required: helpers.withMessage("Şifre Tekrar zorunludur.", required) };
  } else {
    page_data.name = "";
    page_data.surname = "";
    page_data.email = "";
    page_data.phone_country = null;
    page_data.phone = "";
    page_data.password = "";
    page_data.password_confirmation = "";
    page_data.avatar = null;
    page_data.avatar_remove = false;
    page_data.pkds_id = null;
  }
};

const onDrawerHide = () => {
  v$.value.$reset();
  activeTab.value = "0";
};
</script>
