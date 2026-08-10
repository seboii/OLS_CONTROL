# Yetki Matrisi

Kaynak: `src/OLS.API/Controllers/Front/*.cs` içindeki `[RequiresPermission]` özniteliklerinin ve
`LookupControllerBase.HasPermissionAsync` çağrılarının dosya+satır düzeyinde taranmasıyla çıkarıldı
(bu belgeyi yazarken tekrar grep edilip doğrulandı — tahmine dayanmıyor).

## Model

Rol tabanlı DEĞİL — **sayfa-slug × CRUD bayrağı** modeli (olsold'un `RoleHelper`'ının birebir karşılığı):

- `user_permission_pages`: sabit bir sayfa/modül listesi (`permission_page_slug`, `permission_page_name`).
- `user_permissions`: her (kullanıcı, sayfa) çifti için `read`/`create`/`update`/`delete` (0/1, `smallint`).
- Yeni bir kullanıcı oluşturulduğunda (`UserService.CreateAsync`) her sayfa için SIFIR yetkili bir satır
  otomatik açılır — bu yüzden yetki matrisi ekranı her zaman TÜM sayfaları listeler, admin sonradan
  tek tek işaretler.
- **Bilinçli davranış (olsold'dan miras):** `PermissionService.HasPermissionAsync`, slug
  `user_permission_pages`'te TANIMLI DEĞİLSE isteği SERBEST bırakır (`return true`). Sayfa TANIMLIYSA
  ve eşleşen bir `user_permissions` satırı yoksa/bayrak 0'sa **403**.
- **Bilinçli davranış farkı (olsold'dan DEĞİL):** kaynakta yetki kontrolü ya tamamen yorum satırındaydı
  ya da süslü parantezsiz `if` yüzünden fiilen hiçbir şeyi engellemiyordu. Burada yetkisiz istek
  GERÇEKTEN 403 ile reddedilir (`RequiresPermissionAttribute.cs`, `LookupControllerBase.cs` üstündeki notlar).
- **Kimlik doğrulama sırası:** jetonsuz istek → 401. Jetonlu ama yetkisiz istek → 403.

## Seçili 8 modülün yetki slug'ları

| Modül (frontend) | Slug | Sayfa adı | CRUD uygulanan uçlar |
|---|---|---|---|
| Müşteri | `account_management` | Cari Yönetimi | `AccountController`: GET/GET-tekil (read), POST (create), POST update (update), DELETE (delete) |
| Teklif + Yük | `load_management` | Yük/Teklif Yönetimi | `LoadController` (Teklif) VE `LoadTransferController`, `LoadTransferMovementController`, `LoadTransferInvoiceItemController` (Yük) — **AYNI slug'ı paylaşır**; Teklif ile Yük arasında ayrı yetki verilemez |
| Sefer | `expedition_management` | Sefer Yönetimi | `ExpeditionController` (`LoadTransferController.cs` içinde), `ExpeditionLoadMappingController` |
| Fatura | `invoice_management` | Fatura Yönetimi | `InvoiceController`, `InvoiceFooterController` |
| Araç | `car_management` | Araç Yönetimi | `CarController` |
| Kullanıcılar | `user_management` | Kullanıcı Yönetimi | `UserController` |
| Kullanıcılar → Yetkiler sekmesi | `role_management` | Rol/Yetki Yönetimi | `RoleController`: **kendi** yetkini okumak HER ZAMAN serbest (slug kontrolü atlanır); **başkasının** yetkisini okumak `role_management`/read ister; yetki GÜNCELLEMEK (kendi dahil) her zaman `role_management`/update ister |
| Destek Talebi | `support_request_management` | Destek Talebi Yönetimi | `ContactFormController` (liste/detay/durum güncelleme — anonim form gönderimi hariç, o `[AllowAnonymous]`) |

## Kapsam dışı ama seed edilen sayfalar (read-only bağımlılık olarak)

Bu 8 modülün formlarındaki açılır listeler (dropdown) için gerekli, ama kendi YÖNETİM ekranları
("Ayarlar" altındaki tanım/lookup sayfaları) bu projenin kapsamı dışında bırakıldı:

`account_type_management`, `invoice_type_management`, `case_type_management`, `payment_management`,
`transport_type_management`, `loading_type_management`, `work_type_management`,
`status_type_management`, `department_management`, `product_type_management`,
`financial_item_management`, `financial_item_type_management`, `movement_type_management`,
`currency_management`

Bu slug'lar `LookupControllerBase` alt sınıflarını (`WorkTypeController`, `StatusTypeController`, vb.)
korur; bu controller'lar API'de VAR (frontend'in dropdown'ları `GET` ile bunlara istek atıyor) ama
kendilerine özel bir yönetim EKRANI React tarafında yok (bilinçli kapsam kararı, bkz.
SECILI-MODUL-PARITE-MATRISI.md). Admin kullanıcı bu sayfalarda da tam yetkiyle seed edilir.

## Özel/nesne-seviyesi kurallar

### `super_admin` — Cari görünürlüğü

`AccountService.IsSuperAdminAsync`, `"super_admin"` slug'lı ve `Read=1` olan bir satır arar:

- **Var ve Read=1** → kullanıcı TÜM carileri görür/düzenler (yetkisi olduğu CRUD'lar dahilinde).
- **Yok/Read=0** → kullanıcı yalnızca `user_account_mappings` ile kendisine AÇIKÇA atanmış carileri
  görür — `account_management` okuma yetkisi olsa bile, eşleme yoksa **0 kayıt** döner (bkz.
  `AccountVisibilityTests.RegularUser_WithReadPermissionButNoAccountMapping_SeesNoAccounts`).
- Bu sayfa seed edilmezse HİÇBİR kullanıcı (admin dahil) süper admin olamaz — bu oturumda bulunup
  düzeltilen gerçek hata, bkz. TEST-RAPORU.md §1.2.
- `GET /api/v1/account/{id}` tekil görüntüleme AYRICA `IsVisibleToUserAsync` ile korunur: süper admin
  değilseniz VE cari size eşli değilse, `account_management`/read yetkiniz olsa bile **403**.

### `role_management` — kendi kendine erişim istisnası

- `GET /api/v1/role?id={kendi_id}` → HER ZAMAN izinli (yetki matrisini herkes kendi için görebilmeli).
- `GET /api/v1/role?id={başka_id}` → `role_management`/read gerekir, yoksa 403.
- `PUT /api/v1/role` (herhangi bir kullanıcının yetkisini değiştirmek, KENDİSİ dahil) → HER ZAMAN
  `role_management`/update gerekir. (Kaynakta bu kontrol süslü parantezsiz `if` yüzünden çoğu yerde
  etkisizdi ve yetkisiz istek sessizce `{"result":"success"}` dönüyordu; burada gerçekten 403.)

### Bilinmeyen slug

`car_management` gibi bazı slug'lar için kaynak kodda "bu slug seed edilmemişse serbest bırakılır"
notu var — HEDEF'te TÜM 8 modülün slug'ı seed edildiği için bu artık teorik bir durum, ama davranış
`PermissionEnforcementTests.UnknownPermissionSlug_IsOpenByDefault_MatchingLegacyRoleHelperBehavior`
ile ayrıca kilitlendi (gelecekte bir slug yanlışlıkla seed listesinden silinirse, o modül SESSİZCE
serbest kalır — 403 değil).

## Doğrulama

Bu matristeki tüm iddialar `dotnet test tests/OLS.API.IntegrationTests` ile gerçek Postgres'e karşı
çalıştırılan testlerle doğrulandı (bkz. TEST-RAPORU.md §2.1): 401/403 sınırları, `super_admin` kuralı,
`role_management` kendi-kendine-erişim istisnası, bilinmeyen slug'ın serbest davranışı — hepsi otomatik
testle kilitli, elle kontrol edilip belgeye geçirilmedi.
