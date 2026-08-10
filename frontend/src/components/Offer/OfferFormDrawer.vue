<template>
  <div>
    <Drawer v-model:visible="offer_drawer_visible" header=" " position="bottom" class="min-h-svh" @show="onShowDrawer" @hide="onHideDrawer">
      <div v-if="page_loading" class="flex items-center justify-center max-lg:py-20 lg:min-h-full">
        <ProgressSpinner class="size-20!" />
      </div>
      <div v-else class="lg:mx-auto lg:max-w-(--breakpoint-lg)">
        <div class="flex justify-between gap-4 max-lg:flex-col">
          <div class="flex items-center gap-4">
            <div>
              <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">
                {{ offer_id ? "Teklif Bilgileri" : "Yeni Teklif Oluştur" }}
              </div>
              <p class="text-sm text-gray-500">Teklif bilgilerini eksiksiz doldurunuz.</p>
            </div>
            <div class="flex items-center justify-center">
              <div
                v-if="offer_id && offer_data.mail_id"
                class="relative rounded-full border flex items-center justify-center gap-2 bg-gray-50 p-2.5 transition-all max-lg:hidden"
                v-tooltip="{ content: `Yapay zeka ile oluşturuldu.`, delay: 0 }"
              >
                <ArtificialIntelligence04Icon v-if="offer_data.mail_id" class="text-primary" />
                <div v-if="offer_drawer_transition_status" class="text-sm text-gray-600 text-nowrap hidden!">Yapay zeka ile oluşturuldu.</div>
              </div>
            </div>
          </div>
          <div class="flex items-center lg:justify-end gap-2">
            <Button
              @click="createOffer"
              :disabled="offer_loading"
              :loading="offer_loading"
              :label="offer_id ? `Kaydet` : `Yeni Teklif Oluştur`"
              class="w-full lg:w-fit! px-6! py-3! text-nowrap"
              fluid
            />
            <Button v-if="offer_id" type="button" icon="" @click="OfferFormDrawerOpenExtraMenu" outlined class="min-w-12!">
              <template #icon>
                <Menu01Icon size="24" />
              </template>
            </Button>
            <Popover v-if="offer_id" ref="offer_form_drawer_extra_menu">
              <div class="grid grid-cols-1 gap-2 w-full">
                <Button
                  @click="createOffer({ send_mail: true })"
                  :disabled="offer_mail_loading"
                  :loading="offer_mail_loading"
                  label="Kaydet ve E-Posta Yolla"
                  class="w-fit! px-6! py-3! text-nowrap"
                  severity="secondary"
                  size="small"
                  fluid
                  v-tooltip.top="'Teklif bilgilerini kaydet ve müşteriye e-posta yolla.'"
                />
                <Button
                  @click="sendSiberData"
                  :disabled="send_siber_loading"
                  :loading="send_siber_loading"
                  label="Sibere Aktar"
                  class="px-6! py-3! text-nowrap"
                  severity="secondary"
                  size="small"
                  fluid
                  v-tooltip.bottom="'Teklif bilgilerinin içeriklini Siber\'e aktar.'"
                />
                <Button
                  v-if="offer_data.siber_id"
                  @click="createRealLoad"
                  :disabled="create_real_load_loading"
                  :loading="create_real_load_loading"
                  label="Yük Oluştur"
                  class="px-6! py-3! text-nowrap"
                  severity="secondary"
                  size="small"
                  fluid
                  v-tooltip.bottom="'Teklifi gerçek yüke dönüştür.'"
                />
              </div>
            </Popover>
          </div>
        </div>
        <Tabs value="0" class="mt-6!" lazy scrollable>
          <TabList>
            <Tab value="0">Genel Bilgiler</Tab>
            <Tab value="1">Yük İçeriği</Tab>
            <Tab value="2">Finans</Tab>
            <Tab value="3">Görevliler</Tab>
            <Tab v-if="offer_id && offer_response_data.email" value="4">İlgili E-Posta</Tab>
            <Tab value="5">Dosya Arşivi</Tab>
            <Tab value="6">E-Posta Ayarları</Tab>
          </TabList>
          <TabPanels class="px-0!">
            <TabPanel value="0">
              <div class="grid lg:grid-cols-12 gap-4 mt-4 mb-6 lg:mb-10">
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax
                      v-model="offer_data.work_type_id"
                      :api="`/api/v1/work_type`"
                      optionLabel="name"
                      class="w-full"
                      @update:modelValue="onChangeWorkType"
                      filter
                    >
                      <template v-if="usePermissionStatus('work_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_WORK_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">İş Türü</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="offer_data.load_number" v-keyfilter.money fluid />
                    <label> Yük Numarası </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="offer_data.reservation_number" v-keyfilter.money fluid />
                    <label> Rezervasyon Numarası </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="offer_data.loading_type_id" :api="`/api/v1/loading_type`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('loading_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_LOADING_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Yüklenme Durumu</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="offer_data.department_id" :api="`/api/v1/department`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('department_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Departman</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="offer_data.load_transfer_type_id" :api="`/api/v1/load_transfer_type`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Yük Türü</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="offer_data.instruction_type_id" :api="`/api/v1/instruction`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Talimatın Geliş Şekli</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="offer_data.romork_type_id" :api="`/api/v1/romork_type`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('load_transfer_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_DEPARTMENTS_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">İstenilen Römork Cinsi</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax
                      v-model="offer_data.status_type_id"
                      :api="`/api/v1/status_type`"
                      optionLabel="name"
                      class="w-full"
                      @change="onStatusTypeChange"
                      filter
                    >
                      <template v-if="usePermissionStatus('status_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_STATUS_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Durum</label>
                  </FloatLabel>
                </div>
                <div v-if="offer_data.status_type_id?.id == 5" class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <Select v-model="offer_data.way_of_working" :options="way_of_working_options" optionLabel="label" class="w-full" fluid />
                    <label for="username">Çalışma Şekli</label>
                  </FloatLabel>
                </div>
                <div v-if="false">
                  <FloatLabel class="w-full" variant="on">
                    <Select v-model="offer_data.employee" inputId="on_label" :options="users" optionLabel="name" class="w-full">
                      <template #value="slotProps">
                        <div v-if="slotProps.value">
                          <div class="flex items-center gap-2 py-1">
                            <div class="size-9 rounded-full overflow-hidden border shadow-xs">
                              <img :src="slotProps.value.image" class="w-full h-full object-cover" />
                            </div>
                            <div>{{ slotProps.value.name }}</div>
                          </div>
                        </div>
                        <span v-else>
                          <div class="opacity-0">x</div>
                        </span>
                      </template>
                      <template #option="slotProps">
                        <div class="flex items-center gap-2">
                          <div class="size-9 rounded-full overflow-hidden border shadow-xs">
                            <img :src="slotProps.option.image" class="w-full h-full object-cover" />
                          </div>
                          <div>{{ slotProps.option.name }}</div>
                        </div>
                      </template>
                    </Select>
                    <label for="on_label">Görevli</label>
                  </FloatLabel>
                </div>
              </div>
              <div class="space-y-6 lg:space-y-10">
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <Calendar03Icon size="24" />
                    <div class="text-lg font-medium">Tarih</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <DatePicker v-model="offer_data.offer_date" showIcon fluid showButtonBar iconDisplay="input" />
                        <label for="username">Teklif Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <DatePicker v-model="offer_data.marketing_notification_date" showIcon fluid showButtonBar iconDisplay="input" />
                        <label for="username">Pazarlama Bildirim Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <DatePicker v-model="offer_data.offer_validity_date" showIcon fluid showButtonBar iconDisplay="input" />
                        <label for="username">Geçerlilik Tarihi</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <GlobalIcon size="24" />
                    <div class="text-lg font-medium">Konum</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.departure_country_id" :api="`/api/v1/country`" optionLabel="name" class="w-full" filter>
                          <template #value="slotProps">
                            <div v-if="slotProps.value" class="flex items-center gap-2">
                              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                                <img
                                  v-if="slotProps.value.country_code"
                                  :src="`https://flagcdn.com/w160/${slotProps.value.country_code.toLowerCase()}.png`"
                                  class="w-full h-full object-cover"
                                />
                              </div>
                              <div>{{ slotProps.value.name }}</div>
                            </div>
                            <span v-else>
                              {{ slotProps.placeholder }}
                            </span>
                          </template>
                          <template #option="slotProps">
                            <div class="flex items-center gap-2">
                              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                                <img
                                  v-if="slotProps.option.country_code"
                                  :src="`https://flagcdn.com/w160/${slotProps.option.country_code.toLowerCase()}.png`"
                                  class="w-full h-full object-cover"
                                />
                              </div>
                              <div>{{ slotProps.option.name }}</div>
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Kalkış Ülkesi</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.target_country_id" :api="`/api/v1/country`" optionLabel="name" class="w-full" filter>
                          <template #value="slotProps">
                            <div v-if="slotProps.value" class="flex items-center gap-2">
                              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                                <img
                                  v-if="slotProps.value.country_code"
                                  :src="`https://flagcdn.com/w160/${slotProps.value.country_code.toLowerCase()}.png`"
                                  class="w-full h-full object-cover"
                                />
                              </div>
                              <div>{{ slotProps.value.name }}</div>
                            </div>
                            <span v-else>
                              {{ slotProps.placeholder }}
                            </span>
                          </template>
                          <template #option="slotProps">
                            <div class="flex items-center gap-2">
                              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                                <img
                                  v-if="slotProps.option.country_code"
                                  :src="`https://flagcdn.com/w160/${slotProps.option.country_code.toLowerCase()}.png`"
                                  class="w-full h-full object-cover"
                                />
                              </div>
                              <div>{{ slotProps.option.name }}</div>
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Varış Ülkesi</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.transit_country_id" :api="`/api/v1/country`" optionLabel="name" class="w-full" filter>
                          <template #value="slotProps">
                            <div v-if="slotProps.value" class="flex items-center gap-2">
                              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                                <img
                                  v-if="slotProps.value.country_code"
                                  :src="`https://flagcdn.com/w160/${slotProps.value.country_code.toLowerCase()}.png`"
                                  class="w-full h-full object-cover"
                                />
                              </div>
                              <div>{{ slotProps.value.name }}</div>
                            </div>
                            <span v-else>
                              {{ slotProps.placeholder }}
                            </span>
                          </template>
                          <template #option="slotProps">
                            <div class="flex items-center gap-2">
                              <div v-if="false" class="w-6 aspect-4/3 rounded-sm overflow-hidden bg-gray-100">
                                <img
                                  v-if="slotProps.option.country_code"
                                  :src="`https://flagcdn.com/w160/${slotProps.option.country_code.toLowerCase()}.png`"
                                  class="w-full h-full object-cover"
                                />
                              </div>
                              <div>{{ slotProps.option.name }}</div>
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Transfer Ülkesi</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <CreditCardPosIcon size="24" />
                    <div class="text-lg font-medium">Ödeme</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.payment_type_id" :api="`/api/v1/payment_type`" optionLabel="name" class="w-full" filter>
                          <template v-if="usePermissionStatus('payment_type_management').read && false" #footer>
                            <div class="p-2">
                              <Button @click="FeatureStore.SET_PAYMENT_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Ödeme Şekli</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax
                          v-model="offer_data.company_pay_freight_id"
                          :api="`/api/v1/account`"
                          :fetchParams="{
                            account_type_id: 1,
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
                          <template v-if="usePermissionStatus('account_management').read" #footer>
                            <div class="p-2">
                              <Button @click="account_form_data.drawer_visible = true" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Navlun Ödeyecek Firma</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <Calendar03Icon size="24" />
                    <div class="text-lg font-medium">Şirketler</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.customer_id" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
                          <template #option="slotProps">
                            <div class="flex items-center gap-2">
                              <div class="size-8 rounded-lg overflow-hidden border shadow-xs bg-gray-100">
                                <img v-if="slotProps.option.avatar" :src="`/storage/${slotProps.option.avatar}`" class="w-full h-full object-cover" />
                              </div>
                              <div class="text-sm">{{ slotProps.option.name }}</div>
                            </div>
                          </template>
                          <template v-if="usePermissionStatus('account_management').read" #footer>
                            <div class="p-2">
                              <Button @click="account_form_data.drawer_visible = true" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Müşteri</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.sender_id" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
                          <template #option="slotProps">
                            <div class="flex items-center gap-2">
                              <div class="size-8 rounded-lg overflow-hidden border shadow-xs bg-gray-100">
                                <img v-if="slotProps.option.avatar" :src="`/storage/${slotProps.option.avatar}`" class="w-full h-full object-cover" />
                              </div>
                              <div class="text-sm">{{ slotProps.option.name }}</div>
                            </div>
                          </template>
                          <template v-if="usePermissionStatus('account_management').read" #footer>
                            <div class="p-2">
                              <Button @click="account_form_data.drawer_visible = true" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Gönderici</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="offer_data.receiver_id" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
                          <template #option="slotProps">
                            <div class="flex items-center gap-2">
                              <div class="size-8 rounded-lg overflow-hidden border shadow-xs bg-gray-100">
                                <img v-if="slotProps.option.avatar" :src="`/storage/${slotProps.option.avatar}`" class="w-full h-full object-cover" />
                              </div>
                              <div class="text-sm">{{ slotProps.option.name }}</div>
                            </div>
                          </template>
                          <template v-if="usePermissionStatus('account_management').read" #footer>
                            <div class="p-2">
                              <Button @click="account_form_data.drawer_visible = true" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Alıcı</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax
                          v-model="offer_data.agent_id"
                          :api="`/api/v1/account`"
                          :fetchParams="{
                            account_type_id: 5,
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
                          <template v-if="usePermissionStatus('account_management').read" #footer>
                            <div class="p-2">
                              <Button @click="account_form_data.drawer_visible = true" label="Yeni Ekle" fluid severity="secondary" size="small" />
                            </div>
                          </template>
                        </SelectAjax>
                        <label for="username">Acente</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <Note01Icon size="24" />
                    <div class="text-lg font-medium">Taşıma Ayarları</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mt-8">
                    <div>
                      <div class="text-sm text-gray-500">Ön taşıma tarafımızdan yapılır</div>
                      <ToggleButton v-model="offer_data.front_transportation_by_us" onLabel="Evet" offLabel="Hayır" class="w-full mt-2" />
                    </div>
                    <div>
                      <div class="text-sm text-gray-500">Son taşıma tarafımızdan yapılır</div>
                      <ToggleButton v-model="offer_data.final_transportation_by_us" onLabel="Evet" offLabel="Hayır" class="w-full mt-2" />
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <Note01Icon size="24" />
                    <div class="text-lg font-medium">Not</div>
                  </div>
                  <div>
                    <Textarea v-model="offer_data.description" rows="5" cols="30" class="resize-none" fluid />
                  </div>
                </div>
              </div>
            </TabPanel>
            <TabPanel value="1">
              <div class="flex justify-end mt-6 mb-8">
                <Button @click="offer_product_dialog_visible = true" raised outlined size="small">Yeni İçerik Ekle</Button>
              </div>
              <Transition mode="out-in">
                <div v-if="offer_data.content_items.length > 0" class="relative">
                  <TransitionGroup
                    enter-active-class="transition-opacity duration-500"
                    move-class="transition-all duration-500"
                    enter-from-class="opacity-0"
                    enter-to-class="opacity-100"
                    leave-active-class="transition-opacity duration-500 absolute"
                    leave-from-class="opacity-100"
                    leave-to-class="opacity-0"
                  >
                    <div v-for="(product, index) in offer_data.content_items" :key="product" class="mb-4">
                      <OfferFormContentItem :data="product" :index="index" @delete="deleteContentItem" :editable="offer_data.load_number ? false : true" />
                    </div>
                  </TransitionGroup>
                </div>
                <div v-else class="flex items-center justify-center">
                  <div class="py-3 px-6 bg-gray-100 rounded-xl text-sm">İçerik bulunamadı.</div>
                </div>
              </Transition>
            </TabPanel>
            <TabPanel value="2">
              <div class="flex justify-end mt-6 mb-8">
                <Button @click="offer_financial_dialog_visible = true" raised outlined size="small">Yeni Kayıt Ekle</Button>
              </div>
              <Transition mode="out-in">
                <div v-if="offer_data.financial_items.length > 0" class="space-y-8">
                  <div v-if="offer_data.financial_items.filter((item) => item.buysell.value == 1).length > 0">
                    <div class="mb-4 flex items-center gap-4">
                      <div class="size-10 rounded-lg bg-gray-100 flex items-center justify-center">
                        <Download05Icon size="20" class="text-primary" />
                      </div>
                      <div class="text-lg font-medium">Alış Hareketleri</div>
                    </div>
                    <div class="relative">
                      <TransitionGroup
                        enter-active-class="transition-opacity duration-500"
                        move-class="transition-all duration-500"
                        enter-from-class="opacity-0"
                        enter-to-class="opacity-100"
                        leave-active-class="transition-opacity duration-500 absolute"
                        leave-from-class="opacity-100"
                        leave-to-class="opacity-0"
                      >
                        <div v-for="(item, index) in offer_data.financial_items.filter((item) => item.buysell.value == 1)" :key="item" class="mb-4 w-full">
                          <OfferFormFinancialItem :data="item" :index="index" @delete="deleteFinancialItem" :editable="offer_data.load_number ? false : true" />
                        </div>
                      </TransitionGroup>
                    </div>
                  </div>
                  <div v-if="offer_data.financial_items.filter((item) => item.buysell.value == 2).length > 0">
                    <div class="mb-4 flex items-center gap-4">
                      <div class="size-10 rounded-lg bg-gray-100 flex items-center justify-center">
                        <Upload05Icon size="20" class="text-primary" />
                      </div>
                      <div class="text-lg font-medium">Satış Hareketleri</div>
                    </div>
                    <div class="relative">
                      <TransitionGroup
                        enter-active-class="transition-opacity duration-500"
                        move-class="transition-all duration-500"
                        enter-from-class="opacity-0"
                        enter-to-class="opacity-100"
                        leave-active-class="transition-opacity duration-500 absolute"
                        leave-from-class="opacity-100"
                        leave-to-class="opacity-0"
                      >
                        <div v-for="(item, index) in offer_data.financial_items.filter((item) => item.buysell.value == 2)" :key="item" class="mb-4 w-full">
                          <OfferFormFinancialItem :data="item" :index="index" @delete="deleteFinancialItem" :editable="offer_data.load_number ? false : true" />
                        </div>
                      </TransitionGroup>
                    </div>
                  </div>
                </div>
                <div v-else class="flex items-center justify-center">
                  <div class="py-3 px-6 bg-gray-100 rounded-xl text-sm">Hareket bulunamadı.</div>
                </div>
              </Transition>
            </TabPanel>
            <TabPanel value="3">
              <div class="grid grid-cols-1 gap-4 my-4">
                <FloatLabel variant="on">
                  <SelectAjax
                    v-model="offer_data.operation_officers"
                    :api="`/api/v1/user`"
                    :optionLabel="(e) => `${e.name} ${e.surname}`"
                    class="w-full"
                    dataKey="id"
                    filter
                  />
                  <label>Operasyon Yetkilisi</label>
                </FloatLabel>
                <FloatLabel variant="on">
                  <SelectAjax
                    v-model="offer_data.sales_representatives"
                    :api="`/api/v1/user`"
                    :optionLabel="(e) => `${e.name} ${e.surname}`"
                    class="w-full"
                    dataKey="id"
                    filter
                    multiple
                  />
                  <label>Satış Temsilcisi</label>
                </FloatLabel>
              </div>
            </TabPanel>
            <TabPanel v-if="offer_id" value="4">
              <div v-if="offer_response_data.email" class="my-4 p-6 lg:p-8 rounded-lg border bg-white">
                <div class="text-lg text-black font-medium mb-4">{{ offer_response_data.email?.subject }}</div>
                <div v-html="offer_response_data.email?.email_content?.trim()" class="whitespace-pre-line text-sm"></div>
              </div>
            </TabPanel>
            <TabPanel value="5">
              <FileUploader v-model="offer_data.files" />
              <div v-if="offer_id" class="mt-4 flex justify-end">
                <Button @click="updateLoadFiles" label="Dosyaları Kaydet" />
              </div>
            </TabPanel>
            <TabPanel value="6">
              <div class="grid lg:grid-cols-12 gap-4 mt-4 mb-6 lg:mb-10">
                <div class="lg:col-span-12">
                  <FloatLabel class="w-full" variant="on">
                    <AutoComplete v-model="offer_data.email.to" forceSelection multiple :typeahead="false" fluid />
                    <label> Gönderilecek </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-12">
                  <FloatLabel class="w-full" variant="on">
                    <AutoComplete v-model="offer_data.email.cc" forceSelection multiple :typeahead="false" fluid />
                    <label> CC </label>
                  </FloatLabel>
                </div>
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
    </Drawer>
    <Dialog v-model:visible="offer_product_dialog_visible" modal header=" " class="w-full lg:w-[700px]">
      <div>
        <div class="text-xl lg:text-2xl font-semibold mb-1 text-gray-900">Ürün Bilgileri</div>
        <div class="text-sm text-gray-500">Ürün bilgilerini eksiksiz ve doğru girmeye özen gösteriniz.</div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 my-10">
        <div>
          <FloatLabel variant="on">
            <SelectAjax v-model="new_product_data.product_type_id" :api="`/api/v1/product_type`" optionLabel="name" class="w-full" filter>
              <template v-if="usePermissionStatus('product_type_management').read && false" #footer>
                <div class="p-2">
                  <Button @click="FeatureStore.SET_LOAD_PRODUCT_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                </div>
              </template>
            </SelectAjax>
            <label for="username">Mal Cinsi</label>
          </FloatLabel>
        </div>
        <div>
          <FloatLabel variant="on">
            <SelectAjax v-model="new_product_data.case_type_id" :api="`/api/v1/case_type`" optionLabel="name" class="w-full" filter>
              <template v-if="usePermissionStatus('case_type_management').read && false" #footer>
                <div class="p-2">
                  <Button @click="FeatureStore.SET_VESSEL_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                </div>
              </template>
            </SelectAjax>
            <label for="username">Kap Cinsi</label>
          </FloatLabel>
        </div>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.quantity" fluid v-keyfilter.num />
          <label> Adet </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.width" @update:modelValue="calculateLDM" min="0" fluid v-keyfilter.num />
          <label> En (cm)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.length" @update:modelValue="calculateLDM" min="0" fluid v-keyfilter.num />
          <label> Boy (cm)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.height" min="0" fluid v-keyfilter.num />
          <label> Yükseklik (cm)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.gross_weight" min="0" fluid v-keyfilter.num />
          <label> Brüt Ağırlık (kg)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.net_weight" min="0" fluid v-keyfilter.num />
          <label> Net Ağırlık (kg)</label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.volume" min="0" fluid v-keyfilter.num />
          <label> Hacim </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputText v-model="new_product_data.lademeter" min="0" fluid v-keyfilter.num />
          <label> Lademetre </label>
        </FloatLabel>
        <SelectButton
          v-model="new_product_data.stackable"
          :options="product_stackable_types"
          optionLabel="name"
          class="lg:col-span-2 w-full [&>.p-togglebutton]:w-full"
          fluid
        />
      </div>
      <div class="flex justify-end gap-2">
        <Button size="small" type="button" label="İptal" severity="secondary" @click="offer_product_dialog_visible = false"></Button>
        <Button size="small" type="button" label="Kaydet" @click="addContentItem"></Button>
      </div>
    </Dialog>
    <Dialog v-model:visible="offer_financial_dialog_visible" modal header=" " class="w-full lg:w-[700px]">
      <div>
        <div class="text-xl lg:text-2xl font-semibold mb-1 text-gray-900">Ürün Bilgileri</div>
        <div class="text-sm text-gray-500">Ürün bilgilerini eksiksiz ve doğru girmeye özen gösteriniz.</div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 my-10">
        <div class="flex justify-center w-full lg:col-span-2">
          <SelectButton
            v-model="new_financial_item.buysell"
            :options="buysell_types"
            optionLabel="name"
            @change="
              () => {
                new_financial_item.item = null;
                new_financial_item.account_id = null;
              }
            "
            class="w-full [&>.p-togglebutton]:w-full"
          />
        </div>
        <FloatLabel v-if="false" variant="on">
          <SelectAjax v-model="new_financial_item.item_type_id" :api="`/api/v1/item_type`" optionLabel="name" class="w-full" filter>
            <template v-if="usePermissionStatus('financial_item_type_management').read && false" #footer>
              <div class="p-2">
                <Button @click="FeatureStore.SET_LOAD_FINANCIAL_ITEM_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
              </div>
            </template>
          </SelectAjax>
          <label>Kalem Türü</label>
        </FloatLabel>
        <FloatLabel variant="on">
          <SelectAjax
            v-model="new_financial_item.item"
            :api="`/api/v1/financial_item`"
            :fetchParams="{
              type: new_financial_item.buysell?.value,
            }"
            :disabled="!new_financial_item.buysell"
            optionLabel="name"
            class="w-full"
            filter
          >
            <template v-if="usePermissionStatus('financial_item_management').read && false" #footer>
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
              v-model="new_financial_item.account_id"
              :api="`/api/v1/account`"
              :fetchParams="{
                account_type_id: new_financial_item.buysell?.value == 1 ? 2 : 1,
              }"
              :disabled="!new_financial_item.buysell"
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
            <label for="username">{{ new_financial_item.buysell?.value == 1 ? "Tedarikçiler" : "Müşteriler" }}</label>
          </FloatLabel>
        </div>
        <FloatLabel class="w-full" variant="on">
          <InputText @value-change="() => calculateSingleTotalPrice(new_financial_item)" v-model="new_financial_item.quantity" v-keyfilter.int fluid />
          <label> Adet </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputNumber
            @value-change="() => calculateSingleTotalPrice(new_financial_item)"
            v-model="new_financial_item.net_price"
            :maxFractionDigits="2"
            v-keyfilter.money
            fluid
          />
          <label> Fiyat </label>
        </FloatLabel>
        <FloatLabel v-if="false" class="w-full" variant="on">
          <InputText v-model="new_financial_item.tax_price" v-keyfilter.money fluid />
          <label> Vergi Tutarı </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputNumber v-model="new_financial_item.total_price" :maxFractionDigits="2" v-keyfilter.money fluid />
          <label> Toplam Tutar </label>
        </FloatLabel>
        <div>
          <FloatLabel variant="on">
            <Select v-model="new_financial_item.transport_type_id" :options="define_datas.offer.transport_type" optionLabel="name" fluid />
            <SelectAjax v-if="false" v-model="new_financial_item.transport_type_id" :api="`/api/v1/transport_type`" optionLabel="name" class="w-full" filter>
              <template v-if="usePermissionStatus('transport_type_management').read && false" #footer>
                <div class="p-2">
                  <Button @click="FeatureStore.SET_TRANSPORT_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                </div>
              </template>
            </SelectAjax>
            <label for="username">Taşıma Tipi</label>
          </FloatLabel>
        </div>
        <div class="w-full lg:col-span-2">
          <FloatLabel variant="on">
            <SelectAjax v-model="new_financial_item.currency" :api="`/api/v1/currency`" :optionLabel="(e) => `${e.code} - ${e.name}`" class="w-full" filter>
              <template #header>
                <div class="grid grid-cols-2 lg:grid-cols-3 gap-1.5 p-2.5 border-b">
                  <Button
                    v-for="(item, index) in define_datas.currency"
                    :key="index"
                    :label="item.code"
                    :severity="new_financial_item.currency?.code == item.code ? '' : 'secondary'"
                    class="py-1.5! text-sm!"
                    @click="new_financial_item.currency = item"
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
          <FloatLabel class="w-full lg:col-span-2" variant="on">
            <Textarea v-model="new_financial_item.description" autoResize fluid />
            <label> Açıklama </label>
          </FloatLabel>
        </div>
        <div v-if="false">
          <FloatLabel variant="on">
            <Select v-model="new_financial_item.status" :options="financial_item_status_type" optionLabel="name" fluid />
            <label for="username">Durum</label>
          </FloatLabel>
        </div>
      </div>
      <div class="flex justify-end gap-2">
        <Button size="small" type="button" label="İptal" severity="secondary" @click="offer_financial_dialog_visible = false"></Button>
        <Button size="small" type="button" label="Kaydet" @click="addFinancialItem"></Button>
      </div>
    </Dialog>
    <AccountFormDrawer :accountFormData="account_form_data" />
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from "vue";
import { useVuelidate } from "@vuelidate/core";
import { required, email, minLength, helpers } from "@vuelidate/validators";
import { buysell_types, financial_item_status_type, product_stackable_types, define_datas } from "@/data/system_data.js";
import {
  Download05Icon,
  Upload05Icon,
  Calendar03Icon,
  Note01Icon,
  CreditCardPosIcon,
  GlobalIcon,
  Menu01Icon,
  ArtificialIntelligence04Icon,
} from "hugeicons-vue";
import OfferFormContentItem from "@/components/Offer/OfferFormContentItem.vue";
import OfferFormFinancialItem from "@/components/Offer/OfferFormFinancialItem.vue";
import AccountFormDrawer from "@/components/Accounts/AccountFormDrawer.vue";
import FileUploader from "@/components/FileUploader.vue";
import { useDataStore } from "@/stores/data_store.js";
import { useFeatureStore } from "@/stores/general_store.js";
import { toast } from "vue-sonner";
import { usePermissionStatus } from "@/composables/index.js";
import axios from "axios";
import { useRoute, useRouter } from "vue-router";
import { useConfirm } from "primevue";

