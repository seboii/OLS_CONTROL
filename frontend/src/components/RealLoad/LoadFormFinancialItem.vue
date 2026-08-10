<template>
  <div>
    <div class="rounded-xl p-5 lg:py-5 lg:px-8 border bg-white shadow-xs flex items-start lg:items-end justify-between max-lg:flex-col gap-4">
      <div class="w-full">
        <div class="flex items-center gap-2 mb-3">
          <div class="font-semibold">{{ index + 1 }}.Hareket</div>
          <Badge v-if="!data.id" severity="success">Yeni</Badge>
        </div>
        <div class="flex flex-wrap gap-2 max-lg:flex-col">
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Ürün : <span class="font-medium">{{ data.item?.name }}</span>
          </div>
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Adet : <span class="font-medium">{{ parseFloat(data.quantity) }}</span>
          </div>
          <div class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg">
            Toplam Tutar : <span class="font-medium">{{ data.total_price }} {{ data.currency?.code }}</span>
          </div>
          <div v-if="data.load_transfer_einvoice" class="text-xs text-gray-500 py-2 px-3 bg-gray-50 rounded-lg flex items-center gap-2">
            <div class="size-2 rounded-full bg-green-500"></div>
            <div class="font-medium">Faturası Var</div>
          </div>
        </div>
      </div>
      <div class="flex items-center justify-end gap-2">
        <Button @click="item_edit_dialog_visible = true" size="small" outlined label="Düzenle" />
        <Button @click="emits('delete', data)" severity="danger" outlined size="small" label="Sil" class="px-6!" />
      </div>
    </div>
    <Dialog v-model:visible="item_edit_dialog_visible" modal header=" " class="w-full lg:w-[700px]">
      <div>
        <div class="text-xl lg:text-2xl font-semibold mb-1 text-gray-900">{{ index + 1 }}.Ürün Bilgileri</div>
        <div class="text-sm text-gray-500">Ürün bilgilerini eksiksiz ve doğru girmeye özen gösteriniz.</div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mt-10">
        <div class="flex justify-center w-full lg:col-span-2">
          <SelectButton v-model="data.buysell" :options="buysell_types" optionLabel="name" class="w-full [&>.p-togglebutton]:w-full" />
        </div>
        <div v-if="false">
          <FloatLabel variant="on">
            <SelectAjax v-model="data.item_type_id" :api="`/api/v1/item_type`" optionLabel="name" class="w-full" filter />
            <label for="username">Kalem Türü</label>
          </FloatLabel>
        </div>
        <FloatLabel variant="on">
          <SelectAjax
            v-model="data.item"
            :api="`/api/v1/financial_item`"
            :fetchParams="{
              type: data.buysell?.value,
            }"
            optionLabel="name"
            class="w-full"
            filter
          >
            <template v-if="usePermissionStatus('financial_item_management').read" #footer>
              <div class="p-2">
                <Button @click="FeatureStore.SET_LOAD_FINANCIAL_ITEMS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
              </div>
            </template>
          </SelectAjax>
          <label>Kalem</label>
        </FloatLabel>
        <div>
          <FloatLabel variant="on">
            <SelectAjax
              v-model="data.account_id"
              :api="`/api/v1/account`"
              :fetchParams="{
                account_type_id: data.buysell == 1 ? 2 : 1,
              }"
              optionLabel="name"
              class="w-full"
              filter
            >
              <template #option="slotProps">
                <div class="flex items-center gap-2">
                  <div class="size-8 rounded-lg overflow-hidden border shadow-xs bg-gray-100">
                    <img v-if="slotProps.option.avatar" :src="`/storage/${slotProps.option.avatar}`" class="w-full h-full object-cover" />
                  </div>
                  <div class="text-sm">{{ slotProps.option.name }}</div>
                </div>
              </template>
            </SelectAjax>
            <label for="username">{{ data.buysell == 1 ? "Tedarikçiler" : "Müşteriler" }}</label>
          </FloatLabel>
        </div>
        <FloatLabel class="w-full" variant="on">
          <InputText @value-change="() => calculateSingleTotalPrice(data)" v-model="data.quantity" v-keyfilter.int fluid />
          <label> Adet </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputNumber @value-change="() => calculateSingleTotalPrice(data)" v-model="data.net_price" :maxFractionDigits="2" v-keyfilter.money fluid />
          <label> Fiyat </label>
        </FloatLabel>
        <FloatLabel v-if="false" class="w-full" variant="on">
          <InputNumber v-model="data.tax_price" :maxFractionDigits="2" v-keyfilter.money fluid />
          <label> Vergi Tutarı </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputNumber v-model="data.total_price" :maxFractionDigits="2" v-keyfilter.money fluid />
          <label> Toplam Tutar </label>
        </FloatLabel>
        <div>
          <FloatLabel variant="on">
            <SelectAjax v-model="data.currency" :api="`/api/v1/currency`" :optionLabel="(e) => `${e.code} - ${e.name}`" class="w-full" filter>
              <template #header>
                <div class="grid grid-cols-2 lg:grid-cols-3 gap-1.5 p-2.5 border-b">
                  <Button
                    v-for="(item, index) in define_datas.currency"
                    :key="index"
                    :label="item.code"
                    :severity="data.currency?.code == item.code ? '' : 'secondary'"
                    class="py-1.5! text-sm!"
                    @click="data.currency = item"
                  />
                </div>
              </template>
              <template #option="slotProps">
                <div>{{ slotProps.option.code }} - {{ slotProps.option.name }}</div>
              </template>
            </SelectAjax>
            <label for="username">Para Birimi</label>
          </FloatLabel>
        </div>
        <div class="w-full lg:col-span-2">
          <FloatLabel class="w-full" variant="on">
            <Select
              v-model="data.status"
              :options="
                financial_item_status_type.filter((item) => {
                  if (data.buysell?.value == 1) {
                    return item.id != 'invoice_issued';
                  } else {
                    return item.id != 'invoice_received';
                  }
                })
              "
              optionLabel="name"
              fluid
            />
            <label for="username">Durum</label>
          </FloatLabel>
        </div>
        <FloatLabel class="w-full lg:col-span-2" variant="on">
          <Textarea v-model="data.description" autoResize fluid />
          <label> Açıklama </label>
        </FloatLabel>
      </div>
    </Dialog>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { buysell_types, financial_item_status_type, define_datas } from "@/data/system_data.js";
import { usePermissionStatus } from "@/composables/index.js";
import { useFeatureStore } from "@/stores/general_store.js";

const FeatureStore = useFeatureStore();
const props = defineProps(["data", "index"]);
const emits = defineEmits(["delete"]);
const item_edit_dialog_visible = ref(false);

const calculateSingleTotalPrice = (item) => {
  if (item.net_price && item.quantity) {
    item.total_price = item.net_price * item.quantity;
  }
};
</script>
