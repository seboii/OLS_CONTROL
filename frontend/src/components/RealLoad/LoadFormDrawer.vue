<template>
  <div>
    <Drawer v-model:visible="load_drawer_status" header=" " position="bottom" class="min-h-svh" @show="onShowDrawer" @hide="onHideDrawer">
      <div v-if="load_loading" class="flex items-center justify-center max-lg:py-20 lg:min-h-full">
        <ProgressSpinner class="size-20!" />
      </div>
      <div v-else class="lg:mx-auto lg:max-w-(--breakpoint-lg)">
        <div class="flex justify-between gap-4 max-lg:flex-col">
          <div class="flex items-center gap-4">
            <div>
              <div class="mb-1 text-xl font-medium tracking-tight text-black lg:text-4xl">
                {{ load_data.id ? "Yük Bilgileri" : "Yeni Yük Oluştur" }}
              </div>
              <p class="text-sm text-gray-500">Yük bilgilerini eksiksiz doldurunuz.</p>
            </div>
            <div class="flex items-center justify-center">
              <div
                v-if="load_data.id && load_data.mail_id"
                class="relative rounded-full border flex items-center justify-center gap-2 bg-gray-50 p-2.5 transition-all max-lg:hidden"
                v-tooltip="{ content: `Yapay zeka ile oluşturuldu.`, delay: 0 }"
              >
                <ArtificialIntelligence04Icon v-if="load_data.mail_id" class="text-primary" />
                <div class="text-sm text-gray-600 text-nowrap hidden!">Yapay zeka ile oluşturuldu.</div>
              </div>
            </div>
          </div>
          <div class="flex items-center lg:justify-end gap-2">
            <Button
              @click="createOffer"
              :disabled="offer_loading"
              :loading="offer_loading"
              :label="load_data.id ? `Kaydet` : `Yeni Teklif Oluştur`"
              class="w-full lg:w-fit! px-6! py-3! text-nowrap"
              fluid
            />
            <Button v-if="load_data.id && false" type="button" icon="" @click="OfferFormDrawerOpenExtraMenu" outlined class="min-w-12!">
              <template #icon>
                <Menu01Icon size="24" />
              </template>
            </Button>
            <Popover v-if="load_data.id && false" ref="offer_form_drawer_extra_menu">
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
                  v-if="load_data.siber_id"
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
            <Tab v-if="load_data.id && load_data.email" value="4">İlgili E-Posta</Tab>
            <Tab v-if="load_data.id" value="5">Hareketler</Tab>
            <Tab v-if="load_data.id" value="6">Faturalar</Tab>
            <Tab value="7">Dosya Arşivi</Tab>
          </TabList>
          <TabPanels class="px-0!">
            <TabPanel value="0">
              <div class="grid lg:grid-cols-12 gap-4 mt-4 mb-6 lg:mb-10">
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax
                      v-model="load_data.work_type"
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
                  <FloatLabel variant="on">
                    <SelectAjax v-model="load_data.load_transfer_type" :api="`/api/v1/load_transfer_type`" optionLabel="name" class="w-full" filter>
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
                    <SelectAjax v-model="load_data.load_type" :api="`/api/v1/loading_type`" optionLabel="name" class="w-full" filter />
                    <label for="username">Yük Tipi</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="load_data.department" :api="`/api/v1/department`" optionLabel="name" class="w-full" filter>
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
                    <SelectAjax v-model="load_data.instruction" :api="`/api/v1/instruction`" optionLabel="name" class="w-full" filter />
                    <label>Talimat</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="load_data.romork_type" :api="`/api/v1/romork_type`" optionLabel="name" class="w-full" filter>
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
                      v-model="load_data.delivery_method"
                      :api="`/api/v1/load_transfer_deliver_method`"
                      :optionLabel="(e) => `${e.name} - ${e.edikod}`"
                      class="w-full"
                      filter
                    >
                      <template #option="slotProps">
                        <div>
                          <div class="text-sm">{{ slotProps.option.name }}</div>
                          <div class="text-xs text-gray-500">{{ slotProps.option.edikod }}</div>
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Teslimat Şekli</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="load_data.load_status" :api="`/api/v1/load_status_type`" optionLabel="name" class="w-full" filter />
                    <label for="username">Yük Durumu</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <SelectAjax v-model="load_data.payment_type" :api="`/api/v1/payment_type`" optionLabel="name" class="w-full" filter>
                      <template v-if="usePermissionStatus('payment_type_management').read && false" #footer>
                        <div class="p-2">
                          <Button @click="FeatureStore.SET_PAYMENT_TYPES_MODAL_STATUS(true)" label="Yeni Ekle" fluid severity="secondary" size="small" />
                        </div>
                      </template>
                    </SelectAjax>
                    <label for="username">Ödeme Şekli</label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel variant="on">
                    <Select v-model="load_data.way_of_working" :options="way_of_working_options" optionLabel="label" class="w-full" fluid />
                    <label for="username">Çalışma Şekli</label>
                  </FloatLabel>
                </div>
                <div v-if="false" class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.car_height" v-keyfilter.money fluid />
                    <label> Araç Yüksekliği (cm) </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.load_number_work_type" readonly fluid />
                    <label> Yük Numarası </label>
                  </FloatLabel>
                </div>
              </div>
              <div class="space-y-6 lg:space-y-10">
                <div v-if="false">
                  <div class="flex items-center gap-3 mb-4">
                    <DashboardSquareSettingIcon size="24" />
                    <div class="text-lg font-medium">Durumlar</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-4 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <Select v-model="load_data.cmr_waiting" :options="yes_no_options" optionLabel="label" fluid />
                        <label>CMR Bekleniyor</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <Select v-model="load_data.fcr_waiting" :options="yes_no_options" optionLabel="label" fluid />
                        <label>FCR Bekleniyor</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <Select v-model="load_data.in_tail" :options="yes_no_options" optionLabel="label" fluid />
                        <label>Kuyrukta</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <Select v-model="load_data.in_truck" :options="yes_no_options" optionLabel="label" fluid />
                        <label>Tırda</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <Calendar03Icon size="24" />
                    <div class="text-lg font-medium">Tarih</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <DatePicker v-model="load_data.request_arrival_date" showIcon fluid showButtonBar iconDisplay="input" />
                        <label for="username">İstenilen Varış Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <DatePicker v-model="load_data.readiness_date" showIcon fluid showButtonBar iconDisplay="input" />
                        <label for="username">Hazır Olma Tarihi</label>
                      </FloatLabel>
                    </div>
                    <div>
                      <FloatLabel variant="on">
                        <DatePicker v-model="load_data.date_of_receipt_customer" showIcon fluid showButtonBar iconDisplay="input" />
                        <label for="username">Müşteriden Alınış Tarihi</label>
                      </FloatLabel>
                    </div>
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <GlobalIcon size="24" />
                    <div class="text-lg font-medium">Konum</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="load_data.departure_country" :api="`/api/v1/country`" optionLabel="name" class="w-full" filter>
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
                        <SelectAjax v-model="load_data.target_country" :api="`/api/v1/country`" optionLabel="name" class="w-full" filter>
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
                  </div>
                </div>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <Calendar03Icon size="24" />
                    <div class="text-lg font-medium">Şirketler</div>
                  </div>
                  <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
                    <div>
                      <FloatLabel variant="on">
                        <SelectAjax v-model="load_data.customer" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
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
                        <SelectAjax v-model="load_data.sender" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
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
                        <SelectAjax v-model="load_data.receiver" :api="`/api/v1/account`" optionLabel="name" class="w-full" filter>
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
                      <ToggleButton
                        :pt="{
                          root: {
                            class: 'py-3!',
                          },
                        }"
                        v-model="load_data.front_transportation_by_us"
                        onLabel="Evet"
                        offLabel="Hayır"
                        class="w-full mt-2"
                      />
                    </div>
                    <div>
                      <div class="text-sm text-gray-500">Son taşıma tarafımızdan yapılır</div>
                      <ToggleButton
                        :pt="{
                          root: {
                            class: 'py-3!',
                          },
                        }"
                        v-model="load_data.final_transportation_by_us"
                        onLabel="Evet"
                        offLabel="Hayır"
                        class="w-full mt-2"
                      />
                    </div>
                  </div>
                </div>
              </div>
            </TabPanel>
            <TabPanel value="1">
              <div class="flex justify-end mt-6 mb-8">
                <Button @click="offer_product_dialog_visible = true" raised outlined size="small">Yeni İçerik Ekle</Button>
              </div>
              <Transition mode="out-in">
                <div v-if="load_data.load_transfer_packages.length > 0" class="relative">
                  <TransitionGroup
                    enter-active-class="transition-opacity duration-500"
                    move-class="transition-all duration-500"
                    enter-from-class="opacity-0"
                    enter-to-class="opacity-100"
                    leave-active-class="transition-opacity duration-500 absolute"
                    leave-from-class="opacity-100"
                    leave-to-class="opacity-0"
                  >
                    <div v-for="(product, index) in load_data.load_transfer_packages" :key="product" class="mb-4">
                      <LoadFormContentItem :data="product" :index="index" @delete="deleteContentItem" />
                    </div>
                  </TransitionGroup>
                </div>
                <div v-else class="flex items-center justify-center">
                  <div class="py-3 px-6 bg-gray-100 rounded-xl text-sm">İçerik bulunamadı.</div>
                </div>
              </Transition>
              <hr class="my-10" />
              <div class="grid grid-cols-1 lg:grid-cols-12 gap-4 mt-12">
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.total_cap" v-keyfilter.money readonly fluid />
                    <label> Toplam Kap </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.total_gross_weight" v-keyfilter.money readonly fluid />
                    <label> Toplam Brüt Ağırlık </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.total_lademeter" v-keyfilter.money readonly fluid />
                    <label> Toplam Lademetre </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.total_lademeter_m3" v-keyfilter.money readonly fluid />
                    <label> Toplam Lademetre (m³) </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.total_volume" v-keyfilter.money readonly fluid />
                    <label> Toplam Hacim </label>
                  </FloatLabel>
                </div>
                <div class="lg:col-span-4">
                  <FloatLabel class="w-full" variant="on">
                    <InputText v-model="load_data.weight_fee" v-keyfilter.money readonly fluid />
                    <label> Ağırlık Ücreti </label>
                  </FloatLabel>
                </div>
              </div>
            </TabPanel>
            <TabPanel value="2">
              <div class="flex justify-end mt-6 mb-8">
                <Button @click="offer_financial_dialog_visible = true" raised outlined size="small">Yeni Kayıt Ekle</Button>
              </div>
              <Transition mode="out-in">
                <div v-if="load_data.load_transfer_invoice_items.length > 0" class="space-y-8">
                  <div v-if="load_data.load_transfer_invoice_items.filter((item) => item.buysell.value == 1).length > 0">
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
                        <div
                          v-for="(item, index) in load_data.load_transfer_invoice_items.filter((item) => item.buysell.value == 1)"
                          :key="item"
                          class="mb-4 w-full"
                        >
                          <LoadFormFinancialItem :data="item" :index="index" @delete="deleteFinancialItem" />
                        </div>
                      </TransitionGroup>
                    </div>
                  </div>
                  <div v-if="load_data.load_transfer_invoice_items.filter((item) => item.buysell.value == 2).length > 0">
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
                        <div
                          v-for="(item, index) in load_data.load_transfer_invoice_items.filter((item) => item.buysell.value == 2)"
                          :key="item"
                          class="mb-4 w-full"
                        >
                          <LoadFormFinancialItem :data="item" :index="index" @delete="deleteFinancialItem" />
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
                    v-model="load_data.customer_representative"
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
                    v-model="load_data.second_customer_representative"
                    :api="`/api/v1/user`"
                    :optionLabel="(e) => `${e.name} ${e.surname}`"
                    class="w-full"
                    dataKey="id"
                    filter
                  />
                  <label>Satış Temsilcisi</label>
                </FloatLabel>
              </div>
            </TabPanel>
            <TabPanel v-if="load_data" value="4">
              <div v-if="load_data.email" class="my-4 p-6 lg:p-8 rounded-lg border bg-white">
                <div class="text-lg text-black font-medium mb-4">{{ load_data.email?.subject }}</div>
                <div v-html="load_data.email?.email_content?.trim()" class="whitespace-pre-line text-sm"></div>
              </div>
            </TabPanel>
            <TabPanel value="5">
              <LoadFormMovements :load-data="load_data" />
            </TabPanel>
            <TabPanel value="6">
              <LoadFormInvoices :load-data="load_data" />
            </TabPanel>
            <TabPanel value="7">
              <FileUploader v-model="load_data.files" />
              <div v-if="load_data.id" class="mt-4 flex justify-end">
                <Button @click="updateLoadFiles" label="Dosyaları Kaydet" />
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
        <Button type="button" label="İptal" severity="secondary" @click="offer_product_dialog_visible = false"></Button>
        <Button type="button" label="Kaydet" @click="addContentItem"></Button>
      </div>
    </Dialog>
    <Dialog v-model:visible="offer_financial_dialog_visible" modal header=" " class="w-full lg:w-[700px]">
      <div>
        <div class="text-xl lg:text-2xl font-semibold mb-1 text-gray-900">Ürün Bilgileri</div>
        <div class="text-sm text-gray-500">Ürün bilgilerini eksiksiz ve doğru girmeye özen gösteriniz.</div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 my-10">
        <div class="flex justify-center w-full lg:col-span-2">
          <SelectButton v-model="new_financial_item.buysell" :options="buysell_types" optionLabel="name" class="w-full [&>.p-togglebutton]:w-full" />
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
              v-model="new_financial_item.account_id"
              :api="`/api/v1/account`"
              :fetchParams="{
                account_type_id: new_financial_item.buysell?.value == 1 ? 2 : 1,
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
          <InputNumber v-model="new_financial_item.tax_price" :maxFractionDigits="2" v-keyfilter.money fluid />
          <label> Vergi Tutarı </label>
        </FloatLabel>
        <FloatLabel class="w-full" variant="on">
          <InputNumber v-model="new_financial_item.total_price" :maxFractionDigits="2" v-keyfilter.money fluid />
          <label> Toplam Tutar </label>
        </FloatLabel>
        <div v-if="false">
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
        <div>
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
          <FloatLabel variant="on">
            <Select
              v-model="new_financial_item.status"
              :options="
                financial_item_status_type.filter((item) => {
                  if (new_financial_item.buysell?.value == 1) {
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
          <Textarea v-model="new_financial_item.description" autoResize fluid />
          <label> Açıklama </label>
        </FloatLabel>
      </div>
      <div class="flex justify-end gap-2">
        <Button type="button" label="İptal" severity="secondary" @click="offer_financial_dialog_visible = false"></Button>
        <Button type="button" label="Kaydet" @click="addFinancialItem"></Button>
      </div>
    </Dialog>
    <AccountFormDrawer :accountFormData="account_form_data" />
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from "vue";
import { buysell_types, financial_item_status_type, product_stackable_types, define_datas, yes_no_options } from "@/data/system_data.js";
import {
  Download05Icon,
  Upload05Icon,
  Calendar03Icon,
  Note01Icon,
  CreditCardPosIcon,
  GlobalIcon,
  Menu01Icon,
  ArtificialIntelligence04Icon,
  DashboardSquareSettingIcon,
} from "hugeicons-vue";
import FileUploader from "@/components/FileUploader.vue";
import LoadFormContentItem from "@/components/RealLoad/LoadFormContentItem.vue";
import LoadFormFinancialItem from "@/components/RealLoad/LoadFormFinancialItem.vue";
import LoadFormMovements from "@/components/RealLoad/LoadFormMovements.vue";
import LoadFormInvoices from "@/components/RealLoad/LoadFormInvoices.vue";
import AccountFormDrawer from "@/components/Accounts/AccountFormDrawer.vue";
import { useDataStore } from "@/stores/data_store.js";
import { useFeatureStore } from "@/stores/general_store.js";
import { toast } from "vue-sonner";
import { useGetIsoDate, usePermissionStatus } from "@/composables/index.js";
import { useRouter, useRoute } from "vue-router";
import { useConfirm } from "primevue/useconfirm";

const Router = useRouter();
const Route = useRoute();
const confirm = useConfirm();
const emits = defineEmits(["formSubmit", "onHideDrawer"]);
const DataStore = useDataStore();
const FeatureStore = useFeatureStore();

const load_drawer_status = defineModel("visible");
const load_id = defineModel("loadId");
const load_loading = ref(false);

const offer_product_dialog_visible = ref(false);
const offer_financial_dialog_visible = ref(false);
const offer_loading = ref(false);
const offer_mail_loading = ref(false);
const send_siber_loading = ref(false);
const offer_form_drawer_extra_menu = ref(false);
const create_real_load_loading = ref(false);

const account_form_data = reactive({
  active_data: null,
  drawer_visible: false,
  form_type: "create",
});

const OfferFormDrawerOpenExtraMenu = (event) => {
  offer_form_drawer_extra_menu.value.toggle(event);
};

const load_data_default = {
  id: "",
  load_transfer_id: "",
  siber_id: "",
  car_height: "-",
  cmr_waiting: "-",
  customer: "-",
  customer_representative: "", //user_id
  second_customer_representative: "", //user_id
  delivery_method: "-",
  department: "-",
  departure_country: "-",
  fcr_waiting: "-",
  final_transportation_by_us: "-",
  front_transportation_by_us: "-",
  in_tail: "-",
  in_truck: "-",
  instruction: "-",
  load_id: "",
  load_number: "-",
  load_number_work_type: "",
  load_status: "-",
  load_transfer_invoice_items: [],
  load_transfer_invoice_maps: [],
  load_transfer_packages: [],
  load_transfer_type: "-",
  load_type: "-",
  loading_continent: "", //KAPATILACAK
  operation_department: "", //boş
  payment_type: "-",
  receiver: "-",
  romork_type: "-",
  sales_rep_code: "",
  sender: "-",
  target_country: "-",
  total_cap: "-", //GÖSTERİLECEK AMA DEĞİŞTİRİLMEYECEK
  total_gross_weight: "-",
  total_lademeter: "-",
  total_lademeter_m3: "-",
  total_volume: "-",
  unloading_continent: "-",
  usercode_with_notification: "", //user_id
  way_of_working: "-",
  weight_fee: "-",
  work_type: "-",
  request_arrival_date: "",
  readiness_date: "",
  date_of_receipt_customer: "",
  files: [],
};

const load_data = reactive({ ...load_data_default });

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
  load_data.load_transfer_packages.push(item_data);

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
      message: "Bu içerik hemen silinecektir. Silme işlemini onaylıyor musunuz?",
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
        const res = await DataStore.DELETE_REAL_LOAD_PACKAGE(id);
        if (res) {
          load_data.load_transfer_packages = load_data.load_transfer_packages.filter((item) => item.dump_id != dump_id);
        }
      },
    });
  } else {
    load_data.load_transfer_packages = load_data.load_transfer_packages.filter((item) => item.dump_id != dump_id);
  }
};