const Route = useRoute();
const Router = useRouter();
const confirm = useConfirm();
const offer_response_data = ref();
const emits = defineEmits(["formSubmit", "onHideDrawer"]);
const DataStore = useDataStore();
const FeatureStore = useFeatureStore();
const offer_product_dialog_visible = ref(false);
const offer_financial_dialog_visible = ref(false);
const offer_loading = ref(false);
const offer_mail_loading = ref(false);
const send_siber_loading = ref(false);
const offer_form_drawer_extra_menu = ref(false);
const offer_drawer_transition_status = ref(true);
const create_real_load_loading = ref(false);

const offer_drawer_visible = defineModel("visible");
const offer_id = defineModel("offer-id");
const page_loading = ref(false);

const account_form_data = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
});

const OfferFormDrawerOpenExtraMenu = (event) => {
  offer_form_drawer_extra_menu.value.toggle(event);
};

const offer_data = reactive({
  id: null,
  reservation_number: "",
  load_number: "",
  work_type_id: "",
  loading_type_id: "",
  payment_type_id: {
    id: 9,
    name: "PEŞİN",
    code: "2",
    siber_id: "97081C47-4F6A-4F37-9557-BC1CAC802106",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  },
  status_type_id: {
    id: 4,
    name: "TEKLİF",
    number: "10",
    siber_id: "EC922C9E-C2CF-4716-A198-F716FDA50358",
    created_at: "2025-01-03T13:16:38.000000Z",
    updated_at: "2025-01-03T13:16:38.000000Z",
  },
  way_of_working: "",
  load_transfer_type_id: "",
  instruction_type_id: "",
  romork_type_id: "",
  offer_date: new Date(),
  offer_validity_date: new Date(new Date().setDate(new Date().getDate() + 7)),
  marketing_notification_date: new Date(),
  customer_id: "",
  sender_id: "",
  receiver_id: "",
  agent_id: "",
  company_pay_freight_id: "",
  description: "",
  departure_country_id: "",
  transit_country_id: "",
  target_country_id: "",
  department_id: {
    id: 7,
    name: "SATIŞ & PAZARLAMA",
    siber_id: "C249E951-FB3F-4FF9-A1C4-EF0223A00B75",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  },
  content_items: [],
  financial_items: [],
  operation_officers: null,
  sales_representatives: [],
  front_transportation_by_us: false,
  final_transportation_by_us: false,
  mail_id: "",
  siber_id: "",
  email: {
    to: [],
    cc: [],
  },
  files: [],
});
const new_product_data = reactive({
  product_type_id: {
    id: 175,
    name: "YEDEK PARÇA",
    siber_id: "7F06E651-57E8-418E-B914-FFD2B576C8C4",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  },
  case_type_id: {
    id: 120,
    name: "PALET",
    edikod: "",
    siber_id: "EAC8DA4F-895F-435B-BCB4-D5E6FF767D54",
    created_at: "2025-01-03T13:17:11.000000Z",
    updated_at: "2025-01-03T13:17:11.000000Z",
  },
  quantity: null,
  width: null,
  length: null,
  height: null,
  gross_weight: null,
  net_weight: null,
  volume: null,
  lademeter: null,
  stackable: {
    id: 0,
    name: "İstiflenemez",
  },
});
const new_financial_item = reactive({
  buysell: null,
  item_type_id: null,
  quantity: null,
  item: null,
  transport_type_id: null,
  status: null,
  order: 1,
  net_price: null,
  tax_price: null,
  total_price: null,
  currency: {
    id: 98,
    name: "AMERİKA BİRLEŞİK DEVLETLERİ DOLARI",
    symbol: null,
    code: "USD",
    siber_id: "98A1F932-AD6C-4A57-881D-DFE19362FA2C",
    created_at: "2025-01-03T13:28:31.000000Z",
    updated_at: "2025-01-03T13:28:31.000000Z",
  },
  description: "",
  account_id: null,
});
const way_of_working_options = [
  {
    label: "Spot",
    value: 0,
  },
  {
    label: "Yıllık",
    value: 1,
  },
];

