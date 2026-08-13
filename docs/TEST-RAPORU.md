# Test Raporu

Bu belge, bu oturumda **gerçekten çalıştırılan** komutları ve **gerçek** sonuçlarını belgeler.
Hiçbir sonuç varsayılmadı; her iddia altındaki komut çalıştırılıp çıktısı buraya yazıldı.

## Özet

| Proje | Test sayısı | Sonuç | Komut |
|---|---|---|---|
| `OLS.Business.Tests` | 29 | ✅ 29/29 geçti | `dotnet test tests/OLS.Business.Tests` |
| `OLS.API.IntegrationTests` | 38 | ✅ 38/38 geçti | `dotnet test tests/OLS.API.IntegrationTests` |
| `OLS.DataAccess.Tests` | 0 | ⚪ test yok (bilinçli, bkz. "Neden DataAccess.Tests boş") | — |
| **Toplam** | **67** | **✅ 67/67** | `dotnet test` (çözüm kökünde) |

Ayrıca: `dotnet build` (tüm çözüm) — 0 hata, 2 pre-existing nullability uyarısı (bu oturumda dokunulmayan
`TransferSiberService.cs`/`ExpeditionLoadMappingService.cs` dosyalarında, davranışı etkilemiyor).
`docker compose build frontend` ve `docker compose up -d --build api` — ikisi de gerçekten çalıştırıldı,
bkz. "Docker doğrulaması" bölümü.

---

## 1. Bu oturumda canlı (Docker + tarayıcı) doğrulanıp düzeltilen hatalar

Bu ikisi otomatik testlerden ÖNCE, gerçek Docker yığınına karşı tarayıcı ile manuel test sırasında
bulundu; her ikisi de artık `OLS.API.IntegrationTests` içinde regresyon testiyle kilitli.

### 1.1 `/api/v1/role` zarf uyuşmazlığı

**Belirti:** Giriş yaptıktan sonra sidebar tamamen boştu, "Yeni Müşteri" gibi hiçbir yetki-korumalı buton
görünmüyordu — admin kullanıcısında bile.

**Kök neden:** `RoleController.cs`, diğer tüm uçlardan FARKLI olarak `{data, message}` zarfı KULLANMIYOR;
bilinçli olarak çıplak `{id, stats: {permission_data, user_name}}` dönüyor (olsold'un `data_store.js`
sözleşmesiyle birebir). Frontend (`lib/auth.tsx`, `pages/users/UsersPage.tsx`) bunu yanlışlıkla
`{data: {...}}` sarmalı sanıp `res.data.stats...` okuyordu → her zaman `undefined`.

**Düzeltme:** `auth.tsx` ve `UsersPage.tsx`'te doğrudan `res.stats.permission_data` okunacak şekilde
düzeltildi. Tarayıcıda doğrulandı: giriş sonrası tüm 8 modül sidebar'da göründü, Kullanıcılar →
Yetkiler sekmesinde 23 sayfalık tam yetki matrisi doğru render edildi (ekran görüntüsüyle teyit edildi).

**Regresyon testi:** `RoleEndpointContractTests.GetRole_ForSelf_ReturnsBareIdStatsEnvelope_NotWrappedInDataMessage`
— kök seviyede `data`/`message` alanı OLMADIĞINI, `id`+`stats` alanlarının VAR olduğunu doğrudan doğrular.

### 1.2 `super_admin` yetki sayfası seed edilmemiş

**Belirti:** Yeni bir cari (`Test Lojistik A.Ş.`) oluşturuldu, `POST` `200` ve gerçek `id` döndü, ama
hemen ardından `GET` listesi `total:0` gösterdi — kayıt "kayboldu".

**Kök neden:** `AccountService.IsSuperAdminAsync`, `"super_admin"` slug'lı ve `Read=1` olan bir
`user_permission_pages` satırı arar; bulamazsa kullanıcı süper admin sayılmaz ve yalnızca
`user_account_mappings` ile açıkça eşlenmiş carileri görür. Bu sayfa seed edilmediği için admin dahil
HİÇBİR kullanıcı süper admin olamıyordu.

