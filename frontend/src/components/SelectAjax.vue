<template>
  <div>
    <AutoComplete
      :modelValue="modelValue"
      v-bind="$attrs"
      @update:modelValue="onValueChange"
      :suggestions="items"
      :optionLabel="optionLabel"
      :placeholder="placeholder"
      :class="classes"
      :disabled="disabled"
      :invalid="invalid"
      :virtualScrollerOptions="{ itemSize: 46, lazy: true, showLoader: true, loading: loading_spinner, onScrollIndexChange: handleScroll }"
      @complete="onComplete"
      @show="onShow"
      @hide="onHide"
      forceSelection
      completeOnFocus
      dataKey="id"
      dropdown
      class="[&_.p-autocomplete-dropdown]:pointer-events-none!"
    >
      <template v-for="(_, name) in $slots" :key="name" #[name]="slotProps">
        <slot :name="name" v-bind="slotProps"></slot>
      </template>

      <!-- Loading message template -->
      <template #footer>
        <Transition enter-active-class="transition-all duration-500" enter-from-class="opacity-0 -translate-y-2" enter-to-class="opacity-100" mode="out-in">
          <div v-if="loading" class="flex justify-center w-full pb-3">
            <ProgressSpinner class="size-6!" />
          </div>
        </Transition>
      </template>
    </AutoComplete>
  </div>
</template>

<script setup>
import { ref, onUnmounted } from "vue";
import axios from "axios";

const props = defineProps({
  modelValue: {
    type: [Object, String, Number],
    default: null,
  },
  optionLabel: {
    type: String,
    required: true,
  },
  placeholder: {
    type: String,
    default: "‏‏‎‏‏‎",
  },
  invalid: {
    type: Boolean,
    default: false,
  },
  classes: {
    type: String,
    default: "w-full",
  },
  api: {
    type: String,
    required: true,
  },
  fetchParams: {
    type: Object,
    default: () => ({}),
  },
  searchKey: {
    type: String,
    default: "search",
  },
  debounceTime: {
    type: Number,
    default: 500,
  },
  disabled: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue"]);

const dropdown_count = ref(0);
const search_value = ref(null);
const loading = ref(false);
const loading_spinner = ref(false);
const items = ref([]);
const pagination = ref(null);
const debounceTimeout = ref(null);

const fetchData = async (params = {}) => {
  try {
    const response = await axios.get(props.api, {
      params: { per_page: 30, ...params, ...props.fetchParams },
    });
    return response.data.data;
  } catch (error) {
    console.error("Error fetching data:", error);
    return null;
  }
};

const handleScroll = async (event) => {
  const { first, last } = event;
  if (pagination.value && last + 10 > pagination.value.to && pagination.value.to < pagination.value.total && !loading.value) {
    loading.value = true;
    try {
      const response = await fetchData({
        page: pagination.value.current_page + 1,
        [props.searchKey]: search_value.value,
      });

      if (response?.data) {
        items.value = [...items.value, ...response.data];
        pagination.value = response;
      }
    } finally {
      loading.value = false;
    }
  }
};

const searchItems = async (event) => {
  if (dropdown_count.value > 1) {
    search_value.value = event.query;
  }
  loading.value = true;
  try {
    let req_body = {};
    req_body = { [props.searchKey]: search_value.value };
    /*
    if (dropdown_count.value >= 1) {
    }
    */
    const response = await fetchData(req_body);
    if (response?.data) {
      items.value = response.data;
      pagination.value = response;
    }
  } finally {
    loading.value = false;
    loading_spinner.value = false;
  }
};

const onValueChange = (e) => {
  ++dropdown_count.value;
  emit("update:modelValue", e);
};

const onShow = (event) => {
  dropdown_count.value = 1;
};

const onComplete = (event) => {
  debouncedSearch(event);
};

const onHide = () => {
  dropdown_count.value = 0;
  search_value.value = "";
  return true;
};

const debouncedSearch = (event) => {
  loading_spinner.value = true;
  searchItems(event);
  return true;
  if (debounceTimeout.value) {
    clearTimeout(debounceTimeout.value);
  }

  debounceTimeout.value = setTimeout(() => {
    searchItems(event);
  }, props.debounceTime);
};

onUnmounted(() => {
  if (debounceTimeout.value) {
    clearTimeout(debounceTimeout.value);
  }
});
</script>