const addContentItem = async () => {
  let item_data = { ...new_product_data, dump_id: Math.random() };
  offer_data.content_items.push(item_data);

  new_product_data.product_type_id = {
    id: 175,
    name: "YEDEK PARÇA",
    siber_id: "7F06E651-57E8-418E-B914-FFD2B576C8C4",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  };
  new_product_data.case_type_id = {
    id: 120,
    name: "PALET",
    edikod: "",
    siber_id: "EAC8DA4F-895F-435B-BCB4-D5E6FF767D54",
    created_at: "2025-01-03T13:17:11.000000Z",
    updated_at: "2025-01-03T13:17:11.000000Z",
  };
  new_product_data.quantity = null;
  new_product_data.width = null;
  new_product_data.length = null;
  new_product_data.height = null;
  new_product_data.gross_weight = null;
  new_product_data.net_weight = null;
  new_product_data.volume = null;
  new_product_data.lademeter = null;
  new_product_data.stackable = {
    id: 0,
    name: "İstiflenemez",
  };

  offer_product_dialog_visible.value = false;
};

const deleteContentItem = (data) => {
  let id = data.id;
  let dump_id = data.dump_id;
  if (id) {
    confirm.require({
      message: "Bu yük içeriği hemen silinecektir. Silme işlemini onaylıyor musunuz?",
      header: "Uyarı",
      acceptProps: {
        label: "Evet",
        severity: "danger",
        size: "small",
      },
      rejectProps: {
        label: "Hayır",
        severity: "secondary",
        size: "small",
      },
      accept: async () => {
        const res = await DataStore.DELETE_OFFER_CONTENT_ITEM(id);
        if (res) {
          offer_data.content_items = offer_data.content_items.filter((item) => item.dump_id != dump_id);
        }
      },
    });
  } else {
    offer_data.content_items = offer_data.content_items.filter((item) => item.dump_id != dump_id);
  }
};

