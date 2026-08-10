<template>
  <div v-if="item.permission_slug ? usePermissionStatus(item.permission_slug).read : true" class="w-full">
    <div @click="toggleExpand" v-if="item.children" class="group w-full py-1 flex justify-center transition-all duration-[350ms] cursor-pointer">
      <div
        :class="{
          'size-11 justify-center rounded-full! group-hover:bg-gray-100/20': !bigMenuStatus,
          'min-h-10 pl-6 pr-4 w-full gap-2': bigMenuStatus,
          // 'bg-white/15': isExpanded,
        }"
        class="relative flex items-center rounded-lg rounded-l-none transition-all duration-100 hover:bg-gray-50/5"
        v-ripple
      >
        <component
          :is="item.icon"
          :size="18"
          :class="{
            'text-white!': currentRoute == item.path && bigMenuStatus,
            'text-primary!': currentRoute == item.path && !bigMenuStatus,
          }"
          class="text-gray-300 shrink-0"
        />
        <Transition enter-active-class="transition-all duration-300" enter-from-class="opacity-0 -translate-x-8" enter-to-class="opacity-100" mode="out-in">
          <div v-if="bigMenuStatus" class="flex items-center justify-between w-full">
            <span
              :class="{
                'text-white!': currentRoute == item.path && bigMenuStatus,
              }"
              class="text-gray-400 text-sm transition-all"
              >{{ item.name }} <span v-if="item.count" class="text-sm">({{ item.count }})</span></span
            >
            <ArrowDown01Icon :class="{ 'transform rotate-0!': isExpanded }" class="transition-transform -rotate-90" size="18" color="#FFFFFF" />
          </div>
        </Transition>
      </div>
    </div>

    <router-link v-else :to="item.path" @click="item.click" class="group w-full py-0.5 flex justify-center transition-all duration-[350ms]">
      <div
        :class="{
          'bg-white! shadow-xl': currentRoute == item.path && !bigMenuStatus,
          'bg-white/15! shadow-xl active-menu-item': currentRoute == item.path && bigMenuStatus,
          'size-11 justify-center rounded-full! group-hover:bg-gray-100/20': !bigMenuStatus,
          'min-h-10 pl-6 pr-4 w-full gap-2': bigMenuStatus,
        }"
        class="relative flex items-center rounded-lg rounded-l-none transition-all duration-100 hover:bg-gray-50/5"
        v-ripple
      >
        <component
          :is="item.icon"
          :size="18"
          :class="{
            'text-white!': currentRoute == item.path && bigMenuStatus,
            'text-primary!': currentRoute == item.path && !bigMenuStatus,
          }"
          class="text-gray-300 shrink-0"
        />
        <Transition enter-active-class="transition-all duration-300" enter-from-class="opacity-0 -translate-x-8" enter-to-class="opacity-100" mode="out-in">
          <div
            v-if="bigMenuStatus"
            :class="{
              'text-white!': currentRoute == item.path && bigMenuStatus,
            }"
            class="w-full text-gray-400 text-sm flex items-center justify-between transition-all"
          >
            <div>{{ item.name }}</div>
            <div class="relative">
              <span v-if="'count' in item" class="shrink-0 size-5 rounded-full bg-white text-primary text-xs flex items-center justify-center">{{
                item.count
              }}</span>
              <div v-if="item.count > 0" class="w-full h-full absolute top-0 right-0 size-5 rounded-full bg-white animate-ping"></div>
            </div>
          </div>
        </Transition>
      </div>
    </router-link>

    <div class="relative">
      <Transition
        enter-active-class="transition-all duration-[400ms]"
        enter-from-class="opacity-0 -translate-y-2"
        enter-to-class="opacity-100"
        leave-active-class=""
        leave-from-class=""
        leave-to-class=""
        mode="out-in"
      >
        <div v-if="item.children" v-show="isExpanded && bigMenuStatus" class="ml-4 border-l border-gray-600">
          <MenuItem v-for="child in item.children" :key="child.id" :item="child" :big-menu-status="bigMenuStatus" :current-route="currentRoute" /></div
      ></Transition>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { ArrowDown01Icon } from "hugeicons-vue";
import { usePermissionStatus } from "@/composables/index.js";

const props = defineProps({
  item: {
    type: Object,
    required: true,
  },
  bigMenuStatus: {
    type: Boolean,
    required: true,
  },
  currentRoute: {
    type: String,
    required: true,
  },
});

const isExpanded = ref(false);

const toggleExpand = () => {
  if (props.item.children) {
    isExpanded.value = !isExpanded.value;
  }
};
</script>

<style scoped>
.active-menu-item::before {
  content: "";
  position: absolute;
  width: 4px;
  height: 50%;
  background-color: var(--color-primary-default);
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  border-radius: 10px;
}

.nested-menu-enter-active,
.nested-menu-leave-active {
  transition: all 0.3s ease;
}

.nested-menu-enter-from,
.nested-menu-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