**Düzeltme:** `DbSeeder.cs`'e `("super_admin", "Süper Admin")` sayfası eklendi (seed listesinin ilk
elemanı, açıklayıcı yorumla). Docker'da `docker compose up -d --build api` ile yeniden dogrulandı:
sayfa eklenip admin'e (idempotent bootstrap ile) otomatik atandıktan sonra cari listede göründü —
hem `curl` hem tarayıcı ekran görüntüsüyle teyit edildi.

**Regresyon testi:** `AccountVisibilityTests.Admin_IsSuperAdmin_AndSeesAccountsWithoutExplicitMapping`
(pozitif) ve `AccountVisibilityTests.RegularUser_WithReadPermissionButNoAccountMapping_SeesNoAccounts`
(negatif — `account_management` okuma yetkisi olan ama eşlemesi olmayan bir kullanıcının GERÇEKTEN
0 cari gördüğünü doğrular).

---

## 2. Otomatik test paketi

### 2.1 `OLS.API.IntegrationTests` (38 test)

Gerçek ASP.NET Core pipeline'ı üzerinden çalışır — `WebApplicationFactory<Program>` gerçek `Program.cs`'i
(JWT auth, `[RequiresPermission]` filtreleri, CORS, rate limiting, EF Core migrasyonları, `DbSeeder`)
hiçbir katmanı mock'lamadan ayağa kaldırır. Her test sınıfı `[Collection("OlsApi")]` ile TEK bir izole
Postgres veritabanını paylaşır (izolasyon mekanizması ve bunu bulurken çıkan iki gerçek hata için bkz. §3.2–3.3).

