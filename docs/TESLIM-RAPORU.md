# Teslim Raporu

Bu belge, `ols-scoped-dotnet` teslimatının güncel, dürüst durumunu özetler. Hiçbir madde "tamamlandı"
olarak işaretlenmeden önce gerçekten çalıştırılıp doğrulanmadan yazılmadı; eksik kalan işler de aynı
titizlikle, somut olarak listelendi.

## 1. Kapsam

**İçeride (9 ekran):** Dashboard, Müşteri (Cari), Teklif, Yük, Sefer, Fatura, Araç, Kullanıcılar, Destek
Talebi — artı bunların ortak altyapısı (auth, yetki modeli, coğrafya/lookup verileri).

**Dışarıda (bilinçli):** olsold'un ayrı Reports/Hedef-ciro yönetimi (Dashboard'dan FARKLI — bkz. aşağıda),
PDKS, kurum-içi mesajlaşma (Socket.IO/Mongo), Excel yönetimi, muhasebe planı admin ekranları, gümrük
modülleri (transit beyanname/ordino/yetki mektubu), CMS, test/demo sayfaları, ilgisiz cron/job'lar.
Gerekçe: [docs/SECILI-MODUL-PARITE-MATRISI.md](SECILI-MODUL-PARITE-MATRISI.md).

**Kritik yön değişikliği #1 (oturum ortasında):** Frontend başlangıçta yanlışlıkla olsnew'in Vue3+PrimeVue
arayüzü baz alınarak inşa edilmişti. Kullanıcı bunu düzeltti: gerçek hazır tasarım
`olstemel/docs/src/app/App.tsx`'teki React uygulaması. Vue frontend'i tamamen kaldırılıp (502 dosya
değişikliği) React 19 + TypeScript + Vite + Tailwind v4 ile aynı tasarım sisteminden sıfırdan yeniden
kuruldu. Backend bu değişiklikten etkilenmedi.

