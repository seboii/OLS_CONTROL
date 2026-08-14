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

**Kritik yön değişikliği #3 (bu güncellemede — önceki "Siber kimlik eşleşmesi kısıtı" tersine döndü):**
Kullanıcı "tamamen sahte Siber ile bütün sistemin eksiksiz çalıştığından emin olmamız lazım" dedi. Bu
oturumda önceden "kalıcı, düzeltilemez bir kısıt" olarak belgelenen §8'deki iki madde ("Siber kimlik
eşleşmesi kısıtı", "Sefer boş lookup tablolarıyla bloke") araştırılınca GERÇEKTEN kalıcı bir kısıt
OLMADIĞI, olsold'un `TransferDataController`'ının (Siber referans/tanım verisini yerel tabloya aktaran
ETL) bu porta hiç taşınmamış olmasından kaynaklandığı ortaya çıktı. `SiberImportService` bu boşluğu
kapattı (bkz. §2) ve canlıda doğrulandı: (a) 9 aktarım ucu da (`POST /transfer_data` + 8 `getX`) sıfır
hata/sıfır mükerrerle çalışıyor, tüm referans tablolarında `siber_id` %100 dolu; (b) gerçek bir Teklif
uçtan uca (transfer_to_siber → load_transfer) Yük'e dönüştürüldü, hem PostgreSQL'de (`load_transfers`,
paket, 2 fatura kalemi) hem mock Siber'de (`skn_yuk`/`skn_yukkoli`/`sfy_modulkalem`) doğrulandı; (c)
Sefer oluşturma artık `POST /api/v1/expedition` ile gerçekten çalışıyor (`expedition_types`/
`expedition_statuses` artık boş değil). Bu araştırma sırasında `ValidateRequired`/`MatchesReservation`
kontrollerinin olsold'daki tam listenin yalnızca ~üçte birini içerdiği, `InsUser`'ın yanlış kullanıcıdan
okunduğu ve birkaç lookup tablosunun (`RomorkType`/`LoadingType`) `Code` alanının hiç dolmadığı da
bulunup düzeltildi — ayrıntı aşağıda "Teklif→Yük dönüşümü artık gerçekten çalışıyor" bölümünde.

**Kritik yön değişikliği #4 (bu güncellemede — sistematik alan denetimi, kullanıcı isteğiyle):**
Kullanıcı "Teklif ve Yük modüllerinin tam denetimine devam et" dedi. olsold'un gerçek
`OfferFormDrawer.vue`/`LoadFormDrawer.vue`'sü ve alt bileşenleri (`*FormContentItem.vue`,
`*FormFinancialItem.vue`) SATIR SATIR taranıp her alan bu portun karşılığıyla tek tek karşılaştırıldı
(önceki oturumlarda yalnızca SEKME/TAB düzeyinde, yani "sekme var mı" diye bakılmıştı — bu kez her
sekmenin İÇİNDEKİ her alan). Sonuç, en ciddisi gerçek bir veri kaybı hatası olan 5 gerçek bulgu:

1. **[EN CİDDİ] Yük düzenleme formu 9 alanı sessizce siliyordu.** `LoadTransferDetailDto`'da (okuma)
   `romork_type_id`/`instruction_id`/`delivery_method_id`/`load_transfer_type_id`/`way_of_working`/
   `front_transportation_by_us`/`final_transportation_by_us`/`departure_country_id`/`target_country_id`
   HİÇ YOKTU — yazma tarafında (`LoadTransferUpdateRequest`) hepsi vardı. Formu AÇIP dokunmadan Kaydet'e
   basmak bu 9 alanı boşaltıyordu. Bu TEORİK değildi: bu oturumda DAHA ÖNCE yapılan Görevliler/Finans/
   Hareketler testleri sırasında canlı bir Yük kaydının bu alanlarını GERÇEKTEN sıfırlamıştı — DB'de
   doğrulandı. Aynı desende bir alan daha bulundu: paketlerin `case_type_id`'si.
2. Teklif'in Mali Kalemler sekmesinde Alış/Satış değerleri TERSTİ (`{Satış:"1",Alış:"0"}` — olması
   gereken `{Alış:1,Satış:2}`, Yük'te doğruydu). Bu oturumda önceden oluşturulan test verileri dahil,
   var olan her mali kalemin anlamı ters etiketleniyordu.
