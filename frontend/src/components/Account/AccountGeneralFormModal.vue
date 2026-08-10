<template>
  <Dialog
    v-model:visible="general_form_modal_status"
    pt:header:class="pb-0!"
    modal
    header=" "
    class="w-full! lg:w-[400px]!"
    @show="onModalShow"
    @hide="onModalHide"
  >
    <div>
      <div class="text-xl lg:text-2xl font-medium text-black mb-1">Profil</div>
      <p class="text-xs text-gray-500">Profil bilgilerinizi doğru ve eksiksiz girmeye özen gösteriniz.</p>
    </div>
    <div class="space-y-8 mt-8">
      <div class="space-y-4 lg:space-y-6">
        <div>
          <AvatarFile v-model="general_form.avatar" v-model:image-remove="general_form.avatar_remove" preview-path="/storage" rounded-sm size="128" />
        </div>
        <div>
          <FloatLabel variant="on" class="w-full">
            <InputText v-model="general_form.name" :invalid="v$.name.$error" type="text" fluid />
            <label>Ad</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.name.$errors" />
        </div>
        <div>
          <FloatLabel variant="on" class="w-full">
            <InputText v-model="general_form.surname" :invalid="v$.surname.$error" type="text" fluid />
            <label>Soyad</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.surname.$errors" />
        </div>
        <div>
          <FloatLabel variant="on">
            <SelectAjax v-model="general_form.country" :api="`/api/v1/country`" optionLabel="name" :invalid="v$.country.$error" class="w-full" filter>
              <template #value="slotProps">
                <div v-if="slotProps.value" class="flex items-center gap-2">
                  <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                    <img
                      v-if="slotProps.value.country_code"
                      :src="`https://flagcdn.com/w160/${slotProps.value.country_code.toLowerCase()}.png`"
                      class="w-full h-full object-cover"
                    />
                  </div>
                  <div>{{ slotProps.value.name }}</div>
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
                  <div>{{ slotProps.option.name }}</div>
                </div>
              </template>
            </SelectAjax>
            <label for="username">Ülke</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.country.$errors" />
        </div>
      </div>
    </div>
    <template #footer>
      <Button size="small" :disabled="general_form.loading" type="button" label="İptal" @click="general_form_modal_status = false" outlined></Button>
      <Button size="small" :disabled="general_form.loading" :loading="general_form.loading" type="button" label="Kaydet" @click="handleForm"></Button>
    </template>
  </Dialog>
</template>

<script setup>
import { inject, reactive } from "vue";
import { useVuelidate } from "@vuelidate/core";
import { required, helpers } from "@vuelidate/validators";
import { useDataStore } from "@/stores/data_store.js";
const emits = defineEmits(["formSubmit"]);
const DataStore = useDataStore();

let rules = {
  name: {
    required: helpers.withMessage("İsim alanı boş bırakılamaz.", required),
  },
  surname: {
    required: helpers.withMessage("Soyisim alanı boş bırakılamaz.", required),
  },
  country: {
    required: helpers.withMessage("Ülke alanı boş bırakılamaz.", required),
  },
};
const general_form_modal_status = inject("general_form_modal_status");
const general_form = reactive({
  name: "",
  surname: "",
  country: "",
  avatar: null,
  avatar_remove: false,
  loading: false,
});

const v$ = useVuelidate(rules, general_form);

const handleForm = async () => {
  const validate_status = await v$.value.$validate();

  if (validate_status) {
    general_form.loading = true;

    const form_data = new FormData();
    form_data.append("name", general_form.name);
    form_data.append("surname", general_form.surname);
    form_data.append("country_id", general_form.country.id);
    if (general_form.avatar && general_form.avatar instanceof File) {
      form_data.append("avatar", general_form.avatar);
    }
    if (general_form.avatar_remove) {
      form_data.append("avatar_remove", 1);
    }

    await DataStore.UPDATE_PROFILE_GENERAL({ data: form_data });
    emits("formSubmit");

    general_form.loading = false;
    general_form_modal_status.value = false;
  }
};

const onModalShow = () => {
  general_form.name = DataStore.user.data.name;
  general_form.surname = DataStore.user.data.surname;
  general_form.country = DataStore.user.data.country_id;
  general_form.avatar = DataStore.user.data.avatar;
  general_form.avatar_remove = false;
};

const onModalHide = () => {
  v$.value.$reset();
  general_form.name = "";
  general_form.surname = "";
  general_form.country = "";
  general_form.avatar = null;
  general_form.avatar_remove = false;
};
</script>
