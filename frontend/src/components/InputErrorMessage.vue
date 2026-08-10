<template>
  <div v-if="errors && errors.length > 0" class="text-red-500 text-xs mt-1">
    <template v-if="Array.isArray(errors)">
      <div v-for="error in errors" :key="error">{{ error.$message }}</div>
    </template>
    <template v-else>
      {{ errors.$message }}
    </template>
  </div>
</template>

<script setup>
const props = defineProps(["errors"]);
</script>