3. HEM Teklif HEM Yük'te "Kalem" alanı yanlış tabloyu (`item_type`) kullanıyordu; backend id'yi
   `financial_items` tablosunda arıyor. Bu ortamda iki tablonun id'leri TESADÜFEN örtüştüğü için hata
   gizli kalmıştı.
4. Acente/Navlun Ödeyen Firma/Mali Kalem Cari seçicileri hiç filtrelenmiyordu; kaynak `account_type_id`
   ile filtreliyor (ör. Acente yalnızca tip 5). Backend ucu filtreyi destekliyordu, hiç kullanılmıyordu.
5. Teklif formunda "Ön/Son Taşıma Tarafımızdan Yapılır" alanları hiç yoktu (backend destekliyordu),
   Yük formunda 5 çalışan alan (Yük Tipi/Yük Türü/Talimat/Teslimat Şekli/Çalışma Şekli/ülkeler) hiç
   yoktu.

Backend'e ayrıca 23+ modülün paylaştığı `LookupService<TEntity>`'ye opsiyonel `type` filtresi eklendi
(yalnızca `Type` sütunu olan entity'lerde devreye girer — bugün yalnızca `FinancialItem`). Tüm bulgular
canlı Docker'a karşı yazma→okuma round-trip'le doğrulandı; hiçbiri için "muhtemelen çalışıyordur"
denmedi. 71/71 test hâlâ geçiyor. Ayrıntı: §8.

**Kritik yön değişikliği #5 (bu güncellemede — Yük'ün Hareketler/Faturalar/Dosya-Arşivi denetimi,
kullanıcı isteğiyle):** #4'ün aynı satır-satır yöntemi bu kez Yük'ün henüz bu titizlikte incelenmemiş
3 sekmesine uygulandı: `LoadFormMovements.vue`, `LoadFormInvoices.vue`, ve `LoadFormDrawer.vue`'nin
Dosya Arşivi TabPanel'i. Sonuç, 4 gerçek bulgu (Dosya Arşivi'nde hiçbir sorun bulunmadı — orijinal
Teklif'in `load_id`'sine yazdığı zaten doğruydu):

1. **"Silinen Hareketler" görünümü hiç yoktu.** Backend zaten `{data, deleted_movements}` zarfını
   dönüyordu (`LoadTransferMovementController`/`MovementService` — soft-delete kayıtları ayrı anahtarla
   taşıyordu) ama frontend yalnızca `data`yı okuyup `deleted_movements`i tamamen görmezden geliyordu.
   Kullanıcı bir hareketi sildiğinde o kayıt kalıcı olarak GÖRÜNMEZ oluyordu (veri kaybolmuyordu, sadece
   erişilemez hale geliyordu). Buton + modal eklendi (`v-if="deletedMovements.length > 0"` davranışı
   dahil); canlı Docker'da bu oturumdan kalan gerçek bir silinmiş kayıtla doğrulandı.
2. Hareketi oluşturan kullanıcı yalnızca adıyla gösteriliyordu (`{name} {surname} ({email})` değil,
   yalnızca `name`). `MovementRefDto` yerine zaten var olan `MappedUserDto` kullanılarak hem Sefer hem
   Yük hareketlerinde `User` alanı genişletildi.
3. Faturalar çapraz görünümünde "Fatura Ticareti Tipi" (`commercial_type`) alanı hiç yoktu, ve
   Tutar/KDV Hariç/KDV alanları önceki bir oturumda "Uyumsoft'a bağlı, bu portta hiç yok" gerekçesiyle
   BİLİNÇLİ dışlanmıştı — **bu gerekçe yanlıştı**: `Invoice` entity'sinde bu sütunlar zaten var ve ana
   Fatura modülü (`InvoiceService`/`InvoicesPage.tsx`) bunları zaten okuyup gösteriyor; yalnızca Yük'ün
   çapraz görünümünde unutulmuşlar. Kaynağın kendi null→"0,00" davranışıyla (`useMoneyFormat`) birebir
   eklendi.
4. `box_type` etiketi hem Yük'ün çapraz görünümünde hem STANDALONE Fatura modülünde "Gider (Alış)"/
   "Gelir (Satış)" idi — YANLIŞ. Kaynağın gerçek `invoice_box_types` dizisi (ve `pages/invoices.vue`'nin
   gerçek Tab başlıkları) "Gelen Fatura(lar)"/"Giden Fatura(lar)" kullanıyor — gelen/giden EVRAK yönü,
   Alış/Satış değil. Her iki modülde de düzeltildi (3 UI konumu: filtre/tab listesi + create + edit form).

