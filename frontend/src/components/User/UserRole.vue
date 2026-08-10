<template>
  <div
    :class="{
      'opacity-0 translate-y-6': page_transition,
    }"
    class="space-y-2 transition-all duration-300"
  >
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 py-3.5 px-6 rounded-lg bg-white border shadow-xs">
      <div>
        <div>İşlem</div>
      </div>
      <div class="grid grid-cols-4 gap-2 lg:*:text-sm *:text-center">
        <div v-tooltip.top="'Görüntüleme'" class="flex justify-center">
          <ViewIcon />
        </div>
        <div v-tooltip.top="'Oluşturma'" class="flex justify-center">
          <AddCircleIcon />
        </div>
        <div v-tooltip.top="'Düzenleme'" class="flex justify-center">
          <PencilEdit02Icon />
        </div>
        <div v-tooltip.top="'Silme'" class="flex justify-center">
          <Delete01Icon />
        </div>
      </div>
    </div>
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 py-3.5 px-6">
      <div>Tümünü Seç</div>
      <div class="grid grid-cols-4 gap-2">
        <div class="flex justify-center">
          <Checkbox v-model="check_all_permission.read" @update:modelValue="changeAllPermission('read')" :trueValue="true" :falseValue="false" binary />
        </div>
        <div class="flex justify-center">
          <Checkbox v-model="check_all_permission.create" @update:modelValue="changeAllPermission('create')" :trueValue="true" :falseValue="false" binary />
        </div>
        <div class="flex justify-center">
          <Checkbox v-model="check_all_permission.update" @update:modelValue="changeAllPermission('update')" :trueValue="true" :falseValue="false" binary />
        </div>
        <div class="flex justify-center">
          <Checkbox v-model="check_all_permission.delete" @update:modelValue="changeAllPermission('delete')" :trueValue="true" :falseValue="false" binary />
        </div>
      </div>
    </div>
    <div v-for="role in user_roles" :key="role.id" class="grid grid-cols-1 lg:grid-cols-2 gap-4 py-3.5 px-6 rounded-lg bg-gray-100">
      <div>
        <div class="text-sm text-gray-600">{{ role.permission_page_name }}</div>
      </div>
      <div class="grid grid-cols-4 gap-2">
        <div class="flex justify-center">
          <Checkbox v-model="role.read" @update:modelValue="changePermission('read', role)" :trueValue="1" :falseValue="0" binary />
        </div>
        <div class="flex justify-center">
          <Checkbox v-model="role.create" @update:modelValue="changePermission('create', role)" :trueValue="1" :falseValue="0" binary />
        </div>
        <div class="flex justify-center">
          <Checkbox v-model="role.update" @update:modelValue="changePermission('update', role)" :trueValue="1" :falseValue="0" binary />
        </div>
        <div class="flex justify-center">
          <Checkbox v-model="role.delete" @update:modelValue="changePermission('delete', role)" :trueValue="1" :falseValue="0" binary />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from "vue";
import { useDataStore } from "@/stores/data_store.js";
import { AddCircleIcon, Delete01Icon, PencilEdit02Icon, ViewIcon } from "hugeicons-vue";

const props = defineProps(["userId"]);
const DataStore = useDataStore();

const user_roles = ref([]);
const check_all_permission = reactive({
  read: true,
  create: true,
  update: true,
  delete: true,
});
const page_transition = ref(true);

const changePermission = async (type, role) => {
  const res = await DataStore.UPDATE_USER_ROLE({
    crud: type,
    permission_page_id: role.id,
    is_data: role[type],
    user_id: props.userId,
  });
  if (res) {
    if (user_roles.value.find((r) => r.id == role.id)[type] == 0) {
      check_all_permission[type] = false;
    } else {
      if (user_roles.value.filter((r) => r[type] == 1).length == user_roles.value.length) {
        check_all_permission[type] = true;
      }
    }
  } else {
    role[type] = role[type] == 1 ? 0 : 1;
  }
};

const changeAllPermission = async (type) => {
  const res = await DataStore.UPDATE_USER_ROLE({
    crud: type,
    is_data: check_all_permission[type] == true ? 1 : 0,
    user_id: props.userId,
  });
  if (res) {
    user_roles.value.forEach((role) => {
      role[type] = check_all_permission[type] == true ? 1 : 0;
    });
  }
};

onMounted(async () => {
  const res = await DataStore.GET_USER_ROLE(props.userId);
  if (res) {
    user_roles.value = res.stats.permission_data;

    res.stats.permission_data.forEach((role) => {
      if (role.read == 0) {
        check_all_permission.read = false;
      }
      if (role.create == 0) {
        check_all_permission.create = false;
      }
      if (role.update == 0) {
        check_all_permission.update = false;
      }
      if (role.delete == 0) {
        check_all_permission.delete = false;
      }
    });

    page_transition.value = false;
  }
});
</script>
