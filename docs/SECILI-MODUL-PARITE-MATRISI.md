# Seçili Modül Parite Matrisi

Kaynaklar: `olsold` (Laravel 10, üretim davranışı), `olsnew` (ASP.NET Core 9, olgun port — çoğu modül zaten
"tamamlandı, test edildi"), `olsnew/docs/PROJE-ANALIZ-RAPORU.md` (bağımsız analiz, 08 Ağustos 2026).
Ayrıntılı, satır numaralı kanıtlar için: `../../olstemel` oturumunda üretilen üç envanter raporu
(olsold, olsnew backend, olsnew frontend) bu matrisin temelini oluşturmuştur; buradaki her karar o
envanterlerdeki dosya+satır kanıtlarına dayanır.

Durum kısaltmaları: **H**=Hazır (olsnew'de birebir var, sadece taşınacak), **HD**=Hazır ama Düzeltilecek
(olsnew'de var ama parite/güvenlik gereği değiştirilecek), **E**=Eksik (yeniden yazılacak), **KD**=Kapsam Dışı.

---

## 0. Genel kararlar (tüm modülleri etkiler)

| Konu | Karar |
|---|---|
| Backend kaynağı | olsnew (src/OLS.API, OLS.Business, OLS.DataAccess) — dosyalar büyük ölçüde birebir kopyalanacak, aynı namespace yapısı korunacak |
| Frontend kaynağı | olsnew/frontend (Vue3+Vite+PrimeVue4+Tailwind4) — "hazır tasarım" budur; olstemel/docs'taki React/shadcn kiti OLS'e özgü ekran içermediği için ilgisiz kabul edildi |
| DbContext stratejisi | olsnew'in tek "Baseline" migration'ı 91 tablodan yalnızca `revoked_tokens`'ı gerçekten oluşturuyor (diğerleri var sayılıyor) — temiz kurulum için KULLANILAMAZ. Karar: ~47 entity'ye trim edilmiş yeni bir `OlsDbContext`, boş bir Postgres'e karşı gerçek bir `ScopedBaseline` migration'ı üretilecek (temiz kurulumda çalışsın diye) |
| status_type_id (DATA-002) | olsold'un `StatusTypeSeeder` çıktısı (Teklif/İşlemde/Onaylandı/Tamamlandı/İptal) ile kodun gerçekte varsaydığı anlam (1=Olumsuz,2=Sipariş,3=Düzeltme,4=Teklif,5=Olumlu) TAMAMEN farklı. Karar: `status_types` tablosuna kararlı `code` sütunu eklenip (`REJECTED/ORDER/CORRECTION/OFFER/APPROVED`) gerçek çalışma zamanı anlamıyla seed edilecek; C# tarafında `StatusTypeCodes` sabitleri + tek bir lookup kullanılacak, ham sayısal literal kalmayacak |
| TransferData (Siber ETL) | 20 action'lı toplu geçmiş-veri aktarım aracı (~1500 satır) KAPSAM DIŞI bırakıldı — sıfır geçmiş Siber verisiyle başlanacak, ileri yazımlar (Account/Car/Expedition/LoadTransfer save akışları) zaten kendi Siber senkronunu yapıyor. Bilinçli kapsam kararı, teslim raporunda gerekçelendirilecek |
| FluentValidation paketi | olsnew'de referans var ama sıfır kullanım (bağımsız doğrulandı) — yeni solution'a paket olarak taşınmayacak, mevcut el-yazımı `Validate()` deseni korunacak |
| Test projesi | olsnew'de (ve olsold'da) SIFIR otomatik test var — HEDEF'te sıfırdan 3 test projesi kurulacak (Business.Tests, API.IntegrationTests, DataAccess.Tests) |
| Eksik altyapı | Health check, correlation ID, rate limiting olsnew'de YOK — HEDEF'e eklenecek |

---

## 1. Müşteri (Cari / `Account`)

| Ekran/işlem | Eski FE (olsold) | Yeni FE (olsnew) | Laravel route+method | .NET route+servis | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Liste (sayfalama+arama+filtre) | `resources/js/pages/accounts/index.vue` | `pages/accounts/index.vue` + `AccountTable.vue` | `GET /api/v1/account` `FrontAccountController::all` | `GET /api/v1/account` `AccountController.All`→`AccountService.ListAsync` | query `account_type_id,search,per_page` → `{data,message}` 200 | `accounts`,`account_type_mappings`,`user_account_mappings` | `account_management` (read) | Country/City/District/TaxOffice/AccountType | H | Birebir taşı |
| Detay | aynı sayfa (drawer) | `AccountFormDrawer.vue` (631 satır) | `GET /api/v1/account/{id}` `single` | `GET /api/v1/account/{id:long}` `Single` | `{data,message}` 200 / 403 (yetkisiz cari) | aynı | `account_management` (read) | aynı | H | Birebir taşı |
| Oluşturma | `AccountFormDrawer.vue` | aynı (+Offer/Load drawer'larından da açılıyor) | `POST /api/v1/account` `save` — **kaynakta yetki kontrolü YOK** | `POST /api/v1/account` `Save`→`CreateAsync` | FormData → `{data,message}` 200 / 500 | aynı + Siber `skn_firma`/`sbr_firmatemsilcisi` | olsnew: `account_management` (create) — **kaynaktaki eksik kontrol olsnew'de zaten düzeltilmiş** | aynı | H | Birebir taşı (olsnew'in düzeltmesini koru) |
| Güncelleme | aynı | aynı | `POST /api/v1/account/update` `update` | `POST /api/v1/account/update` `Update` | FormData(`id` dahil) → `{data,message}` | aynı | `account_management` (update) | aynı | H | Birebir taşı |
| Silme | aynı | aynı | `DELETE /api/v1/account` (`deletion_id[]`) | `DELETE /api/v1/account` `Delete` | `{message}` | aynı | `account_management` (delete) | — | H | Birebir taşı. Not: yerel silme Siber'e yansımıyor — kaynakta da öyle (bilinçli, kod yorumda), korunuyor |
| account_type_id hatası | `LoadController::saveAi` → `Account::create(['account_type_id'=>1])` — **kolon yok, 500** | `LoadAiImportService` → `account_type_mappings` satırı üretir | — | — | — | `account_type_mappings` | — | HD (olsnew zaten düzeltmiş) | Düzeltmeyi koru, olsold hatasını KOPYALAMA |

**21 alan / form karşılaştırması:** `AccountFormRequest` (olsnew) alanları = `id,name,tax_number,tax_office,invoice_type,country_id,city_id,district_id,address,phone,phone_country_id,email,contact_person,discount,individual_personal,contact_language,avatar,avatar_remove,account_type_mapping[],account_charge_person[],contact_persons[]` — olsold'un `accounts` tablosu + `account_type_mappings` + `account_contact_people` alanlarıyla birebir eşleşiyor. Formda görünmeyen `account_meta_data` alanı olsold'da da kullanılmıyor (kod yorumda) — taşınmayacak.

---

## 2. Teklif (`Load`, `status_type_id` ile ayrışan teklif aşaması)

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Liste+filtre+arama | `pages/offer.vue`? (eski isim farklı olabilir) | `pages/offer.vue`→`components/Offer/Offer.vue`(584)→`OfferTable.vue`(122) | `GET /api/v1/load` `all` | `GET /api/v1/load` `LoadController.All`→`LoadService.ListAsync` | `status_type_id,timeout,search,per_page` → `{data,message}` | `loads`+6 alt tablo | `load_management`(read, buggy scoping olsold'da — olsnew'de düzeltilmiş) | WorkType,LoadingType,PaymentType,StatusType,Country,Account | H | Birebir |
| Detay | aynı | `OfferFormDrawer.vue` (**1882 satır — DOKUNMA**) | `GET /api/v1/load/{id}` | `GET /api/v1/load/{id:long}` | `{data,message}` | aynı | read | aynı | H | Birebir, DOM/davranış değiştirme |
| Oluşturma | `OfferFormDrawer.vue` | aynı | `POST /api/v1/load` `save` | `POST /api/v1/load` `Save` | çok alanlı FormData (`load_content[]`,`load_financial_item[]`,`load_charge_person[]`,`email_to/cc[]`,`files[]`) → `{data,message}` | `loads`+alt tablolar | create | RomorkType,Instruction,Department,Country×3,Account×4 | H | Birebir; **her alt kayıt tek yazılır** (olsold'un çift-yazma hatası olsnew'de düzeltilmiş, doğrulandı) |
| Güncelleme | aynı | aynı | `POST /load/{id?}` `update` — `load_number` doluysa 400 | `POST /api/v1/load/{id:long}` `Update` | aynı şekil | aynı | update | aynı | H | Birebir; route sırası (`saveAi` önce, `{id}` sonra) .NET attribute routing'te açıkça korunmalı |
| Silme | aynı | aynı | `DELETE /load` — `load_number` doluysa 400 | `DELETE /api/v1/load` `Delete` | `{message}` | — | delete | — | H | Birebir |
| Alt satır silme (içerik/mali kalem) | aynı drawer içi | aynı | `DELETE /load/load_content`, `/load_financial_item` | `DELETE /api/v1/load/load_content`, `/load_financial_item` | `{message}` | `load_contents`,`load_financial_items` | delete,`load_management` | — | H | Birebir |
| Zaman aşımı işaretleme | — | — | `POST /load/updateTimeOut` | `POST /api/v1/load/updateTimeOut` | `{id}` | `loads.updated_at` | update (kaynakta buggy: sadece transaction guard'lı) | — | H | Birebir |
| AI'dan teklif (saveAi) | `Offer.vue` (OpenAI tarayıcıda çağrılır) | aynı (Lottie animasyonlu) | `POST /load/saveAi` | `POST /api/v1/load/saveAi` `SaveAi`→`LoadAiImportService` | düz JSON → `{data,message}`/500 | `loads`+alt+`accounts`(yeni müşteri) | yok (kaynakta da yok) | Account,Currency | HD | olsnew'in 2 düzeltmesini koru: (1) account_type_mappings üzerinden müşteri açma, (2) ürün satırları artık doğru okunuyor. AI çağrısı backend'de YAPILMAZ (frontend→OpenAI→backend'e JSON) |
| Dosya ekleri | drawer içi | `FileUploader.vue`(824, paylaşımlı) | `POST /load/file/upload` | `LoadFileService` | "gönderilen liste son hal" (BR-013) | `load_files` | yok | — | H | Birebir; BR-013 test edilecek |
| Teklif e-postası | — | — | `POST /offer_send_email` | `OfferEmailService` (Mail:Enabled=false ise `{sent:false,...}`) | `{id}` | — | yok | SMTP (kapalı varsayılan) | H | Birebir; gerçek SMTP yapılandırılmadan sahte başarı YOK |
| Teklifi Siber'e aktarma | — | Offer.vue içi buton | `POST /transfer_to_siber` `TransferSiberController::save` | `POST /api/v1/transfer_to_siber` `TransferSiberService.TransferOfferAsync` | `{id:Load.id}` → değişken zarf | `loads`+Siber `skn_rezervasyon` | update,`load_management` | Siber | H | Birebir; Siber erişilemezse 503, PG yarım güncellenmez |

**status_type_id gerçek anlamı (olsold'dan doğrulanan, StatusTypeSeeder DEĞİL):** `1=Olumsuz, 2=Sipariş, 3=Düzeltme Talebi, 4=Teklif, 5=Olumlu`. HEDEF'te `status_types.code` = `REJECTED/ORDER/CORRECTION/OFFER/APPROVED` olarak seed edilecek, kod bu code'lara göre çalışacak.

---

## 3. Yük (`LoadTransfer` — teklif→yük dönüşümü)

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Liste+filtre | — | `pages/real-load/list.vue`→`LoadTable.vue`(52) | `GET /load_transfer` | `GET /api/v1/load_transfer` `LoadTransferService.ListAsync` | `search,work_type_id,per_page`→`{data,message}` | `load_transfers` | yok (kaynakta da yok) | — | H | Birebir |
| Detay | — | `LoadFormDrawer.vue` (**1557 satır — DOKUNMA**) | `GET /load_transfer/{id}` | `GET /api/v1/load_transfer/{id:long}` | `{data,message}` (derin graph) | +`load_transfer_invoice_maps`,`load_transfer_packages` | yok | — | H | Birebir |
| Tekliften dönüşüm | Offer.vue "Yüke Çevir" butonu | aynı | `POST /load_transfer` (id=Load.siber_id) | `POST /api/v1/load_transfer` `ConvertOffer` | `{id}`→`{success,message,yuk_no}` | `load_transfers`+Siber `skn_yuk/skn_yukkoli/sfy_modulkalem` | yok | Load,Siber | H | **BR-002/003/004/005 birebir**: zaten açılmış yük tekrar açılamaz; durum Olumlu(5→code APPROVED) olmalı; Siber'e aktarılmış olmalı; 15 alan Siber rezervasyonuyla karşılaştırılır, uyuşmazlık alan adıyla bildirilir (olsnew'in iyileştirmesi — olsold hepsi aynı mesajı veriyordu) |
| Güncelleme | `LoadFormDrawer.vue` | aynı | `POST /load_transfer/{id}` | `POST /api/v1/load_transfer/{id:long}` `Update` | tam nesne (eksik alan = silinir, kaynakta da öyle) → `{data,success,message}` | aynı | yok | — | H | Birebir; snake_case zorunlu (camelCase = tüm kayıt boşalır) |
| Silme | — | — | `DELETE /load_transfer` — **KAYNAKTA KIRIK** (yanlış `PaymentType` sınıfı) | `DELETE /api/v1/load_transfer` `Delete` | `{message}` | — | delete,`payment_management`(kaynak leak) | — | HD | olsnew'de çalışır hale getirilmiş — **olsold'un kırık/yanlış-yetkili halini KOPYALAMA**; HEDEF'te doğru CRUD yetkisi (`load_management` veya özel `load_transfer_management`) verilecek |
| Paket/kalem silme | — | — | `DELETE /load_transfer/load_transfer_package`, `/load_transfer_invoice_item` — kaynakta yetkisiz/yorumda | aynı yollar | `{message}` | `load_transfer_packages`,`...invoice_items` | HEDEF'te gerçek yetki eklenecek | — | HD | Kaynaktaki "yetki yok" durumunu KOPYALAMA — CRUD yetkisi ekle |
| Fatura kalemi CRUD | — | `InvoiceFormInvoiceItems.vue` üzerinden dolaylı | `/load_transfer_invoice_item[...]` — **save/update KIRIK** (olmayan `name`/`siber_id` kolonuna yazıyor) | `LoadTransferInvoiceItemController` | `{data,message}` | `load_transfer_invoice_items` | yalnızca DELETE korumalı (olsnew) | — | HD | olsnew doğru kolonlara yazıyor (`insert_name`,`modulkalemid`) — düzeltmeyi koru; **HEDEF'te GET/POST/PUT'a da CRUD yetkisi eklenecek** (şu an olsnew'de sadece DELETE korumalı, boşluk) |
| Yük hareketleri | — | `LoadFormMovements.vue`(342) | `/load_transfer_movement[...]` — kaynakta yetki YOK | `LoadTransferMovementController` — **olsnew'de de yetki YOK, sadece [Authorize]** | `{status,message,data,deleted_movements}` | `load_transfer_movements` | **HEDEF'te eklenecek** | — | HD | Boşluk kapatılacak: uygun CRUD yetkisi eklenecek |

---

## 4. Sefer (`Expedition` + `ExpeditionLoadMapping` + hareketler)

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Liste+detay | — | `expedition/list.vue`,`form.vue`→`ExpeditionTable.vue`(51),`ExpeditionFormDrawer.vue`(383) | `GET /expedition`,`/expedition/{id}` | `GET /api/v1/expedition`(+`/{id}`) | `{data,message}` | `expeditions` | yok (kaynak) / olsnew: `load_management` | Car,City,WorkType,Department | H | Birebir |
| Oluşturma | aynı | aynı | `POST /expedition` `save` | `POST /api/v1/expedition` `Save` | Siber sefer/pozisyon numaralama | `expeditions`+Siber `skn_sefer/skn_pozisyon` | **HEDEF: özel `expedition_management` slug'ı verilecek** (olsold: leak yok burda; olsnew: `load_management` paylaşıyor — netleştirilecek) | RomorkType(Car uyumu, BR-006) | HD | BR-006 (araç/romork tipi uyumu) birebir; **permission slug netleştirilecek** |
| Güncelleme | aynı | aynı | `PUT /expedition` | `PUT /api/v1/expedition` `Update` | `expedition_status_id==8` özel alan seti | aynı | HD (yukarıdaki gibi) | ExpeditionStatus | H | Birebir |
| Silme | aynı | aynı | `DELETE /expedition` — **kaynakta `case_type_management` leak** | `DELETE /api/v1/expedition` `Delete` | `{message}` | — | olsnew: `load_management` | — | HD | Kaynağın leak'ini KOPYALAMA — **`expedition_management`(delete) verilecek** |
| Sefer hareketleri | — | `ExpeditionFormMovements.vue`(325) | `/expedition/{id}/movements[...]` — **kaynakta id parametresi kullanılmıyor (body'den okunuyor) — BUG** | `MovementService` | `{status,message,data,deleted_movements}` | `expedition_movements` | yok | Load zinciri (BR-010) | HD | olsnew'de düzeltilmiş mi doğrulanacak; BR-010 (sefer hareketi→bağlı yük hareketi) test edilecek; null-check eklenecek (kaynakta uncaught Error) |
| Yük-sefer bağlama | — | `ExpeditionLoad.vue`(241) | `/expedition_load_mapping[...]` | `ExpeditionLoadMappingController` — **sıfır CRUD yetkisi, sadece [Authorize]** | `{data,message}`/`{data,total_expedition_values,message}` | `expedition_load_mappings`+Siber `skn_yukaktarma` | **HEDEF'te eklenecek** | — | HD | BR-007 (aynı yük tekrar bağlanamaz) korunuyor; **permission boşluğu kapatılacak**; iki-DB transaction (olsnew düzeltmesi) korunacak |

---

## 5. Fatura (`Invoice` + `InvoiceFooter`)

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Liste (3 sekme: gider/gelir/onay bekleyen) | — | `invoices.vue`+`incoming/outgoing/pending.vue`→`InvoiceTable.vue`(99) | `GET /invoice?box_type=&invoice_status_id=` | `GET /api/v1/invoice` | varsayılan `invoice_status_id!=7` gizli | `invoices` | `invoice_management`(read) | Account,InvoiceType,InvoiceStatus | H | Birebir; `7` sabiti yerine `invoice_statuses.code` kullanılabilir ama zarf/URL paritesi için sayı da korunacak |
| Detay | — | `InvoiceFormDrawer.vue`(576) | `GET /invoice/{id}` | `GET /api/v1/invoice/{id}` | `{data,message}` | +`load_transfer_invoice_maps` | read | — | H | Birebir |
| Oluşturma | — | aynı | `POST /invoice` | `POST /api/v1/invoice` `Create` | `{message,data}` 201 | `invoices` | create | — | H | Birebir |
| Güncelleme (kalem eşleme) | — | `InvoiceFormInvoiceItems.vue`(195) | `POST /invoice/update` — kalem eşlemeleri SİLİNİP YENİDEN KURULUR | `POST /api/v1/invoice/update` | `{message}` | `invoices`+`load_transfer_invoice_maps` | update | LoadTransferInvoiceItem | H | Birebir — çift göndermede çiftlenme YOK (doğrulanmış davranış) |
| Silme | — | — | `DELETE /invoice/delete` | `DELETE /api/v1/invoice` | `{message}`/404 | — | delete | — | H | Birebir |
| Dipnot CRUD | — | `InvoiceFormDescription.vue`(184) | `/invoice/footer[...]` | `InvoiceFooterController` (aynı dosya) | `{data,message}` | `invoice_footers` | `invoice_management` | — | H | Birebir |
| Kabul/Ret | — | — | `POST /invoice/accept-or-reject` — **KAYNAKTA KIRIK** (`"declined"` küçük harf hiç eşleşmiyor, her zaman Approved oluyor) | **olsnew'de yok (Uyumsoft'a bağlı, portlanmadı)** | — | — | — | — | E (bilinçli) | **Kaynağın hatasını KOPYALAMA.** Uyumsoft yapılandırılmadan bu uç açık "yapılandırılmadı" (503) döndürecek biçimde eklenecek, sahte başarı verilmeyecek |
| Taslak gönder/iptal/onay, PDF görüntüleme | — | `InvoiceFormDocumentViewer.vue`(123) | `/invoice/draft/*`, `/invoice/pdf-view/*` | **olsnew'de yok (SOAP/Uyumsoft gerektirir)** | — | — | — | Uyumsoft (yapılandırılmamış) | E (bilinçli) | Interface tabanlı adaptör + "entegrasyon yapılandırılmadı" 503 döndüren fake implementasyon eklenecek; FE butonu disabled/bilgilendirici durum gösterecek |

---

## 6. Araç (`Car` + `CarType` + `RomorkType` + `CarOwner` + `CarStatusType`)

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Liste+arama | — | `car/list.vue` (özel component yok, generic `DatatableAjax`) | `GET /car` | `GET /api/v1/car` | `search(plate),per_page`→`{data,message}` | `cars` | yok (kaynak) | CarType,RomorkType,CarOwner,CarStatusType | H | Birebir |
| Detay/Form | — | `car/form.vue` | `GET /car/{id}` | `GET /api/v1/car/{id}` | `{data,message}` | aynı | yok | — | H | Birebir |
| Oluşturma | — | aynı | `POST /car` `save` | `POST /api/v1/car` `Save` (JSON body, form değil — istisna) | `{data,message}` | `cars`+Siber `skn_arac` | yok | — | H | Birebir; **`save()`'de FK'lar guard'sız çözülüyordu (olsold) → olsnew'de `optional()` benzeri güvenli çözüm kullanılmalı, doğrulanacak** |
| Güncelleme | — | aynı | `PUT /car` `update` | `PUT /api/v1/car` + `POST /api/v1/car/update` (iki yol da) | `{message}` | aynı | yok | — | H | Birebir |
| Silme | — | — | `DELETE /car` — **`car_management` slug'ı seed'de YOK, kod bunu bildiği yorumla itiraf ediyor** | `DELETE /api/v1/car` — **olsnew'de de aynı slug, aynı şekilde tanımsız (self-documented gap)** | `{message}` | — | `car_management` (fiilen herkese açık) | — | HD | **Bariz güvenlik boşluğu — HEDEF'te gerçek bir `car_management` sayfası seed edilip yetki fiilen uygulanacak** (olsold/olsnew'in "izin ver" varsayılanı KOPYALANMAYACAK) |
| CarType/CarOwner/CarStatus/RomorkType CRUD | — | `FeatureModals/` içindeki hızlı-ekle modalleri (Offer/Load/Expedition/Car formlarına gömülü, **ayrı admin ekranı değil**) | 4 ayrı controller — **kaynakta save/update/delete TAMAMEN KIRIK** (yanlış donör sınıf import edilmemiş → fatal) | `LookupControllerBase<T>` generic altyapı üzerinden — olsnew'de ÇALIŞIYOR | `{data,message}` | `car_types`,`car_owners`,`car_status_types`,`romork_types` | `case_type_management`(paylaşımlı, kaynaktan kasıtlı korunmuş) | — | HD | olsnew'in çalışan generic altyapısı taşınacak; **olsold'un "sadece okuma çalışır" kırıklığı KOPYALANMAYACAK**. Paylaşımlı yetki slug'ı davranış parites için korunuyor, teslim raporunda not düşülecek |

---

## 7. Kullanıcılar (User + Auth + Permission/Role)

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Bağımlılık | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|---|
| Giriş | `login.vue` | `pages/guest/login.vue`→`GuestLogin.vue`(79) | `POST /api/v1/login` (Passport OAuth2) | `POST /api/v1/login` `AuthController.Login`(JWT+jti) | `{token,user}` → cookie | `users` | AllowAnonymous | — | H | Birebir + **HEDEF'te login'e rate limit eklenecek (olsnew'de yok, SEC-009)** |
| Oturum doğrulama | router guard | `authUser` guard | `GET /api/v1/auth` | `GET /api/v1/auth` `CheckAuth` | `{data,authenticated}` (standart zarf DEĞİL) | — | Authorize | — | H | Birebir |
| Çıkış | — | Header hesap menüsü | — (Passport token silme) | `POST /api/v1/logout` (jti iptali) | `{status,message}` | `revoked_tokens` | Authorize | — | H | Birebir |
| Liste+arama | — | `user/list.vue`→`UserTable.vue`(84) | `GET /user` | `GET /api/v1/user` | `search,working_tracking,per_page`→`{data,message}` | `users` | `user_management`(read, buggy olsold'da→olsnew'de düzeltilmiş) | — | H | Birebir |
| Oluşturma | — | `UserFormDrawer.vue`(301) | `POST /user` `save` — **her yeni kullanıcıya TÜM sayfalarda 0 yetki satırı açılıyor** | `POST /api/v1/user` `Save` | FormData → `{data,message}` | `users`+34 satır `user_permissions` | create | — | H | Birebir (bootstrap-sıfır-yetki davranışı korunuyor — güvenlik açısından doğru varsayılan) |
| Güncelleme | — | aynı | `POST /user/update` — parola boşsa korunuyor | `POST /api/v1/user/update` | `{data,message}` | `users` | update | — | H | Birebir |
| Silme (soft delete) | — | — | `DELETE /user` | `DELETE /api/v1/user` | `{message}` | `users.deleted_at`+avatar dosya silme | delete | — | H | Birebir; tüm okuma sorgularında `DeletedAt==null` kontrolü şart |
| Yetki atama (sayfa×CRUD) | — | `UserRole.vue`(139, drawer içi tab) | `GET/PUT /role` | `GET/PUT /api/v1/role` `RoleController` | `{id,stats:{...}}` / `{result:'success'}` (**kaynakta yanlış giden çağrıda da her zaman 'success' dönüyor — BUG**) | `user_permission_pages`,`user_permissions` | own-record veya `role_management` | — | HD | olsnew'de kendi-kaydı-okuma güvenli biçimde ayrılmış; **kaynağın "her zaman success" hatası olsnew'de düzeltilmiş olmalı, doğrulanacak — düzeltilmemişse HEDEF'te düzeltilecek** |
| Kendi profilim | — | `pages/account.vue`→`AccountDetail.vue`(25)+3 alt sekme | `/profile[...]` (6 uç) | `ProfileController` (6 uç) | FormData→`{data,message}` veya `{message}` | `users` | Authorize (kimlik her zaman token'dan) | — | H | Birebir |
| Parola değiştirme (BR-012) | — | `AccountSecurityFormModal.vue` | `POST /profile/password/update` | `POST /api/v1/profile/password/update` | mevcut parola doğrulanır | `users.password` | Authorize | — | H | Birebir; BR-012 test edilecek |
| Kullanıcı hedefi (Hedef sekmesi) | — | `UserTarget.vue`(223, drawer içi tab) | `/user_goal[...]` | `GoalController`/`UserGoalController` **[KAPSAM DIŞI modül ama bu sekme Kullanıcılar formunun bir parçası]** | değişken | `user_goals` | yok | — | HD | **İstisnai kapsam-içi bağımlılık**: "hedef takibi" genel olarak kapsam dışı ama bu sekme UserFormDrawer'ın görsel/işlevsel parçası olduğu için taşınacak (aksi halde form eksik görünür) |

---

## 8. Destek Talebi (`WebsiteContactForm`)

Ayrı bir destek/ticket modülü **yoktur** — olsold'da `destek/support/ticket` için tam repo taraması yapıldı,
tek eşleşme bu modülün Türkçe arayüz metinleridir (bkz. envanter raporu §8.3). Karar kesinleşti: Destek
Talebi = Website Contact Form.

| Ekran/işlem | Eski FE | Yeni FE | Laravel | .NET | İstek/Response | Tablolar | Yetki | Durum | Karar |
|---|---|---|---|---|---|---|---|---|---|
| Dış form gönderimi (public) | (harici site) | (harici site, uygulama dışı) | `POST /api/website/contact/form` — auth YOK (kasıtlı) | `POST /api/website/contact/form` `[AllowAnonymous]` | `{success,message,data}` 201 | `website_contact_forms` | yok (kasıtlı) | H | Birebir + **rate limit eklenecek (spam koruması)** |
| Liste | — | `pages/website/contact/forms.vue`→`FormsTable.vue`(96) | `GET /api/website/contact/form` — **KAYNAKTA TAMAMEN ANONİM (SEC-003)** | `GET /api/website/contact/form` — **olsnew'de sadece `[Authorize]`, CRUD yetkisi YOK** | `{success,data:sayfalı}` | aynı | HEDEF'te eklenecek | HD | **Kaynağın anonim-okuma açığı KOPYALANMAYACAK.** Zaten olsnew Authorize şartı koymuş (iyi) ama gerçek bir `support_request_management` sayfa yetkisi + CRUD bayrakları HEDEF'te eklenecek |
| Detay (okundu işaretleme yan etkisi) | — | `FormDetailDrawer.vue`(152) | `GET /api/website/contact/form/{id}` — anonim, GET yan etkili (`is_read=true`) | aynı yol, `[Authorize]` | `{success,data}`/404 | aynı | HEDEF'te eklenecek | HD | Yan etki (GET'in okundu işaretlemesi) davranış parites için korunuyor; yetki eklenecek |
| Yanıtlandı işaretleme | — | aynı drawer | `PATCH .../answered` — anonim | aynı, `[Authorize]` | `{success,message,data}` | aynı | HEDEF'te eklenecek | HD | Yetki eklenecek |
| **Tehlikeli ölü kod** | — | `FormDetailDrawer.vue` içinde kullanılmayan `handleDelete()` → `DELETE_USER(...)` çağırıyor (buton yok, UNREACHABLE) | — | — | — | — | — | — | **HEDEF'e taşınmayacak** — kopyala-yapıştır kalıntısı, ileride birisi "silme butonu ekleyeyim" derse yanlışlıkla kullanıcı silebilir |

PII (ad/soyad/e-posta/telefon/mesaj) loglanmayacak; boyut sınırı ve backend validasyonu (§7 genel kurallar) uygulanacak.

---

## Ortak/paylaşılan bağımlılıklar (8 modülün hepsini etkiler)

| Bileşen | Kaynak (olsnew) | Karar |
|---|---|---|
| JWT auth + jti iptali | `Services/Authentication/*`, `RevokedToken` | Birebir taşı |
| Yetkilendirme | `RequiresPermissionAttribute`+`PermissionService` | Birebir taşı; yukarıdaki HD satırlarındaki boşluklar kapatılacak |
| Yanıt zarfı | `ApiResponse.cs` (5 fabrika metodu) + modül-özel ad-hoc zarflar | Birebir taşı — **zarfı standartlaştırma, mevcut FE beklentisini kırar** |
| Hata orta katmanı | `ExceptionHandlingMiddleware.cs` | Birebir taşı |
| JSON serileştirme (snake_case, boş koleksiyon gizleme, `User` alan gizleme) | `EloquentJsonModifiers.cs` | Birebir taşı |
| Statik dosya sunumu (`/storage`) | `Program.cs` UseStaticFiles bloğu | Birebir taşı |
| IClock / TurkishDecimal | `Common/IClock.cs`, `Common/TurkishDecimal.cs` | Birebir taşı |
| CORS/Swagger | `Program.cs` | Birebir taşı |
| Health check | — | **EKSİK, HEDEF'te eklenecek** (API+PostgreSQL) |
| Correlation ID | — | **EKSİK, HEDEF'te eklenecek** |
| Rate limiting | — | **EKSİK, HEDEF'te eklenecek** (login + destek formu) |
| Generic lookup altyapısı | `LookupService.cs`+`LookupControllerBase.cs`+`LookupControllers.cs` | Birebir taşı, 27 modülden 8 modülün gerçekten kullandığı ~23'e trim edilecek (yalnız `EinvoicePrefix` kesin kapsam dışı) |
| FeatureModals (16 hızlı-ekle modal) | `components/FeatureModals/*` (1878 satır) | **AYRI admin ekranı DEĞİL** — Offer/Load/Expedition/Car formlarına gömülü paylaşımlı altyapı, birebir taşınacak |

Kapsam dışı ama 8 modülden birine veri sağladığı için READ-ONLY taşınan tablolar: `work_types, loading_types,
payment_types, instructions, departments, product_types, case_types, transport_types, currencies, item_types,
financial_items, movement_types, countries, cities, districts, tax_offices, destinations, load_status_types,
load_transfer_types, load_transfer_delivery_methods, expedition_statuses, expedition_types, car_types,
car_owners, car_status_types, romork_types, account_types, invoice_types, invoice_statuses`.
