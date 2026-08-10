<template>
  <span>{{ displayedNumber }}</span>
</template>

<script setup>
import { ref, watch, onMounted } from "vue";

const props = defineProps({
  number: {
    type: [Number, String],
  },
  duration: {
    type: Number,
    default: 500, // Varsayılan süre 1 saniye
  },
});

const displayedNumber = ref(0);
let startTime = null;
let startNumber = 0;

const startAnimation = (newNumber = 0) => {
  startTime = performance.now();
  startNumber = displayedNumber.value;
  const animationStep = (timestamp) => {
    if (!startTime) startTime = timestamp;
    const progress = Math.min((timestamp - startTime) / props.duration, 1);
    displayedNumber.value = Math.floor(startNumber + (newNumber - startNumber) * progress);
    if (progress < 1) {
      requestAnimationFrame(animationStep);
    } else {
      displayedNumber.value = newNumber;
    }
  };
  requestAnimationFrame(animationStep);
};

watch(
  () => props.number,
  (newVal) => {
    startAnimation(newVal);
  },
  { immediate: true }
);
</script>

<style scoped>
span {
  transition: all 0.3s ease;
}
</style>
