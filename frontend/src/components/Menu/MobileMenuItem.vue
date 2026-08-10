<template>
  <div>
    <button
      :style="{
        opacity: GeneralStore.mobile_bottom_menu.menu_in_visible ? 1 : 0,
        transitionDelay: `${depth * 50}ms`,
        transitionDuration: GeneralStore.mobile_bottom_menu.menu_in_visible ? '400ms' : '0ms',
        transform: GeneralStore.mobile_bottom_menu.menu_in_visible ? 'translateY(0)' : 'translateY(20px)',
      }"
      :class="{
        'py-1': props.depth > 0,
        'py-2': props.depth == 0,
      }"
      class="w-full rounded-lg flex items-center justify-between gap-2 px-6"
      v-ripple
    >
      <div @click="handleItemClick(item)" class="flex items-center gap-2 grow">
        <div class="size-11 bg-white rounded-lg flex items-center justify-center">
          <component v-if="item.icon" :is="item.icon" size="20" class="text-primary" />
        </div>
        <div
          :class="{
            'text-lg text-gray-700': props.depth == 0,
            'text-base text-gray-600': props.depth > 0,
          }"
          class="tracking-tight flex items-center justify-between gap-4"
        >
          {{ item.name }}
          <div class="relative">
            <span v-if="'count' in item" class="shrink-0 size-5 rounded-full bg-black text-white text-xs flex items-center justify-center">{{
              item.count
            }}</span>
            <div v-if="item.count > 0" class="w-full h-full absolute top-0 right-0 size-5 rounded-full bg-black animate-ping"></div>
          </div>
        </div>
      </div>
      <button v-if="hasChildren" @click.prevent="toggleChildren" class="size-7 flex items-center justify-center aspect-square h-full bg-gray-50 rounded-md">
        <ArrowRight01Icon
          :class="{
            'transform rotate-90': isExpanded,
            'transform rotate-0': !isExpanded,
          }"
          class="transition-transform"
          size="18"
        />
      </button>
    </button>

    <Transition name="child-menu-transition">
      <div v-if="hasChildren && isExpanded" :class="childrenWrapperClass">
        <MobileMenuItem v-for="child in item.children" :key="child.id" :item="child" :depth="depth + 1" @menu-click="handleChildClick" />
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useGeneralStore } from "@/stores/general_store.js";
import { ArrowRight01Icon } from "hugeicons-vue";

const props = defineProps({
  item: {
    type: Object,
    required: true,
  },
  depth: {
    type: Number,
    default: 0,
  },
});

const emit = defineEmits(["menu-click"]);
const router = useRouter();
const GeneralStore = useGeneralStore();
const isExpanded = ref(false);

const hasChildren = computed(() => {
  return props.item.children && props.item.children.length > 0;
});

const childrenWrapperClass = computed(() => {
  return {
    "space-y-1 my-3": props.depth == 0,
    "pl-6": props.depth > -1,
    "mt-2": props.depth > -1,
  };
});

const toggleChildren = () => {
  isExpanded.value = !isExpanded.value;
};

const handleItemClick = (item) => {
  if (!hasChildren.value) {
    emit("menu-click", item);

    if (item.click) {
      item.click();
    }
    if (item.path) {
      router.push(item.path);
      GeneralStore.mobile_bottom_menu.visible = false;
    }
  }
};

const handleChildClick = (item) => {
  emit("menu-click", item);
};
</script>

<style scoped>
.child-menu-transition-enter-active {
  transition: all 0.3s ease;
}

.child-menu-transition-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
