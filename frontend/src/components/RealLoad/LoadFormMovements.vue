<template>
  <div class="mt-4">
    <div class="flex justify-end items-center mb-4">
      <div class="flex gap-2">
        <Button v-if="deletedMovements.length > 0" label="Silinen Hareketler" size="small" severity="secondary" @click="showDeletedMovementsDialog = true" />
        <Button label="Yeni Hareket Ekle" size="small" @click="openNewMovementDialog" />
      </div>
    </div>
    <div>
      <div v-if="loading" class="text-center py-8"><ProgressSpinner /></div>
      <div v-else-if="movements.length === 0" class="text-center py-8">
        <p class="text-gray-500">Henüz hareket kaydı bulunmamaktadır.</p>
      </div>
      <div v-else class="space-y-3">
        <Card v-for="movement in movements" :key="movement.id">
          <template #content>
            <div class="flex justify-between items-start max-lg:flex-col gap-4">
              <div>
                <div class="py-2 px-4 border-l-2 border-primary pl-4 font-medium text-sm mb-4">{{ movement.expedition_status?.name }}</div>
                <h6 class="font-medium mb-1">{{ movement.destination?.name }}</h6>
                <div class="mt-2">
                  <p class="text-xs text-gray-500 mb-2">
                    Oluşturulma Tarihi:
                    {{
                      new Date(movement.created_at).toLocaleString("tr-TR", {
                        day: "2-digit",
                        month: "long",
                        year: "numeric",
                        hour: "2-digit",
                        minute: "2-digit",
                      })
                    }}
                  </p>
                  <p class="text-xs text-gray-500 mb-2">
                    Oluşturan:
                    {{ movement.user?.name }} {{ movement.user?.surname }} ({{ movement.user?.email }})
                  </p>
                  <Badge v-if="movement.expedition_movement?.expedition" severity="primary" class="text-xs">
                    <CopyText> {{ movement.expedition_movement?.expedition?.expedition_number }} </CopyText> numaralı sefer hareketinin değişikliği sonrasında
                    otomatik oluşmuştur.
                  </Badge>
                </div>
              </div>
              <div class="flex justify-end gap-2 max-lg:w-full">
                <Button label="Sil" size="small" severity="danger" outlined class="max-lg:w-full" @click="confirmDeleteMovement(movement)" />
              </div>
            </div>
          </template>
        </Card>
      </div>
    </div>
    <!-- Yeni/Düzenleme Dialog -->
    <Dialog v-model:visible="showMovementDialog" :header="editMode ? 'Hareketi Düzenle' : 'Yeni Hareket Ekle'" modal class="w-full lg:w-140">
      <div class="pt-4 space-y-4">
        <div>
          <FloatLabel variant="on">
            <SelectAjax v-model="movementForm.expedition_status_id" :api="`/api/v1/expedition_status`" optionLabel="name" class="w-full" filter />
            <label>Durum</label>
          </FloatLabel>
        </div>
        <div>
          <FloatLabel variant="on">
            <SelectAjax v-model="movementForm.destination_id" :api="`/api/v1/destination`" optionLabel="name" class="w-full" filter />
            <label>Konum</label>
          </FloatLabel>
        </div>
        <div>
          <FloatLabel variant="on">
            <Textarea v-model="movementForm.address" autoResize class="w-full" />
            <label>Adres</label>
          </FloatLabel>
        </div>
        <div>
          <FloatLabel variant="on">
            <Textarea v-model="movementForm.description" autoResize class="w-full" />
            <label>Açıklama</label>
          </FloatLabel>
        </div>
      </div>
      <template #footer>
        <Button label="İptal" size="small" severity="secondary" @click="closeMovementDialog" />
        <Button :label="editMode ? 'Güncelle' : 'Kaydet'" size="small" @click="saveMovement" :loading="saving" />
      </template>
    </Dialog>

    <!-- Silinen Hareketler Dialog -->
    <Dialog v-model:visible="showDeletedMovementsDialog" :style="{ width: '700px' }" header="Silinen Hareketler" :modal="true">
      <div v-if="loadingDeleted" class="text-center py-8">
        <i class="pi pi-spin pi-spinner text-4xl"></i>
        <p class="mt-2">Yükleniyor...</p>
      </div>
      <div v-else-if="deletedMovements.length === 0" class="text-center py-8">
        <p class="text-gray-500">Silinen hareket bulunmamaktadır.</p>
      </div>
      <div v-else class="space-y-3">
        <Card v-for="movement in deletedMovements" :key="movement.id">
          <template #content>
            <div>
              <div class="py-2 px-4 border-l-2 rounded-r-lg border-primary pl-4 font-medium text-sm mb-4">
                {{ movement.expedition_status?.name }}
              </div>
              <div class="font-medium mb-4">{{ movement.destination?.name || "Belirtilmemiş" }}</div>
              <div class="space-y-2">
                <div class="text-xs text-gray-500 mb-1">
                  Oluşturan:
                  {{ movement.user?.name }} {{ movement.user?.surname }} ({{ movement.user?.email }})
                </div>
                <div class="text-xs text-gray-500 mb-2">
                  Oluşturulma Tarihi:
                  {{
                    new Date(movement.created_at).toLocaleString("tr-TR", {
                      day: "2-digit",
                      month: "2-digit",
                      year: "numeric",
                      hour: "2-digit",
                      minute: "2-digit",
                    })
                  }}
                </div>
                <div class="text-xs text-red-400 mb-2">
                  Silinme Tarihi:
                  {{
                    new Date(movement.deleted_at).toLocaleString("tr-TR", {
                      day: "2-digit",
                      month: "2-digit",
                      year: "numeric",
                      hour: "2-digit",
                      minute: "2-digit",
                    })
                  }}
                </div>
                <Badge v-if="movement.expedition_movement?.expedition" severity="primary" class="text-xs">
                  <CopyText> {{ movement.expedition_movement?.expedition?.expedition_number }} </CopyText> numaralı sefer hareketinin değişikliği sonrasında
                  otomatik oluşmuştur.
                </Badge>
              </div>
              <hr v-if="movement.description" class="my-4" />
              <div class="text-sm text-gray-500" v-if="movement.description">{{ movement.description }}</div>
            </div>
          </template>
        </Card>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from "vue";