const addFinancialItem = async () => {
  console.log(new_financial_item);
  if (
    !new_financial_item.buysell ||
    !new_financial_item.quantity ||
    !new_financial_item.item ||
    !new_financial_item.transport_type_id ||
    new_financial_item.net_price === "" ||
    new_financial_item.total_price === "" ||
    !new_financial_item.currency ||
    !new_financial_item.account_id
  ) {
    toast.error("Lütfen tüm alanları doldurunuz.");
    return;
  }
  let item_data = { ...new_financial_item, dump_id: Math.random() };
  console.log(item_data);
  offer_data.financial_items.push(item_data);

  new_financial_item.buysell = null;
  new_financial_item.item_type_id = null;
  new_financial_item.quantity = null;
  new_financial_item.item = null;
  new_financial_item.transport_type_id = null;
  // new_financial_item.status = null;
  new_financial_item.order = null;
  new_financial_item.net_price = null;
  // new_financial_item.tax_price = null;
  new_financial_item.total_price = null;
  new_financial_item.currency = {
    id: 98,
    name: "AMERİKA BİRLEŞİK DEVLETLERİ DOLARI",
    symbol: null,
    code: "USD",
    siber_id: "98A1F932-AD6C-4A57-881D-DFE19362FA2C",
    created_at: "2025-01-03T13:28:31.000000Z",
    updated_at: "2025-01-03T13:28:31.000000Z",
  };
  new_financial_item.account_id = null;
  new_financial_item.description = "";
  offer_financial_dialog_visible.value = false;
};
const deleteFinancialItem = (data) => {
  let id = data.id;
  let dump_id = data.dump_id;
  if (id) {
    confirm.require({
      message: "Bu finansal öğe hemen silinecektir. Silme işlemini onaylıyor musunuz?",
      header: "Uyarı",
      acceptProps: {
        label: "Evet",
        severity: "danger",
        size: "small",
      },
      rejectProps: {
        label: "Hayır",
        severity: "secondary",
        size: "small",
      },
      accept: async () => {
        const res = await DataStore.DELETE_OFFER_FINANCIAL_ITEM(id);
        if (res) {
          offer_data.financial_items = offer_data.financial_items.filter((item) => item.dump_id != dump_id);
        }
      },
    });
  } else {
    offer_data.financial_items = offer_data.financial_items.filter((item) => item.dump_id != dump_id);
  }
};
const createOffer = async ({ send_mail } = {}) => {
  offer_loading.value = true;
  const form_data = new FormData();
  form_data.append("reservation_number", offer_data.reservation_number ?? "");
  form_data.append("load_number", offer_data.load_number ?? "");
  form_data.append("work_type_id", offer_data.work_type_id?.id ?? "");
  form_data.append("loading_type_id", offer_data.loading_type_id?.id ?? "");
  form_data.append("payment_type_id", offer_data.payment_type_id?.id ?? "");
  form_data.append("status_type_id", offer_data.status_type_id?.id ?? "");
  form_data.append("way_of_working", offer_data.way_of_working?.value ?? "");
  form_data.append("load_transfer_type_id", offer_data.load_transfer_type_id?.id ?? "");
  form_data.append("instruction_id", offer_data.instruction_type_id?.id ?? "");
  form_data.append("romork_type_id", offer_data.romork_type_id?.id ?? "");
  if (offer_data.offer_date) {
    const year = offer_data.offer_date.getFullYear();
    const month = String(offer_data.offer_date.getMonth() + 1).padStart(2, "0");
    const day = String(offer_data.offer_date.getDate()).padStart(2, "0");
    const formattedDate = `${year}-${month}-${day}`;
    form_data.append("offer_date", offer_data.offer_date ? formattedDate : "");
  }
  if (offer_data.offer_validity_date) {
    const year = offer_data.offer_validity_date.getFullYear();
    const month = String(offer_data.offer_validity_date.getMonth() + 1).padStart(2, "0");
    const day = String(offer_data.offer_validity_date.getDate()).padStart(2, "0");
    const formattedDate = `${year}-${month}-${day}`;
    form_data.append("offer_validity_date", offer_data.offer_validity_date ? formattedDate : "");
  }
  if (offer_data.marketing_notification_date) {
    const year = offer_data.marketing_notification_date.getFullYear();
    const month = String(offer_data.marketing_notification_date.getMonth() + 1).padStart(2, "0");
    const day = String(offer_data.marketing_notification_date.getDate()).padStart(2, "0");
    const formattedDate = `${year}-${month}-${day}`;
    form_data.append("marketing_notification_date", offer_data.marketing_notification_date ? formattedDate : "");
  }
  form_data.append("customer_id", offer_data.customer_id?.id ?? "");
  form_data.append("sender_id", offer_data.sender_id?.id ?? "");
  form_data.append("receiver_id", offer_data.receiver_id?.id ?? "");
  form_data.append("agent_id", offer_data.agent_id?.id ?? "");
  form_data.append("company_pay_freight_id", offer_data.company_pay_freight_id?.id ?? "");
  form_data.append("description", offer_data.description ?? "");
  form_data.append("departure_country_id", offer_data.departure_country_id?.id ?? "");
  form_data.append("transit_country_id", offer_data.transit_country_id?.id ?? "");
  form_data.append("target_country_id", offer_data.target_country_id?.id ?? "");
  form_data.append("department_id", offer_data.department_id?.id ?? "");

  form_data.append("front_transportation_by_us", offer_data.front_transportation_by_us ? 1 : 0);
  form_data.append("final_transportation_by_us", offer_data.final_transportation_by_us ? 1 : 0);

  offer_data.content_items.forEach((item, index) => {
    if (item.id) {
      form_data.append(`load_content[${index}][id]`, item.id);
    }
    form_data.append(`load_content[${index}][product_type_id]`, item.product_type_id?.id ?? "");
    form_data.append(`load_content[${index}][case_type_id]`, item.case_type_id?.id ?? "");
    form_data.append(`load_content[${index}][quantity]`, item.quantity ?? "");
    form_data.append(`load_content[${index}][width]`, item.width ?? "");
    form_data.append(`load_content[${index}][length]`, item.length ?? "");
    form_data.append(`load_content[${index}][height]`, item.height ?? "");
    form_data.append(`load_content[${index}][gross_weight]`, item.gross_weight ?? "");
    form_data.append(`load_content[${index}][net_weight]`, item.net_weight ?? "");
    form_data.append(`load_content[${index}][volume]`, item.volume ?? "");
    form_data.append(`load_content[${index}][lademeter]`, item.lademeter ?? "");
    form_data.append(`load_content[${index}][stackable]`, item.stackable?.id ?? "");
  });
  offer_data.financial_items.forEach((item, index) => {
    if (item.id) {
      form_data.append(`load_financial_item[${index}][id]`, item.id);
    }
    form_data.append(`load_financial_item[${index}][buysell]`, item.buysell.value ? item.buysell.value : "");
    if (false) {
      form_data.append(`load_financial_item[${index}][item_type_id]`, item.item_type_id?.id ?? "");
    }
    form_data.append(`load_financial_item[${index}][quantity]`, item.quantity ?? "");
    form_data.append(`load_financial_item[${index}][item]`, item.item?.id ?? "");
    form_data.append(`load_financial_item[${index}][transport_type_id]`, item.transport_type_id?.id ?? "");
    if (false) {
      form_data.append(`load_financial_item[${index}][status]`, item.status?.id ?? "");
    }
    form_data.append(`load_financial_item[${index}][order]`, 1);
    form_data.append(`load_financial_item[${index}][net_price]`, item.net_price ?? "");
    if (false) {
      form_data.append(`load_financial_item[${index}][tax_price]`, item.tax_price ?? "");
    }
    form_data.append(`load_financial_item[${index}][total_price]`, item.total_price ?? "");
    form_data.append(`load_financial_item[${index}][currency]`, item.currency?.id ?? "");
    form_data.append(`load_financial_item[${index}][account_id]`, item.account_id?.id ?? "");
    form_data.append(`load_financial_item[${index}][description]`, item.description ?? "");
  });
  if (offer_data.operation_officers) {
    if (offer_data.operation_officers.id) {
      form_data.append(`load_charge_person[0][id]`, offer_data.operation_officers.id);
    }
    form_data.append(`load_charge_person[0][user_id]`, offer_data.operation_officers.id);
    form_data.append(`load_charge_person[0][user_type]`, 1);
  }

  if (offer_data.sales_representatives && offer_data.sales_representatives.length > 0) {
    offer_data.sales_representatives.forEach((item, index) => {
      if (item.id) {
        form_data.append(`load_charge_person[${index + 1}][id]`, item.id);
      }
      form_data.append(`load_charge_person[${index + 1}][user_id]`, item.id);
      form_data.append(`load_charge_person[${index + 1}][user_type]`, 2);
    });
  }

  if (offer_data.email.to.length > 0) {
    offer_data.email.to.forEach((item, index) => {
      form_data.append(`email[to][${index}]`, item);
    });
  }
  if (offer_data.email.cc.length > 0) {
    offer_data.email.cc.forEach((item, index) => {
      form_data.append(`email[cc][${index}]`, item);
    });
  }

  if (offer_data.files.length > 0) {
    offer_data.files.forEach((file, index) => {
      if (file.data?.id) {
        form_data.append(`files[${index}][id]`, file.data.id);
      }
      if (!file.data) {
        form_data.append(`files[${index}][file]`, file.file);
      }
    });
  }

  if (send_mail) {
    form_data.append("send_mail", 1);
  }

  if (offer_id.value) {
    form_data.append("id", offer_id.value);
  }
  let res = null;
  if (offer_id.value) {
    res = await DataStore.UPDATE_LOAD({ data: form_data, id: offer_id.value });
    if (res) {
      toast.success("Teklif başarıyla güncellendi.");
      const load_data = res.data;

      offer_data.content_items = [];
      offer_data.financial_items = [];

      if (load_data.load_content && load_data.load_content.length > 0) {
        load_data.load_content.forEach((item) => {
          let product = {
            id: item.id,
            product_type_id: item.product_type_id,
            case_type_id: item.case_type_id,
            quantity: item.quantity,
            width: item.width,
            length: item.length,
            height: item.height,
            gross_weight: item.gross_weight,
            net_weight: item.net_weight,
            volume: item.volume,
            lademeter: item.lademeter,
            stackable: product_stackable_types.find((type) => type.id == item.stackable),
            dump_id: Math.random(),
          };
          offer_data.content_items.push(product);
        });
      }

      if (load_data.load_financial_item && load_data.load_financial_item.length > 0) {
        load_data.load_financial_item.forEach((item) => {
          let financial_item = {
            id: item.id,
            buysell: buysell_types.find((type) => type.value == item.buysell),
            item_type_id: item.item_type_id,
            quantity: item.quantity,
            item: item.item,
            transport_type_id: item.transport_type_id,
            status: financial_item_status_type.find((type) => type.id == item.status),
            order: item.order,
            net_price: item.net_price,
            tax_price: item.tax_price,
            total_price: item.total_price,
            currency: item.currency,
            account_id: item.account_id,
            description: item.description,
            dump_id: Math.random(),
          };
          offer_data.financial_items.push(financial_item);
        });
      }
    }
  } else {
    res = await DataStore.CREATE_LOAD({ data: form_data });
    if (res) {
      toast.success("Teklif başarıyla oluşturuldu.");
      offer_drawer_visible.value = false;

      emits("formSubmit", {
        status_type: offer_data.status_type_id,
      });
    }
    if (res) {
      offer_data.reservation_number = "";
      offer_data.load_number = "";
      offer_data.work_type_id = "";
      offer_data.loading_type_id = "";
      offer_data.payment_type_id = "";
      offer_data.status_type_id = "";
      offer_data.way_of_working = "";
      offer_data.load_transfer_type_id = "";
      offer_data.instruction_type_id = "";
      offer_data.romork_type_id = "";
      offer_data.offer_date = new Date();
      offer_data.offer_validity_date = new Date(new Date().setDate(new Date().getDate() + 7));
      offer_data.marketing_notification_date = new Date();
      offer_data.customer_id = "";
      offer_data.sender_id = "";
      offer_data.receiver_id = "";
      offer_data.agent_id = "";
      offer_data.company_pay_freight_id = "";
      offer_data.description = "";
      offer_data.departure_country_id = "";
      offer_data.transit_country_id = "";
      offer_data.target_country_id = "";
      offer_data.department_id = {
        id: 7,
        name: "SATIŞ & PAZARLAMA",
        siber_id: "C249E951-FB3F-4FF9-A1C4-EF0223A00B75",
        created_at: "2025-01-03T13:17:10.000000Z",
        updated_at: "2025-01-03T13:17:10.000000Z",
      };
      offer_data.content_items = [];
      offer_data.financial_items = [];
      offer_data.front_transportation_by_us = false;
      offer_data.final_transportation_by_us = false;
      offer_data.email.to = [];
      offer_data.email.cc = [];
      offer_data.files = [];
      offer_data.siber_id = "";

      new_product_data.product_type_id = {
        id: 175,
        name: "YEDEK PARÇA",
        siber_id: "7F06E651-57E8-418E-B914-FFD2B576C8C4",
        created_at: "2025-01-03T13:17:10.000000Z",
        updated_at: "2025-01-03T13:17:10.000000Z",
      };
      new_product_data.case_type_id = {
        id: 120,
        name: "PALET",
        edikod: "",
        siber_id: "EAC8DA4F-895F-435B-BCB4-D5E6FF767D54",
        created_at: "2025-01-03T13:17:11.000000Z",
        updated_at: "2025-01-03T13:17:11.000000Z",
      };
      new_product_data.quantity = null;
      new_product_data.width = null;
      new_product_data.length = null;
      new_product_data.height = null;
      new_product_data.gross_weight = null;
      new_product_data.net_weight = null;
      new_product_data.volume = null;
      new_product_data.lademeter = null;
      new_product_data.stackable = {
        id: 0,
        name: "İstiflenemez",
      };

      new_financial_item.buysell = null;
      new_financial_item.item_type_id = null;
      new_financial_item.quantity = null;
      new_financial_item.item = null;
      new_financial_item.transport_type_id = null;
      new_financial_item.status = null;
      new_financial_item.order = null;
      new_financial_item.net_price = null;
      new_financial_item.tax_price = null;
      new_financial_item.total_price = null;
      new_financial_item.description = "";
      new_financial_item.currency = {
        id: 98,
        name: "AMERİKA BİRLEŞİK DEVLETLERİ DOLARI",
        symbol: null,
        code: "USD",
        siber_id: "98A1F932-AD6C-4A57-881D-DFE19362FA2C",
        created_at: "2025-01-03T13:28:31.000000Z",
        updated_at: "2025-01-03T13:28:31.000000Z",
      };

      offer_product_dialog_visible.value = false;
      offer_financial_dialog_visible.value = false;
    }
  }
  offer_loading.value = false;
};