| Sınıf | Test | Ne doğrular |
|---|---|---|
| `AuthenticationTests` | `Login_WithSeededAdminCredentials_ReturnsTokenAndUser` | Gerçek giriş, gerçek token+user |
| | `Login_WithWrongPassword_Returns401WithoutLeakingWhichFieldWasWrong` | Kimlik hatası → 401 |
| | `Login_WithMissingEmail_ReturnsFieldValidationError` | Alan doğrulama → 400 + `errors.email` |
| | `ProtectedEndpoint_WithoutBearerToken_Returns401` | Jetonsuz korumalı uç → 401 |
| | `ProtectedEndpoint_WithGarbageToken_Returns401` | Bozuk jeton → 401 |
| | `CheckAuth_WithValidAdminToken_ReturnsAuthenticatedTrue` | `/api/v1/auth` gerçek akış |
| | `Logout_ThenReusingSameToken_Returns401` | Çıkış sonrası jeton gerçekten iptal (jti kara listesi) |
| `RoleEndpointContractTests` | `GetRole_ForSelf_ReturnsBareIdStatsEnvelope_NotWrappedInDataMessage` | §1.1 regresyonu |
| | `GetRole_ForAnotherUser_WithoutRoleManagementPermission_Returns403` | Başkasının yetkisini okumak `role_management`/read ister |
| | `GetRole_ForSelf_IsAlwaysAllowed_EvenWithoutRoleManagementPermission` | Kendi yetkisini herkes okuyabilir (olsold kuralı) |
| `PermissionEnforcementTests` | `CreateCar_AsFreshUserWithoutCarManagementPermission_Returns403` | `[RequiresPermission]` gerçekten engelliyor (legacy'de etkisizdi — bilinçli davranış farkı) |
| | `CreateCar_AfterGrantingCreatePermission_Succeeds` | Yetki verilince gerçek uçtan uca akış |
| | `CreateCar_WithoutPlateNumber_ReturnsValidationError_NotServerError` | Zorunlu alan → 400, 500 değil |
| | `UnknownPermissionSlug_IsOpenByDefault_MatchingLegacyRoleHelperBehavior` | Seed edilmemiş slug → serbest (RoleHelper birebir) |
| `AccountVisibilityTests` | `Admin_IsSuperAdmin_AndSeesAccountsWithoutExplicitMapping` | §1.2 regresyonu (pozitif) |
| | `RegularUser_WithReadPermissionButNoAccountMapping_SeesNoAccounts` | §1.2 regresyonu (negatif) |
| | `RegularUser_WithoutAccountManagementPermission_Returns403OnList` | Yetkisiz erişim engelleniyor |
| `DashboardTests` | `GetDashboard_WithoutToken_Returns401` | Jetonsuz erişim engelleniyor |
| | `GetDashboard_AsAuthenticatedUser_ReturnsRealAggregatesNotFakeData` | Zarf şekli + boş diziler sahte satırla doldurulmuyor |
| | `GetDashboard_ActiveCustomers_MatchesRealAccountCount` | Panel sayısı, gerçek `/api/v1/account` toplamıyla birebir eşleşiyor (uydurma değil) |
| `LoadTests` | `CreateLoad_WithPartiesRouteAndFinancialItems_RoundTripsCorrectly` | Teklif'in TAM alan kapsamı (taraflar+güzergah+çoklu mali kalem) gerçekten kaydedilip geri okunuyor; Türkçe ondalık (`"250,5"`, `"1.250,75"`) doğru ayrıştırılıyor |
| | `CreateLoad_WithoutRequiredFields_ReturnsValidationErrors_NotServerError` | Eksik zorunlu alan → 400 + `errors` sözlüğü, 500 değil |
| | `UpdateLoad_RemovingAFile_DeletesBothDatabaseRowAndPhysicalFile` | Canlıda bulunan gerçek bir hatanın regresyonu: dosya kaldırma DB satırıyla BİRLİKTE fiziksel dosyayı da siliyor (gerçek dosya yazıp `File.Exists` ile doğrulanıyor) |
| `LoadTransferTests` | `UpdateLoadTransfer_WithCoreFieldsAndPackages_RoundTripsCorrectly` | Yük güncelleme uç noktası (çekirdek alanlar + paket ekleme) gerçek Postgres'e karşı doğru çalışıyor |
| | `DeletePackage_RemovesItFromSubsequentRead` | Ayrı paket-silme uç noktası, sonraki okumada kaydın gerçekten gittiğini doğruluyor |
| `ExpeditionLoadMappingTests` | `SaveMapping_WithMatchingRomorkType_LinksLoadAndAppearsInDetail` | Sefere yük bağlama + silme gerçek Postgres'e karşı doğru çalışıyor; `total_expedition_values`'un zarfın KÖKÜNDE döndüğü doğrulanıyor |
| | `SaveMapping_WithMismatchedRomorkType_ReturnsValidationError` | BR-006/007: araç ile yük romork tipi uyuşmazsa bağlama reddediliyor |
| `InvoiceTests` | `CreateInvoice_WithoutExecutionDate_ReturnsValidationError` | Canlıda bulunan gerçek bir hatanın regresyonu: Vade Tarihi boş bırakılırsa 422 |
| | `UpdateInvoice_WithItemMap_LinksItemAndFlipsItsStatus` | Kalem eşleme uç noktası + kaynağın alış/satışa göre kalem durumu değiştirme kuralı; eşlemeler baştan kurulduğu için boş liste gönderilince hepsi silindiği de doğrulanıyor |
| | `Footer_CreateThenDelete_RoundTripsCorrectly` | Dipnot CRUD'u gerçek Postgres'e karşı doğru çalışıyor |
| `TransferSiberTests` | `TransferToSiber_WhenSiberNotConfigured_ReturnsBadRequestWithMessage` | `transfer_to_siber`'ın Siber-yapılandırılmamış hatası HTTP üzerinden gerçek |
| | `LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable` | Daha önce testsiz kalan "Siber-503" davranışı: `loadSave` GERÇEKTEN 503 dönüyor |
| | `LoadSave_WithoutId_ReturnsValidationError_BeforeCheckingSiberConfiguration` | Alan doğrulaması Siber kontrolünden önce çalışıyor |
| | `TransferOfferAsync_WhenLoadAlreadyHasLoadNumber_ReturnsError` | Yük oluşmuş teklif tekrar Siber'e aktarılamaz |
| | `TransferOfferAsync_WithoutPaymentType_ReturnsPaymentTypeRequiredError` | Canlıda bulunan gerçek kısıtın (bkz. TESLIM-RAPORU.md §8) servis seviyesinde regresyonu |
| | `ConvertOfferAsync_WhenLoadAlreadyConverted_ReturnsBR002Error` | BR-002: zaten yüke dönüştürülmüş teklif tekrar dönüştürülemez |
| | `ConvertOfferAsync_WhenStatusNotApproved_ReturnsBR003Error` | BR-003: durum "Olumlu" değilse dönüşüm reddedilir |
| | `ConvertOfferAsync_WhenNotTransferredToSiber_ReturnsBR004Error` | BR-004: önce Siber'e aktarılmamış teklif dönüştürülemez |

**Neden `LoadTransferTests` bir Teklif'i gerçekten Yük'e çevirmiyor:** `LoadTransfer` kayıtları normalde
YALNIZCA Siber'e aktarılmış bir teklifin dönüştürülmesiyle oluşur, bu da gerçek Siber-mock'a bağımlı bir
zincir. Test bunun yerine `OlsDbContext` üzerinden doğrudan minimal bir `LoadTransfer` satırı ekliyor
(şema incelemesiyle doğrulandı: `id` dışında NOT NULL kısıtı yok) ve gerçek `Update`/paket-silme uç
noktalarını buna karşı çalıştırıyor — amaç dönüşümün kendisini değil, güncelleme sözleşmesini kilitlemek.
Dönüşümün kendisi bu oturumda tarayıcıda canlı denendi ve BU ORTAMDA çalışmadığı doğrulandı (bkz.
TESLIM-RAPORU.md §8 "Siber kimlik eşleşmesi kısıtı") — `payment_types.siber_id` hiçbir satırda dolu
değil, olsold'un kendi seeder'ı da bu alanı hiç yazmıyor.

**`ExpeditionLoadMappingTests` neden aynı sorunu yaşamıyor:** `Expedition` ve `LoadTransfer` yine
doğrudan `OlsDbContext` ile seed ediliyor (Sefer oluşturma da benzer şekilde bloke — bkz. TESLIM-RAPORU.md
§8 "Sefer oluşturma/güncelleme de boş lookup tablolarıyla bloke"), ama `ExpeditionLoadMappingService.
SaveAsync` Siber yapılandırmasına BAĞIMLI DEĞİL (yalnızca yapılandırılmışsa Siber'e YAZMAYI DENER,
yapılandırılmamışsa yerel GUID üretip PostgreSQL'e yazmaya devam eder) — bu yüzden test ortamında
(Siber bilinçli olarak kapalı) tam işlevsel olarak çalışıyor. Bu akış ayrıca canlı Docker'da GERÇEK
Siber-mock'a karşı da doğrulandı (bkz. TESLIM-RAPORU.md §8) — hem PostgreSQL hem `skn_yukaktarma`
satırının oluştuğu `docker exec`+`sqlcmd` ile birebir teyit edildi.

**`TransferSiberTests` neden bazı testleri sahte Siber depolarıyla, bazılarını gerçek HTTP ile
yapıyor:** Siber (legacy MSSQL) senkronizasyonuna dokunan akışlar (`transfer_to_siber`, araç/cari
Siber senkronu) test ortamında `ConnectionStrings:Siber` bilinçli olarak tanımsız — gerçek dış sisteme
ASLA bağlanılmıyor. Bu, iki farklı test stratejisi gerektirdi:
- **"Yapılandırılmamışsa ne olur" testleri** (`TransferToSiber_WhenSiberNotConfigured_...`,
  `LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable`) gerçek HTTP ile çalışır — test
  ortamının KENDİSİ zaten unconfigured olduğu için bu davranış hiçbir hazırlık gerektirmeden doğal
  olarak tetiklenir.
- **BR-002/003/004/005 iş kuralı testleri** `TransferSiberService`/`LoadTransferWriteService`'i HTTP
  üzerinden DEĞİL, doğrudan örnekleyerek çalışır — `IsConfigured=true` döndüren ama her Siber G/Ç
  metodu çağrılırsa `NotSupportedException` fırlatan sahte depolarla (`FakeSiberLoadRepository`,
  `FakeSiberReservationRepository`, aynı dosyada). Bu, testin BR kurallarını (ki hepsi gerçek bir
  Siber çağrısından ÖNCE çalışır) gerçekten kilitlediğinin, yanlışlıkla hep-aynı-hatayı-döndüren bir
  sahte test olmadığının kanıtı — bir kontrol beklenenden geç devreye girerse test gürültülü
  başarısız olur.
Dönüşümün MUTLU YOLU (gerçek Siber yazma + rezervasyon karşılaştırma) hâlâ test edilmiyor — bunun
için gerçek bir Siber-mock bağlantısı gerekir, bkz. TESLIM-RAPORU.md §8.

### 2.2 `OLS.Business.Tests` (29 test)

Saf birim testler, veritabanı yok.

- **`TurkishDecimalTests`** (14 test) — para/hacim/ağırlık alanlarının Türkçe virgül/nokta ayrıştırması.
  DATA-002 sınıfı risk: yanlış ayrıştırma parayı 10x/100x büyütüp küçültebilir. İki test yazılırken
  ilk varsayımım YANLIŞ çıktı (`int.TryParse(..., NumberStyles.Any)`'nin virgülü ne kadar hoşgörülü
  yorumladığını hafife almıştım) — gerçek çalıştırma sonucuna göre düzeltildi, tahminle bırakılmadı.
- **`BcryptPasswordHasherTests`** (6 test) — hash/verify sözleşmesi, salt rastgeleliği, bozuk hash'te
  exception fırlatmama. §3.1'deki gerçek arızadan sonra eklendi.
- **`LengthAwarePaginatorTests`** (5 test) — Laravel sayfalama zarfının from/to/last_page hesaplaması
  ve JSON alan adları (`current_page`, `per_page`, ...). 13 farklı serviste kullanılan ortak bir
  sözleşme; burada bozulursa TÜM liste ekranlarının sayfalaması sessizce yanlış çalışır.

### 2.3 Neden `OLS.DataAccess.Tests` boş

EF Core entity konfigürasyonları ve migrasyon davranışı, izole bir birim testinde anlamlı şekilde
doğrulanamaz (gerçek bir sağlayıcıya karşı çalıştırılmaları gerekir). Bu davranış zaten 20 entegrasyon
testinin HER BİRİNDE dolaylı olarak uçtan uca doğrulanıyor: her test gerçek Postgres'e karşı migrasyon
çalıştırıyor, gerçek sorgular yürütüyor. Anlamsız/her zaman geçen sahte bir test eklemek yerine proje
bilinçli olarak boş bırakıldı — `dotnet test` bunu "test yok" olarak dürüstçe raporluyor.

---

## 3. Test geliştirme sürecinde bulunan ek gerçek sorunlar

Bunlar otomatik test YAZARKEN ortaya çıktı; üçü de gerçek, bağımsız doğrulamayla teyit edildi.

### 3.1 Dev admin şifresi bu oturumda kazara değişmiş

İlk entegrasyon testi çalıştırıldığında gerçek Docker API'sine (`localhost:8106`) karşı
`admin@ols-scoped.local` / `ChangeMe!Dev1` ile giriş `401` döndü — daha önce bu oturumda tarayıcıyla
başarıyla giriş yapılmışken. Kök neden araştırması (`docker exec ... psql` ile doğrudan satır incelemesi)
şunu gösterdi: saklı hash `"ChangeMe!Dev1"` ile doğrulanamıyordu VE maliyet faktörü 11'di (kod her zaman
12 üretir) — yani hash, `DbSeeder`'ın ürettiği hash DEĞİLDİ. `UserService.UpdateAsync`'in
`if (!string.IsNullOrWhiteSpace(request.Password))` koruması doğru ve kasıtlı (olsold kuralı) — kod
hatası yok. En olası açıklama: bu oturumun daha önceki bir adımında Kullanıcılar → Profil sekmesinde
"Yeni Şifre" alanına dolu bir değerle kaydet tetiklenmiş (muhtemelen tarayıcı test akışı sırasında).
Bu bir KOD hatası değil, bu oturuma özgü dev-veri kazası. Şifre, testler sırasında hasher'dan gerçekten
üretilmiş doğrulanmış bir hash ile `UPDATE users SET password = ...` şeklinde geri yüklendi; gerçek
`POST /api/v1/login` çağrısıyla düzeldiği doğrulandı.

**Etki:** Sadece bu makinenin dev veritabanı. Üretim yok, gerçek kullanıcı yok. Docker imajını yeniden
oluşturursanız (`docker compose down -v && docker compose up -d`) sorun zaten kendiliğinden düzelir
çünkü seed yeniden çalışır.

### 3.2 Testcontainers bu makinede gerçek dev Postgres'e yanlış yönlendiriliyor

İlk entegrasyon testi tasarımı `Testcontainers.PostgreSql` ile HER TEST ÇALIŞTIRMASINDA taze, izole bir
konteyner kullanmayı hedefliyordu. Testler çalıştı ama tuhaf sonuçlar verdi (yanlış veritabanı adı,
yanlış zaman damgaları). Adım adım adli inceleme:

1. `SELECT current_database()` → `"ols_scoped"` döndü — istenen `"ols_scoped_test"` DEĞİL.
2. `SELECT pg_postmaster_start_time()` → HER "taze" konteynerde (farklı container id, farklı host
   portu olmasına rağmen) AYNI zaman damgasını döndürdü.
3. `docker exec ols-scoped-postgres psql -c "SELECT pg_postmaster_start_time()"` (gerçek dev konteyneri,
   port 5443) → BİREBİR AYNI zaman damgası (mikrosaniyeye kadar).

**Sonuç:** Bu geliştirme makinesinde (Docker Desktop + WSL2) Testcontainers'ın dinamik olarak atadığı
host portları, bir şekilde gerçek `docker compose` Postgres konteynerine yönlendiriliyor — Testcontainers
kendi taze konteynerini oluşturduğunu/yok ettiğini bildirse de. Bu, uygulama kodunda DEĞİL, bu makinenin
Docker ağ katmanında bir sorun (muhtemelen Docker Desktop/WSL2 port-proxy önbelleği). Kod tarafında
düzeltilebilecek bir şey yok.

**Uygulanan çözüm (ilk deneme — YETERSİZ çıktı, bkz. §3.2.1):** `OlsApiFactory`, Testcontainers yerine
AYNI (doğrulanmış çalışan) dev Postgres'e, her test çalıştırması için rastgele adlı İZOLE bir
veritabanıyla bağlanmaya çalıştı (`CREATE DATABASE` ile oluşturulup `DROP DATABASE ... WITH (FORCE)`
ile temizleniyor), `ConfigureAppConfiguration` ile `ConnectionStrings:Postgres` override edilerek.

**Bilinen yan etki:** Bu geçiş sırasında birkaç test çalıştırması (sorun teşhis edilmeden önce)
yanlışlıkla GERÇEK dev veritabanına test verisi yazdı (`@example.test` e-postalı 24 kullanıcı, GUID'li
8 sahte cari, 4 sahte araç). Bu kayıtlar tespit edilip SQL ile temizlendi; dev veritabanında yalnızca
gerçek seed admin + bu oturumun daha önce tarayıcıyla oluşturduğu 1 cari + 1 araç kaldı (doğrulandı).

### 3.3 İlk izolasyon denemesi de sessizce başarısızdı — AYNI kök neden sınıfı §3.4'te

**Bu, bu oturumdaki EN ÖNEMLİ bulgu:** §3.2'deki "izole veritabanı" düzeltmesi (`ConfigureAppConfiguration`
ile `ConnectionStrings:Postgres` override) İLK BAKIŞTA çalışıyor GÖRÜNDÜ — testler geçti, hatta birkaç
tur boyunca. Gerçekte HİÇBİR ZAMAN çalışmadı: `OLS.DataAccess/DependencyInjection.cs`'teki `AddDataAccess`,
`ConnectionStrings:Postgres`'i `AddDbContext`'in options lambda'sı İÇİNDE değil, lambda'dan ÖNCE bir
yerel değişkene (`var postgres = configuration.GetConnectionString("Postgres")`) okuyup lambda'yı bu
DEĞİŞKENİN closure'ıyla kaydediyor — §3.4'teki `Jwt:Key` sorunuyla BİREBİR AYNI kök neden sınıfı
(config'i `builder.Build()`'dan önce yerel değişkene okumak). `ConfigureAppConfiguration` override'ı bu
erken okumadan SONRA devreye girdiği için SESSİZCE yok sayılıyordu — uygulama her zaman GERÇEK dev
veritabanına (`ols_scoped`) bağlanıyordu; `CREATE DATABASE ols_scoped_inttest_...` / `DROP DATABASE`
çağrıları BOŞ, hiç kullanılmayan bir veritabanı yaratıp siliyordu.