import { useConfirm } from "primevue/useconfirm";
import { toast } from "vue-sonner";
import { useDataStore } from "@/stores/data_store";
import FloatLabel from "primevue/floatlabel";

const props = defineProps({
  loadData: {
    type: Object,
    required: true,
  },
});

// Stores ve Utilities
const dataStore = useDataStore();
const confirm = useConfirm();

// State
const movements = ref([]);
const deletedMovements = ref([]);
const loading = ref(true);
const loadingDeleted = ref(false);
const saving = ref(false);
const showMovementDialog = ref(false);
const showDeletedMovementsDialog = ref(false);
const editMode = ref(false);
const selectedMovement = ref(null);

// Form
const movementForm = ref({
  load_number: null,
  load_transfer_id: null,
  destination_id: null,
  expedition_status_id: null,
  description: "",
  address: "",
});

// Load ID değiştiğinde form'u güncelle
watch(
  () => props.loadData?.id,
  (newVal) => {
    if (newVal) {
      movementForm.value.load_number = newVal;
      fetchMovements();
    }
  }
);

// Metodlar
const fetchMovements = async () => {
  if (!props.loadData?.id) return;

  loading.value = true;
  try {
    const response = await dataStore.GET_LOAD_TRANSFER_MOVEMENTS({ load_transfer_id: props.loadData.id });
    movements.value = response?.data || [];
    deletedMovements.value = response?.deleted_movements || [];
  } catch (error) {
    console.error("Hareketler yüklenirken hata oluştu:", error);
    toast.error("Hareketler yüklenirken bir hata oluştu");
  } finally {
    loading.value = false;
  }
};