Backend build + `tsc -b` temiz; ilgili 8 test (Movement/Expedition/Invoice/LoadTransfer) yeşil; canlı
Docker'da üç sekme de gerçek verilerle doğrulandı.

**Kritik yön değişikliği #6 (bu güncellemede — Teklif'in Görevliler/Dosyalar denetimi):** Aynı yöntem
Teklif'in `OfferFormDrawer.vue` TabPanel value="3" (Görevliler) ve value="5" (kaynakta "Dosya Arşivi")
bölümlerine uygulandı. Bu kez SADECE 1 kozmetik bulgu: port'ta sekme adı "Dosyalar"dı, kaynakta
"Dosya Arşivi" — düzeltildi. Görevliler'in kendisi (Operasyon Yetkilisi tekil + Satış Temsilcisi çoğul,
`load_charge_person` ilişki tablosu, update'te eski kayıtları silip yeniden yazma) ZATEN birebir
doğruydu; canlı Docker'da API yanıtı (`GET /api/v1/load/1` → `load_charge_person` iki kayıt, ikisi de
dolu `user_id`) VE gerçek DOM `<input>` value'ları ("Ahmet Yılmaz") ile doğrulandı.

Bu doğrulama sırasında kendi metodolojimdeki bir tuzağa düşüp geri çıktım: `get_page_text` ile ilk
bakışta her iki alan da "boş/seçilmemiş" görünüyordu (yalnızca picker'ı açan "Seç" butonunun metnini
görüyordum) — ama `get_page_text` `<input>` elemanlarının `value`'sunu OKUMUYOR, yalnızca DOM metin
düğümlerini okuyor. `document.querySelectorAll('input[disabled]')[i].value` ile doğru kontrol edilince
verinin baştan beri doğru geldiği görüldü. Bu, önceki oturumda `<select>` için belgelenen aynı tuzağın
`<input>` sürümü — ders: seçici/picker bileşenlerinin dolu/boş durumunu HER ZAMAN gerçek DOM
`value`/`selectedOptions` sorgusuyla doğrula, düz metin dökümüyle değil.

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

71/71 otomatik test geçiyor (29 OLS.Business.Tests + 42 OLS.API.IntegrationTests). Kapsanan: auth
(giriş/çıkış/jeton iptali), yetki zorlaması (401/403 sınırları, bilinmeyen slug davranışı), iki kritik
regresyon (rol zarfı, super_admin), para ayrıştırma, şifre hash'leme, sayfalama sözleşmesi, Dashboard
agregelerinin gerçek veriyle birebir eşleştiği (uydurma sayı olmadığı), Teklif'in TAM alan kapsamıyla
(taraflar/güzergah/mali kalem, Türkçe ondalık biçimiyle) round-trip ettiği, Yük güncelleme uç noktasının
(çekirdek alanlar + paket upsert + paket silme) gerçek Postgres'e karşı doğru çalıştığı, Sefer-Yük
bağlamanın (BR-006/007 romork tipi eşleşme kuralı dahil) doğru çalıştığı, Fatura kalem eşlemesinin
(+ kalem durumunun alış/satışa göre doğru değiştiği) ve dipnot CRUD'unun doğru çalıştığı, Teklif→Yük
dönüşüm zincirinin BR-002/003/004/005 kurallarının VE (bu güncellemede genişletilen) tam 21 alanlık
`ValidateRequired` listesinin doğrudan servis örneklemesiyle (sahte Siber depoları ile) doğru çalıştığı,
gerçek "Siber-503" davranışının doğru çalıştığı, Teklif dosya yükleme/kaldırmanın (gerçek disk + DB
round-trip) doğru çalıştığı, avatar yükleme/kaldırmanın ve BR-012'nin (mevcut şifre yanlışsa reddetme +
şifrenin gerçekten değiştiği) doğru çalıştığı. Dönüşümün MUTLU YOLU (gerçek Siber'e yazma + 18 alanlık
rezervasyon karşılaştırması + gerçek Yük oluşumu) artık ÇALIŞIYOR ve CANLIDA doğrulandı (bkz. §8) ama
bu spesifik uçtan-uca senaryo henüz OTOMATİK bir xUnit testine dönüştürülmedi — yalnızca ValidateRequired/
MatchesReservation'ın RET yolları otomatik test altında. Kapsanmayan (bilinçli, dürüstçe not edildi):
`SiberImportService`'in kendisi için otomatik test (yalnızca canlı/manuel doğrulandı — bkz. §8), BR-010,
dosya yükleme doğrulama (belge türü/boyut kısıtları). Ayrıntı: TEST-RAPORU.md.

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

