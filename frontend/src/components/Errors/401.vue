<template>
  <section
    :class="{
      'h-full': !size,
    }"
    class="flex items-center justify-center"
  >
    <div class="py-8 px-4 mx-auto max-w-(--breakpoint-xl) lg:py-16 lg:px-6">
      <div class="mx-auto max-w-(--breakpoint-sm) text-center">
        <h1
          v-if="!size"
          :class="{
            'text-7xl lg:text-9xl': !size,
            'text-5xl lg:text-7xl': size == 'small' || size,
          }"
          class="mb-4 tracking-tight font-extrabold text-primary"
        >
          401
        </h1>
        <p
          :class="{
            'text-3xl md:text-4xl mb-4': !size,
            'text-xl lg:text-2xl mb-1': size == 'small' || size,
          }"
          class="tracking-tight font-bold text-gray-900"
        >
          Yetkiniz Bulunamadı
        </p>
        <p
          :class="{
            'text-lg': !size,
            'text-base': size == 'small' || size,
          }"
          class="mb-4 font-light text-gray-500"
        >
          Üzgünüz, erişmek istediğiniz sayfaya yetkiniz bulunmamaktadır.
        </p>
        <router-link v-if="!size" to="/panel">
          <Button label="Anasayfa" />
        </router-link>
      </div>
    </div>
  </section>
</template>

<script setup>
const props = defineProps(["size"]);
</script>