**Nasıl fark edildi:** Gerçek frontend'de (`localhost:8105/musteriler`) beklenmeyen kayıtlar
(`Baska Musteri d7f4e034...`, id C10/C11) görüldü — testler "geçtikten" SONRA bile dev veritabanı
kirleniyordu. Bu, "izolasyon" düzeltmesinin hiç işe yaramadığının kanıtıydı; varsayımla bırakılmadı,
doğrudan araştırıldı.

**Doğrulama:** `DiagnosticTests` içine geçici bir test eklenip `SELECT current_database()` ve
`db.Accounts.CountAsync()` doğrudan sorgulandı:
- Düzeltmeden ÖNCE: `currentDb=ols_scoped` (gerçek dev veritabanı).
- Düzeltmeden SONRA: `currentDb=ols_scoped_inttest_5e524e69...` (izole, `accountCount=0`).

**Kalıcı çözüm:** `ConfigureAppConfiguration` yerine, `InitializeAsync` içinde (host hiç kurulmadan ÖNCE)
`Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", ...)` çağrılıyor. Ortam değişkenleri
`WebApplication.CreateBuilder(args)`'ın KENDİ ilk yapılandırma taramasının bir parçası olduğundan,
Program.cs'in erken okuduğu yerel değişken de bu değeri görüyor. `dotnet test` çıktısında migrasyon
logu da bunu doğruluyor — düzeltmeden önce her zaman "No migrations were applied. The database is
already up to date." (zaten migre edilmiş `ols_scoped`'a bağlanıldığının işareti), düzeltmeden sonra
gerçekten "Applying migration '20260810122004_ScopedBaseline'." (gerçekten boş, taze bir veritabanı).

