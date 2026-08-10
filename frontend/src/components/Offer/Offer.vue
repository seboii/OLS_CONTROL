<template>
  <div>
    <div class="mb-4 lg:mb-8 flex justify-between lg:items-center gap-6 max-lg:flex-col">
      <div>
        <h2 class="text-gray-800 font-medium text-4xl tracking-tight">Teklifler</h2>
        <div class="text-gray-500 text-sm mt-1">Sistemde kayıtlı yapılan teklifleri ve teklif taleplerini inceleyebilirsiniz.</div>
      </div>
      <div class="max-lg:w-full flex lg:items-center justify-end gap-1 lg:gap-2">
        <Button size="small" @click="analysis_mail_modal_status = true" class="max-lg:w-full" label="Mail Analizi" />
        <Button size="small" v-if="false" @click="analysisMail" class="max-lg:w-full" label="Mailleri Analiz Et" />
        <Button size="small" @click="offer_drawer_status = true" class="max-lg:w-full" label="Yeni Teklif Oluştur" />
      </div>
    </div>
    <div>
      <Tabs :value="active_detail_tab" scrollable lazy>
        <TabList>
          <Tab value="4">Talep</Tab>
          <Tab value="5">Olumlu</Tab>
          <Tab value="1">Olumsuz</Tab>
          <Tab value="2">Sipariş</Tab>
          <Tab value="3">Düzeltme Talebi</Tab>
          <Tab value="6">Zaman Aşımı</Tab>
        </TabList>
        <TabPanels>
          <TabPanel value="4">
            <Card>
              <template #content>
                <OfferTable ref="load_status_4_table" :statusType="4" @onRowSelect="onRowSelect" />
              </template>
            </Card>
          </TabPanel>
          <TabPanel value="5">
            <Card>
              <template #content>
                <OfferTable ref="load_status_5_table" :statusType="5" @onRowSelect="onRowSelect" />
              </template>
            </Card>
          </TabPanel>
          <TabPanel value="1">
            <Card>
              <template #content>
                <OfferTable ref="load_status_1_table" :statusType="1" @onRowSelect="onRowSelect" />
              </template>
            </Card>
          </TabPanel>
          <TabPanel value="2">
            <Card>
              <template #content>
                <OfferTable ref="load_status_2_table" :statusType="2" @onRowSelect="onRowSelect" />
              </template>
            </Card>
          </TabPanel>
          <TabPanel value="3">
            <Card>
              <template #content>
                <OfferTable ref="load_status_3_table" :statusType="3" @onRowSelect="onRowSelect" />
              </template>
            </Card>
          </TabPanel>
          <TabPanel value="6">
            <Card>
              <template #content>
                <OfferTable ref="load_status_6_table" :statusType="6" :timeout="true" @onRowSelect="onRowSelect" />
              </template>
            </Card>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
    <Dialog
      v-model:visible="analysis_mail_modal_status"
      modal
      header="Mail Analizi"
      class="w-full lg:w-[600px]"
      @show="onMailAnalysisModalShow"
      @hide="onMailAnalysisModalHide"
    >
      <div class="mb-4">
        <SelectButton
          v-model="mail_anaylsis_tab_value"
          :options="mail_analysis_options"
          optionLabel="title"
          class="w-full *:w-full"
          fluid
          :allowEmpty="false"
        />
      </div>
      <div class="relative h-[700px] overflow-auto">
        <Transition
          enter-active-class="transition duration-500 absolute"
          enter-from-class="-translate-x-full opacity-0"
          enter-to-class="translate-x-0 opacity-100"
          leave-active-class="transition duration-500 absolute"
          leave-from-class="translate-x-0 opacity-100"
          leave-to-class="translate-x-full opacity-0"
        >
          <div v-if="mail_anaylsis_tab_value.value == 1" class="bg-white w-full h-full flex flex-col">
            <div class="mb-4 w-full h-full">
              <Textarea
                v-model="analysis_mail_content"
                :disabled="analysis_mail_loading"
                :loading="analysis_mail_loading"
                rows="10"
                cols="30"
                class="resize-none h-full"
                fluid
              />
            </div>
          </div>
          <div v-else-if="mail_anaylsis_tab_value.value == 2" class="bg-white w-full h-full">
            <div class="relative w-full h-full overflow-auto">
              <div class="h-full">
                <div v-if="false">
                  <div @click="analysis_mail_status = 'analysis'">analiz</div>
                  <div @click="analysis_mail_status = 'success'">succes</div>
                  <div @click="analysis_mail_status = 'default'">default</div>
                </div>
                <Transition
                  enter-active-class="transition-all duration-500 absolute top-0 left-0"
                  enter-from-class="blur-md opacity-0"
                  enter-to-class="blur-none opacity-100"
                  leave-active-class="transition-all duration-500 absolute top-0 left-0"
                  leave-from-class="blur-none opacity-100"
                  leave-to-class="blur-md opacity-0"
                  mode="out-in"
                >
                  <div v-if="analysis_mail_status == 'analysis'" class="w-full h-full">
                    <div class="flex flex-col items-center justify-center">
                      <Vue3Lottie :animationData="MailAnalyzeAiLottie" :height="300" :width="300" />
                      <div class="text-gray-500">Analiz Ediliyor</div>
                    </div>
                  </div>
                  <div v-else-if="analysis_mail_status == 'success'" class="w-full h-full">
                    <Vue3Lottie :animationData="MailAnalyzeSuccessLottie" :height="300" :width="300" />
                  </div>
                  <div v-else-if="analysis_mail_status == 'result'" class="w-full h-full">
                    <div v-html="analysis_mail_result" class="whitespace-pre-line pb-8 text-sm"></div>
                  </div>
                  <div v-else-if="analysis_mail_status == 'default'" class="w-full h-full flex flex-col justify-between">
                    <div class="mb-4 h-full">
                      <Textarea v-model="analysis_mail_content" rows="10" cols="30" class="resize-none h-full" fluid />
                    </div>
                  </div>
                </Transition>
              </div>
            </div>
          </div>
        </Transition>
      </div>
      <div class="w-full mt-4">
        <Button
          type="button"
          :label="analysis_mail_loading ? `Analiz Ediliyor` : 'Analiz Et'"
          :disabled="analysis_mail_loading"
          :loading="analysis_mail_loading"
          @click="analysisMail"
          fluid
        />
      </div>
    </Dialog>
    <OfferFormDrawer v-model:visible="offer_drawer_status" v-model:offer-id="selected_offer_id" @formSubmit="refreshDatatable" @onHideDrawer="onHideDrawer" />
  </div>
