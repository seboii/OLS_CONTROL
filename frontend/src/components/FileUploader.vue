<template>
  <div class="w-full" @paste="handlePaste">
    <!-- Dosya Yükleme Alanı -->
    <div
      @drop.prevent="handleDrop"
      @dragover.prevent="isDragging = true"
      @dragleave.prevent="isDragging = false"
      @dragenter.prevent="isDragging = true"
      :class="[
        'border border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer',
        isDragging ? 'border-primary-500 bg-primary-50' : 'border-gray-200 hover:border-primary-300 hover:bg-gray-100',
        compact ? 'py-4' : 'py-20',
      ]"
      @click="$refs.fileInput.click()"
      v-if="modelValue.length < fileLimit || !fileLimit"
    >
      <input ref="fileInput" type="file" multiple class="hidden" @change="handleFileSelect" accept="*/*" />
      <div class="space-y-1">
        <div
          :class="{
            'text-sm': compact,
          }"
          class="text-gray-600"
        >
          Dosyaları sürükleyip bırakın veya seçmek için tıklayın
        </div>
        <div v-if="!compact" class="text-sm text-gray-500">Tüm dosya formatları desteklenmektedir</div>
      </div>
    </div>

    <!-- Yüklenen Dosyalar Listesi -->
    <div v-if="modelValue.length > 0" class="relative mt-4">
      <!-- Toplu Yükleme Butonu -->
      <div class="flex justify-between items-end mb-2">
        <div
          :class="{
            'text-sm': compact,
          }"
          class="text-gray-500"
        >
          Dosyalar ({{ modelValue.length }})
        </div>
        <Button v-if="api && !autoupload" label="Tümünü Yükle" size="small" @click="uploadAllFiles" />
      </div>
      <TransitionGroup
        tag="ul"
        enter-active-class="transition-all duration-[400ms]"
        move-class="transition-all duration-[400ms]"
        enter-from-class="opacity-0 scale-90"
        enter-to-class="opacity-100"
        leave-active-class="transition-all duration-[400ms] absolute"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0 scale-90"
      >
        <div v-for="(item, index) in modelValue" :key="item.uuid" class="w-full border bg-gray-50 rounded-lg mb-2 overflow-hidden">
          <div class="p-3 lg:p-5">
            <div class="flex items-center justify-between">
              <!-- Dosya Önizleme -->
              <div class="flex space-x-3">
                <!-- Dosya İkonu/Önizleme -->
                <div role="button" class="w-10 h-10 shrink-0">
                  <div v-if="item.upload_loading" class="w-full h-full bg-gray-200 rounded-sm flex items-center justify-center text-gray-600 text-xs">
                    <ProgressSpinner class="size-6!" />
                  </div>
                  <div v-else class="w-full h-full">
                    <img v-if="isImage(item)" :src="getFileUrl(item)" class="w-full h-full object-cover rounded-sm" @click="openPreview(item)" />
                    <div v-else class="w-full h-full bg-gray-200 rounded-sm flex items-center justify-center text-gray-600 text-xs" @click="openPreview(item)">
                      {{ getFileExtension(getFileProperty(item, "name")) }}
                    </div>
                  </div>
                </div>

                <!-- Dosya Bilgileri -->
                <div>
                  <div v-if="item.ordino_data?.name_edit_visible" class="mb-1 flex items-center gap-1 h-10">
                    <InputText v-model="item.ordino_data.new_file_name" @keypress.enter="onSaveFileNameEdit(item)" size="small" class="h-8" />
                    <Button outlined size="small" @click="onCancelFileNameEdit(item)" class="p-0! aspect-square! h-8! w-auto!">
                      <template #icon>
                        <Cancel01Icon size="16" />
                      </template>
                    </Button>
                    <Button outlined size="small" @click="onSaveFileNameEdit(item)" class="p-0! aspect-square! h-8! w-auto!">
                      <template #icon>
                        <Tick02Icon size="16" />
                      </template>
                    </Button>
                  </div>
                  <div v-else @click="onShowFileNameEdit(item)" class="text-sm font-medium text-gray-900 line-clamp-1">
                    <span v-if="item.ordino_data?.new_file_name">{{ item.ordino_data?.new_file_name }}</span>
                    <span v-else>{{ getFileProperty(item, "name") }}</span>
                  </div>
                  <div class="text-xs text-gray-500">{{ formatFileSize(getFileProperty(item, "size")) }}</div>
                  <div v-if="item.data?.created_at" class="text-xs text-gray-500">
                    {{ item.data.created_at ? new Date(item.data.created_at).toLocaleString() : "" }}
                  </div>
                </div>
              </div>

              <!-- İşlem Butonları -->
              <div class="flex space-x-2">
                <button
                  v-if="api && !autoupload && !item.data"
                  @click="uploadFile(item, index)"
                  class="p-1 text-gray-600 hover:text-green-600 transition-colors"
                  v-tooltip="'Yükle'"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                  </svg>
                </button>
                <button @click="downloadFile(item)" class="p-1 text-gray-600 hover:text-primary-600 transition-colors" v-tooltip="'İndir'">
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                  </svg>
                </button>
                <button @click="openPreview(item)" class="p-1 text-gray-600 hover:text-primary-600 transition-colors" v-tooltip="'Önizleme'">
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                    />
                  </svg>
                </button>
                <button @click="removeFile(index)" class="p-1 text-gray-600 hover:text-red-600 transition-colors" v-tooltip="'Sil'">
                  <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                </button>
              </div>
            </div>
            <div>
              <div v-if="ordino">
                <hr class="my-5" />
                <div
                  :class="{
                    'xl:grid-cols-4': ordinoLoadInputView,
                    'xl:grid-cols-3': !ordinoLoadInputView,
                  }"
                  class="grid grid-cols-1 lg:grid-cols-2 gap-x-3 gap-y-4 mt-4"
                >
                  <div v-if="ordinoLoadInputView">
                    <FloatLabel variant="on">
                      <InputText size="small" v-model="item.ordino_data.yuk_no" class="h-9" fluid />
                      <label for="username">Yük No</label>
                    </FloatLabel>
                  </div>
                  <div>
                    <FloatLabel variant="on">
                      <DatePicker
                        size="small"
                        :pt="{
                          pcInputText: {
                            root: {
                              class: 'py-(--p-inputtext-sm-padding-y)! px-(--p-inputtext-sm-padding-x)! h-9 text-sm!',
                            },
                          },
                          inputiconcontainer: {
                            class: 'relative -translate-y-0.5',
                          },
                        }"
                        v-model="item.ordino_data.gelis_tarihi"
                        :invalid="!item.ordino_data.gelis_tarihi"
                        showIcon
                        showButtonBar
                        iconDisplay="input"
                        fluid
                      />
                      <label for="username">Geliş Tarihi</label>
                    </FloatLabel>
                  </div>
                  <div>
                    <FloatLabel variant="on">
                      <InputText size="small" v-model="item.ordino_data.defter_no" :invalid="!item.ordino_data.defter_no" class="h-9" fluid />
                      <label for="username">Defter No</label>
                    </FloatLabel>
                  </div>
                  <div>
                    <FloatLabel variant="on">
                      <InputText size="small" v-model="item.ordino_data.ozet_beyan_no" :invalid="!item.ordino_data.ozet_beyan_no" class="h-9" fluid />
                      <label for="username">Özet Beyan No</label>
                    </FloatLabel>
                  </div>
                  <div>
                    <FloatLabel variant="on">
                      <InputText size="small" v-model="item.ordino_data.recipient_name" :invalid="!item.ordino_data.recipient_name" class="h-9" fluid />
                      <label>Alıcı Adı</label>
                    </FloatLabel>
                  </div>
                  <div>
                    <FloatLabel variant="on">
                      <InputText size="small" v-model="item.ordino_data.vessel" class="h-9" fluid />
                      <label>Kap</label>
                    </FloatLabel>
                  </div>
                  <div>
                    <FloatLabel variant="on">
                      <InputNumber size="small" v-model="item.ordino_data.weight" :maxFractionDigits="2" class="h-9" fluid />
                      <label>Kilogram</label>
                    </FloatLabel>
                  </div>
                </div>
                <div class="mt-4">
                  <FloatLabel variant="on">
                    <Textarea v-model="item.ordino_data.description" class="resize-none" fluid />
                    <label>Açıklama</label>
                  </FloatLabel>
                </div>
                <div v-if="item.data && item.data.end_date">
                  <div class="w-full flex justify-between items-center mt-1.5 p-1 pl-3 bg-white border rounded-lg">
                    <span
                      v-if="item.data?.end_date"
                      class="text-sm"
                      :class="{
                        'text-red-600': new Date(item.data?.end_date) - new Date() < 3 * 24 * 60 * 60 * 1000,
                      }"
                    >
                      Ordino Bitiş Süresi : {{ new Date(item.data?.end_date).toLocaleDateString() }}
                    </span>
                    <Button @click="showExtendTimeOrdinoModal(item)" size="small" label="Süreyi Uzat" text />
                  </div>
                </div>
              </div>
            </div>
          </div>
          <!-- Loading Progress Bar -->
          <div v-if="item.upload_loading" class="h-1 w-full">
            <ProgressBar mode="indeterminate" style="height: 4px"></ProgressBar>
          </div>
          <div v-if="(item.upload_status == true || item.upload_status == false) && !item.upload_loading" class="h-1 w-full">
            <div
              :class="{
                'bg-green-600': item.upload_status == true,
                'bg-red-600': item.upload_status == false,
              }"
              class="w-full h-1"
            ></div>
          </div>
        </div>
      </TransitionGroup>
    </div>

    <!-- Önizleme Modalı -->
    <Dialog
      v-model:visible="previewFile"
      :pt="{
        header: { class: 'pb-0!' },
      }"
      modal
      :closeOnEscape="true"
      header=" "
    >
      <div class="mt-2">
        <Image
          v-if="isImage(previewFile)"
          :src="getFileUrl(previewFile)"
          class="w-full lg:max-w-[800px] max-h-[70vh] object-contain rounded-lg overflow-hidden"
          preview
        />
        <iframe v-else-if="isPdf(previewFile)" :src="getFileUrl(previewFile)" class="w-full lg:w-[800px] h-[80vh]"></iframe>
        <video v-else-if="isVideo(previewFile)" :src="getFileUrl(previewFile)" controls class="max-w-full max-h-[80vh]"></video>
        <div v-else class="text-center p-4">Bu dosya türü önizlenemez</div>
      </div>
    </Dialog>
    <Dialog v-model:visible="ordino_extend_time_data.modal_status" modal header="Ordino Süresini Uzat" class="w-full lg:w-[500px]">
      <div>
        <div class="text-sm">{{ ordino_extend_time_data.data.ordino_data.yuk_no }} yük numaralı ordino süresini 30 gün uzatmak istediğinize emin misiniz?</div>
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-2 my-4">
          <div class="p-2.5 bg-gray-100 rounded-lg text-sm">
            <span class="font-semibold">Yük No : </span> <span>{{ ordino_extend_time_data.data.ordino_data.yuk_no }}</span>
          </div>
          <div class="p-2.5 bg-gray-100 rounded-lg text-sm">
            <span class="font-semibold">Geliş Tarihi : </span>
            <span>{{
              ordino_extend_time_data.data.ordino_data.gelis_tarihi ? new Date(ordino_extend_time_data.data.ordino_data.gelis_tarihi).toLocaleDateString() : ""
            }}</span>
          </div>
          <div class="p-2.5 bg-gray-100 rounded-lg text-sm">
            <span class="font-semibold">Defter No : </span> <span>{{ ordino_extend_time_data.data.ordino_data.defter_no }}</span>
          </div>
          <div class="p-2.5 bg-gray-100 rounded-lg text-sm">
            <span class="font-semibold">Özet Beyan No : </span> <span>{{ ordino_extend_time_data.data.ordino_data.ozet_beyan_no }}</span>
          </div>
        </div>
      </div>
      <div class="flex justify-end gap-2">
        <Button
          type="button"
          size="small"
          label="İptal Et"
          severity="secondary"
          :disabled="ordino_extend_time_data.loading_status"
          @click="ordino_extend_time_data.modal_status = false"
        ></Button>
        <Button
          type="button"
          size="small"
          label="Onayla"
          :disabled="ordino_extend_time_data.loading_status"
          :loading="ordino_extend_time_data.loading_status"
          @click="extendTimeOrdino"
        ></Button>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, reactive } from "vue";