watch(offer_id, async (newVal) => {
  if (newVal) {
    offer_drawer_visible.value = true;
    page_loading.value = true;
    const res = await DataStore.GET_LOAD(newVal);
    offer_response_data.value = res;
    if (res) {
      let load_data = res;
      offer_data.id = load_data.id;
      offer_data.reservation_number = load_data.reservation_number;
      offer_data.load_number = load_data.load_number;
      offer_data.work_type_id = load_data.work_type_id;
      offer_data.loading_type_id = load_data.loading_type_id;
      offer_data.payment_type_id = load_data.payment_type_id;
      offer_data.status_type_id = load_data.status_type_id;
      offer_data.way_of_working = (() => {
        if (load_data.way_of_working == null) return null;
        return way_of_working_options.find((item) => item.value == load_data.way_of_working);
      })();
      offer_data.load_transfer_type_id = load_data.load_transfer_type_id;
      offer_data.instruction_type_id = load_data.instruction_id;
      offer_data.romork_type_id = load_data.romork_type_id;
      offer_data.offer_date = new Date(load_data.offer_date);
      offer_data.offer_validity_date = new Date(load_data.offer_validity_date);
      offer_data.marketing_notification_date = new Date(load_data.marketing_notification_date);
      offer_data.customer_id = load_data.customer_id;
      offer_data.sender_id = load_data.sender_id;
      offer_data.receiver_id = load_data.receiver_id;
      offer_data.agent_id = load_data.agent_id;
      offer_data.company_pay_freight_id = load_data.company_pay_freight_id;
      offer_data.description = load_data.description;
      offer_data.departure_country_id = load_data.departure_country_id;
      offer_data.transit_country_id = load_data.transit_country_id;
      offer_data.target_country_id = load_data.target_country_id;
      offer_data.department_id = load_data.department_id;
      offer_data.front_transportation_by_us = load_data.front_transportation_by_us == 1 ? true : false;
      offer_data.final_transportation_by_us = load_data.final_transportation_by_us == 1 ? true : false;
      offer_data.mail_id = load_data.mail_id;
      offer_data.siber_id = load_data.siber_id;

      if (load_data.load_content && load_data.load_content.length > 0) {
        load_data.load_content.forEach((item) => {
          let product = {
            id: item.id,
            product_type_id: item.product_type_id,
            case_type_id: item.case_type_id,
            quantity: item.quantity,
            width: item.width,
            length: item.length,
            height: item.height,
            gross_weight: item.gross_weight,
            net_weight: item.net_weight,
            volume: item.volume,
            lademeter: item.lademeter,
            stackable: product_stackable_types.find((type) => type.id == item.stackable),
            dump_id: Math.random(),
          };
          offer_data.content_items.push(product);
        });
      }

      if (load_data.load_financial_item && load_data.load_financial_item.length > 0) {
        load_data.load_financial_item.forEach((item) => {
          let financial_item = {
            id: item.id,
            buysell: buysell_types.find((type) => type.value == item.buysell),
            item_type_id: item.item_type_id,
            quantity: item.quantity,
            item: item.item,
            transport_type_id: item.transport_type_id,
            status: financial_item_status_type.find((type) => type.id == item.status),
            order: item.order,
            net_price: item.net_price,
            tax_price: item.tax_price,
            total_price: item.total_price,
            currency: item.currency,
            account_id: item.account_id,
            description: item.description,
            dump_id: Math.random(),
          };
          offer_data.financial_items.push(financial_item);
        });
      }

      if (load_data.load_charge_person && load_data.load_charge_person.length > 0) {
        load_data.load_charge_person.forEach((item) => {
          if (item.user_type == 1) {
            offer_data.operation_officers = item.user_id;
          } else {
            offer_data.sales_representatives.push(item.user_id);
          }
        });
      }

      if (load_data.load_email_id && Array.isArray(load_data.load_email_id) && load_data.load_email_id.length > 0) {
        load_data.load_email_id.forEach((item) => {
          if (item.key == "to") {
            offer_data.email.to.push(item.email);
          } else if (item.key == "cc") {
            offer_data.email.cc.push(item.email);
          }
        });
      }

      if (load_data.load_file && load_data.load_file.length > 0) {
        load_data.load_file.forEach((item) => {
          offer_data.files.push({
            data: item,
            file: null,
          });
        });
      }
    }
    page_loading.value = false;
  }
});

