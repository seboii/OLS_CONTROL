<template>
  <span
    class="block px-2.5 py-1 text-xs"
    :class="[
      severityClass,
      {
        'border border-current bg-transparent': outlined,
        'shadow-xs': raised,
        'rounded-full': rounded,
        'rounded-md': !rounded,
      },
    ]"
  >
    <slot></slot>
  </span>
</template>

<script setup>
import { computed } from "vue";

const props = defineProps({
  severity: {
    type: String,
    default: "primary",
    validator: (value) => ["primary", "secondary", "success", "danger", "info", "warn"].includes(value),
  },
  outlined: {
    type: Boolean,
    default: false,
  },
  raised: {
    type: Boolean,
    default: false,
  },
  rounded: {
    type: Boolean,
    default: false,
  },
});

const severityClass = computed(() => {
  if (props.outlined) {
    return {
      "text-primary-700": props.severity === "primary",
      "text-gray-700": props.severity === "secondary",
      "text-green-700": props.severity === "success",
      "text-red-700": props.severity === "danger",
      "text-cyan-700": props.severity === "info",
      "text-yellow-700": props.severity === "warn",
    };
  }

  return {
    "bg-primary-100 text-primary-800": props.severity === "primary",
    "bg-gray-100 text-gray-700": props.severity === "secondary",
    "bg-green-100 text-green-800": props.severity === "success",
    "bg-red-100 text-red-800": props.severity === "danger",
    "bg-cyan-100 text-cyan-800": props.severity === "info",
    "bg-yellow-100 text-yellow-800": props.severity === "warn",
  };
});
</script>