const addFinancialItem = async () => {
  if (
    !new_financial_item.buysell ||
    !new_financial_item.quantity ||
    !new_financial_item.item ||
    !new_financial_item.net_price ||
    !new_financial_item.total_price ||
    !new_financial_item.currency ||
    !new_financial_item.status
  ) {
    toast.error("Lütfen tüm alanları doldurunuz.");
    return;
  }
  let item_data = { ...new_financial_item, dump_id: Math.random() };
  load_data.load_transfer_invoice_items.push(item_data);

  new_financial_item.buysell = null;
  new_financial_item.item_type_id = null;
  new_financial_item.quantity = null;
  new_financial_item.item = null;
  new_financial_item.transport_type_id = null;
  new_financial_item.status = null;
  new_financial_item.order = null;
  new_financial_item.net_price = null;
  // new_financial_item.tax_price = null;
  new_financial_item.total_price = null;
  new_financial_item.description = null;
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
        const res = await DataStore.DELETE_REAL_LOAD_INVOICE_ITEM(id);
        if (res) {
          load_data.load_transfer_invoice_items = load_data.load_transfer_invoice_items.filter((item) => item.dump_id != dump_id);
        }
      },
    });
  } else {
    load_data.load_transfer_invoice_items = load_data.load_transfer_invoice_items.filter((item) => item.dump_id != dump_id);
  }
};
const createOffer = async ({ send_mail } = {}) => {
  offer_loading.value = true;
  const form_data = new FormData();
  if (load_data.id) {
    form_data.append("id", load_data.id);
  }
  form_data.append("load_status_id", load_data.load_status?.id ?? "");
  form_data.append("load_type_id", load_data.load_type?.id ?? "");
  form_data.append("customer_id", load_data.customer?.id ?? "");
  form_data.append("sender_id", load_data.sender?.id ?? "");
  form_data.append("receiver_id", load_data.receiver?.id ?? "");
  form_data.append("payment_type_id", load_data.payment_type?.id ?? "");
  form_data.append("in_truck", load_data.in_truck?.value ?? "");
  form_data.append("in_tail", load_data.in_tail?.value ?? "");
  form_data.append("cmr_waiting", load_data.cmr_waiting?.value ?? "");
  form_data.append("fcr_waiting", load_data.fcr_waiting?.value ?? "");
  form_data.append("instruction_id", load_data.instruction?.id ?? "");
  form_data.append("romork_type_id", load_data.romork_type?.id ?? "");
  form_data.append("total_gross_weight", load_data.total_gross_weight ?? "");
  form_data.append("total_volume", load_data.total_volume ?? "");
  form_data.append("total_lademeter", load_data.total_lademeter ?? "");
  form_data.append("total_lademeter_m3", load_data.total_lademeter_m3 ?? "");
  form_data.append("weight_fee", load_data.weight_fee ?? "");
  form_data.append("customer_representative_name", load_data.customer_representative?.id ?? "");
  form_data.append("second_customer_representative_name", load_data.second_customer_representative?.id ?? "");
  form_data.append("department_id", load_data.department?.id ?? "");
  form_data.append("car_height", load_data.car_height ?? "");
  form_data.append("load_transfer_type_id", load_data.load_transfer_type?.id ?? "");
  form_data.append("departure_country_id", load_data.departure_country?.id ?? "");
  form_data.append("target_country_id", load_data.target_country?.id ?? "");
  form_data.append("way_of_working", load_data.way_of_working?.value ?? "");
  form_data.append("delivery_method_id", load_data.delivery_method?.id ?? "");
  form_data.append("front_transportation_by_us", load_data.front_transportation_by_us ? 1 : 0);
  form_data.append("final_transportation_by_us", load_data.final_transportation_by_us ? 1 : 0);

  if (load_data.request_arrival_date) {
    form_data.append("request_arrival_date", useGetIsoDate(load_data.request_arrival_date));
  }
  if (load_data.readiness_date) {
    form_data.append("readiness_date", useGetIsoDate(load_data.readiness_date));
  }
  if (load_data.date_of_receipt_customer) {
    form_data.append("date_of_receipt_customer", useGetIsoDate(load_data.date_of_receipt_customer));
  }

  load_data.load_transfer_packages.forEach((item, index) => {
    if (item.id) {
      form_data.append(`load_content[${index}][id]`, item.id);
    }
    form_data.append(`load_transfer_content_item[${index}][product_type_id]`, item.product_type_id?.id ?? "");
    form_data.append(`load_transfer_content_item[${index}][case_type_id]`, item.case_type_id?.id ?? "");
    form_data.append(`load_transfer_content_item[${index}][quantity]`, item.quantity ?? "");
    form_data.append(`load_transfer_content_item[${index}][width]`, item.width ?? "");
    form_data.append(`load_transfer_content_item[${index}][length]`, item.length ?? "");
    form_data.append(`load_transfer_content_item[${index}][height]`, item.height ?? "");
    form_data.append(`load_transfer_content_item[${index}][gross_weight]`, item.gross_weight ?? "");
    form_data.append(`load_transfer_content_item[${index}][net_weight]`, item.net_weight ?? "");
    form_data.append(`load_transfer_content_item[${index}][volume]`, item.volume ?? "");
    form_data.append(`load_transfer_content_item[${index}][lademeter]`, item.lademeter ?? "");
    form_data.append(`load_transfer_content_item[${index}][stackable]`, item.stackable?.id ?? "");
    form_data.append(`load_transfer_content_item[${index}][yukkoliid]`, item.yukkoliid ?? "");
  });
  load_data.load_transfer_invoice_items.forEach((item, index) => {
    if (item.id) {
      form_data.append(`load_transfer_invoice_item[${index}][id]`, item.id);
    }
    form_data.append(`load_transfer_invoice_item[${index}][buysell]`, item.buysell.value ? item.buysell.value : "");
    form_data.append(`load_transfer_invoice_item[${index}][quantity]`, item.quantity ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][item_id]`, item.item?.id ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][transport_type_id]`, item.transport_type_id?.id ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][order]`, 1);
    form_data.append(`load_transfer_invoice_item[${index}][net_price]`, item.net_price ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][total_price]`, item.total_price ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][currency_code]`, item.currency?.id ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][account_id]`, item.account_id?.id ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][description]`, item.description ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][modulkalemid]`, item.modulkalemid ?? "");
    form_data.append(`load_transfer_invoice_item[${index}][status]`, item.status?.id ?? "");
  });

  let res = null;
  if (load_data.id) {
    res = await DataStore.UPDATE_REAL_LOAD({ data: form_data, id: load_data.id });
    if (res) {
      toast.success("Yük başarıyla güncellendi.");
      emits("formSubmit", {
        load_data: load_data,
      });

      const res_load_data = res.data;

      load_data.load_transfer_packages = [];
      if (res_load_data.load_transfer_package && res_load_data.load_transfer_package.length > 0) {
        res_load_data.load_transfer_package.forEach((item) => {
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
            yukkoliid: item.yukkoliid,
            dump_id: Math.random(),
          };
          load_data.load_transfer_packages.push(product);
        });
      }

      load_data.load_transfer_invoice_items = [];
      if (res_load_data.load_transfer_invoice_item && res_load_data.load_transfer_invoice_item.length > 0) {
        res_load_data.load_transfer_invoice_item.forEach((item) => {
          let financial_item = {
            id: item.id,
            buysell: buysell_types.find((type) => type.value == item.buysell),
            item_type_id: item.item_type_id,
            quantity: item.quantity,
            item: item.item_id,
            transport_type_id: item.transport_type_id,
            status: financial_item_status_type.find((type) => type.id == item.status),
            order: item.order,
            net_price: item.net_price,
            tax_price: item.tax_price,
            total_price: item.total_price,
            currency: item.currency_code,
            account_id: item.account_id,
            description: item.description,
            modulkalemid: item.modulkalemid,
            load_transfer_einvoice: item.load_transfer_einvoice,
            dump_id: Math.random(),
          };
          load_data.load_transfer_invoice_items.push(financial_item);
        });
      }
    }
  } else {
    res = await DataStore.CREATE_REAL_LOAD({ data: form_data });
    if (res) {
      toast.success("Yük başarıyla oluşturuldu.");
      load_drawer_status.value = false;
      emits("formSubmit", {
        load_data: load_data,
      });

      Object.assign(load_data, load_data_default);

      offer_product_dialog_visible.value = false;
      offer_financial_dialog_visible.value = false;
    }
  }
  offer_loading.value = false;
};

