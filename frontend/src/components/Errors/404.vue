<template>
  <section class="bg-white flex items-center justify-center h-screen">
    <div class="py-8 px-4 mx-auto max-w-(--breakpoint-xl) lg:py-16 lg:px-6">
      <div class="mx-auto max-w-(--breakpoint-sm) text-center">
        <h1 class="mb-4 text-7xl tracking-tight font-extrabold lg:text-9xl text-primary">404</h1>
        <p class="mb-4 text-3xl tracking-tight font-bold text-gray-900 md:text-4xl">Sayfa bulunamadı</p>
        <p class="mb-4 text-lg font-light text-gray-500">Üzgünüz, aradığınız sayfayı bulamadık.</p>
        <router-link to="/panel">
          <Button label="Anasayfa" />
        </router-link>
      </div>
    </div>
  </section>
</template>
