<template>
  <div>
    <div class="max-lg:container">
      <div class="mb-8 flex justify-between items-center gap-6">
        <div>
          <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Hesabım</h2>
          <div class="text-gray-500 text-sm mt-1">Kişisel bilgilerinizi ve hesap ayarlarınızı yönetebilirsiniz.</div>
        </div>
      </div>
    </div>
    <div class="grid grid-cols-1 xl:grid-cols-3 gap-4">
      <AccountGeneral />
      <AccountContact />
      <AccountSecurity />
      <ThemeSetting />
    </div>
  </div>
</template>

<script setup>
import AccountGeneral from "@/components/Account/AccountGeneral.vue";
import AccountContact from "@/components/Account/AccountContact.vue";
import AccountSecurity from "@/components/Account/AccountSecurity.vue";
import ThemeSetting from "@/components/Account/ThemeSetting.vue";
</script>