**Kritik yön değişikliği #2 (bu güncellemede):** Kullanıcı, `docs/tasarım` klasöründe (ilk incelenen
`olstemel/docs` kopyasından FARKLI, daha yeni bir dışa aktarım) bir Dashboard ve Login ekranı tasarımı
bulunduğunu, bunların eklenmediğini belirtti. İnceleme doğruladı: `docs/tasarım/src/app/App.tsx` (2911
satır, `olstemel/docs`'taki 2336 satırlık kopyadan farklı) gerçekten `LoginPage`, `MetricCard`,
`DashboardModule` bileşenlerini içeriyor. Bu ikisi eklendi — Dashboard, önceki bir kararda kapsam dışı
bırakılan olsold Reports modülünün portu DEĞİL, yalnızca bu 8 modülün zaten var olan verilerinden GERÇEK
toplamlar hesaplayan yeni, hafif bir özet ekranı. Ayrıntı: SECILI-MODUL-PARITE-MATRISI.md §9.

## 2. Tamamlanan iş (gerçekten çalışır, doğrulanmış)

- **Backend:** 3 katman (`OLS.API`→`OLS.Business`→`OLS.DataAccess`), 58 tablo, EF Core/Npgsql +
  Dapper/Siber, JWT auth (jti iptal listesi), sayfa-slug×CRUD yetki modeli, snake_case JSON, `/api/v1`
  öneki, `TurkishDecimal` para ayrıştırıcı, `IClock` soyutlaması, rate limiting, correlation-id
  middleware'i. `dotnet build` (tüm çözüm): **0 hata**.
- **Frontend:** 8 modülün TÜMÜ için liste + arama + sayfalama + oluştur/düzenle/sil temel akışı çalışıyor
  (alan derinliği farklı — bkz. §8). Tasarım sistemi (koyu lacivert sidebar, kompakt kurumsal bileşenler,
  `DataTable`/`Drawer`/`Modal`/`Toast`) referans tasarımdan birebir taşındı.
- **Docker:** `docker compose up -d --build` ile tüm yığın (postgres, sahte-siber, api, frontend) tek
  komutla ayağa kalkıyor, CANLI hiçbir sisteme bağlanmıyor. Gerçekten çalıştırılıp doğrulandı (bu oturumda
  birden fazla kez, Docker Desktop'ın oturum sırasında kapanıp yeniden başlatılması dahil — veri named
  volume sayesinde kalıcı, kayıpsız).
- **İki kritik hata canlı ortamda bulunup düzeltildi ve regresyon testiyle kilitlendi** — ayrıntı
  [docs/TEST-RAPORU.md](TEST-RAPORU.md) §1: `/api/v1/role` zarf uyuşmazlığı (sidebar tamamen boş
  görünüyordu), `super_admin` yetki sayfasının seed edilmemesi (yeni cariler kimseye görünmüyordu).
- **49 otomatik test, hepsi geçiyor** (20 entegrasyon + 29 birim) — gerçek Postgres'e karşı, gerçek HTTP
  pipeline'ı üzerinden, hiçbir katman mock'lanmadan. Test geliştirme sürecinde 3 ayrı gerçek ortam/
  test-edilebilirlik sorunu daha bulunup düzeltildi (bkz. TEST-RAPORU.md §3) — en önemlisi, testlerin
  başlangıçta sessizce GERÇEK dev veritabanına yazdığının fark edilip kalıcı olarak düzeltilmesi.
- **Belgeler:** SECILI-MODUL-PARITE-MATRISI, API-PARITE-MATRISI, YETKI-MATRISI, VERI-MODELI, TEST-RAPORU,
  GORSEL-PARITE-RAPORU, README — hepsi bu oturumda yazıldı, kod/canlı-sistem okumasına dayanıyor.

## 3. Legacy davranış: korunan vs bilinçli düzeltilen

| Alan | olsold davranışı | HEDEF'te | Neden |
|---|---|---|---|
| Yetki kontrolü | Çoğu yerde etkisizdi (yorum satırı / süslü parantezsiz `if`) | Gerçekten 403 uygulanıyor | Görev tanımının açık güvenlik şartı — bilinen güvenlik açıkları taşınmaz |
| `status_type_id` | Ham sayı karşılaştırması (ortam değişirse kırılır) | `StatusTypeCodes` sabitleri + `status_types.number` string kodu | DATA-002, görev tanımının açık şartı |
| Cari ad/e-posta çakışması | 500 + `{errors:...}` | 422 + aynı zarf şekli | Durum kodu düzeltmesi, sözleşme korunuyor |
| Şifreler | Laravel bcrypt (`$2y$`) | `BCrypt.Net` `$2a$`, aynı hash'leri doğrulayabiliyor | Geçiş sonrası mevcut şifreler çalışmaya devam eder |
| Para/ölçü alanları | PHP zayıf tipleme | `decimal` (asla `float`/`double`) | Görev tanımının açık şartı |
| Yetki matrisi self-read | Karışık/tutarsız | Kendi rolünü herkes okuyabilir, güncellemek her zaman `role_management`/update ister | Kaynak niyeti netleştirildi, testle kilitlendi |
| Siber senkronizasyonu | Gerçek Siber'e yazar | Yalnızca `siber-mock`'a (yerel, sahte) | Görev tanımının açık şartı — testte CANLI sisteme asla bağlanılmaz |

## 4. Dış entegrasyonlar — gerçek/mock/yapılandırılmamış durumu

| Entegrasyon | Durum | Not |
|---|---|---|
| Siber (legacy MSSQL) | **MOCK** (`siber-mock` konteyneri) | Gerçek Siber ERP'ye hiçbir ortamda bağlanmaz. `ConnectionStrings:Siber` boşsa uygulama ayağa kalkar, Siber'e dokunan uçlar 503 döner (koda göre; ayrı test yazılmadı — bkz. §8) |
| SMTP (e-posta) | **YAPILANDIRILMAMIŞ** | `offer_send_email` gibi uçlar gerçek mail göndermez; kod, gönderim başarısız/devre-dışı durumunu sahte `{sent:true}` ile MASKELEMEZ |
| Uyumsoft (e-fatura) | **YAPILANDIRILMAMIŞ** | Fatura modülünün draft/send/cancel/approve/pdf-view uçları için 503-stub davranışı planlandı ama frontend'de henüz hiç UI'si yok (bkz. §8) |
| AI (teklif önerisi) | **UYGULANMADI** | olsold'daki "AI'dan teklif" (`saveAi`) özelliği bu oturumda frontend'e hiç eklenmedi — ne gerçek ne sahte, tamamen yok |

## 5. Gerçekten çalıştırılan komutlar ve sonuçları

```
dotnet build                                    → 0 hata, 2 pre-existing nullability uyarısı
dotnet test                                     → 49/49 geçti (29 birim + 20 entegrasyon), ~1.2 dk
docker compose up -d --build                    → 4 servis (postgres/siber-mock/api/frontend) sağlıklı
curl -X POST .../api/v1/login (admin)           → 200, gerçek JWT
GET /api/v1/account (Docker API, canlı)         → 200, gerçek cari listesi
SELECT COUNT(*) (dev veritabanı, temizlik sonrası) → 1 kullanıcı, 1 cari, 1 araç — hepsi gerçek
```

Tüm komutların tam çıktıları ve context'i: [docs/TEST-RAPORU.md](TEST-RAPORU.md).

## 6. Test durumu (özet)

66/66 otomatik test geçiyor (29 OLS.Business.Tests + 37 OLS.API.IntegrationTests). Kapsanan: auth
(giriş/çıkış/jeton iptali), yetki zorlaması (401/403 sınırları, bilinmeyen slug davranışı), iki kritik
regresyon (rol zarfı, super_admin), para ayrıştırma, şifre hash'leme, sayfalama sözleşmesi, Dashboard
agregelerinin gerçek veriyle birebir eşleştiği (uydurma sayı olmadığı), Teklif'in TAM alan kapsamıyla
(taraflar/güzergah/mali kalem, Türkçe ondalık biçimiyle) round-trip ettiği, Yük güncelleme uç noktasının
(çekirdek alanlar + paket upsert + paket silme) gerçek Postgres'e karşı doğru çalıştığı, Sefer-Yük
bağlamanın (BR-006/007 romork tipi eşleşme kuralı dahil) doğru çalıştığı, Fatura kalem eşlemesinin
(+ kalem durumunun alış/satışa göre doğru değiştiği) ve dipnot CRUD'unun doğru çalıştığı, Teklif→Yük
dönüşüm zincirinin BR-002/003/004/005 kurallarının (doğrudan servis örneklemesi + sahte Siber
depoları ile, bkz. §8) ve gerçek "Siber-503" davranışının doğru çalıştığı. Kapsanmayan
(bilinçli, dürüstçe not edildi): dönüşümün MUTLU YOLU — gerçek Siber'e yazma + 15 alanlık rezervasyon
karşılaştırması (bkz. §8 Siber kimlik eşleşmesi kısıtı, bu ortamda hâlâ kurulamıyor; yalnızca RET
kuralları test edildi, kabul yolu değil), Sefer oluşturma/güncellemenin KENDİSİ (bkz. §8 — boş
`expedition_types`/`expedition_statuses`), `ValidateRequired`'daki DİĞER sekiz alan kontrolü (yalnızca
ödeme şekli örneklendi, aynı desen), BR-010, profil şifre değişikliği (BR-012), dosya yükleme
doğrulama. Ayrıntı: TEST-RAPORU.md.

