<template>
  <div>
    <div class="rounded-xl p-5 lg:py-5 lg:px-8 border bg-white shadow-xs flex items-start lg:items-end justify-between max-lg:flex-col gap-4">
      <div class="w-full">
        <div class="flex items-center gap-2 mb-3">
          <div class="font-semibold">{{ index + 1 }}.Ürün</div>
          <Badge v-if="!data.id" severity="success">Yeni</Badge>
        </div>
        <div class="flex flex-wrap gap-2 max-lg:flex-col">
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Tip : <span class="font-medium">{{ data.product_type_id?.name }}</span>
          </div>
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Adet : <span class="font-medium">{{ parseFloat(data.quantity) }}</span>
          </div>
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Hacim : <span class="font-medium">{{ data.volume }}m³</span>
          </div>
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Lademetre : <span class="font-medium">{{ data.lademeter }}</span>
          </div>
        </div>
      </div>
      <div class="flex items-center justify-end gap-2 max-lg:w-full">
        <Button @click="item_edit_dialog_visible = true" size="small" outlined :label="editable ? 'Düzenle' : 'Görüntüle'" class="max-lg:w-full" />
        <Button v-if="editable" @click="emits('delete', data)" severity="danger" outlined size="small" label="Sil" class="px-6! max-lg:w-full" />
      </div>
    </div>
    <Dialog v-model:visible="item_edit_dialog_visible" modal header=" " class="w-full lg:w-[700px]">
      <div>
        <div class="text-xl lg:text-2xl font-semibold mb-1 text-gray-900">{{ index + 1 }}.Ürün Bilgileri</div>
        <div class="text-sm text-gray-500">Ürün bilgilerini eksiksiz ve doğru girmeye özen gösteriniz.</div>
      </div>
      <div class="grid grid-cols-2 gap-4 mt-10 mb-4">
        <div class="max-lg:col-span-2">
          <FloatLabel variant="on">
            <SelectAjax
              v-model="data.product_type_id"
              :api="`/api/v1/product_type`"
              optionLabel="name"
              class="w-full"
              :disabled="!editable"
              filter
            ></SelectAjax>
            <label for="username">Mal Cinsi</label>
          </FloatLabel>
        </div>
        <div class="max-lg:col-span-2">
          <FloatLabel variant="on">
            <SelectAjax v-model="data.case_type_id" :api="`/api/v1/case_type`" optionLabel="name" class="w-full" :disabled="!editable" filter />
            <label for="username">Kap Cinsi</label>
          </FloatLabel>
        </div>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.quantity" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Adet </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.width" @update:modelValue="calculateLDM" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> En (cm)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.length" @update:modelValue="calculateLDM" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Boy (cm)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.height" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Yükseklik (cm)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.gross_weight" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Brüt Ağırlık (kg)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.net_weight" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Net Ağırlık (kg)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.volume" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Hacim </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="data.lademeter" min="0" fluid v-keyfilter.num :readonly="!editable" />
          <label> Lademetre </label>
        </FloatLabel>
        <div class="col-span-2">
          <SelectButton
            v-model="data.stackable"
            :options="product_stackable_types"
            optionLabel="name"
            class="w-full [&>.p-togglebutton]:w-full"
            :disabled="!editable"
            fluid
          />
        </div>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { product_stackable_types } from "@/data/system_data.js";

const props = defineProps(["data", "index", "editable"]);
const emits = defineEmits(["delete"]);
const item_edit_dialog_visible = ref(false);

const calculateLDM = () => {
  props.data.lademeter = (((props.data.width * props.data.length) / 24000) * props.data.quantity).toFixed(2);
};
</script>
