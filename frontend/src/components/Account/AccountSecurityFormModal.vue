<template>
  <Dialog v-model:visible="security_form_modal_status" pt:header:class="pb-0!" modal header=" " class="w-full! lg:w-[500px]!" @hide="onModalHide">
    <div>
      <div class="text-xl lg:text-2xl font-medium text-black mb-1">Güvenlik</div>
      <p class="text-xs text-gray-500">Güvenlik bilgilerinizi doğru ve eksiksiz girmeye özen gösteriniz.</p>
    </div>
    <div class="space-y-8 mt-8">
      <div class="space-y-3">
        <div>
          <FloatLabel variant="on" class="w-full">
            <InputText v-model="contact_form.current_password" :invalid="v$.current_password.$error" type="password" autocomplete="off" fluid />
            <label>Mevcut Şifreniz</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.current_password.$errors" />
        </div>
        <div>
          <FloatLabel variant="on" class="w-full">
            <InputText v-model="contact_form.new_password" :invalid="v$.new_password.$error" type="password" autocomplete="off" fluid />
            <label>Yeni Şifre</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.new_password.$errors" />
        </div>
        <div>
          <FloatLabel variant="on" class="w-full">
            <InputText
              v-model="contact_form.new_password_confirmation"
              :invalid="v$.new_password_confirmation.$error"
              type="password"
              autocomplete="off"
              fluid
            />
            <label>Yeni Şifre Tekrar</label>
          </FloatLabel>
          <InputErrorMessage :errors="v$.new_password_confirmation.$errors" />
        </div>
      </div>
    </div>
    <template #footer>
      <Button size="small" :disabled="contact_form.loading" type="button" label="İptal" @click="security_form_modal_status = false" outlined></Button>
      <Button size="small" :disabled="contact_form.loading" :loading="contact_form.loading" type="button" label="Kaydet" @click="handleForm"></Button>
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
  current_password: {
    required: helpers.withMessage("Mevcut alanı boş bırakılamaz.", required),
  },
  new_password: {
    required: helpers.withMessage("Yeni şifre alanı boş bırakılamaz.", required),
  },
  new_password_confirmation: {
    required: helpers.withMessage("Yeni şifre onaylama alanı boş bırakılamaz.", required),
  },
};
const security_form_modal_status = inject("security_form_modal_status");
const contact_form = reactive({
  current_password: "",
  new_password: "",
  new_password_confirmation: "",
  loading: false,
});

const v$ = useVuelidate(rules, contact_form);

const handleForm = async () => {
  const validate_status = await v$.value.$validate();

  if (validate_status) {
    contact_form.loading = true;

    const form_data = new FormData();
    form_data.append("current_password", contact_form.current_password);
    form_data.append("new_password", contact_form.new_password);
    form_data.append("new_password_confirmation", contact_form.new_password_confirmation);

    await DataStore.UPDATE_PROFILE_PASSWORD({ data: form_data });
    emits("formSubmit");

    contact_form.loading = false;
    security_form_modal_status.value = false;
  }
};

const onModalHide = () => {
  v$.value.$reset();
  contact_form.current_password = "";
  contact_form.new_password = "";
  contact_form.new_password_confirmation = "";
};
</script>