const onShowDrawer = () => {};

const sendCustomerOfferMail = async () => {
  offer_mail_loading.value = true;
  const res = await axios.post("/api/v1/offer_send_email", {
    id: offer_id.value,
  });
  if (res) {
    toast.success("Teklif maili başarıyla gönderildi.");
  } else {
    toast.error("Teklif maili gönderilirken bir hata oluştu.");
  }
  offer_mail_loading.value = false;
};
const sendSiberData = async () => {
  send_siber_loading.value = true;
  const res = await DataStore.SEND_LOAD_SIBER(offer_id.value);
  if (res) {
    offer_data.reservation_number = res.reservation_number;
    offer_data.siber_id = res.siber_id;
    toast.success("Veriler sibere başarıyla gönderildi.");
  }
  send_siber_loading.value = false;
};
const createRealLoad = async () => {
  create_real_load_loading.value = true;
  const res = await DataStore.CREATE_REAL_LOAD(offer_data.siber_id);
  if (res) {
    offer_data.load_number = res.yuk_no;
    toast.success("Yük oluşturuldu.");
  }
  create_real_load_loading.value = false;
};

const calculateSingleTotalPrice = (item) => {
  if (item.net_price !== undefined && item.net_price !== null && item.quantity !== undefined && item.quantity !== null) {
    item.total_price = item.net_price * item.quantity;
  }
};