watch(
  () => load_id.value,
  async (newVal) => {
    if (newVal) {
      load_loading.value = true;
      const res = await DataStore.GET_REAL_LOAD(newVal);
      if (res) {
        let new_load_data = res.data;
        load_data.id = new_load_data.id;
        load_data.load_transfer_id = new_load_data.load_transfer_id;
        load_data.siber_id = new_load_data.siber_id;
        load_data.car_height = new_load_data.car_height;
        load_data.cmr_waiting = yes_no_options.find((option) => option.value == new_load_data.cmr_waiting);
        load_data.customer = new_load_data.customer_id;
        load_data.customer_representative = new_load_data.customer_representative_name;
        load_data.second_customer_representative = new_load_data.second_customer_representative_name;
        load_data.delivery_method = new_load_data.delivery_method_id;
        load_data.department = new_load_data.department_id;
        load_data.departure_country = new_load_data.departure_country_id;
        load_data.fcr_waiting = yes_no_options.find((option) => option.value == new_load_data.fcr_waiting);
        load_data.final_transportation_by_us = new_load_data.final_transportation_by_us;
        load_data.front_transportation_by_us = new_load_data.front_transportation_by_us;
        load_data.in_tail = yes_no_options.find((option) => option.value == new_load_data.in_tail);
        load_data.in_truck = yes_no_options.find((option) => option.value == new_load_data.in_truck);
        load_data.instruction = new_load_data.instruction_id;
        load_data.load_number = new_load_data.load_number;
        load_data.load_number_work_type = new_load_data.load_number_work_type;
        load_data.load_status = new_load_data.load_status_id;
        load_data.load_transfer_type = new_load_data.load_transfer_type_id;
        load_data.load_type = new_load_data.load_type_id;
        load_data.loading_continent = new_load_data.loading_continent;
        load_data.operation_department = new_load_data.operation_department_id;
        load_data.payment_type = new_load_data.payment_type_id;
        load_data.receiver = new_load_data.receiver_id;
        load_data.romork_type = new_load_data.romork_type_id;
        load_data.sales_rep_code = new_load_data.sales_rep_code;
        load_data.sender = new_load_data.sender_id;
        load_data.target_country = new_load_data.target_country_id;
        load_data.total_cap = new_load_data.total_cap;
        load_data.total_gross_weight = new_load_data.total_gross_weight;
        load_data.total_lademeter = new_load_data.total_lademeter;
        load_data.total_lademeter_m3 = new_load_data.total_lademeter_m3;
        load_data.total_volume = new_load_data.total_volume;
        load_data.unloading_continent = new_load_data.unloading_continent;
        load_data.usercode_with_notification = new_load_data.usercode_with_notification;
        load_data.way_of_working = way_of_working_options.find((option) => option.value == new_load_data.way_of_working);
        load_data.weight_fee = new_load_data.weight_fee;
        load_data.work_type = new_load_data.work_type;
        load_data.request_arrival_date = new_load_data.request_arrival_date ? new Date(new_load_data.request_arrival_date) : "";
        load_data.readiness_date = new_load_data.readiness_date ? new Date(new_load_data.readiness_date) : "";
        load_data.date_of_receipt_customer = new_load_data.date_of_receipt_customer ? new Date(new_load_data.date_of_receipt_customer) : "";
        load_data.load_transfer_invoice_maps = new_load_data.load_transfer_invoice_maps;
        load_data.load_id = new_load_data.load_belongs.id;

        load_data.load_transfer_packages = [];
        if (new_load_data.load_transfer_package && new_load_data.load_transfer_package.length > 0) {
          new_load_data.load_transfer_package.forEach((item) => {
            let product = {
              id: item.id,
              yukkoliid: item.yukkoliid,
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
            load_data.load_transfer_packages.push(product);
          });
        }

        load_data.load_transfer_invoice_items = [];
        if (new_load_data.load_transfer_invoice_item && new_load_data.load_transfer_invoice_item.length > 0) {
          new_load_data.load_transfer_invoice_item.forEach((item) => {
            let financial_item = {
              id: item.id,
              modulkalemid: item.modulkalemid,
              buysell: buysell_types.find((type) => type.value == item.buysell),
              item_type_id: item.item_type_id,
              quantity: item.quantity,
              item: item.item_id,
              transport_type_id: item.transport_type_id,
              status: financial_item_status_type.find((type) => type.id == item.status),
              order: item.order,
              net_price: item.net_price,
              tax_price: item.tax_price,
              total_price: item.total_price,
              currency: item.currency_code,
              account_id: item.account_id,
              description: item.description,
              load_transfer_einvoice: item.load_transfer_einvoice,
              dump_id: Math.random(),
            };
            load_data.load_transfer_invoice_items.push(financial_item);
          });
        }

        load_data.files = [];
        if (new_load_data.load_belongs && new_load_data.load_belongs.load_file.length > 0) {
          new_load_data.load_belongs.load_file.forEach((item) => {
            load_data.files.push({
              data: item,
              file: null,
            });
          });
        }
      }
      load_loading.value = false;
    }
  }
);

const onShowDrawer = () => {};

const sendSiberData = async () => {
  send_siber_loading.value = true;
  const res = await DataStore.SEND_LOAD_SIBER(load_data.id);
  if (res) {
    toast.success("Veriler sibere başarıyla gönderildi.");
  }
  send_siber_loading.value = false;
};
const createRealLoad = async () => {
  create_real_load_loading.value = true;
  const res = await DataStore.CREATE_REAL_LOAD(load_data.siber_id);
  if (res) {
    toast.success("Yük oluşturuldu.");
  }
  create_real_load_loading.value = false;
};

const calculateSingleTotalPrice = (item) => {
  if (item.net_price && item.quantity) {
    item.total_price = item.net_price * item.quantity;
  }
};

const calculateLDM = () => {
  new_product_data.lademeter = ((new_product_data.width * new_product_data.length) / 24000).toFixed(2);
};

const onChangeWorkType = (value) => {
  switch (value.id) {
    case 1: // İhracat
      load_data.front_transportation_by_us = true;
      load_data.final_transportation_by_us = false;
      break;
    case 2: // İthalat
      load_data.front_transportation_by_us = true;
      load_data.final_transportation_by_us = false;
      break;
    case 3: // Transit
      load_data.front_transportation_by_us = false;
      load_data.final_transportation_by_us = false;
      break;
    default:
      load_data.front_transportation_by_us = false;
      load_data.final_transportation_by_us = false;
  }
};

const updateLoadFiles = async () => {
  const form_data = new FormData();
  form_data.append("load_id", load_data.load_id);
  load_data.files.forEach((item, index) => {
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
  Object.assign(load_data, load_data_default);
  load_id.value = null;

  let new_query = { ...Route.query };
  delete new_query.load_id;
  Router.push({
    query: new_query,
  });

  offer_product_dialog_visible.value = false;
  offer_financial_dialog_visible.value = false;

  emits("onHideDrawer");
};

watch(
  () => Route.query.load_id,
  (newVal) => {
    if (!newVal) {
      load_id.value = null;
      load_drawer_status.value = false;
    }
  }
);

onMounted(() => {
  if (Route.query.load_id) {
    load_id.value = Route.query.load_id;
    load_drawer_status.value = true;
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