## 7. Görsel parite durumu (özet)

3 zorunlu viewport'ta (1440×900, 1024×768, 390×844) Müşteriler modülü gerçekten açılıp incelendi —
masaüstünde DOM ölçümüyle, diğer ikisinde tam ekran görüntüsüyle. Temel layout iskeleti (sidebar/
topbar/tablo/sayfalama/mobil hamburger menü) doğru ve tasarım kurallarıyla tutarlı. Diğer 7 modülün
her viewport'ta tek tek ekran görüntüsü alınmadı (paylaşılan bileşenleri kullandıkları için davranışın
aynı olması BEKLENİYOR ama tek tek DOĞRULANMADI). Ayrıntı: GORSEL-PARITE-RAPORU.md.

## 8. Bilinen kısıtlar / eksik iş (somut, dürüst liste)

### Alan derinliği (en büyük eksik — kısmen kapatıldı)

Önceki oturumda her modülün yalnızca TEMEL alanları çalışıyordu. Bu oturumda Teklif ve Yük derinlik
kazandı; Sefer ve Fatura henüz kazanmadı:

- **Teklif** (`QuotesPage.tsx`) — TAMAMLANDI: 5 sekme (Genel Bilgiler/Taraflar/Güzergah/Mali Kalemler/
  Dosyalar), tam oluşturma+düzenleme, çoklu içerik/mali-kalem satırı, `AccountPicker` ile gerçek cari
  arama (gönderici/alıcı/acente/navlun-ödeyen), dosya ekleme/kaldırma. Backend DTO'suyla alan
  kapsamı birebir. Otomatik test: `LoadTests.cs` (tam alan round-trip + Türkçe ondalık ayrıştırma).