**Temizlik:** Bu ikinci sızıntı sırasında oluşan ek sahte kayıtlar (2 cari, 1 araç, 6 kullanıcı) da
SQL ile temizlendi; nihai doğrulama (`SELECT COUNT(*)`): 1 kullanıcı, 1 cari, 1 araç — hepsi gerçek.

### 3.4 `Program.cs`'in `Jwt:Key` okuma sırası, `WebApplicationFactory` override'ını es geçiyor

Yukarıdaki iki sorun düzeltildikten sonra bile: gerçek admin girişi (200 + token) başarılıydı ama O
JETONLA yapılan bir sonraki istek HER ZAMAN 401 dönüyordu. `JwtBearerEvents.OnAuthenticationFailed`'e
geçici bir yakalayıcı eklenerek gerçek iç istisna görüldü: `IDX10517: Signature validation failed` —
imzalama ve doğrulama FARKLI anahtarlar kullanıyordu.

**Kök neden:** `Program.cs`, `var jwtKey = builder.Configuration["Jwt:Key"]` satırını `builder.Build()`
çağrısından ÖNCE, yerel bir değişkene okuyup `AddJwtBearer`'ın `IssuerSigningKey`'ine bu değişkenin
kapanışıyla (closure) veriyor. `WebApplicationFactory`'nin `ConfigureAppConfiguration` override'ı ise
`.Build()` bir `DiagnosticListener` olayıyla yakalandığında uygulanıyor — yani Program.cs'in erken
okuduğu yerel değişkenden SONRA. Sonuç: `JwtTokenService` (girişte imzalama; `IConfiguration`'ı DI
üzerinden İSTEK ANINDA okur) test override'ını GÖRÜYOR, ama doğrulama tarafı (erken yakalanan yerel
değişken) GÖRMÜYOR — iki farklı anahtar, her jeton reddediliyor.

