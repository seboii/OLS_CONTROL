<template>
  <span
    ref="text_ref"
    @click="copyText"
    class="cursor-pointer"
    v-tooltip="{
      content: 'Kopyala',
      delay: 100,
    }"
  >
    <slot></slot>
  </span>
</template>

<script setup>
import { ref } from "vue";
import { toast } from "vue-sonner";

const text_ref = ref(null);

const copyText = () => {
  let text = text_ref.value.innerHTML;
  navigator.clipboard
    .writeText(text)
    .then(() => {
      toast.success("Metin kopyalandı");
    })
    .catch(() => {
      toast.error("Metin kopyalanırken bir hata oluştu");
    });
};
</script>