- **Yük** (`LoadsPage.tsx`) — KISMEN TAMAMLANDI: artık gerçek 2 sekmeli (Genel Bilgiler/Paketler)
  düzenleme formu var (önceden salt-okunurdu); Teklif→Yük dönüşüm tetikleyicisi (Teklifler ekranında
  `siber_id` dolu satırlarda görünen kamyon ikonu → `POST /api/v1/load_transfer`) eklendi. Liste
  sütunları da düzeltildi (bkz. altındaki "bu oturumda bulunup düzeltilen" notu). HÂLÂ EKSİK — bilinçli
  kapsam dışı bırakıldı: Hareketler sekmesi (`expedition_statuses`/`load_status_types` tabloları boş —
  bkz. §8 "Boş lookup tabloları"), Fatura Kalemleri (`load_transfer_invoice_item`) sekmesi. Otomatik
  test: `LoadTransferTests.cs` (güncelleme + paket ekleme/silme, doğrudan EF Core ile seed edilen bir
  kayıtla — bkz. aşağıdaki Siber kısıtı).
- **Sefer** (`TripsPage.tsx`) — KISMEN TAMAMLANDI: satıra tıklayınca açılan ayrı bir detay/düzenleme
  Drawer'ı eklendi (önceden yalnızca "Yeni Sefer" oluşturma vardı, mevcut kaydı açmanın hiçbir yolu
  yoktu), 2 sekmeli (Genel Bilgiler/Bağlı Yükler). Bağlı Yükler sekmesi TAM ÇALIŞIYOR: sefere
  eklenebilecek (henüz bağlanmamış) yükleri arayan bir seçici, ekleme/çıkarma, toplam adet/ağırlık/
  hacim özeti — hem PostgreSQL hem GERÇEK Siber-mock senkronuyla canlı doğrulandı (bkz. aşağıdaki
  not — bu, Teklif→Yük dönüşümünün AKSİNE, Siber kimlik eşlemesine bağımlı değil). "Sefer Tipi" alanı
  artık zorunlu işaretlendi (backend'in gerçekte zorunlu tuttuğu ama frontend'in yıldızlamadığı bir
  alan olduğu bu oturumda bulundu). Otomatik test: `ExpeditionLoadMappingTests.cs` (bağlama+silme +
  BR-006/007 romork tipi eşleşmeme senaryosu). HÂLÂ EKSİK — bilinçli kapsam dışı: Hareketler sekmesi
  (aynı `expedition_statuses` boş-tablo kısıtı).