import { toast } from "vue-sonner";
import axios from "axios";
import { Cancel01Icon, Tick02Icon } from "hugeicons-vue";
import { useDataStore } from "@/stores/data_store.js";

const props = defineProps({
  fileLimit: {
    type: Number,
  },
  maxFileSize: {
    type: Number,
    default: 100 * 1024 * 1024, // 100MB
  },
  allowDuplicates: {
    type: Boolean,
    default: false,
  },
  api: {
    type: String,
    required: false,
  },
  deleteApi: {
    type: String,
    required: false,
  },
  fileKey: {
    type: String,
    required: false,
  },
  compact: {
    type: Boolean,
    default: false,
  },
  autoupload: {
    type: Boolean,
    default: false,
  },
  fetchParams: {
    type: Object,
    default: null,
  },
  deleteParams: {
    type: Object,
    default: null,
  },
  singleFile: {
    type: Boolean,
    default: false,
  },
  filePreviewRoute: {
    type: String,
    default: null,
  },
  fileNameEditable: {
    type: Boolean,
    default: false,
  },
  ordino: {
    type: Boolean,
    default: false,
  },
  ordinoLoadInputView: {
    type: Boolean,
    default: false,
  },
});

const files_data = defineModel();
const DataStore = useDataStore();

const previewFile = ref(null);
const isDragging = ref(false);
const uploadingFiles = ref(new Set());
const ordino_extend_time_data = reactive({
  modal_status: false,
  data: {},
  loading_status: false,
});