</template>

<script setup>
import { ref, watch, onMounted, provide } from "vue";
import OpenAI from "openai";
import { toast } from "vue-sonner";
import OfferFormDrawer from "@/components/Offer/OfferFormDrawer.vue";
import OfferTable from "@/components/Offer/OfferTable.vue";
import { Add01Icon, Delete01Icon, Mail01Icon } from "hugeicons-vue";
import { useDataStore } from "@/stores/data_store.js";
import { Vue3Lottie } from "vue3-lottie";
import MailAnalyzeAiLottie from "@/assets/lottie/mail-analyze-ai.json";
import MailAnalyzeSuccessLottie from "@/assets/lottie/mail-analyze-success.json";
import { useRouter } from "vue-router";

const Router = useRouter();

// VITE_OPENAI_API_KEY tanımsızken constructor senkron hata fırlatıyor;
// üst seviyede çağrılırsa tüm bileşenin setup()'ı çöküp sayfa boş kalıyordu.
// Sadece gerçekten kullanılacağı yerde (analysisMail) örnekleniyor.
const getOpenAiClient = () =>
  new OpenAI({
    apiKey: import.meta.env.VITE_OPENAI_API_KEY,
    dangerouslyAllowBrowser: true,
  });

const mail_anaylsis_tab_value = ref({
  title: "Yük Oluşturma",
  value: 1,
});
const mail_analysis_options = ref([
  {
    title: "Yük Oluşturma",
    value: 1,
  },
  {
    title: "Analiz",
    value: 2,
  },
]);

const active_detail_tab = ref("4");

const offer_drawer_status = ref(false);
const selected_offer_id = ref(null);

const DataStore = useDataStore();

