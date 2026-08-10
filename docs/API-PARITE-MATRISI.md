# API Parite Matrisi — olsold'daki gerçek endpoint'lerin tek tek işaretlenmesi

Kapsam: yalnızca 8 seçili modül + bunların zorunlu lookup/entegrasyon bağımlılıkları. Kaynak: `olsold/routes/api.php`
+ ilgili controller'lar (bağımsız envanter ajanı tarafından dosya+satır düzeyinde doğrulandı).

Durum kodları:
- **PORT** = olsnew'de birebir karşılığı var, HEDEF'e taşınacak
- **PORT-DUZELT** = olsnew'de karşılığı var ama bir parite/güvenlik boşluğu HEDEF'te kapatılacak
- **YENI** = olsnew'de yok, HEDEF'te sıfırdan eklenecek (küçük, bilinçli kapsam kararı)
- **CALISMIYOR-LEGACY** = olsold'da route TANIMLI ama controller metodu yok/import hatası var → olsold'da da çağrılırsa 500/fatal. Körlemesine portlanmadı.
- **KOPYALANMAYACAK-BUG** = olsold'da çalışıyor ama bariz hata/güvenlik açığı — bilinçli olarak taşınmıyor
- **KAPSAM-DISI** = 8 modülün dışında, bilinçli olarak dahil edilmedi

---

## Müşteri (Account)