const response_data_key_list = {
  url: "file",
  name: "file",
  size: "size",
};

const handleFileSelect = (event) => {
  const files = Array.from(event.target.files);
  addFiles(files);
  event.target.value = "";
};

const handleDrop = (event) => {
  isDragging.value = false;
  const files = Array.from(event.dataTransfer.files);
  addFiles(files);
};

const addFiles = async (files) => {
  const validFiles = files.filter((file) => file.size <= props.maxFileSize);

  if (validFiles.length !== files.length) {
    toast.error(`Bazı dosyalar ${formatFileSize(props.maxFileSize)} boyut sınırını aşıyor!`);
  }

  let filesToAdd = validFiles;

  if (!props.allowDuplicates) {
    filesToAdd = validFiles.filter((newFile) => {
      return !files_data.value.some((existingItem) => {
        if (existingItem.data) {
          return existingItem.data[response_data_key_list["name"]] == newFile.name;
        } else {
          return existingItem.file.name === newFile.name && existingItem.file.size === newFile.size;
        }
      });
    });

    if (filesToAdd.length < validFiles.length) {
      toast.error("Bu dosya zaten yüklenmiş!");
    }
  }

  const newFiles = filesToAdd.map((file) => ({
    uuid: crypto.randomUUID(),
    file,
    data: null,
    ordino_data: {
      yuk_no: "",
      gelis_tarihi: "",
      defter_no: "",
      ozet_beyan_no: "",
      file_name: "",
      new_file_name: "",
      recipient_name: "",
      vessel: "",
      weight: "",
      description: "",
      name_edit_visible: false,
    },
    upload_loading: false,
    upload_status: null,
  }));
  const new_files_ids = newFiles.map((item) => item.uuid);
  const old_files_ids = files_data.value.map((item) => item.uuid);

  newFiles.forEach((item) => {
    files_data.value.push(item);
  });

  if (props.autoupload && props.api && filesToAdd.length > 0) {
    const formData = new FormData();
    filesToAdd.forEach((file) => {
      formData.append(`${props.fileKey ? props.fileKey : "files"}${props.singleFile ? "" : "[]"}`, file);
    });

    if (props.fetchParams) {
      Object.keys(props.fetchParams).forEach((item) => {
        formData.append(item, props.fetchParams[item]);
      });
    }

    try {
      filesToAdd.forEach((_, index) => {
        uploadingFiles.value.add(files_data.value.length - filesToAdd.length + index);
      });

      files_data.value.forEach((item) => {
        if (new_files_ids.includes(item.uuid)) {
          item.upload_loading = true;
        }
      });

      const response = await axios.post(props.api, formData);

      files_data.value.forEach((item) => {
        if (new_files_ids.includes(item.uuid)) {
          item.upload_loading = false;
        }
      });
      if (response) {
        files_data.value.forEach((item) => {
          if (new_files_ids.includes(item.uuid)) {
            item.upload_status = true;
          }
        });
      }
      const responseData = response.data;

      // Update v-model with response data
      const updatedFiles = [...files_data.value];

      filesToAdd.forEach((file, index) => {
        const fileIndex = updatedFiles.findIndex((item) => item.file === file);
        if (fileIndex !== -1) {
          updatedFiles[fileIndex].upload_loading = false;
          if (response.data && Array.isArray(responseData.data)) {
            updatedFiles[fileIndex].data = responseData.data[index];
          }
          if (props.ordino) {
            updatedFiles[fileIndex].ordino_data.file_name = responseData.data[index].file;
          }
        }
      });

      files_data.value = updatedFiles;
      toast.success("Dosyalar başarıyla yüklendi");
    } catch (error) {
      toast.error(error.response?.data?.message || "Dosyalar yüklenirken bir hata oluştu");
      console.error("Upload error:", error);

      files_data.value.forEach((item) => {
        if (new_files_ids.includes(item.uuid)) {
          item.upload_status = false;
        }
      });
      throw new Error(error.response?.data?.message || "Yükleme başarısız");
    } finally {
      filesToAdd.forEach((_, index) => {
        uploadingFiles.value.delete(files_data.value.length - filesToAdd.length + index);
      });
      files_data.value.forEach((item) => {
        if (new_files_ids.includes(item.uuid)) {
          item.upload_loading = false;
        }
      });
    }
  }
};