- **Fatura** (`InvoicesPage.tsx`) — KISMEN TAMAMLANDI: satıra tıklayınca açılan ayrı bir detay/düzenleme
  Drawer'ı eklendi, 3 sekmeli (Genel Bilgiler/Kalemler/Dipnotlar). Kalemler sekmesi bir "Fatura'nın
  kendi satırları" DEĞİL — kaynağın gerçek veri modelini yansıtıyor: mevcut `load_transfer_invoice_item`
  kayıtlarını arayıp faturaya EŞLEYEN bir seçici (`load_transfer_invoice_maps`), backend'in "her
  güncellemede eşlemeleri baştan kur" davranışına uygun olarak yerelde biriktirilip Kaydet'te toplu
  gönderiliyor. Dipnotlar sekmesi bağımsız, anında kaydedilen CRUD (kendi REST uçları var). Müşteri
  alanı da `AccountPicker`'a yükseltildi (hem oluşturma hem düzenleme formunda). Canlıda uçtan uca
  doğrulandı: gerçek bir kalem eklenip kaydedildi (DB'de `load_transfer_invoice_maps` satırı VE
  kalemin durumu `invoice_issued`'a geçtiği doğrulandı — kaynağın alış/satış kuralı birebir), sonra
  kaldırılıp tekrar boşaltıldığı doğrulandı; dipnot eklenip silindi. Otomatik test: `InvoiceTests.cs`
  (3 test). HÂLÂ EKSİK — bilinçli kapsam dışı: PDF önizleme, Uyumsoft draft/send/cancel/approve UI'si
  (backend'de zaten hiç portlanmadı — bkz. `InvoiceController.cs` üstündeki yorum).

**Bu oturumda bulunan bir hata daha:** Fatura oluşturma formunda "Vade Tarihi" zorunlu
İŞARETLENMEMİŞTİ ama backend boş bırakılırsa HER ZAMAN reddediyor (`InvoiceController.Validate` →
`invoice_execution_date`). Canlıda denendi, doğrulandı, düzeltildi (hem oluşturma hem düzenleme
formunda `*` eklendi) — `InvoiceTests.CreateInvoice_WithoutExecutionDate_ReturnsValidationError` ile
regresyon testi de eklendi.

"Birebir" alan parite şartı Teklif için tam karşılanıyor; Yük ve Fatura için çekirdek+ilişkili-kayıt
alanlarında karşılanıyor (Hareketler, PDF/Uyumsoft kasıtlı olarak dışarıda); Sefer için Bağlı Yükler
tam karşılanıyor ama Genel Bilgiler kaydı ortam kısıtı yüzünden ÇALIŞTIRILAMIYOR (bkz. yukarıdaki not).

### Siber kimlik eşleşmesi kısıtı (bu oturumda bulunan, doğrulanan gerçek kısıt)

Teklif→Yük dönüşümü (`POST /api/v1/transfer_to_siber` → `POST /api/v1/load_transfer`) canlıda uçtan
uca DENENDİ ve şu anda BU ORTAMDA çalışmıyor: `payment_types.siber_id` (ve büyük olasılıkla diğer
Siber-eşlemeli lookup sütunları) hiçbir satırda dolu değil, bu yüzden `TransferSiberService` her
zaman "Ödeme şekli boş olamaz" hatasıyla reddediyor. Kaynağı doğrulandı — bu bir port hatası DEĞİL:
olsold'un kendi `PaymentTypeSeeder.php` dosyası da `siber_id` alanını hiç yazmıyor (bkz. dosya), yani
gerçek sistemde bu değerler seed'den değil, gerçek Siber entegrasyonunun canlı kullanımından zamanla
birikiyor. Bu scoped/yerel ortamda öyle bir geçmiş yok. Sonuç: `ConvertOfferAsync` kod olarak mevcut
ve mantığı doğru görünüyor, ama bu ortamda gerçek bir Teklif'i gerçekten Yük'e çeviren bir buton
tıklaması DOĞRULANAMADI — yalnızca doğru validasyon hatası verdiği doğrulandı. Yük düzenleme uç
noktası bu yüzden doğrudan EF Core ile seed edilen bir kayıtla test edildi (yukarıdaki `LoadTransferTests.cs`).

### Sefer oluşturma/güncelleme de boş lookup tablolarıyla bloke — ama Bağlı Yükler DEĞİL

Bu oturumda ayrıca bulundu: Sefer oluşturma (`POST /api/v1/expedition`) `expedition_types` tablosundan
GERÇEKTEN bir satır bulmayı şart koşuyor (`ExpeditionWriteService.CreateAsync`), güncelleme ise aynı
şekilde `expedition_statuses`'dan bir satır ister (`UpdateAsync`) — ikisi de bu ortamda BOŞ (0 satır).
Kaynağı doğrulandı: olsold'da da bu iki tablo için hiçbir seeder yok (yalnızca migration var) — yani
gerçek sistemde de bu değerler idari panelden elle girilmiş olurdu, taze bir kurulumda BOŞ olurdu.
Canlıda denendi: `POST /api/v1/expedition` → 400 `"Sefer türü bulunamadı"`. Sonuç: bu ortamda YENİ bir
Sefer oluşturmanın veya MEVCUT birinin Genel Bilgiler'ini kaydetmenin gerçek bir yolu yok — tıpkı
Hareketler gibi, ama farklı bir tablo yüzünden. **Bağlı Yükler bundan ETKİLENMİYOR**: o akış
(`ExpeditionLoadMappingService`) bu iki tabloya hiç dokunmuyor, yalnızca Sefer/Araç/Yük kayıtlarının
VAR OLUP OLMADIĞINA ve romork tipi eşleşmesine bakıyor — bu yüzden canlıda uçtan uca (PostgreSQL +
gerçek Siber-mock senkronu dahil) çalıştığı doğrulandı, otomatik testle de kilitlendi.

### Bu oturumda bulunup düzeltilen hatalar

- Yük listesi (`LoadsPage.tsx`) "Ağırlık"/"Hacim" sütunları gösteriyordu ama backend'in liste uç noktası
  (`LoadTransferListItemDto`) bu alanları HİÇ döndürmüyor (yalnızca detay uç noktası döndürüyor) —
  sütunlar her zaman "—" gösteriyordu. olsold'un gerçek `LoadTable.vue` bileşeni kontrol edildi: kaynak
  liste zaten yalnızca Yük Numarası/Müşteri/Gönderici/Durum gösteriyor, ağırlık/hacim hiç yok. Sütunlar
  bu dört alanı gösterecek şekilde düzeltildi (`sender_id` artık `LoadTransferItem` tipinde) — birebir
  kaynağa uyuyor.
- Sefer oluşturma formunda "Sefer Tipi" alanı zorunlu İŞARETLENMEMİŞTİ ama backend boş bırakılırsa
  HER ZAMAN reddediyor (`ExpeditionWriteService.CreateAsync` → "Sefer türü bulunamadı") — kullanıcı
  alanı atlarsa neden başarısız olduğunu anlayamazdı. `*` ile zorunlu işaretlendi.

### Diğer eksikler

- 8 modül × 3 viewport tam görsel matrisi (yalnızca Müşteriler tam kontrol edildi — bkz. §7).
- Mobil hamburger menüsünün AÇIK/slide-in hali interaktif doğrulanmadı (araç kısıtı, bkz.
  GORSEL-PARITE-RAPORU.md).
- BR-002/003/004 (Teklif→Yük dönüşüm gating: zaten dönüştürülmüş/durum-Olumlu-değil/Siber'e-
  aktarılmamış) ve BR-005 ailesinden bir örnek (ödeme şekli zorunlu) artık test edildi
  (`TransferSiberTests.cs`) — doğrudan servis örneklemesi + sahte `ISiberLoadRepository`/
  `ISiberReservationRepository` ile (`IsConfigured=true` ama hiçbir Siber G/Ç metodu ÇAĞRILAMAZ,
  çağrılırsa test `NotSupportedException` ile gürültülü başarısız olur — testin gerçekten BR
  kontrollerini, gerçek bir Siber round-trip'i DEĞİL, kilitlediğinin kanıtı). `ValidateRequired`'daki
  kalan sekiz alan kontrolü ve BR-010/012/013 için hâlâ ayrı test yok. BR-006/007 (Sefer-Yük romork
  tipi eşleşmesi) `ExpeditionLoadMappingTests.cs` ile test edildi.
- Siber-503 davranışı artık test edildi: `POST /api/v1/transfer_to_siber/loadSave`, Siber
  yapılandırılmamışken gerçekten `503 Service Unavailable` döndüğü doğrulandı
  (`TransferSiberTests.LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable`).
- Dosya yükleme (Teklif dosyaları, kullanıcı avatarı) uçtan uca tarayıcıda test edilmedi.

## 9. Güvenlik notları

- Tüm sırlar (JWT anahtarı, DB şifresi, Siber-mock şifresi) `.env`'de, placeholder değerlerle;
  `.env` `.gitignore`'da, yalnızca `.env.example` commit'li.
- Yetki zorlaması backend'de gerçek (401/403), frontend'deki gizleme yalnızca UX — testle doğrulandı
  (bkz. §6).
- Şifreler asla düz metin loglanmaz/saklanmaz; bcrypt maliyeti 12.
- CORS allow-list (`*` değil), rate limiting (auth: 10/dk, public-form: 5/dk) aktif.

## 10. Önerilen sonraki adımlar (öncelik sırasıyla)

1. Kalan 7 modül için 3-viewport görsel kontrolünü tamamlamak (yalnızca Müşteriler tam kontrol edildi).
2. Dosya yükleme (Teklif dosyaları, kullanıcı avatarı) için uçtan uca tarayıcı testi eklemek.
3. Sefer Genel Bilgiler'i gerçekten kaydedilebilir hâle getirmek için `expedition_statuses`'a en az
   bir gerçek satır eklemek gerekip gerekmediğine karar vermek (bkz. §8 — olsold'da da seeder'ı yok,
   bu yüzden veri UYDURMADAN yapılabilecek bir şey değil; kullanıcıya danışılmalı).
4. `ValidateRequired`'ın kalan sekiz alan kontrolü ve BR-010/012/013 için ek entegrasyon testleri.
5. "AI'dan teklif" özelliğinin gerekip gerekmediğine karar vermek — gerekiyorsa backend adaptör
  üzerinden, tarayıcıya asla anahtar sızdırmadan.