- **Teklif** (`QuotesPage.tsx`) — TAMAMLANDI: 6 sekme (Genel Bilgiler/Taraflar/Güzergah/Görevliler/
  Mali Kalemler/Dosyalar), tam oluşturma+düzenleme, çoklu içerik/mali-kalem satırı, `AccountPicker`/
  `UserPicker` ile gerçek cari/kullanıcı arama (gönderici/alıcı/acente/navlun-ödeyen/operasyon-yetkilisi/
  satış-temsilcisi), "Çalışma Şekli" + "Ön/Son Taşıma Tarafımızdan Yapılır" alanları (ikisi de bu
  güncellemede eklendi — kaynakta var, backend zaten destekliyordu, frontend'de hiç yoktu), dosya
  ekleme/kaldırma. Otomatik test: `LoadTests.cs` (tam alan round-trip + Türkçe ondalık ayrıştırma).
  Bu güncellemede AYRICA düzeltildi (bkz. §1 Kritik yön değişikliği #4): Mali Kalemler'deki Alış/Satış
  değerlerinin TERS olması, "Kalem" alanının yanlış tabloyu (`item_type` yerine `financial_item`)
  kullanması, Acente/Navlun Ödeyen Firma/Mali Kalem Cari seçicilerinin `account_type_id`'ye göre hiç
  filtrelenmemesi. DÜRÜST NOT — sekme YAPISI kaynaktan farklı: olsold'un gerçek `OfferFormDrawer.vue`'sunda
  Taraflar/Güzergah ayrı sekme değil, Genel Bilgiler içinde; buradaki alan kapsamı birebir ama gruplama
  farklı. Ayrıca olsold'da olup burada HÂLÂ eklenmeyen: "E-Posta Ayarları" sekmesi (backend `EmailTo`/
  `EmailCc` alanlarını zaten destekliyor, frontend'den hiç gönderilmiyor), "İlgili E-Posta" sekmesi
  (yalnızca teklif AI'dan/mail'den oluştuysa görünür, `saveAi` bu kapsamda zaten YOK — bkz. §4 AI satırı).
- **Yük** (`LoadsPage.tsx`) — TAMAMLANDI: 7 sekmeli (Genel Bilgiler/Paketler/Finans/Görevliler/
  Hareketler/Faturalar/Dosya Arşivi — son 5'i bu güncellemede eklendi, önceden yalnızca ilk 2 vardı ve
  form salt-okunurdu) düzenleme formu; Teklif→Yük dönüşüm tetikleyicisi (Teklifler ekranında `siber_id`
  dolu satırlarda görünen kamyon ikonu → `POST /api/v1/load_transfer`) eklendi. Liste sütunları düzeltildi.
  olsold'un gerçek `LoadFormDrawer.vue`'sunun 8 sekmesiyle birebir — yalnızca koşullu "İlgili E-Posta"
  eksik (yalnızca AI/mail'den oluşan tekliflerde görünür, `saveAi` zaten kapsam dışı — bkz. §4).
  - **Görevliler**: Teklif'ten FARKLI olarak `load_charge_person` ilişki tablosu DEĞİL — `LoadTransfer`
    üzerinde doğrudan iki `int` alan (`customer_representative_name`/`second_customer_representative_name`
    — adlandırma yanıltıcı, içerik kullanıcı kimliği). Dönüşüm sırasında ikisi de hep işlemi yapan
    kullanıcıya sabitleniyordu, hiçbir uç bu alanlara dokunmuyordu; `LoadTransferUpdateRequest`'e eklendi.
  - **Finans**: backend (`LoadTransferUpdateRequest.InvoiceItems` + `UpsertInvoiceItemsAsync`)
    `load_transfer_invoice_item` upsert'ini ZATEN DESTEKLİYORDU, hiç frontend'den gönderilmiyordu. Alış
    (`buysell="1"`)/Satış (`buysell="2"`) iki bölüme ayrılarak gösteriliyor, düzenlenebiliyor, siliniyor.
  - **Hareketler**: backend (`MovementService.LoadMovementsAsync`/`SaveLoadMovementAsync`/
    `DeleteLoadMovementsAsync` + `LoadTransferMovementController`, tam CRUD) ZATEN TAMAMDI, hiç frontend
    bağlanmamıştı. `destination_id` zorunlu ama `destinations` tablosu (olsold'da da) hiç seed edilmiyor
    — Siber kaynaklı değil, tamamen yerel/admin-yönetimli; canlı test için bir Destination oluşturuldu.
    **[Kritik yön değişikliği #5'te düzeltildi]** Backend `deleted_movements`i zaten dönüyordu ama
    frontend hiç okumuyordu — "Silinen Hareketler" görünümü tamamen eksikti, artık eklendi. Ayrıca
    oluşturan kullanıcı yalnızca adıyla değil, kaynaktaki gibi ad+soyad+email ile gösteriliyor.
  - **Faturalar**: salt-okunur çapraz görünüm — `LoadTransferInvoiceMap.LoadTransferId` doğrudan FK
    olduğundan tek join ile yazıldı (şema değişikliği gerekmedi). **[Kritik yön değişikliği #5'te
    düzeltildi]** KDV/tutar alanları önceden "Uyumsoft'a bağlı, hiç portlanmadı" gerekçesiyle bilinçli
    dışlanmıştı — bu YANLIŞTI, sütunlar `Invoice` entity'sinde zaten var ve ana Fatura modülü onları
    zaten gösteriyordu; artık burada da gösteriliyor (kaynağın null→"0,00" davranışıyla birebir).
    "Fatura Ticareti Tipi" alanı da eksikti, eklendi. `box_type` etiketi "Alış/Satış" YANLIŞLIĞINDAN
    kaynağın gerçek "Gelen Fatura/Giden Fatura" etiketine düzeltildi (standalone Fatura modülünde de).
  - **Dosya Arşivi**: Yük'ün kendi dosya tablosu yok (olsold'da da yok) — dönüşümün geldiği ORİJİNAL
    Teklif'in `load_file` kayıtlarını paylaşır (`load_number_work_type` ↔ `loads.load_number` eşlemesiyle
    bulunan `load_id`); zaten var olan `POST /api/v1/load/file/upload` ucu yeniden kullanıldı.
  - Beşi de canlıda uçtan uca doğrulandı (yazma → okuma round-trip, gerçek Docker API'ye karşı — Finans'ta
    bir alan değiştirilip kalıcılığı, Hareketler'de ekle/listele/sil tam döngüsü, Dosya Arşivi'nde
    yüklenen dosyanın HEM Yük HEM orijinal Teklif görünümünde aynı kayıt olduğu teyit edildi). Otomatik
    test: `LoadTransferTests.cs` (güncelleme + paket ekleme/silme) — bu 5 yeni sekme için ayrı otomatik
    regresyon testi henüz yazılmadı, yalnızca canlı doğrulandı.
  - **[EN CİDDİ BULGU, düzeltildi + regresyon testi eklendi]** `LoadTransferDetailDto` (okuma)
    `romork_type_id`/`instruction_id`/`delivery_method_id`/`load_transfer_type_id`/`way_of_working`/
    `front_transportation_by_us`/`final_transportation_by_us`/`departure_country_id`/`target_country_id`/
    paketlerin `case_type_id`'si HİÇ YOKTU — yazma tarafı zaten destekliyordu. Formu AÇIP dokunmadan
    Kaydet'e basmak bu alanları SESSİZCE boşaltıyordu; bu oturumda DAHA ÖNCE yapılan Görevliler/Finans/
    Hareketler testleri sırasında canlı bir kaydın bu alanlarını GERÇEKTEN sıfırladığı DB'de doğrulandı.
    Genel Bilgiler'e ayrıca kaynakta çalışan ama arayüzde hiç olmayan 5 alan eklendi: Yük Tipi, Yük Türü,
    Talimat, Teslimat Şekli (`/api/v1/load_transfer_deliver_method`), Çalışma Şekli, Kalkış/Varış
    Ülkesi. `work_type` bilinçli olarak eklenmedi — kaynağın KENDİ update metodunda yorum satırıyla
    devre dışı (dönüşümde sabitleniyor, kaynak da göstermesine rağmen kaydetmiyor). Tüm 10 alan
    ac+dokunmadan+kaydet ile korundugu canlı teyit edildi (bkz. §1 Kritik yön değişikliği #4). Otomatik
    regresyon testi: `LoadTransferTests.UpdateLoadTransfer_SetsAllPreviouslyMissingReadFields_
    AllRoundTripCorrectly` — tüm 10 alanı set edip GET'te dönüp dönmediğini VE aç-dokunma-tekrar-kaydet
    senaryosunda sıfırlanmadığını doğruluyor. Mutasyon testiyle (bir alanın ataması geçici olarak null'a
    çevrilip testin gerçekten kırıldığı, geri alınca tekrar geçtiği) tautolojik olmadığı kanıtlandı.
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

### Teklif→Yük dönüşümü artık gerçekten çalışıyor (önceki "Siber kimlik eşleşmesi kısıtı" ÇÖZÜLDÜ)

**Önceki durum (artık YANLIŞ, tarihi kayıt olarak bırakıldı):** Bu bölüm önceden, `payment_types.siber_id`
(ve diğer Siber-eşlemeli lookup sütunlarının) hiçbir satırda dolu olmadığını, bunun "gerçek Siber
entegrasyonunun canlı kullanımından zamanla biriken" ve bu yüzden taze bir ortamda YAPISAL OLARAK
telafi edilemeyen bir veri eksikliği olduğunu iddia ediyordu. Bu YANLIŞTI: gerçek kök neden, olsold'un
`TransferDataController`'ının (Front\TransferData) — Siber'in KENDİ referans tablolarından
(`skn_sabittanim`, `sky_kullanici`, ...) yerel tabloya `siber_id` dolduran ETL — bu porta hiç
taşınmamış olmasıydı. Bu "canlı kullanım geçmişi" değil, tek seferlik bir kurulum adımı.

**Şimdiki durum:** `SiberImportService` + `TransferDataController` (bkz. §2) bu ETL'i portladı. Canlı
doğrulama, DOĞRUDAN bir Teklif üzerinde, gerçek buton tıklamalarıyla eşdeğer API çağrılarıyla yapıldı:

1. `POST /api/v1/transfer_data` + 8 `getX` ucu sırayla çağrıldı → hepsi 200, sıfır hata, ikinci
   çalıştırmada `created: 0` (idempotent, mükerrer kayıt YOK — 15 referans tablosu tek tek sayıldı).
2. Bir Teklif'in TÜM zorunlu alanları dolduruldu (müşteri/gönderici/alıcı/departman/ülkeler/görevliler/
   içerik/mali kalem/Çalışma Şekli), durum "Olumlu"ya çekildi.
3. `POST /api/v1/transfer_to_siber` → **200**, gerçek bir `skn_rezervasyon` satırı mock Siber'e yazıldı.
4. `POST /api/v1/load_transfer` → **200**, `"Yük başarıyla oluşturuldu"`. PostgreSQL'de doğrulandı:
   `load_transfers` (doğru toplam ağırlık/hacimle), 1 paket, 2 fatura kalemi (alış+satış çifti) satırı
   oluştu, Teklif'in `load_number`'ı doldu. Mock Siber'de doğrulandı: `skn_yuk`/`skn_yukkoli`/
   `sfy_modulkalem` tablolarına karşılık gelen satırlar eklendi.

**Bu araştırma sırasında bulunup düzeltilen yan hatalar** (`TransferSiberService.cs`/
`LoadTransferWriteService.cs`): `ValidateRequired` olsold'daki ~21 kontrolden yalnızca ~8'ini
içeriyordu (talimat/römork/iş türü/yükleme tipi/tarihler/gönderici/alıcı hiç doğrulanmıyordu — eksik
alanlı bir teklif sessizce Siber'e aktarılabiliyordu); `MatchesReservation` 18 karşılaştırmadan
yalnızca 9'unu yapıyordu; Siber'e yazılan `insuser`, işlemi yapan kullanıcı yerine yanlışlıkla
görevli[0]'ın kodundan okunuyordu; "Çalışma şekli boş olamaz" kontrolü olsold'da hiç tetiklenmeyen
(NOT NULL+default 0 sütun) ölü bir kontrolün birebir-olmayan bir çevirisiydi ve geçerli "Spot" (0)
seçimini yanlışlıkla reddediyordu — hepsi düzeltildi, `TransferSiberTests.cs`'e 2 yeni regresyon testi
eklendi (toplam 71/71 test geçiyor).

**Frontend'de bulunan, aynı zincirin gerçek kullanılabilirliğini engelleyen ayrı bir eksik:** Teklif
formunda ne "Çalışma Şekli" (Spot/Yıllık) alanı ne de "Görevliler" (Operasyon Yetkilisi/Satış
Temsilcisi) sekmesi vardı — olsold'un gerçek `OfferFormDrawer.vue`'sunda ikisi de var, backend
(`LoadWriteModel`/`LoadFormRequest`) ikisini de zaten destekliyordu. Sonuç: arayüzden oluşturulan HER
teklif `way_of_working=0` ile ve HER İKİ görevli slotu da (formu açan) mevcut kullanıcıya sabitlenerek
kaydediliyordu — kullanıcının Siber kodu yoksa (yerel seed admin gibi) o teklif arayüzden asla Yük'e
dönüştürülemezdi. `UserPicker.tsx` (yeni, `AccountPicker`'ın User karşılığı) + `QuotesPage.tsx`'e
"Görevliler" sekmesi ve "Çalışma Şekli" alanı eklendi; canlıda doğrulandı (dönüştürülmüş teklif
açıldığında iki alan da doğru değerlerle geliyor).

### Sefer oluşturma/güncelleme de aynı kökten bloke ediliyordu — artık ÇALIŞIYOR

**Önceki durum (artık YANLIŞ):** Bu bölüm önceden Sefer oluşturmanın (`POST /api/v1/expedition`)
`expedition_types`/`expedition_statuses` tablolarının BOŞ olması yüzünden bu ortamda YAPISAL OLARAK
imkansız olduğunu, gerçek sistemde bu değerlerin "idari panelden elle girilmiş" olacağını iddia
ediyordu. Bu da aynı kök nedene (portlanmamış `TransferDataController`) bağlıydı — bu iki tablo da
Siber'in `skn_sabittanim` (SEFERTUR grubu) referansından geliyor, elle girilen veri değil.

**Şimdiki durum:** `SiberImportService.ImportExpeditionTypesAsync`/`ImportExpeditionStatusesAsync` bu
tabloları doldurdu (2 + 3 satır). Canlıda doğrulandı: `POST /api/v1/expedition` gerçek bir gövdeyle
çağrıldı → **200**, gerçek bir Sefer kaydı oluştu (`id`, `expedition_id`, `sefer_id` dolu). Bağlı
Yükler zaten bundan etkilenmiyordu (bkz. altındaki not) — artık Genel Bilgiler de etkilenmiyor.

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

### Dosya yükleme uçtan uca test edildi — iki gerçek hata bulundu ve düzeltildi

Teklif'in Dosyalar sekmesi canlı Docker'da uçtan uca denendi (yükleme → kaydetme → indirme →
kaldırma → kaydetme): DataTransfer API'siyle tarayıcıda gerçek bir dosya enjekte edilip yüklendi.

1. **Dosya bağlantısı hiç çalışmıyordu.** `QuotesPage.tsx`, indirme linkini `href={f.file}` olarak
   kuruyordu — backend'in döndürdüğü `file` alanı YALNIZCA saklanan dosya adı (ör.
   `8def9...609273.txt`), tam yol değil. Gerçek dosyalar `/storage/{ad}` altında sunuluyor (nginx
   `location ^~ /storage/` → API'nin `app.UseStaticFiles(...RequestPath="/storage")`). Sonuç:
   linke tıklamak dosyayı DEĞİL, React uygulamasının kendi `index.html`'ini döndürüyordu. `href={
   \`/storage/${f.file}\`}` olarak düzeltildi, canlıda gerçek dosya içeriğinin döndüğü doğrulandı.
2. **Dosya kaldırma diskte yetim bırakıyordu.** `LoadWriteService.UpdateAsync`, listeden çıkarılan
   dosyaların veritabanı satırını siliyordu ama FİZİKSEL dosyaya hiç dokunmuyordu — `OLS.Business`
   katmanı `IFileStorage`'a (API katmanı) mimari olarak erişemediği için silme çağrısını yapacak bir
   mekanizma yoktu. Canlıda bir dosya yükleyip kaldırılarak, `docker exec` ile diskte kalan yetim
   dosya doğrulandı. Düzeltme: `LoadWriteService.UpdateAsync` artık `LoadUpdateResult` (id + silinen
   dosya adları) döndürüyor; `LoadController.Update` bu adlar için `IFileStorage.Delete` çağırıyor —
   `LoadFileController.Upload`'daki (ayrı, zaten doğru olan) uçtaki aynı desen. Canlıda hem DB satırının
   hem fiziksel dosyanın gittiği doğrulandı; `LoadTests.UpdateLoad_RemovingAFile_
   DeletesBothDatabaseRowAndPhysicalFile` ile de kilitlendi (gerçek dosya yazıp okuyarak, `File.Exists`
   ile).

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
  kalan sekiz alan kontrolü ve BR-010/013 için hâlâ ayrı test yok. BR-006/007 (Sefer-Yük romork
  tipi eşleşmesi) `ExpeditionLoadMappingTests.cs`, BR-012 (profil şifre değişikliğinde mevcut şifre
  kontrolü) `ProfileTests.cs` ile test edildi.
- Siber-503 davranışı artık test edildi: `POST /api/v1/transfer_to_siber/loadSave`, Siber
  yapılandırılmamışken gerçekten `503 Service Unavailable` döndüğü doğrulandı
  (`TransferSiberTests.LoadSave_WhenSiberNotConfigured_ReturnsServiceUnavailable`).
- Teklif dosya yükleme artık uçtan uca test edildi (yukarıdaki bölüm — iki gerçek hata bulundu ve
  düzeltildi). Kullanıcı avatarı yükleme de artık hem uygulandı hem test edildi (aşağıdaki bölüm).

### Avatar hiçbir yerde gösterilmiyordu — backend zaten doğruydu, frontend hiç kullanmıyordu

Bu oturumda bulundu: `ProfileController`/`IFileStorage` avatar yükleme/kaldırmayı (üç yollu: kaldır/
değiştir/koru) doğru şekilde uyguluyordu, ama frontend'in HİÇBİR yerinde gerçek avatar görseli
gösterilmiyordu — `TopBar.tsx`, `UsersPage.tsx` ve `ProfilePage.tsx`'in hepsi HER ZAMAN isim
baş harflerini gösteriyordu, `avatar` alanını okumuyordu bile. Ayrıca `ProfilePage.tsx`'te avatar
yüklemek/kaldırmak için hiçbir UI yoktu. Üçü de düzeltildi/eklendi:
- `TopBar.tsx` ve `UsersPage.tsx`: gerçek `avatar` doluysa `/storage/{avatar}` gösterir, yoksa
  (kaynaktaki gibi) baş harflere döner.
- `ProfilePage.tsx` "Genel" sekmesi: mevcut avatarı gösterir, "Değiştir" (dosya seçici + anlık
  önizleme) ve "Kaldır" düğmeleri eklendi, `general/update` uç noktasının zaten var olan `avatar`/
  `avatar_remove` alanlarına bağlandı. Kaydettikten sonra `useAuth()`'un artık dışa açılan
  `refresh()`'i çağrılıyor — TopBar'daki avatar SAYFA YENİLEMEDEN güncelleniyor.
Canlıda uçtan uca doğrulandı (gerçek PNG yükleme → üç yerde de görünme → kaldırma → hem DB alanının
hem fiziksel dosyanın gittiği `docker exec` ile teyit edildi). `ProfileTests.cs` ile kilitlendi.

## 9. Güvenlik notları

- Tüm sırlar (JWT anahtarı, DB şifresi, Siber-mock şifresi) `.env`'de, placeholder değerlerle;
  `.env` `.gitignore`'da, yalnızca `.env.example` commit'li.
- Yetki zorlaması backend'de gerçek (401/403), frontend'deki gizleme yalnızca UX — testle doğrulandı
  (bkz. §6).
- Şifreler asla düz metin loglanmaz/saklanmaz; bcrypt maliyeti 12.
- CORS allow-list (`*` değil), rate limiting (auth: 10/dk, public-form: 5/dk) aktif.

## 10. Önerilen sonraki adımlar (öncelik sırasıyla)

1. Kalan 7 modül için 3-viewport görsel kontrolünü tamamlamak (yalnızca Müşteriler tam kontrol edildi).
2. Sefer Genel Bilgiler'i gerçekten kaydedilebilir hâle getirmek için `expedition_statuses`'a en az
   bir gerçek satır eklemek gerekip gerekmediğine karar vermek (bkz. §8 — olsold'da da seeder'ı yok,
   bu yüzden veri UYDURMADAN yapılabilecek bir şey değil; kullanıcıya danışılmalı).
3. `ValidateRequired`'ın kalan sekiz alan kontrolü ve BR-010/013 için ek entegrasyon testleri.
4. "AI'dan teklif" özelliğinin gerekip gerekmediğine karar vermek — gerekiyorsa backend adaptör
  üzerinden, tarayıcıya asla anahtar sızdırmadan.