const removeFile = async (index) => {
  const item = files_data.value[index];

  if (item.data && props.deleteApi) {
    try {
      let req_body = { ...props.deleteParams };
      if (props.deleteParams?.id) {
        if (props.ordino) {
          req_body.deletion_id = [props.deleteParams.id];
        } else {
          req_body.id = props.deleteParams.id;
        }
      } else {
        if (props.ordino) {
          req_body.deletion_id = [item.data.id];
        } else {
          req_body.id = item.data.id;
        }
      }

      console.log(req_body);
      const response = await axios.delete(props.deleteApi, {
        data: req_body,
      });

      if (!response.data) throw new Error("Silme işlemi başarısız");
      toast.success("Dosya başarıyla silindi");
    } catch (error) {
      toast.error("Dosya silinirken bir hata oluştu");
      console.error("Delete error:", error);
      return;
    }
  }

  const newFiles = [...files_data.value];
  newFiles.splice(index, 1);
  files_data.value = newFiles;
};

const uploadFile = async (file_item, index) => {
  try {
    uploadingFiles.value.add(index);
    const formData = new FormData();
    formData.append(`${props.fileKey ? props.fileKey : "files"}${props.singleFile ? "" : "[]"}`, file_item.file);

    if (props.fetchParams) {
      Object.keys(props.fetchParams).forEach((item) => {
        formData.append(item, props.fetchParams[item]);
      });
    }

    files_data.value.forEach((item) => {
      if (item.uuid == file_item.uuid) {
        item.upload_loading = true;
      }
    });

    const response = await axios.post(props.api, formData);

    files_data.value.forEach((item) => {
      if (item.uuid == file_item.uuid) {
        item.upload_loading = false;
        item.upload_status = true;
      }
    });

    const updatedFiles = [...files_data.value];
    updatedFiles[index].data = response.data.data[0];
    files_data.value = updatedFiles;

    toast.success("Dosya başarıyla yüklendi");
  } catch (error) {
    toast.error("Dosya yüklenirken bir hata oluştu");
    console.error("Upload error:", error);

    files_data.value.forEach((item) => {
      if (item.uuid == file_item.uuid) {
        item.upload_status = false;
      }
    });
    console.log(error);
    throw new Error("Yükleme başarısız");
  } finally {
    uploadingFiles.value.delete(index);
    files_data.value.forEach((item) => {
      if (item.uuid == file_item.uuid) {
        item.upload_loading = false;
      }
    });
  }
};

