<template>
  <Dialog
    v-model:visible="contact_form_modal_status"
    pt:header:class="pb-0!"
    modal
    header=" "
    class="w-full! lg:w-[500px]!"
    @show="onModalShow"
    @hide="onModalHide"
  >
    <div>
      <div class="text-xl lg:text-2xl font-medium text-black mb-1">İletişim Bilgileri</div>
      <p class="text-xs text-gray-500">İletişim bilgilerinizi doğru ve eksiksiz girmeye özen gösteriniz.</p>
    </div>
    <div class="space-y-8 mt-8">
      <div class="space-y-4 lg:space-y-6">
        <div>
          <FloatLabel variant="on" class="w-full">
            <InputText v-model="contact_form.email" :invalid="v$.email.$error" type="email" autocomplete="off" fluid />
            <label>E-Posta</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.email.$errors" />
        </div>
        <div class="flex gap-2">
          <div>
            <FloatLabel variant="on">
              <SelectAjax
                v-model="contact_form.phone_country"
                :api="`/api/v1/country`"
                searchKey="phone_code"
                optionLabel="phone_code"
                :invalid="v$.phone_country.$error"
                class="w-full lg:w-44"
                filter
              >
                <template #value="slotProps">
                  <div v-if="slotProps.value" class="flex items-center gap-2">
                    <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                      <img
                        v-if="lotProps.value.country_code"
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
            <InputErrorMessage :errors="v$.phone_country.$errors" />
          </div>
          <div class="w-full">
            <FloatLabel variant="on" class="w-full">
              <InputText v-model="contact_form.phone" :invalid="v$.phone.$error" autocomplete="off" fluid v-keyfilter.int />
              <label>Telefon Numarası</label>
            </FloatLabel>
            <InputErrorMessage :errors="v$.phone.$errors" />
          </div>
        </div>
      </div>
    </div>
    <template #footer>
      <Button size="small" :disabled="contact_form.loading" type="button" label="İptal" @click="contact_form_modal_status = false" outlined></Button>
      <Button size="small" :disabled="contact_form.loading" :loading="contact_form.loading" type="button" label="Kaydet" @click="handleForm"></Button>
    </template>
  </Dialog>
</template>

<script setup>
import { inject, reactive } from "vue";
import { useVuelidate } from "@vuelidate/core";
import { required, email, minLength, helpers, integer } from "@vuelidate/validators";
import { useDataStore } from "@/stores/data_store.js";
const emits = defineEmits(["formSubmit"]);
const DataStore = useDataStore();

let rules = {
  email: {
    required: helpers.withMessage("E-Posta alanı boş bırakılamaz.", required),
    email: helpers.withMessage("Geçerli bir e-posta adresi giriniz.", email),
  },
  phone: {
    required: helpers.withMessage("Telefon alanı boş bırakılamaz.", required),
    integer: helpers.withMessage("Telefon numarası sadece rakamlardan oluşmalıdır.", integer),
  },
  phone_country: {
    required: helpers.withMessage("Ülke kodu boş bırakılamaz.", required),
  },
};
const contact_form_modal_status = inject("contact_form_modal_status");
const contact_form = reactive({
  phone: "",
  phone_country: "",
  email: "",
  loading: false,
});

const v$ = useVuelidate(rules, contact_form);

const handleForm = async () => {
  const validate_status = await v$.value.$validate();

  if (validate_status) {
    contact_form.loading = true;

    const form_data = new FormData();
    form_data.append("email", contact_form.email);
    form_data.append("phone", contact_form.phone);
    form_data.append("phone_country_id", contact_form.phone_country.id);

    await DataStore.UPDATE_PROFILE_CONTACT({ data: form_data });
    emits("formSubmit");

    contact_form.loading = false;
    contact_form_modal_status.value = false;
  }
};

const onModalShow = () => {
  contact_form.email = DataStore.user.data.email;
  contact_form.phone = DataStore.user.data.phone;
  contact_form.phone_country = DataStore.user.data.phone_country_id;
};
const onModalHide = () => {
  v$.value.$reset();
  contact_form.email = "";
  contact_form.phone = "";
  contact_form.phone_country = "";
};
</script>
