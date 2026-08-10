<template>
  <div>
    <div @click="toggle" class="inline-block popover-trigger">
      <slot name="trigger"></slot>
    </div>
    <Popover v-bind="$attrs" ref="op">
      <slot name="content"></slot>
    </Popover>
  </div>
</template>

<script setup>
import { ref } from "vue";

const op = ref();
const toggle = (event) => {
  op.value.toggle(event);
};
</script>

<style>
.popover-content {
  background-color: white;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  border-radius: 4px;
  padding: 1rem;
  min-width: 200px;
}
</style>