const uploadAllFiles = async () => {
  try {
    files_data.value.forEach((_, index) => uploadingFiles.value.add(index));

    const formData = new FormData();
    const unuploadedFiles = files_data.value.filter((item) => !item.data);

    unuploadedFiles.forEach((item) => {
      formData.append(`${props.fileKey ? props.fileKey : "files"}${props.singleFile ? "" : "[]"}`, item.file);
    });

    if (props.fetchParams) {
      Object.keys(props.fetchParams).forEach((item) => {
        formData.append(item, props.fetchParams[item]);
      });
    }

    const response = await axios.post(props.api, formData);

    if (!response.data) throw new Error("Toplu yükleme başarısız");

    const updatedFiles = [...files_data.value];
    unuploadedFiles.forEach((item, index) => {
      const fileIndex = updatedFiles.findIndex((f) => f.file === item.file);
      if (fileIndex !== -1) {
        updatedFiles[fileIndex].data = response.data.data[index];
      }
    });

    files_data.value = updatedFiles;
    toast.success("Tüm dosyalar başarıyla yüklendi");
  } catch (error) {
    toast.error(error.response?.data?.message || "Dosyalar yüklenirken bir hata oluştu");
    console.error("Bulk upload error:", error);
  } finally {
    uploadingFiles.value.clear();
  }
};

