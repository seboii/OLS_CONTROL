<template>
  <div class="bg-white">
    <div v-if="search || ($attrs.selection && $attrs.selection.length > 0)" class="flex justify-between mb-4">
      <div v-if="search">
        <InputText size="small" @input="(e) => tableState.handleSearch(e.target.value)" placeholder="Arama" />
      </div>
      <div>
        <Button v-if="excel" @click="downloadExcel" outlined raised class="p-0! size-10!" icon v-tooltip="'Excel İndir'">
          <template #icon>
            <Csv02Icon size="18" />
          </template>
        </Button>
        <div v-if="$attrs.selection && $attrs.selection.length > 0">
          <Button size="small" severity="danger" @click="newCustomer" label="Seçilenleri Sil" class="px-4!" />
        </div>
      </div>
    </div>
    <DataTable
      :value="tableState.state.items"
      v-bind="$attrs"
      class="table-nowrap table-loading relative"
      @rowSelect="onRowSelect"
      @sort="tableState.handleSort"
    >
      <Transition mode="out-in">
        <div v-if="tableState.state.loading" class="w-full h-full absolute top-0 left-0 bg-white/80 flex items-center justify-center z-2">
          <div class="size-12 bg-white rounded-xl border shadow-xl flex items-center justify-center">
            <ProgressSpinner class="size-10!" />
          </div>
        </div>
      </Transition>
      <template #empty>
        <div class="text-sm text-gray-400 text-center py-10">Sonuç Bulunamadı</div>
      </template>
      <template #loading>
        <div class="size-12 bg-white rounded-xl border shadow-xl flex items-center justify-center">
          <ProgressSpinner class="size-10!" />
        </div>
      </template>
      <slot></slot>
      <Column
        v-if="!noAction"
        :pt="{
          columnHeaderContent: {
            class: 'justify-end!',
          },
        }"
        header="İşlemler"
      >
        <template #body="{ data }">
          <div class="flex items-center justify-end gap-1">
            <Button v-if="viewApi" severity="secondary" outlined @click="() => tableState.handleView(data.id)" class="size-9! p-0!">
              <ViewIcon size="18" />
            </Button>
            <Button
              v-if="tableState.state.routes.delete"
              severity="secondary"
              outlined
              @click="() => tableState.showDeleteDialog(data)"
              class="size-9! p-0!"
              label="Sil"
            >
              <Delete01Icon size="18" />
            </Button>
          </div>
        </template>
      </Column>
    </DataTable>
    <Dialog v-model:visible="tableState.dialogState.deleteVisible" modal header="Dikkat" class="w-full lg:w-[460px]">
      <div class="mt-2 mb-8 flex items-center flex-col gap-4">
        <div class="flex items-center justify-center size-20 bg-red-50 rounded-full">
          <AlertCircleIcon size="52" class="text-red-500" />
        </div>
        <div class="text-center">Tüm bilgileri sileceksiniz ve geri alamayacaksınız. Silmek istediğinizden emin misiniz?</div>
      </div>
      <div class="grid grid-cols-2 gap-2">
        <Button :disabled="tableState.state.delete_loading" type="button" label="İptal" severity="secondary" @click="tableState.closeDeleteDialog"></Button>
        <Button
          :disabled="tableState.state.delete_loading"
          :loading="tableState.state.delete_loading"
          type="button"
          label="Sil"
          severity="danger"
          @click="tableState.handleDelete"
        ></Button>
      </div>
    </Dialog>
    <div class="grid grid-cols-12 gap-4 mt-2">
      <div class="col-span-12 lg:col-span-3 flex items-center">
        <div class="text-sm text-gray-500">{{ tableState.state?.meta?.total }} kayıt bulundu.</div>
      </div>
      <div class="col-span-12 lg:col-span-6">
        <Paginator
          v-if="tableState.state.meta"
          :pt:root:class="`rounded-none!`"
          :rows="tableState.state.meta.per_page"
          :totalRecords="tableState.state.meta.total"
          :first="tableState.state.meta.per_page * (tableState.state.meta.current_page - 1)"
          @page="tableState.handlePageChange"
        />
      </div>
      <div class="col-span-12 lg:col-span-3"></div>
    </div>
  </div>
</template>

<script setup>
import { toast } from "vue-sonner";
import { Delete01Icon, ViewIcon, AlertCircleIcon, Csv02Icon } from "hugeicons-vue";

const props = defineProps({
  tableState: {
    type: Object,
    required: true,
  },
  search: {
    type: Boolean,
    default: false,
  },
  deleteApi: {
    type: String,
    default: null,
  },
  viewApi: {
    type: String,
    default: null,
  },
  noAction: {
    type: Boolean,
    default: false,
  },
  excel: {
    type: String,
    default: null,
  },
});

const emit = defineEmits(["rowSelect"]);

const onRowSelect = (event) => {
  emit("rowSelect", event);
};

const downloadExcel = () => {
  let url = props.excel.route;
  let params = new URLSearchParams(props.excel.params);
  window.open(`${url}?${params.toString()}`, "_blank");
  toast.success("Excel dosyası indirildi.");
};
</script>