const openNewMovementDialog = () => {
  editMode.value = false;
  selectedMovement.value = null;
  resetForm();
  showMovementDialog.value = true;
};

const editMovement = (movement) => {
  editMode.value = true;
  selectedMovement.value = movement;
  movementForm.value = {
    id: movement.id,
    load_number: movement.load_relation.load_number,
    load_transfer_id: movement.load_transfer_id,
    destination_id: movement.destination,
    expedition_status_id: movement.expedition_status,
    description: movement.description || "",
    address: movement.address || "",
  };
  showMovementDialog.value = true;
};

const closeMovementDialog = () => {
  showMovementDialog.value = false;
  resetForm();
};

const resetForm = () => {
  movementForm.value = {
    load_number: props.loadData?.load_number_work_type,
    load_transfer_id: props.loadData?.id,
    destination_id: null,
    expedition_status_id: null,
    description: "",
    address: "",
  };
};

const saveMovement = async () => {
  // Basit form kontrolü
  if (!movementForm.value.destination_id) {
    toast.error("Lütfen varış noktası seçiniz");
    return;
  }

  if (!movementForm.value.expedition_status_id) {
    toast.error("Lütfen durum seçiniz");
    return;
  }

  saving.value = true;
  try {
    // Form verilerini hazırla
    const form_data = new FormData();

    console.log(props.loadData);

    if (movementForm.value.id) {
      form_data.append("id", movementForm.value.id);
    }
    form_data.append("load_number", movementForm.value.load_number);
    form_data.append("load_transfer_id", movementForm.value.load_transfer_id);
    form_data.append("destination_id", movementForm.value.destination_id.id);
    form_data.append("expedition_status_id", movementForm.value.expedition_status_id.id);
    form_data.append("description", movementForm.value.description);
    form_data.append("address", movementForm.value.address);

    if (editMode.value) {
      await dataStore.UPDATE_LOAD_TRANSFER_MOVEMENT({ data: form_data });
      toast.success("Hareket başarıyla güncellendi");
    } else {
      await dataStore.CREATE_LOAD_TRANSFER_MOVEMENT({ data: form_data });
      toast.success("Yeni hareket başarıyla eklendi");
    }
    closeMovementDialog();
    fetchMovements();
  } catch (error) {
    console.error("Hareket kaydedilirken hata oluştu:", error);
    toast.error("Hareket kaydedilirken bir hata oluştu");
  } finally {
    saving.value = false;
  }
};

const confirmDeleteMovement = (movement) => {
  confirm.require({
    message: "Bu hareketi silmek istediğinizden emin misiniz?",
    header: "Silme Onayı",
    acceptProps: {
      severity: "danger",
      size: "small",
      label: "Sil",
    },
    rejectProps: {
      severity: "secondary",
      size: "small",
      label: "İptal",
    },
    accept: () => deleteMovement(movement),
    reject: () => {},
  });
};

const deleteMovement = async (movement) => {
  try {
    await dataStore.DELETE_LOAD_TRANSFER_MOVEMENT(movement.id);
    toast.success("Hareket başarıyla silindi");
    fetchMovements();
  } catch (error) {
    console.error("Hareket silinirken hata oluştu:", error);
    toast.error("Hareket silinirken bir hata oluştu");
  }
};

const restoreMovement = async (movement) => {
  // Burada silinen hareketi geri yüklemek için bir API çağrısı yapılacak
  // Gerçek implementasyonda backend'de restore() metodunu kullanarak silinen kaydı geri yükleyen bir endpoint oluşturulmalı
  toast.info("Bu özellik henüz uygulanmadı");
};

// Component yüklendiğinde
onMounted(() => {
  if (props.loadData?.id) {
    movementForm.value.load_number = props.loadData.load_number_work_type;
    movementForm.value.load_transfer_id = props.loadData.id;
    fetchMovements();
  }
});
</script>