const getFileProperty = (item, property) => {
  if (item.data) {
    return response_data_key_list[property] ? item.data[response_data_key_list[property]] : "";
  }

  return item.file[property];
};

const handlePaste = (event) => {
  const items = event.clipboardData?.items;
  if (!items) return;

  const files = [];
  for (let i = 0; i < items.length; i++) {
    const item = items[i];
    if (item.kind === "file") {
      const file = item.getAsFile();
      if (file) files.push(file);
    }
  }

  if (files.length > 0) {
    addFiles(files);
  }
};

const downloadFile = (file) => {
  let url = "";
  if (file.data) {
    if (!props.filePreviewRoute) {
      url = `/storage/${file.data[response_data_key_list["url"]]}`;
    } else {
      url = `${props.filePreviewRoute}/${file.data.file}`;
    }
  } else {
    url = file.url || URL.createObjectURL(file);
  }
  const a = document.createElement("a");
  a.href = url;
  a.download = file.file?.name ? file.file.name : file.data?.org_name;
  if (!file.file?.name && !file.data?.org_name) {
    a.download = "file";
  }
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  if (!file.url) URL.revokeObjectURL(url);
};

const openPreview = (file) => {
  previewFile.value = file;
};

const closePreview = () => {
  previewFile.value = null;
};

const getFileUrl = (item) => {
  if (item.data) {
    let url = `${props.filePreviewRoute}/${item.data[response_data_key_list["url"]]}`;
    if (!props.filePreviewRoute) {
      url = `/storage/${item.data[response_data_key_list["url"]]}`;
    }
    return url;
  }
  const url = getFileProperty(item, "url");
  return url || URL.createObjectURL(item.file);
};
const isImage = (file) => {
  const name = file.file?.name || file.data.file;
  console.log(name);
  return /\.(jpg|jpeg|png|gif|webp)$/i.test(name);
};

const isPdf = (file) => {
  const name = file.file?.name || file.data.file;
  return /\.pdf$/i.test(name);
};

const isVideo = (file) => {
  const name = file.file?.name || file.data.file;
  return /\.(mp4|webm|ogg)$/i.test(name);
};

const getFileExtension = (filename) => {
  if (filename) return filename.split(".").pop().toUpperCase();
};
const formatFileSize = (bytes) => {
  if (!bytes) {
    return "";
  }
  if (bytes === 0) return "0 Bytes";
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
};

const edit_file_name_data = reactive({
  visible: false,
  name: "",
});

const onShowFileNameEdit = (item) => {
  item.ordino_data.new_file_name = item.ordino_data.file_name;
  item.ordino_data.name_edit_visible = true;
};

const onCancelFileNameEdit = (item) => {
  if (item.ordino_data.new_file_name) {
    item.ordino_data.new_file_name = item.ordino_data.file_name;
    item.ordino_data.file_name = item.ordino_data.new_file_name;
  }
  item.ordino_data.name_edit_visible = false;
};

const onSaveFileNameEdit = (item) => {
  item.ordino_data.name_edit_visible = false;
  item.ordino_data.file_name = item.ordino_data.new_file_name;
};

const showExtendTimeOrdinoModal = (item) => {
  ordino_extend_time_data.data = item;
  ordino_extend_time_data.modal_status = !ordino_extend_time_data.modal_status;
};

const extendTimeOrdino = async () => {
  ordino_extend_time_data.loading_status = true;
  const res = await DataStore.ADD_EXTEND_TIME_ORDINO(ordino_extend_time_data.data.data.id);
  if (res) {
    ordino_extend_time_data.modal_status = false;
    toast.success("Ordino süresi başarıyla uzatıldı.");
    files_data.value.find((item) => item.data.id == ordino_extend_time_data.data.data.id).data.end_date = res.data.end_date;
  }
  ordino_extend_time_data.loading_status = false;
};
</script>

<style scoped>
@keyframes loading {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}
</style>