const calculateLDM = () => {
  new_product_data.lademeter = (((new_product_data.width * new_product_data.length) / 24000) * new_product_data.quantity).toFixed(2);
};

const onChangeWorkType = (value) => {
  switch (value.id) {
    case 1: // İhracat
      offer_data.front_transportation_by_us = true;
      offer_data.final_transportation_by_us = false;
      break;
    case 2: // İthalat
      offer_data.front_transportation_by_us = true;
      offer_data.final_transportation_by_us = false;
      break;
    case 3: // Transit
      offer_data.front_transportation_by_us = false;
      offer_data.final_transportation_by_us = false;
      break;
    default:
      offer_data.front_transportation_by_us = false;
      offer_data.final_transportation_by_us = false;
  }
};

const onStatusTypeChange = (data) => {
  if (data.value.id == 5) {
  }
};

const updateLoadFiles = async () => {
  const form_data = new FormData();
  console.log(offer_data.files);
  form_data.append("load_id", offer_id.value);
  offer_data.files.forEach((item, index) => {
    if (item.data) {
      form_data.append(`files[${index}][id]`, item.data.id);
    }
    if (item.file) {
      form_data.append(`files[${index}][file]`, item.file);
    }
  });
  const res = await DataStore.UPDATE_LOAD_FILE({
    data: form_data,
  });
  if (res) {
    toast.success("Dosyalar başarıyla kaydedildi.");
  } else {
    toast.error("Dosyalar kaydedilirken bir hata oluştu.");
  }
};