Bu, ASP.NET Core'da bilinen bir `WebApplicationFactory` test edilebilirlik tuzağı (config'i `.Build()`
öncesi yerel değişkene okuyup closure'a kapatmak) — güvenlik açığı DEĞİL, üretimde (WebApplicationFactory
devrede değilken) hiçbir etkisi yok.

**Uygulanan çözüm (kod DEĞİL, test tarafı):** `OlsApiFactory`, `Jwt:Key`'i override ETMİYOR; bunun
yerine `appsettings.Development.json`'daki mevcut anahtarı kullanıyor, böylece imzalama ve doğrulama
tarafı zaten aynı (override edilmemiş) değerde hizalanıyor. `Program.cs`'e dokunulmadı — üretim
davranışını etkileme riski almamak için bilinçli tercih. Gelecekte gerçekten `Jwt:Key`'i test başına
override etmek gerekirse, doğru düzeltme `Program.cs`'te bu değeri `services.AddOptions<JwtBearerOptions>()
.Configure<IConfiguration>(...)` gibi DI-dostu, GEÇ-okunan bir desene taşımak olur.

---

## 4. `ToPagedOrListAsync` — bug değil, olsold'un birebir kopyası

İlk test çalıştırmasında `GET /api/v1/account` ve `GET /api/v1/car` gibi uçlar `per_page` VERİLMEDEN
çağrıldığında `{data: {total: ..., data: [...]}}` yerine `{data: [...]}` (çıplak dizi) döndürdüğü için
3 test başarısız oldu. İnceleme (`QueryableExtensions.ToPagedOrListAsync`, 13 serviste ortak kullanılan
helper) bunun olsold'un `$request->has('per_page') ? paginate() : get()` deyiminin bilinçli, birebir
portu olduğunu doğruladı — gerçek React frontend'i (CustomersPage, VehiclesPage) zaten HER ZAMAN
`per_page` göndererek paginated şekli alıyor. Testler gerçek frontend kullanımını yansıtacak şekilde
`per_page` eklenerek düzeltildi; üretim kodunda hiçbir değişiklik yapılmadı.

---

## 5. Docker doğrulaması

```bash
docker compose build frontend   # React build'i başarıyla derledi (tsc -b && vite build, 0 hata)
docker compose up -d --no-deps frontend   # konteyner yeniden oluşturuldu, ayağa kalktı
```

Sonrasında `http://localhost:8105` (nginx + React build, Vite dev sunucusu DEĞİL) tarayıcıda açılıp
`/api` ve `/storage` proxy'sinin gerçek Docker API konteynerine (8106) sorunsuz ulaştığı ağ isteği
kayıtlarıyla doğrulandı (tüm istekler 200 OK).

