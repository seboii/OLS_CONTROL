<template>
  <div class="lg:min-h-screen lg:w-screen flex items-center justify-center lg:bg-gray-100 max-lg:py-10">
    <div class="max-lg:container max-lg:w-full">
      <form @submit.prevent="handleSubmit">
        <div class="w-full lg:w-[440px] lg:rounded-xl overflow-hiden lg:border lg:shadow-xs p-4 lg:py-12 lg:px-8 bg-white">
          <div class="mb-6 lg:mb-12 space-y-4 lg:space-y-8 flex flex-col items-center">
            <div>
              <img src="/assets/media/logo/ols-icon-primary.svg" class="w-11 lg:w-14" />
            </div>
            <div class="text-center">
              <div class="text-2xl font-medium text-gray-700">Giriş Yap</div>
              <div class="text-xs text-gray-500 text-center">Giriş bilgilerinizi eksiksiz doldurunuz.</div>
            </div>
          </div>
          <div class="space-y-3">
            <div>
              <FloatLabel variant="on" class="w-full">
                <InputText v-model="login_data.email" variant="filled" type="text" fluid />
                <label>E-Posta</label>
              </FloatLabel>
            </div>
            <div>
              <FloatLabel variant="on" class="w-full">
                <Password v-model="login_data.password" variant="filled" type="password" toggleMask :feedback="false" fluid />
                <label>Şifre</label>
              </FloatLabel>
            </div>
          </div>
          <div class="mt-6">
            <Button type="submit" :disabled="login_data.loading" :loading="login_data.loading" label="Giriş Yap" fluid />
          </div>
        </div>
      </form>
      <div class="mt-8 lg:mt-16">
        <div class="flex items-center justify-center gap-2">
          <img src="/assets/media/logo/jeetwork.png" class="w-16 lg:w-16 opacity-50" />
          <div class="text-gray-500">tarafından geliştirilmiştir.</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { toast } from "vue-sonner";
import axios from "axios";
import { reactive, inject } from "vue";
import Cookies from "js-cookie";
const login_data = reactive({
  email: "",
  password: "",
  loading: false,
});

const handleSubmit = async () => {
  if (!login_data.email || !login_data.password) {
    toast.error("Lütfen tüm alanları doldurun.");
    return;
  }
  login_data.loading = true;
  try {
    const res = await axios.post("/api/v1/login", {
      email: login_data.email,
      password: login_data.password,
    });
    let token = res.data.data.token;
    Cookies.set("token", token, { expires: 7 });
    if (token) {
      window.location.href = "/panel";
    } else {
      toast.error("Giriş bilgileri hatalı.");
    }
  } catch (error) {
    toast.error("Giriş bilgileri hatalı.");
    console.log(error);
  }
  login_data.loading = false;
};
</script>