const load_status_1_table = ref(null);
const load_status_2_table = ref(null);
const load_status_3_table = ref(null);
const load_status_4_table = ref(null);
const load_status_5_table = ref(null);
const load_status_6_table = ref(null);

const refreshDatatable = (data) => {
  const { status_type } = data;
  if (status_type.id == 1) {
    load_status_1_table.value?.refresh();
  }
  if (status_type.id == 2) {
    load_status_2_table.value?.refresh();
  }
  if (status_type.id == 3) {
    load_status_3_table.value?.refresh();
  }
  if (status_type.id == 4) {
    load_status_4_table.value?.refresh();
  }
  if (status_type.id == 5) {
    load_status_5_table.value?.refresh();
  }
  load_status_6_table.value?.refresh();
};

const onRowSelect = async (data) => {
  let offer_id = data.data.id;
  selected_offer_id.value = offer_id;
  offer_drawer_status.value = true;
  Router.push({
    query: {
      offer_id: offer_id,
    },
  });
};

const onHideDrawer = () => {};

const analysis_mail_loading = ref(false);
const analysis_mail_content = ref("");
const analysis_mail_modal_status = ref(false);
const analysis_mail_status = ref("default");
const analysis_mail_success = ref(false);
const analysis_mail_result = ref("");
const onMailAnalysisModalShow = () => {};
const onMailAnalysisModalHide = () => {
  analysis_mail_status.value = "default";
  analysis_mail_content.value = "";
  analysis_mail_result.value = "";
};
const analysisMail = async () => {
  if (analysis_mail_content.value == "") {
    toast.error("Lütfen bir mail içeriği giriniz.");
    return;
  }
  analysis_mail_status.value = "analysis";
  if (mail_anaylsis_tab_value.value.value == 1) {
    analysis_mail_loading.value = true;
    const openai = getOpenAiClient();
    const response = await openai.chat.completions.create({
      messages: [
        {
          role: "system",
          content: [
            {
              text: 'Aşağıdaki yük teklifi e-postasını analiz et ve JSON formatında bir çıktı oluştur. JSON çıktısı, aşağıdaki alanları içermeli ve her biri verilen e-posta verilerine uygun şekilde doldurulmalıdır: Eğer teklif içeriğine benzemeyen mail ise response olarak status false olarak döndür. Eğer sana herhangi bir iletişim bilgisi verirse , bunu not kısmına ekle. Mail içeriği her dilde gelebilir. \n\n### Alanlar\n- reservation_number: (E-postada belirtilmez, boş bırak)\n- load_number: (E-postada belirtilmez, boş bırak)\n- work_type_id: İşlem tipi (ör. İhracat veya İthalat, uygun bir ID döndür)\n- loading_type_id: (E-postada belirtilmez, boş bırak)\n- payment_type_id: Ödeme tipi (E-postada belirtilmez, boş bırak)\n- status_type_id: (E-postada belirtilmez, boş bırak)\n- offer_date: (E-postada belirtilmez, boş bırak)\n- offer_validity_date: (E-postada belirtilmez, boş bırak)\n- marketing_notification_date: (E-postada belirtilmez, boş bırak)\n- customer_id: Firma Adı\n- description: (E-postanın açıklama kısmından türet, ör. "Elektronik ürün taşıma teklifi.")\n- departure_country_id: (Kalkış Ülkesi , isim döndür, yoksa boş bırak)\n- transit_country_id: (E-postada belirtilmez, boş bırak)\n- target_country_id: (Varış Ülkesi , isim döndür, yoksa boş bırak)\n- department_id: (E-postada belirtilmez, boş bırak)\n-front_transportation_by_us\n-final_transportation_by_us\n-load_transfer_type_id\n-company_pay_freight_id\n-instruction_id\n-romork_type_id\n\n### Ürünler İçin:\nHer bir ürün için şu alanları JSON içerisinde oluştur:\n- product_type_id: Ürün adı (ör. Elektronik Ürün, uygun isim döndür)\n- case_type_id: (E-postada belirtilmez, boş bırak)\n- quantity: Ürün adedi\n- width: (E-postada belirtilmez, boş bırak)\n- height: (E-postada belirtilmez, boş bırak)\n- length: (E-postada belirtilmez, boş bırak)\n- gross_weight: Brüt ağırlık (KG)\n- net_weight: Net ağırlık (KG)\n- volume: Hacim (E-postada verilen Lademetre bilgisi)\n- lademeter: Lademetre\n- stackable: (E-postada belirtilmez, boş bırak)\n\n### Ek Alanlar:\n- buysell: (E-postada belirtilmez, boş bırak)\n- item_type_id: (E-postada belirtilmez, boş bırak)\n- item: Ürün adı (ör. Elektronik Ürün)\n- transport_type_id: (E-postada belirtilmez, boş bırak)\n- status: (E-postada belirtilmez, boş bırak)\n- order: (E-postada belirtilmez, boş bırak)\n- net_price: (E-postada belirtilmez, boş bırak)\n- tax_price: (E-postada belirtilmez, boş bırak)\n- total_price: (E-postada belirtilmez, boş bırak)\n- currency: (E-postada belirtilmez, boş bırak)\n- account_id\n\n### Örnek E-posta:\nFirma Adı : Jeetwork Lojistik  \nİşlem Tipi : İhracat  \nİşlem Tarihi : 20.12.2024  \nKalkış : Türkiye  \nVarış : Rusya  \nÖdeme Tipi : Peşin  \n\nÜrünler:  \n1- Elektronik Ürün , Net : 20 KG , Brüt : 30 KG , Lademetre : 5 , 3 Adet  \n\n### Beklenen JSON Çıktısı:\n{\n  "reservation_number": null,\n  "load_number": null,\n  "work_type_id": "İhracat",\n  "loading_type_id": null,\n  "payment_type_id": "Peşin",\n  "status_type_id": null,\n  "offer_date": null,\n  "offer_validity_date": null,\n  "marketing_notification_date": null,\n  "customer_id": null,\n  "sender_id": null,\n  "receiver_id": null,\n  "agent_id": null,\n  "payer_company": "Jeetwork Lojistik",\n  "description": "Elektronik ürün taşıma teklifi.",\n  "departure_country_id": "Türkiye",\n  "transit_country_id": null,\n  "target_country_id": "Rusya",\n  "department_id": null,\n  "products": [\n    {\n      "product_type_id":null,\n      "case_type_id": null,\n      "quantity": 3,\n      "width": null,\n      "height": null,\n      "length": null,\n      "gross_weight": 30,\n      "net_weight": 20,\n      "volume": 5,\n      "lademeter": 5,\n      "stackable": null\n    }\n  ],\n  "buysell": null,\n  "item_type_id": null,\n  "item": "Elektronik Ürün",\n  "quantity": 3,\n  "transport_type_id": null,\n  "status": null,\n  "order": null,\n  "net_price": null,    // Eğer fiyat verilmişse buraya eklenir.\n  "tax_price": null,    // Eğer vergi fiyatı verilmişse buraya eklenir.\n  "total_price": null,  // Eğer toplam fiyat verilmişse buraya eklenir.\n  "currency": null\n}\n',
              type: "text",
            },
          ],
        },
        {
          role: "user",
          content: [
            {
              type: "text",
              text: analysis_mail_content.value,
            },
          ],
        },
      ],
      model: "gpt-4o-mini",
      response_format: {
        type: "json_schema",
        json_schema: {
          name: "reservation",
          strict: false,
          schema: {
            type: "object",
            required: ["offer_date"],
            properties: {
              item: {
                type: "string",
                description: "Name of the item.",
              },
              order: {
                type: "null",
                description: "Order associated with the reservation.",
              },
              status: {
                type: "null",
                description: "Current status of the reservation.",
              },
              buysell: {
                type: "null",
                description: "Indicates if the item is for buy or sell.",
              },
              agent_id: {
                type: "null",
                description: "Identifier for the agent.",
              },
              currency: {
                type: "null",
                description: "Currency used for the reservation.",
              },
              products: {
                type: "array",
                items: {
                  type: "object",
                  required: [
                    "product_type_id",
                    "case_type_id",
                    "quantity",
                    "width",
                    "height",
                    "length",
                    "gross_weight",
                    "net_weight",
                    "volume",
                    "lademeter",
                    "stackable",
                  ],
                  properties: {
                    width: {
                      type: "null",
                      description: "Width of the product.",
                    },
                    height: {
                      type: "null",
                      description: "Height of the product.",
                    },
                    length: {
                      type: "null",
                      description: "Length of the product.",
                    },
                    volume: {
                      type: "number",
                      description: "Volume of the product.",
                    },
                    quantity: {
                      type: "number",
                      description: "Quantity of the product.",
                    },
                    lademeter: {
                      type: "number",
                      description: "Lademeter length of the product.",
                    },
                    stackable: {
                      type: "null",
                      description: "Indicates if the product is stackable.",
                    },
                    net_weight: {
                      type: "number",
                      description: "Net weight of the product.",
                    },
                    case_type_id: {
                      type: "null",
                      description: "Identifier for the case type.",
                    },
                    gross_weight: {
                      type: "number",
                      description: "Gross weight of the product.",
                    },
                    product_type_id: {
                      type: "string",
                      description: "Identifier for the type of product.",
                    },
                  },
                  additionalProperties: false,
                },
                description: "List of products associated with the reservation.",
              },
              quantity: {
                type: "number",
                description: "Quantity of the item.",
              },
              net_price: {
                type: "null",
                description: "Net price if a price is provided.",
              },
              sender_id: {
                type: "null",
                description: "Identifier for the sender.",
              },
              tax_price: {
                type: "null",
                description: "Tax price if a tax price is provided.",
              },
              offer_date: {
                type: "null",
                description: "Date of the offer.",
              },
              customer_id: {
                type: "string",
                description: "Identifier for the customer.",
              },
              description: {
                type: "string",
                description: "Description of the reservation or offering.",
              },
              load_number: {
                type: "null",
                description: "The number associated with the load.",
              },
              receiver_id: {
                type: "null",
                description: "Identifier for the receiver.",
              },
              total_price: {
                type: "null",
                description: "Total price if a total price is provided.",
              },
              item_type_id: {
                type: "null",
                description: "Identifier for the item type.",
              },
              work_type_id: {
                type: "string",
                description: "Identifier for the type of work.",
              },
              department_id: {
                type: "null",
                description: "Identifier for the department.",
              },
              payer_company: {
                type: "null",
                description: "Name of the payer company.",
              },
              status_type_id: {
                type: "null",
                description: "Identifier for the status type.",
              },
              loading_type_id: {
                type: "null",
                description: "Identifier for the loading type.",
              },
              payment_type_id: {
                type: "string",
                description: "Identifier for the type of payment.",
              },
              target_country_id: {
                type: "string",
                description: "Identifier for the target country.",
              },
              transport_type_id: {
                type: "null",
                description: "Identifier for the transport type.",
              },
              reservation_number: {
                type: "null",
                description: "The unique identifier for the reservation.",
              },
              transit_country_id: {
                type: "null",
                description: "Identifier for the transit country.",
              },
              offer_validity_date: {
                type: "null",
                description: "Validity date of the offer.",
              },
              departure_country_id: {
                type: "string",
                description: "Identifier for the departure country.",
              },
              marketing_notification_date: {
                type: "null",
                description: "Notification date for marketing.",
              },
              front_transportation_by_us: {
                type: "null",
                description: "",
              },
              final_transportation_by_us: {
                type: "null",
                description: "",
              },
              load_transfer_type_id: {
                type: "null",
                description: "",
              },
              company_pay_freight_id: {
                type: "null",
                description: "",
              },
              instruction_id: {
                type: "null",
                description: "",
              },
              romork_type_id: {
                type: "null",
                description: "",
              },
            },
            additionalProperties: false,
          },
        },
      },
      temperature: 1,
      max_tokens: 10000,
      top_p: 1,
      frequency_penalty: 0,
      presence_penalty: 0,
    });
    let response_data = JSON.parse(response.choices[0].message.content);
    console.log(response_data);
    if (response_data.status == false) {
      toast.error("Mail analizi başarısız oldu. Lütfen geçerli bir mail içeriği giriniz.");
    } else {
      const res = await DataStore.CREATE_LOAD_AI({ data: response_data });
      toast.success("Mail analizi başarılı bir şekilde gerçekleştirildi, yük teklifi oluşturuldu.");
      analysis_mail_modal_status.value = false;
    }
    analysis_mail_loading.value = false;
  } else if (mail_anaylsis_tab_value.value.value == 2) {
    const openai = getOpenAiClient();
    const response = await openai.chat.completions.create({
      messages: [
        {
          role: "system",
          content: [
            {
              type: "text",
              text: "Sen OLS Firmasının maillerini analiz ederek, gelen maillerin bir fiyat talebi mi ya da başka içerikli bir mail mi olduğunu anlayan bir analistsin. Buna göre seninle bir mail paylaşacağım, bu maili analiz edip maili gönderen kişinin kim olduğunu, ne istediğini ve diğer detayları bana açıklamanı istiyorum. Bu e-postayı analiz et ve bir çıktı oluştur. Eğer  gelen mailin içeriği teklif talebine benzemiyorsa, response olarak status false olarak döndür  ve mesaj olarak da bu mailin ne ile ilgili olduğunu yaz. Mailin dili bir çok farklı dilde olabilir, ancak ağırlıklı olarak Türkçe, Rusça ve İngilizce mailler geliyor. \n\nEğer taşıma işlemi başka bir ülkeden Türkiye’ye gelecekse İthalat, Türkiyeden çıkıp başka bir ülkeye gidecekse ihracat, eğer bir yük Türkiye dışından bir ülkeden çıkıp Türkiye’ye uğradıktan sonra yine başka bir ülkeye gidecekse transit, Türkiye‘den çıkıp yine Türkiye’de kalacaksa yurt içi olarak belirlenmeli. Eğer bir yükün çıkış noktası Rusya'da herhangi bir yer ise ve varış lokasyonu Türkiye dışında bir yer ise de mutlaka transit olarak değerlendirmelisin. İthalat'ın id karşılığı 1, ihracatın 2, transitin 3, yurtiçinin 4 olacak.\n\nOluşturduğun çıktı, yük hakkında elde edebileceğin bütün bilgileri içersin, bu çıktı mailin dilinden bağımsız olarak TÜrkçe olmalı ve verdiğin çıktı şöyle olsun:\n\nMerhaba, bu maili analiz ettim, gönderilmiş olan mailde.....\nşeklinde detaylıca açıkla, yükün tipi, nereden nereye gideceği, yükün kaç parçadan oluştuğu ne kadar bir ağırlığı olduğu, ölçüleri varsa lademetre ve desi hesapları gibi detaylarını çok güzel ve kurumsal şekilde anlat.\n\nEk olarak yine kurumsal bir formatta çok güzel bir şekilde ek olarak neler yapılabilecğeini, müşteri memnuniyeti için bu yüke en hızlı ve uygun teklifin nasıl verilmesi gerektiğini kısaca anlatan bir bilgi notu da yaz.\n\nBu arada maile kurumsal bir dille cevap verebilmek için bana bir mail taslağı da oluştur. Kurumsal ve kalitekli bir anlatım dili olmalı. Maili orijinal dilinde yazabilirsin.\n\nBana verdiğin cevapta mailin analizi, bilgi notu ve kurumsal mail cevabı üç farklı bölümde olmalı.  Tüm cevanı tek metin olarak ver.",
            },
          ],
        },
        {
          role: "user",
          content: [
            {
              type: "text",
              text: analysis_mail_content.value,
            },
          ],
        },
      ],
      model: "gpt-4o-mini",
      temperature: 1,
      max_tokens: 10000,
      top_p: 1,
      frequency_penalty: 0,
      presence_penalty: 0,
    });
    let response_data = response.choices[0].message.content;
    analysis_mail_result.value = response.choices[0].message.content;
    if (response_data.status == false) {
      toast.error("Mail analizi başarısız oldu. Lütfen geçerli bir mail içeriği giriniz.");
    } else {
      toast.success("Mail analizi başarılı bir şekilde gerçekleştirildi.");
    }
    analysis_mail_status.value = "result";
    analysis_mail_content.value = "";
  }
};
watch(analysis_mail_modal_status, (value) => {
  if (!value) {
    analysis_mail_success.value = true;
    setTimeout(() => {
      analysis_mail_success.value = false;
    }, 300);
  }
});
</script>