| Method+Path (olsold) | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/v1/account` | `FrontAccountController::save` | PORT-DUZELT | Kaynakta yetki kontrolü yok; olsnew `account_management`(create) ekliyor — korunacak |
| `POST /api/v1/account/update` | `::update` | PORT-DUZELT | Aynı gerekçe, (update) |
| `DELETE /api/v1/account` | `::delete` | PORT | |
| `GET /api/v1/account` | `::all` | PORT | Süper-admin hard-code kontrolü (`UserPermissionPage::where('slug','super_admin')->first()->id` — `first()` null dönerse fatal) yerine güvenli null-check ile taşınacak |
| `GET /api/v1/account/{id}` | `::single` | PORT | Aynı güvenli null-check notu |
| `FrontUserAccountMappingController` rotaları | — | CALISMIYOR-LEGACY | `routes/api.php:255-262`'de tamamen yorum satırında, hiç kayıtlı değil — portlanmadı |

## Teklif (Load)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/v1/load` | `LoadController::save` | PORT | Alt kayıtlar tek yazılır (olsold'un çift-yazma hatası düzeltildi) |
| `POST /api/v1/load/saveAi` | `::saveAi` | PORT-DUZELT | account_type_id hatası + ürün-satırı-boş-okuma hatası olsnew'de düzeltilmiş, korunacak |
| `POST /api/v1/load/{id?}` | `::update` | PORT | Route sırası (`saveAi` → `update`'ten önce) attribute routing'te açıkça korunacak |
| `DELETE /api/v1/load` | `::delete` | PORT | |
| `GET /api/v1/load` | `::all` | PORT | |
| `GET /api/v1/load/{id}` | `::single` | PORT | |
| `POST /api/v1/load/updateTimeOut` | `::updateTimeOut` | PORT | Kaynakta yalnız transaction guard'lı (yetkisiz de çalışıyordu) — HEDEF'te düzgün yetki kontrolü |
| `POST /api/v1/load/file/upload` | `::fileUpload` | PORT | BR-013 (liste=son hal) testli |
| `DELETE /api/v1/load/load_content` | `::delete_load_contents` | PORT | |
| `DELETE /api/v1/load/load_financial_item` | `::delete_load_financial_items` | PORT | |
| `POST /api/v1/offer_send_email` | `OfferEmailController::save` | PORT | Mail:Enabled=false → `{sent:false}`, sahte başarı yok |
| `POST /api/v1/transfer_to_siber` | `TransferSiberController::save` | PORT | |
| `POST /api/v1/transfer_to_siber/loadSave` | `::loadSave` | PORT-DUZELT | Kaynak: 17 karşılaştırma hep aynı mesaj + SQL string-concat (enjeksiyon riski). olsnew: alan adı mesajda + Dapper parametreleri — korunacak |

## Yük (LoadTransfer)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/v1/load_transfer` | `LoadTransferController::save` | PORT | BR-002/003/004/005 |
| `POST /api/v1/load_transfer/{id?}` | `::update` | PORT | snake_case zorunlu |
| `DELETE /api/v1/load_transfer` | `::delete` | KOPYALANMAYACAK-BUG→PORT-DUZELT | Kaynak: yanlış `PaymentType` sınıfı referansı → fatal. olsnew'de çalışır durumda, doğru yetkiyle taşınacak |
| `GET /api/v1/load_transfer` | `::all` | PORT | |
| `GET /api/v1/load_transfer/{id}` | `::single` | PORT | |
| `DELETE /api/v1/load_transfer/load_transfer_package` | `::delete_load_transfer_package` | PORT-DUZELT | Kaynakta yetki kontrolü tamamen yorumda — HEDEF'te gerçek yetki eklenecek |
| `DELETE /api/v1/load_transfer/load_transfer_invoice_item` | `::delete_load_transfer_invoice_item` | PORT-DUZELT | Aynı gerekçe |
| `GET/POST/PUT/DELETE /api/v1/load_transfer_movement[...]` | `LoadTransferMovementController` | PORT-DUZELT | Kaynakta VE olsnew'de sıfır yetki kontrolü (yalnız [Authorize]) — HEDEF'te CRUD yetkisi eklenecek |
| `GET/POST/PUT/DELETE /api/v1/load_transfer_invoice_item[...]` | `LoadTransferInvoiceItemController` | PORT-DUZELT | Kaynak: save/update olmayan kolonlara yazıyor (`name`,`siber_id`) → fatal. olsnew doğru kolonlara yazıyor (korunacak); yalnız DELETE korumalı — GET/POST/PUT'a da yetki eklenecek |
| `GET /api/v1/transfer_data/pullLoad` | `TransferDataController::pullLoad` | KAPSAM-DISI (bilinçli) | Toplu Siber geçmiş-veri ETL'i; HEDEF sıfır geçmiş veriyle başlıyor |

## Sefer (Expedition + ExpeditionLoadMapping)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/v1/expedition` | `ExpeditionController::save` | PORT | BR-006 (araç/romork uyumu) |
| `PUT /api/v1/expedition` | `::update` | PORT | `expedition_status_id==8` özel alan seti |
| `DELETE /api/v1/expedition` | `::delete` | KOPYALANMAYACAK-BUG→PORT-DUZELT | Kaynak: `case_type_management` yetki sızıntısı (Kap Tipi yetkisi Sefer siler). HEDEF: özel `expedition_management`(delete) |
| `GET /api/v1/expedition` | `::all` | PORT | |
| `GET /api/v1/expedition/{id}` | `::single` | PORT | |
| `GET /api/v1/expedition/{id}/movements` | `::expeditionMovementAll` | PORT | |
| `POST /api/v1/expedition/{id}/movements` | `::expeditionMovementSave` | PORT-DUZELT | Kaynak: `{id}` yoksayılıp body'den okunuyor (BUG) + null-check yok (bağlı Load/LoadTransfer bulunamazsa uncaught Error) — HEDEF'te düzeltilecek: route id kullanılacak, null-check eklenecek |
| `DELETE /api/v1/expedition/{id}/movements/{movement_id}` | `::expeditionMovementDelete` | PORT-DUZELT | Kaynak: her zaman `status:true` döner (bulunamasa bile) — HEDEF'te gerçek sonuç dönecek |
| `POST /api/v1/expedition_load_mapping` | `ExpeditionLoadMappingController::save` | PORT | BR-007 (mükerrer bağlama engeli) |
| `POST /api/v1/expedition_load_mapping/update` | `::update` | PORT | Alan adı yazım hatası (`expdition_load_mapping_id`, eksik "e") — **kasıtlı olarak birebir korunuyor**, tel kontratı budur |
| `DELETE /api/v1/expedition_load_mapping` | `::delete` | PORT-DUZELT | Kaynakta yetki kontrolü yorum satırında — HEDEF'te gerçek yetki |
| `GET /api/v1/expedition_load_mapping` | `::all` | PORT | Aslında `LoadTransfer` (henüz eşlenmemiş) döner — davranış korunuyor |
| `GET /api/v1/expedition_load_mapping/{id}` | `::single` | PORT | `{id}` aslında expedition_id — davranış korunuyor |

## Fatura (Invoice)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `GET /api/v1/invoice` | `InvoiceController::get` | PORT | Varsayılan `invoice_status_id!=7` gizleme korunuyor |
| `GET /api/v1/invoice/{id}` | `::single` | PORT | |
| `POST /api/v1/invoice` | `::createManualInboxInvoice` | PORT | |
| `POST /api/v1/invoice/update` | `::updateInboxInvoice` | PORT | Kalem eşlemeleri sil-ve-yeniden-kur, çiftlenme yok |
| `DELETE /api/v1/invoice/delete` | `::deleteInvoice` | PORT | |
| `POST /api/v1/invoice/accept-or-reject` | `::invoiceAcceptOrReject` | KOPYALANMAYACAK-BUG→YENI | Kaynak: `"declined"` küçük harf karşılaştırması hiç eşleşmiyor → her zaman Approved. **Bu hata KOPYALANMAYACAK**; Uyumsoft'a bağlı olduğu için olsnew'de zaten yok — HEDEF'te doğru karşılaştırmayla + "entegrasyon yapılandırılmadı" 503 ile eklenecek |
| `GET /api/v1/invoice/pdf-view/inbox` | `::inboxInvoicePdfView` | YENI (mock) | Uyumsoft yok — 503 "yapılandırılmadı" |
| `GET /api/v1/invoice/pdf-view/outbox` | `::outboxInvoicePdfView` | YENI (mock) | Aynı |
| `POST /api/v1/invoice/draft/cancel` | `::cancelDraftInvoice` | YENI (mock) | Aynı |
| `POST /api/v1/invoice/draft/approve` | `::approveDraftInvoice` | YENI (mock) | Aynı |
| `POST /api/v1/invoice/draft/send` | `::sendDraftInvoice` | KOPYALANMAYACAK-BUG→YENI (mock) | Kaynak: `if($vknTcknControl['status'] = 200)` atama-karşılaştırma hatası (her zaman true). Uyumsoft yok — 503 "yapılandırılmadı" |
| `GET/POST/PUT/DELETE /api/v1/invoice/footer[...]` | `InvoiceFooterController` | PORT-DUZELT | Kaynakta kozmetik hata: hata mesajları "TransitDeclaration" diyor (copy-paste) — HEDEF'te doğru metin |
| `POST/PUT/DELETE /api/v1/invoice_status[...]` (create/update/delete) | `InvoiceStatusController` | CALISMIYOR-LEGACY | Kaynakta olmayan kolonlara yazıyor + `QueryException`/`RoleHelper` import edilmemiş → çift fatal. Yalnız `all`/`single` güvenilir; olsnew'in generic LookupControllerBase'i ile doğru CRUD sağlanacak |
| `POST/PUT/DELETE /api/v1/invoice_type[...]` (create/update/delete) | `InvoiceTypeController` | CALISMIYOR-LEGACY | Aynı `name`/`siber_id` hatası — LookupControllerBase ile düzeltilecek |

## Araç (Car + CarType + RomorkType + CarOwner + CarStatusType)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/v1/car` | `CarController::save` | PORT-DUZELT | Kaynak: FK'lar guard'sız çözülüyor (dangling FK → uncaught Error) — HEDEF'te güvenli çözüm |
| `PUT /api/v1/car` | `::update` | PORT | Zaten guard'lı (`optional()`) |
| `DELETE /api/v1/car` | `::delete` | KOPYALANMAYACAK-BUG→PORT-DUZELT | `car_management` slug'ı seed'de yok (kod bunu itiraf ediyor) → fiilen herkese açık. **HEDEF'te gerçek sayfa+yetki seed edilecek** |
| `GET /api/v1/car` | `::all` | PORT | |
| `GET /api/v1/car/{id}` | `::single` | PORT | |
| `POST/PUT/DELETE /api/v1/car_type[...]` | `CarTypeController` | CALISMIYOR-LEGACY→PORT | save/update/delete fatal (yanlış `CaseType` importu). olsnew'in generic `LookupControllerBase` altyapısı ile çalışır hale getirilmiş — taşınacak |
| `POST/PUT/DELETE /api/v1/car_owner[...]` | `CarOwnerController` | CALISMIYOR-LEGACY→PORT | Aynı bug deseni — düzeltilmiş haliyle taşınacak |
| `POST/PUT/DELETE /api/v1/car_status[...]` | `CarStatusController` | CALISMIYOR-LEGACY→PORT | Aynı |
| `POST/PUT/DELETE /api/v1/romork_type[...]` | `RomorkTypeController` | CALISMIYOR-LEGACY→PORT | Donör `TransportType`; aynı bug deseni, düzeltilmiş haliyle taşınacak |

## Kullanıcılar (User + Auth + Role/Permission)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/v1/login` | `FrontLoginController::login` | PORT-DUZELT | Passport→JWT; **HEDEF'te rate limit eklenecek** |
| `GET /api/v1/auth` | `::authUser`(varsayım) | PORT | |
| `POST /api/v1/register` | — | CALISMIYOR-LEGACY | `FrontLoginController::register` metodu yok, rota çağrılırsa olsold'da da 500 — portlanmadı |
| `POST /api/v1/user` | `FrontUserController::save` | PORT | Yeni kullanıcıya 34 sayfada sıfır yetki bootstrap (güvenli varsayılan, korunuyor) |
| `POST /api/v1/user/update` | `::update` | PORT | Parola boşsa korunuyor |
| `DELETE /api/v1/user` | `::delete` | PORT | Soft delete |
| `GET /api/v1/user` | `::all` | PORT | |
| `GET /api/v1/user/{id}` | `::single` | PORT | |
| `GET /api/v1/user/list` | — | CALISMIYOR-LEGACY | `FrontUserController::list` metodu yok VE rota `/user/{id}`'den sonra tanımlı olsa bile Laravel önce `single('list')`'e düşürüyor → olsold'da da işlevsiz. Portlanmadı |
| `GET /api/v1/role` | `UserPermissionController::single` | PORT-DUZELT | Kaynak: yetkisiz istekte tanımsız değişkenlerle 200 dönüyor (buggy scoping) — HEDEF'te düzgün 403 |
| `PUT /api/v1/role` | `::update` | PORT-DUZELT | Kaynak: `permission_page_id` aslında `user_permissions.id`'yi bekliyor (yanıltıcı ad, **tel kontratı için korunacak**); yetkisiz de her zaman `{result:'success'}` dönüyor — HEDEF'te gerçek sonuç; `__lang()` tanımsız fonksiyon çağrısı (fatal risk) düzeltilecek |
| `POST /api/v1/permission` | `PermissionController::save` | PORT-DUZELT | Kaynakta yetki kontrolü yok, yeni sayfa herkese tam yetki veriyor (geliştirici aracı) — HEDEF'te en azından `role_management`(create) yetkisi eklenecek |
| `GET/POST/POST/POST/POST/DELETE /api/v1/profile[...]` | `ProfileController` | PORT | Kimlik her zaman token'dan; BR-012 (mevcut parola kontrolü) testli |

## Destek Talebi (WebsiteContactForm)

| Method+Path | Controller::method | Durum | Not |
|---|---|---|---|
| `POST /api/website/contact/form` | `ContactFormController::store` | PORT | Anonim, kasıtlı |
| `GET /api/website/contact/form` | `::index` | KOPYALANMAYACAK-BUG→PORT-DUZELT | **SEC-003: kaynakta tamamen anonim** (herkes tüm PII'yi okuyabiliyor). olsnew `[Authorize]` eklemiş (iyi) ama CRUD yetki sayfası yok — HEDEF'te gerçek `support_request_management`(read) eklenecek |
| `GET /api/website/contact/form/{id}` | `::show` | KOPYALANMAYACAK-BUG→PORT-DUZELT | Aynı; GET'in `is_read` yan etkisi korunuyor, yetki eklenecek |
| `PATCH /api/website/contact/form/{id}/answered` | `::updateAnsweredStatus` | KOPYALANMAYACAK-BUG→PORT-DUZELT | Aynı; `support_request_management`(update) eklenecek |

---

## Genel (8 modülü etkileyen, kaynakta bulunan ama modül-spesifik olmayan)

| Öğe | Durum | Not |
|---|---|---|
| `GET /cache/clear`, `/panel/cache/clear` | KAPSAM-DISI/KOPYALANMAYACAK-BUG | Kimlik doğrulamasız debug uçları (SEC-004) — HEDEF'e hiç taşınmıyor |
| `GET /test`, `/yucel-test`, `/yucel-test-2`, `/panel/dev`, `/panel/test` | KAPSAM-DISI | Aynı gerekçe |
| `config('cors.allowed_origins') = '*'` | KOPYALANMAYACAK-BUG | olsnew zaten `Cors:AllowedOrigins` beyaz listesiyle düzeltmiş — korunacak |
| `$guarded = []` (tüm modellerde mass-assignment açık) | KOPYALANMAYACAK-BUG | HEDEF'te DTO'lar açıkça alan bazlı tanımlanacak, mass-assignment yok zaten (EF Core + DTO deseni) |
