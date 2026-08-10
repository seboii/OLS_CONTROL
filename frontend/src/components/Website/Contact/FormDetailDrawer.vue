<template>
  <Drawer v-model:visible="userState.drawer_visible" header=" " position="bottom" class="min-h-svh" @show="onDrawerShow" @hide="onDrawerHide">
    <div class="lg:mx-auto lg:max-w-(--breakpoint-sm)">
      <div class="flex justify-between gap-4">
        <div>
          <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">Destek Talebi</div>
        </div>
      </div>
      <div v-if="userState.loading_status" class="flex items-center justify-center pt-20">
        <ProgressSpinner class="size-14!" />
      </div>
      <div v-else>
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-x-8 gap-y-8 mt-8">
          <div>
            <div class="mb-2 font-medium text-gray-500 py-2.5 px-5 bg-gray-100 rounded-lg">Kullanıcı</div>
            <div>{{ page_data.first_name }} {{ page_data.last_name }}</div>
          </div>
          <div>
            <div class="mb-2 font-medium text-gray-500 py-2.5 px-5 bg-gray-100 rounded-lg">E-Posta</div>
            <div>{{ page_data.email }}</div>
          </div>
          <div>
            <div class="mb-2 font-medium text-gray-500 py-2.5 px-5 bg-gray-100 rounded-lg">Telefon Numarası</div>
            <div>{{ page_data.phone }}</div>
          </div>
          <div>
            <div class="mb-2 font-medium text-gray-500 py-2.5 px-5 bg-gray-100 rounded-lg">Telefon Numarası</div>
            <div>{{ new Date(page_data.created_at).toLocaleString() }}</div>
          </div>
          <div class="lg:col-span-2">
            <div class="mb-2 font-medium text-gray-500 py-2.5 px-5 bg-gray-100 rounded-lg">Yanıtlanma Durumu</div>
            <Select
              v-model="page_data.is_answered"
              :options="answeredSelectOptions"
              optionLabel="title"
              placeholder="Yanıtlanma Durumu"
              class="w-full"
              dataKey="value"
              @change="handleAnsweredStatusChange"
            />
          </div>
          <div class="lg:col-span-2">
            <div class="mb-2 font-medium text-gray-500 py-2.5 px-5 bg-gray-100 rounded-lg">Mesaj</div>
            <div class="whitespace-pre-line">{{ page_data.message }}</div>
          </div>
        </div>
      </div>
    </div>
  </Drawer>
</template>

<script setup>
import { reactive, inject, watch, toRefs, ref } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import { useVuelidate } from "@vuelidate/core";
import { required, email, minLength, helpers } from "@vuelidate/validators";
import { toast } from "vue-sonner";

const props = defineProps(["data", "userState"]);

const emits = defineEmits(["formSubmit"]);
const DataStore = useDataStore();
const { base_url } = inject("base_url");

const activeTab = ref("0");

const answeredSelectOptions = [
  { value: 0, title: "Yanıtlanmadı" },
  { value: 1, title: "Yanıtlandı" },
];

const page_data = reactive({
  first_name: "",
  last_name: "",
  email: "",
  phone: null,
  message: "",
  is_answered: "",
  is_read: "",
  created_at: "",
  loading_status: false,
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

const onDrawerShow = () => {
  if (props.userState.form_type == "edit") {
    page_data.first_name = props.userState.active_data.first_name;
    page_data.last_name = props.userState.active_data.last_name;
    page_data.email = props.userState.active_data.email;
    page_data.phone = props.userState.active_data.phone;
    page_data.message = props.userState.active_data.message;
    page_data.is_read = props.userState.active_data.is_read;
    page_data.is_answered = answeredSelectOptions.find((x) => x.value == props.userState.active_data.is_answered);
    page_data.created_at = props.userState.active_data.created_at;
  } else {
    page_data.first_name = "";
    page_data.last_name = "";
    page_data.email = "";
    page_data.phone = "";
    page_data.message = "";
    page_data.is_read = "";
    page_data.is_answered = "";
  }
};

const handleAnsweredStatusChange = async () => {
  const res = await DataStore.UPDATE_WEBSITE_CONTACT_FORM_ANSWERED_STATUS({
    id: props.userState.active_data.id,
    is_answered: page_data.is_answered.value,
  });
  if (res) {
    toast.success("Yanıtlanma durumu güncellendi.");
    emits("formSubmit");
  }
};

const onDrawerHide = () => {
  v$.value.$reset();
  activeTab.value = "0";
};
</script>
