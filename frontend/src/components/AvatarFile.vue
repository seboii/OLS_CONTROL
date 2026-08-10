<template>
  <div class="flex flex-col items-center gap-4">
    <!-- Görünmeyen dosya input'u -->
    <input type="file" ref="fileInput" @change="handleFileChange" accept="image/*" class="hidden" />

    <!-- Profil resmi önizleme alanı -->
    <div
      :style="{
        width: size + 'px',
        height: size + 'px',
      }"
      :class="{
        'rounded-full': rounded,
        'rounded-xl': !rounded,
      }"
      class="border-2 border-gray-300 overflow-hidden cursor-pointer relative group"
      @click="triggerFileInput"
    >
      <img v-if="valueIsPreview" :src="`${previewPath}/${modelValue}`" class="w-full h-full object-cover" alt="Profil resmi önizleme" />
      <img v-else-if="previewUrl" :src="previewUrl" class="w-full h-full object-cover" alt="Profil resmi önizleme" />
      <div v-else class="w-full h-full flex items-center justify-center bg-gray-100 transition-all hover:bg-slate-200">
        <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
        </svg>
      </div>

      <!-- Overlay buttons - Only show when there's an image -->
      <div
        v-if="previewUrl || valueIsPreview"
        class="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2"
        @click.stop
      >
        <!-- Geri Alma Butonu - Only show when there's a previous image -->
        <button v-if="previousImage" @click="handleUndo" class="p-2 rounded-full bg-white/20 hover:bg-white/30 transition-colors" title="Önceki resme geri dön">
          <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" />
          </svg>
        </button>

        <!-- Kaldırma Butonu -->
        <button @click="handleRemove" class="p-2 rounded-full bg-white/20 hover:bg-white/30 transition-colors" title="Resmi kaldır">
          <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Hata mesajı -->
    <p v-if="errorMessage" class="text-red-500 text-sm">{{ errorMessage }}</p>
  </div>
</template>

<script setup>
import { ref, watch, computed } from "vue";

const props = defineProps({
  modelValue: {
    type: [File, String, null],
    default: null,
  },
  previewPath: {
    type: String,
  },
  size: {
    type: Number,
    default: 128,
  },
  rounded: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["update:modelValue", "update:imageRemove"]);

const fileInput = ref(null);
const previewUrl = ref("");
const errorMessage = ref("");
const previousImage = ref(null);
const valueIsPreview = computed(() => typeof props.modelValue === "string");

// Dosya seçme dialogunu tetikle
const triggerFileInput = () => {
  fileInput.value.click();
};

// Dosya seçildiğinde
const handleFileChange = (event) => {
  const file = event.target.files[0];

  if (file) {
    // Dosya tipini kontrol et
    if (!file.type.startsWith("image/")) {
      errorMessage.value = "Lütfen sadece resim dosyası yükleyin.";
      event.target.value = "";
      return;
    }

    // Dosya boyutunu kontrol et (örn: 5MB)
    if (file.size > 5 * 1024 * 1024) {
      errorMessage.value = "Dosya boyutu 5MB'dan küçük olmalıdır.";
      event.target.value = "";
      return;
    }

    errorMessage.value = "";
    // Mevcut resmi previousImage'e kaydet
    if (props.modelValue) {
      previousImage.value = {
        file: props.modelValue,
        preview: valueIsPreview.value ? props.modelValue : previewUrl.value,
      };
    }
    emit("update:modelValue", file);
    emit("update:imageRemove", false); // Resim eklendiğinde imageRemove false olarak emit ediliyor

    // Önizleme URL'ini oluştur
    const reader = new FileReader();
    reader.onload = (e) => {
      previewUrl.value = e.target.result;
    };
    reader.readAsDataURL(file);
  }
};

// Önceki resme geri dön
const handleUndo = () => {
  if (previousImage.value) {
    emit("update:modelValue", previousImage.value.file);
    if (typeof previousImage.value.file === "string") {
      previewUrl.value = "";
    } else {
      previewUrl.value = previousImage.value.preview;
    }
    previousImage.value = null;
    emit("update:imageRemove", false); // Geri alındığında imageRemove false olarak emit ediliyor
  }
};

// Resmi kaldır
const handleRemove = () => {
  // Mevcut resmi previousImage'e kaydet
  if (props.modelValue) {
    previousImage.value = {
      file: props.modelValue,
      preview: valueIsPreview.value ? props.modelValue : previewUrl.value,
    };
  }
  emit("update:modelValue", null);
  emit("update:imageRemove", true); // Resim kaldırıldığında imageRemove true olarak emit ediliyor
  previewUrl.value = "";
  fileInput.value.value = "";
};

// modelValue değiştiğinde önizlemeyi güncelle
watch(
  () => props.modelValue,
  (newFile) => {
    if (!newFile) {
      previewUrl.value = "";
      emit("update:imageRemove", true); // Eğer modelValue null ise imageRemove true olarak emit ediliyor
    }
  }
);
</script>