const onHideDrawer = () => {
  let new_query = { ...Route.query };
  delete new_query.offer_id;
  Router.push({
    query: new_query,
  });
  offer_id.value = null;
  offer_data.reservation_number = "";
  offer_data.load_number = "";
  offer_data.work_type_id = "";
  offer_data.loading_type_id = "";
  offer_data.payment_type_id = {
    id: 9,
    name: "PEŞİN",
    code: "2",
    siber_id: "97081C47-4F6A-4F37-9557-BC1CAC802106",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  };
  offer_data.status_type_id = {
    id: 4,
    name: "TEKLİF",
    number: "10",
    siber_id: "EC922C9E-C2CF-4716-A198-F716FDA50358",
    created_at: "2025-01-03T13:16:38.000000Z",
    updated_at: "2025-01-03T13:16:38.000000Z",
  };
  offer_data.way_of_working = "";
  offer_data.load_transfer_type_id = "";
  offer_data.instruction_type_id = "";
  offer_data.romork_type_id = "";
  offer_data.offer_date = new Date();
  offer_data.offer_validity_date = new Date(new Date().setDate(new Date().getDate() + 7));
  offer_data.marketing_notification_date = new Date();
  offer_data.customer_id = "";
  offer_data.sender_id = "";
  offer_data.receiver_id = "";
  offer_data.agent_id = "";
  offer_data.company_pay_freight_id = "";
  offer_data.description = "";
  offer_data.departure_country_id = "";
  offer_data.transit_country_id = "";
  offer_data.target_country_id = "";
  offer_data.department_id = {
    id: 7,
    name: "SATIŞ & PAZARLAMA",
    siber_id: "C249E951-FB3F-4FF9-A1C4-EF0223A00B75",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  };
  offer_data.content_items = [];
  offer_data.financial_items = [];
  offer_data.operation_officers = null;
  offer_data.sales_representatives = [];
  offer_data.front_transportation_by_us = false;
  offer_data.final_transportation_by_us = false;
  offer_data.mail_id = "";
  offer_data.email.to = [];
  offer_data.email.cc = [];
  offer_data.files = [];
  offer_data.siber_id = "";

  new_product_data.product_type_id = {
    id: 175,
    name: "YEDEK PARÇA",
    siber_id: "7F06E651-57E8-418E-B914-FFD2B576C8C4",
    created_at: "2025-01-03T13:17:10.000000Z",
    updated_at: "2025-01-03T13:17:10.000000Z",
  };
  new_product_data.case_type_id = {
    id: 120,
    name: "PALET",
    edikod: "",
    siber_id: "EAC8DA4F-895F-435B-BCB4-D5E6FF767D54",
    created_at: "2025-01-03T13:17:11.000000Z",
    updated_at: "2025-01-03T13:17:11.000000Z",
  };
  new_product_data.quantity = null;
  new_product_data.width = null;
  new_product_data.length = null;
  new_product_data.height = null;
  new_product_data.gross_weight = null;
  new_product_data.net_weight = null;
  new_product_data.volume = null;
  new_product_data.lademeter = null;
  new_product_data.stackable = {
    id: 0,
    name: "İstiflenemez",
  };

  new_financial_item.buysell = null;
  new_financial_item.item_type_id = null;
  new_financial_item.quantity = null;
  new_financial_item.item = null;
  new_financial_item.transport_type_id = null;
  new_financial_item.status = null;
  new_financial_item.order = null;
  new_financial_item.net_price = null;
  new_financial_item.tax_price = null;
  new_financial_item.total_price = null;
  new_financial_item.currency = {
    id: 98,
    name: "AMERİKA BİRLEŞİK DEVLETLERİ DOLARI",
    symbol: null,
    code: "USD",
    siber_id: "98A1F932-AD6C-4A57-881D-DFE19362FA2C",
    created_at: "2025-01-03T13:28:31.000000Z",
    updated_at: "2025-01-03T13:28:31.000000Z",
  };
  new_financial_item.account_id = null;
  new_financial_item.description = "";
  offer_product_dialog_visible.value = false;
  offer_financial_dialog_visible.value = false;

  offer_drawer_transition_status.value = true;

  emits("onHideDrawer");
};

watch(
  () => Route.query.offer_id,
  (newVal) => {
    if (!newVal) {
      offer_id.value = null;
      offer_drawer_visible.value = false;
    }
  }
);

onMounted(() => {
  if (Route.query.offer_id) {
    offer_id.value = Route.query.offer_id;
    offer_drawer_visible.value = true;
  }
});
</script>

<style>
.v-enter-active,
.v-leave-active {
  transition: opacity 0.25s;
}

.v-enter-from,
.v-leave-to {
  opacity: 0;
}
</style>