---

## 6. Kapsam dışı / henüz test edilmeyenler (dürüst liste)

- Teklif→Yük dönüşümünün MUTLU YOLU (gerçek Siber'e yazma + 15 alanlık rezervasyon karşılaştırması):
  kod var, RET eden BR-002/003/004/005 kuralları artık doğrudan servis testleriyle kilitli
  (`TransferSiberTests.cs`), ama KABUL eden yolun kendisi bu ortamda Siber kimlik eşlemesi eksik
  olduğu için gerçekten ÇALIŞTIRILAMIYOR (bkz. TESLIM-RAPORU.md §8). Sefer oluşturma/güncellemenin
  KENDİSİ de benzer şekilde boş lookup tablolarıyla bloke (bkz. TESLIM-RAPORU.md §8) — Sefer-Yük
  BAĞLAMA (BR-006/007) bundan etkilenmiyor ve test edildi (`ExpeditionLoadMappingTests`). Fatura kalem
  EŞLEME + durum geçişi de test edildi (`InvoiceTests`) — ama Uyumsoft'a bağlı KDV/yuvarlama hesaplama
  mantığı (`PayableAmount`/`TaxAmount` vb.) hiç portlanmadı, dolayısıyla test edilecek bir şey yok —
  kasıtlı kapsam dışı. `ValidateRequired`'daki kalan sekiz alan kontrolü (yalnızca ödeme şekli
  örneklendi) ve BR-010 için ek test yazılmadı.
- Profil güncelleme (BR-012 mevcut şifre kontrolü), dosya yükleme doğrulama, destek formu anonim erişim
  kuralları — kod içinde uygulanmış durumda, otomatik testleri bu oturumda eklenmedi.
- Siber senkronizasyonuna dokunan uçların "yapılandırılmamışsa 503" davranışı artık test edildi
  (`TransferSiberTests.LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable`).
- 3 viewport (1440×900, 1024×768, 390×844) görsel karşılaştırma — bu raporun kapsamı dışında,
  GORSEL-PARITE-RAPORU.md'de ayrıca ele alınacak.
