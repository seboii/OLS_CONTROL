# Teslim Raporu

Bu belge, `ols-scoped-dotnet` teslimatının güncel, dürüst durumunu özetler. Hiçbir madde "tamamlandı"
olarak işaretlenmeden önce gerçekten çalıştırılıp doğrulanmadan yazılmadı; eksik kalan işler de aynı
titizlikle, somut olarak listelendi.

## 1. Kapsam

**İçeride (9 ekran):** Dashboard, Müşteri (Cari), Teklif, Yük, Sefer, Fatura, Araç, Kullanıcılar, Destek
Talebi — artı bunların ortak altyapısı (auth, yetki modeli, coğrafya/lookup verileri).

**Dışarıda (bilinçli):** olsold'un ayrı Reports/Hedef-ciro YÖNETİM EKRANI (Dashboard'dan FARKLI — bkz.
aşağıda), PDKS, kurum-içi mesajlaşma (Socket.IO/Mongo), Excel yönetimi, muhasebe planı admin ekranları,
gümrük modülleri (transit beyanname/ordino/yetki mektubu), CMS, test/demo sayfaları, ilgisiz cron/job'lar.
Gerekçe: [docs/SECILI-MODUL-PARITE-MATRISI.md](SECILI-MODUL-PARITE-MATRISI.md).

**İstisna (Kritik yön değişikliği #9):** "Hedef-ciro" ile AYNI `user_goal` veri modelini kullanan ama
AYRI bir yer — Kullanıcılar formunun kendi "Hedefler" sekmesi (kaynakta `UserTarget.vue`, kullanıcı
başına aylık satış hedefi CRUD'u) — İÇERİDE: bu, ayrı bir Reports ekranı değil, kapsam-içi Kullanıcılar
modülünün görsel/işlevsel bir parçası. Ayrıntı: SECILI-MODUL-PARITE-MATRISI.md §7 satır 134.

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

Ayrıca bu güncellemede: (a) kullanıcının talebiyle tüm `ols-scoped-dotnet` ağacı olsold'un yapım
şirketinin adı için tarandı (dosya adı + dosya içeriği) — SIFIR eşleşme, port zaten temiz; (b) eksik
olduğu bilinen "E-Posta Ayarları" sekmesi eklendi (Teklif'in Gönderilecek/CC e-posta listesi) — backend
`LoadDetailDto`'ya `email_to`/`email_cc` okuma alanları eklendi (`LoadEmails` tablosu, yazma tarafı
zaten hazırdı), frontend'e yeni `EmailChipInput` bileşeni eklendi; canlı Docker'da ekle→kaydet→yeniden
aç ve sil→kaydet→DB doğrulaması ile tam döngü test edildi, ayrıca bu save'in Görevliler gibi diğer
sekmelerin verisini bozmadığı ayrıca doğrulandı. Ayrıntı: §8 Teklif bölümü.

**Kritik yön değişikliği #7 (bu güncellemede — Sefer'de "Silinen Hareketler" hatası soruşturulurken
TÜM Hareketler sekmesinin hiç var olmadığı ortaya çıktı):** Kullanıcı, Yük'te düzeltilen "Silinen
Hareketler" hatasının (bkz. Kritik yön değişikliği #5) Sefer'de de olup olmadığının doğrulanmasını
istedi — hipotezi, Sefer'in Hareketler sekmesinin zaten var olup yalnızca `deleted_movements`i
okumadığıydı (Yük'teki BİREBİR aynı hata deseni). `grep -rn "movement" frontend/src/pages/trips/
TripsPage.tsx` SIFIR eşleşme verdi: hipotez KISMEN yanlış çıktı — sorun yalnızca okumama değil,
**Hareketler sekmesinin TAMAMEN yokluğuydu** (`DETAIL_TABS = ["Genel Bilgiler", "Bağlı Yükler"]`,
üçüncü sekme hiç yoktu). Kök neden araştırılınca bunun önceden §8'de "bilinçli kapsam dışı: Hareketler
sekmesi (aynı `expedition_statuses` boş-tablo kısıtı)" olarak belgelendiği bulundu — ama bu gerekçenin
KENDİSİ STALE çıktı: Kritik yön değişikliği #3 bu boş-tablo kısıtını "gerçek değil, taşınmamış ETL"
olarak çözüp `expedition_statuses`/`expedition_types`'ı doldurduktan SONRA, §8'deki bu Sefer/Hareketler
notu hiç güncellenmemişti. Backend zaten tamdı (`ExpeditionController.Movements/SaveMovement/
DeleteMovement`, tam CRUD, Kritik yön değişikliği #5'te `MappedUserDto` ile genişletilmiş DTO) —
Yük'ün Hareketler sekmesiyle BİREBİR AYNI kök durum (bkz. §8 Yük/Hareketler notu): sadece frontend hiç
bağlanmamıştı.

`olsold/resources/js/components/Expedition/ExpeditionFormMovements.vue` satır satır okunup kaynağın
gerçek davranışı doğrulandı: Yük'ün hareket kartından TEK farkı, "X numaralı sefer hareketinden
otomatik oluşmuştur" rozetinin YOK olmasıdır (bu rozet yalnızca Yük tarafında anlamlı — Sefer
hareketleri bu rozetin KAYNAĞI, hedefi değil; `LoadFormMovements.vue`'de karşılaştırmalı doğrulandı).
`ExpeditionFormDrawer.vue`'den sekme sırası da netleşti: Bilgiler → Sefer İçeriği (Bağlı Yükler) →
Hareketler — port'taki sıra buna göre düzeltildi (Hareketler üçüncü sekme, "Bağlı Yükler" ikinci
sekme olarak kaldı).

`TripsPage.tsx`'e Yük'ün Hareketler sekmesiyle birebir aynı desen eklendi: hareket listesi, "Yeni
Hareket Ekle" modalı (Durum + Konum zorunlu select, Adres/Açıklama), silme (soft delete), ve
`v-if="deletedMovements.length > 0"` davranışını taşıyan "Silinen Hareketler" butonu + modalı.
Backend'de HİÇBİR değişiklik gerekmedi. `npx tsc -b --force` temiz. Canlı Docker'da uçtan uca
doğrulandı (gerçek Sefer kaydı SEF-2 üzerinde): POST ile hareket oluşturuldu (201, kart doğru render
oldu — "Açık" durumu + "Istanbul Depo" konumu + "Sistem Yöneticisi (admin@ols-scoped.local)" oluşturan
bilgisi), silindi (soft delete — `window.confirm` bu tarayıcı-otomasyonu ortamında native dialog
gösteremediği için silme adımı doğrudan API'ye karşı doğrulandı, DB'de `deleted_at` dolduğu teyit
edildi), sonrasında "Silinen Hareketler" butonu belirdi ve modal kaynakla birebir alanları gösterdi
(durum rozeti, konum, "Oluşturan: ad soyad (email)", "Oluşturulma Tarihi", kırmızı "Silinme Tarihi",
açıklama). **Metodoloji notu:** aynı doğrulamada, tarayıcı paneli compositing yapmıyorken (arka planda/
görünür değilken) framer-motion'ın `AnimatePresence` çıkış animasyonunun `requestAnimationFrame`
throttling'i yüzünden onlarca saniye "takılı" görünebildiği fark edildi (modal DOM'da kalıp
"Kaydediliyor..." gösterebiliyor) — gerçek bir state hatası DEĞİL; `computer.screenshot` ile paneli
gerçekten compositing'e zorlamak veya ağ/DB durumuna bakmak (DOM'a değil) doğru teşhis yöntemi.

**Kritik yön değişikliği #8 (bu güncellemede — Fatura/Müşteri/Araç'ın satır satır alan denetimi):**
Aynı yöntem (§1 Kritik yön değişikliği #4'te kurulan: kaynağın gerçek `*FormDrawer.vue`'sü satır
satır taranıp bu portun karşılığıyla tek tek karşılaştırılması) sırayla Fatura, Müşteri, Araç'a
uygulandı. En ciddi bulgu, Müşteri'de gerçek bir veri kaybı hatasıydı:

**Fatura (`InvoicesPage.tsx` / `InvoiceFormDrawer.vue`, `InvoiceFormInvoiceItems.vue`):**
1. Kalem eşleme seçicisi (`load_transfer_invoice_item` arama) kaynakta `status=pending` +
   `buysell` (fatura `box_type`'ından türetilen) + cari filtreleriyle daraltılıyordu — port hiçbirini
   uygulamıyordu, ZATEN başka bir faturaya bağlanmış veya alış/satış yönü uyuşmayan kalemler de
   listede görünüyor, seçilirse ÇİFT FATURALANDIRMA riski oluşuyordu. Filtreler eklendi; ayrıca
   zaten-eklenmiş kalemler için "Eklendi" rozeti eklendi. Regresyon testi:
   `InvoiceTests.ListInvoiceItems_FiltersByStatusAndBuysell_ExcludesNonMatchingItems`.
2. Detay sekme adları kaynaktan sapmıştı: "Genel Bilgiler"→"Bilgiler", "Dipnotlar"→"Ek Bilgiler"
   (+ içindeki "Maddeler"/"Yeni Madde Ekle" alt başlıkları) — düzeltildi.
3. Kaynağın 3 sekmeli liste yapısı (Gelen/Giden/**Onay Bekleyen** Faturalar) portta yalnızca 2
   sekmeydi (Onay Bekleyen hiç yoktu) — `invoice_status_id` kaynakta hardcoded `7` idi, bu portta
   farklı sırada seed edildiği için doğrudan taşınamazdı; isimden (`invoice_statuses.name ==
   "Onay Bekliyor"`) çözülerek eklendi.

**Müşteri (`CustomersPage.tsx` / `AccountFormDrawer.vue`, 632 satır) — [EN CİDDİ, gerçek veri kaybı]:**
1. **Hesap Türü alanı kaynakta ÇOKLU seçim (`MultiSelect`), portta TEKİL seçimdi**
   (`account_type_mapping: v ? [v] : []`) — bir cari aynı anda birden fazla tipte olabilir (kaynakta
   normal bir durum). Canlı test verisinde GERÇEKTEN çoklu-tipli bir cari vardı ("Test Lojistik A.Ş."
   → Müşteri+Alıcı+Gönderici); bu formu AÇIP dokunmadan Kaydet'e basmak (Yük'teki 9-alan hatasıyla
   AYNI "sessiz veri kaybı" deseni) 3 tipi sessizce 1'e düşürüyordu. Çoklu-seçim chip-toggle UI'sine
   düzeltildi. Regresyon testi: `AccountFormTests.cs` (3 test — ekle, koru, değiştir; "koru" testi
   tam olarak canlıdaki senaryoyu — aç→dokunma→kaydet→tüm tipler kalsın mı — kilitliyor).
2. Görevli (Charge Person) ve Faturalar sekmeleri hiç yoktu — backend zaten destekliyordu
   (`account_charge_person`, `AccountDetailDto.Invoice`), eklendi.
3. Avatar yükleme/gösterme hiç yoktu (diğer modüllerde zaten kurulan desenle eklendi).
4. `tax_office` serbest-metin bir alandı, kaynakta gerçek bir `tax_office_id` lookup'ı — veri
   bütünlüğü sorunu (elle yazılan metin normalize edilmiyordu), `SelectInput`'a çevrildi.

**Araç (`VehiclesPage.tsx` / `car/form.vue`):**
1. "Kiralanan Firma" (`customer_id`) alanı formda hiç yoktu — backend destekliyordu, eklendi.
2. `cars.customer_id` yerel Account id'si DEĞİL, cari'nin Siber id'sini tutuyor
   (`CarService.SingleAsync`'in `Accounts.Where(a => a.SiberId == c.CustomerId)` eşleşmesinden
   görülüyor) — `AccountOption`/`AccountListItemDto`'ya (önceden yalnızca detay DTO'sunda olan)
   `siber_id` eklenerek `AccountPicker` bu id'yi doğru gönderecek şekilde genişletildi. Regresyon
   testi: `CarTests.CreateCar_WithCustomerSiberId_ResolvesBackToTheSameAccountOnRead`.

Tüm bulgular canlı Docker'a karşı yazma→okuma round-trip'le doğrulandı. Commit'ler: Fatura
(`306952f`, `5b04b9f`), Müşteri (`4434e29`), Araç (`60e54c8`).

**Kritik yön değişikliği #9 (bu güncellemede — Kullanıcılar'ın satır satır alan denetimi + "Hedefler"
sekmesi kapsam çelişkisinin çözümü):** Aynı yöntem `UserFormDrawer.vue`'ye (301 satır) uygulandı.

**Bulunan alan eksikleri** (backend zaten destekliyordu, frontend hiç kullanmıyordu):
1. Profil fotoğrafı yükleme/gösterme/kaldırma hiç yoktu.
2. "Ülke Kodu" (`phone_country_id`) ve "PDKS Numarası" (`pkds_id`) alanları formda yoktu; ayrıca
   liste satırı bu alanları taşımadığından düzenleme açılışında ayrı bir `GET /api/v1/user/{id}`
   çağrısıyla hidratlanmaları gerekiyordu — o da yoktu.
3. `UserRole.vue`'nin "Tümünü Seç" sütun başlığı (bir sütun için tek tıkla kullanıcının TÜM sayfa
   yetkilerini toplu güncelleme) yoktu — backend (`RoleController`) bunu zaten destekliyordu
   (`permission_page_id` gönderilmezse `user_id`'ye ait tüm satırlar güncellenir).
Üçü de eklendi; canlı Docker'da uçtan uca doğrulandı (avatar round-trip, PDKS/ülke kodu
kaydet→yeniden aç, Tümünü Seç'in DB'de 23/23 satırı güncellediği hem DB hem DOM'dan teyit edildi).
Regresyon testi: `UserFormTests.cs` (2 test, ikisi de mutasyon testiyle doğrulandı). Commit: `8dafc3a`.

**"Hedefler" sekmesi — kapsam çelişkisi bulundu ve kullanıcıya soruldu:** `UserFormDrawer.vue`'nin
kaynakta ayrı bir sekmesi daha var — `UserTarget.vue` (223 satır, `api/v1/user_goal`): kullanıcı
başına aylık satış hedefi (tutar + tarih aralığı) ekleme/düzenleme/silme. Bu portta HİÇ yoktu.
Projenin kendi belgeleri bu konuda birbiriyle ÇELİŞİYORDU:
- `SECILI-MODUL-PARITE-MATRISI.md` §7 satır 134 (erken planlama, FAZ 1/2): "İstisnai kapsam-içi
  bağımlılık: 'hedef takibi' genel olarak kapsam dışı ama bu sekme UserFormDrawer'ın görsel/işlevsel
  parçası olduğu için taşınacak (aksi halde form eksik görünür)."
- `DependencyInjection.cs`'in özet yorumu: "Goals"u genel dışlanmışlar listesine dahil ediyordu —
  ama bu YANLIŞ bir genellemeydi: §0 genel kararlar tablosunda "Goals" hiç geçmiyor, atıf verdiği
  §7 satır 134'ün tam tersini söylüyor.

Bu belirsizlik kod okumasıyla çözülemeyecek türden bir kapsam kararıydı — kullanıcıya soruldu,
eklenmesi onaylandı. Backend: `UserGoal` entity + migration (`user_goals`), `UserGoalService`
(CRUD + kaynağın tarih-aralığı çakışma kuralı — "Bu tarih aralığında zaten bir kayıt bulunmaktadır."
— birebir), `UserGoalController` (`/api/v1/user_goal`). Kaynakta `delete()`'in yetki kontrolü YORUM
SATIRINDAYDI (üstelik yanlış slug'la, `transport_type_management`) — fiilen herkese açıktı; burada
`user_management` altında gerçek CRUD yetkisi uygulanıyor (formun geri kalanıyla aynı sayfa).
Kaynağın `all()` yanıtındaki `total_price_sum` alanı (o kullanıcının Teklif-durumundaki satış
kalemlerinin toplamı) `UserTarget.vue` tarafından hiç render edilmediği doğrulandı (ölü alan) —
taşınmadı. Frontend: "Hedefler" sekmesi — hedef kartları, "Yeni Hedef Ekle" modalı (ay seçici →
ayın 1'i/son günü, kaynağın `setNewDate`'iyle birebir), Düzenle/Sil. Canlı Docker'da uçtan uca
doğrulandı: ekleme, çakışan tarih aralığının kaynağın kendi hata metniyle reddi, düzenleme, silme.
Regresyon testi: `UserGoalTests.cs` (6 test, çakışma kontrolü mutasyon testiyle doğrulandı).
Commit: `68a7abf`. §1'deki kapsam listesi ve `DependencyInjection.cs`'in yorumu bu kararı
yansıtacak şekilde güncellendi.

**Kritik yön değişikliği #10 (bu güncellemede — Destek Talebi'nin satır satır alan denetimi):**
Aynı yöntem `FormsTable.vue` (96 satır) + `FormDetailDrawer.vue`'ye (152 satır) uygulandı (bkz.
SECILI-MODUL-PARITE-MATRISI.md §8 — Destek Talebi = Website Contact Form, ayrı bir ticket modülü
olsold'da hiç yok).
1. Liste kolonları kaynaktan sapmıştı: port kaynakta olmayan "Talep No"/"Mesaj" kolonları
   eklemişti, kaynağın "Kullanıcı/Tarih/Telefon/E-Posta/Okunma Durumu/Yanıtlanma Durumu" kolon
   sırasını birebir yansıtmıyordu. "Durum" rozeti kaynakta HİÇ geçmeyen "Çözüldü"/"Açık" metnini
   kullanıyordu — hem kaynakla hem bu sayfanın KENDİ detay panelindeki "Yanıtlandı"/"Yanıtlanmadı"
   metniyle tutarsızdı. Tümü kaynağa birebir düzeltildi.
2. Detay panelinde gönderim tarihi (`created_at`) alanı hiç gösterilmiyordu — kaynakta bu alan VAR
   (kopyala-yapıştır hatasıyla yanlışlıkla "Telefon Numarası" diye ETİKETLENMİŞ olsa da, veri
   kaynakta gerçekten gösteriliyor). Alan eklendi, ama kaynağın etiket HATASI değil verisi taşındı
   (doğru "Tarih" etiketiyle).
3. `FormsTable.vue`'nin arama kutusu kaynakta GÖRSEL AMA İŞLEVSİZDİ — `ContactFormController::index`
   hiçbir istek parametresi okumuyordu (kutuya yazmak hiçbir şeyi filtrelemiyordu). Bilinçli olarak
   gerçek arama eklendi (ad/soyad/e-posta/telefon/mesaj, LIKE joker karakteri kaçırmalı).
Kaynağın admin uçlarının (index/show/updateAnsweredStatus) TAMAMEN anonim olması (gerçek bir
güvenlik açığı, SEC-003) zaten önceki bir FAZ'da `[RequiresPermission]` ile kapatılmıştı — bu
oturumda yalnızca teyit edildi, dokunulmadı. Regresyon testi: `ContactFormTests.cs` (5 test — anonim
gönderim, okundu yan etkisi, yanıtlanma toggle, arama filtresi mutasyon testiyle doğrulanmış, ve
kimliksiz erişimin 401 döndüğü). Commit: `4545dc1`.

**Kritik yön değişikliği #11 (bu güncellemede — FAZ 5: sistematik güvenlik taraması):** Yukarıdaki
modül denetimleri sırasında bulunan güvenlik düzeltmeleri (SEC-003, RoleController'ın gerçek 403'ü,
UserGoal'ın gerçek yetkisi) OPORTUNİSTİKTİ — modül denetimi sırasında rastlanan sorunlardı, sistematik
bir tarama değildi. Bu güncellemede API'deki TÜM 21 controller'ın yetkilendirme kapsamı tek tek
kontrol edildi:
- 8 modülün tüm CRUD uçları + 23+ referans/tanım modülünün paylaşımlı `LookupControllerBase<T>`'i
  (attribute yerine gövde-içi `HasPermissionAsync` deseni kullanıyor — ilk bakışta eksik görünüp
  doğrulamayla GERÇEKTEN doğru bulundu) dahil, her yazma ucunun gerçek bir yetki kontrolü olduğu
  teyit edildi. `car_management` slug'ının seed edilmediğine dair SECILI-MODUL-PARITE-MATRISI.md
  notu STALE çıktı — `DbSeeder.cs`'de zaten var.
- `[Authorize]` var ama `[RequiresPermission]` YOK görünen 3 uç (`PermissionPageController.Save`,
  `OfferEmailController.Save`, `LoadFileController.Upload`) tek tek olsold kaynağıyla karşılaştırıldı:
  üçü de kaynakta ZATEN yalnızca `auth:api` ile korunuyor, hiçbir `RoleHelper::permission` çağrısı yok.
- Dosya yükleme (`FileStorage.cs`): uzantı beyaz listesi, boyut sınırı, HER ZAMAN yeniden üretilen
  rastgele dosya adı (orijinal ad asla yol olarak kullanılmıyor), hem kaydetme hem silmede yol
  gezinmesi koruması (`Path.GetFileName`) — kaynaktan daha güvenli, ek düzeltme gerekmedi.
- Rate limiting (`auth`: 10/dk, `public-form`: 5/dk) doğru kapsamda: uygulamadaki YEGANE iki gerçek
  anonim uç (giriş, iletişim formu) — geri kalan her uç zaten `[Authorize]` gerektiriyor.
- JWT anahtarı yalnızca yapılandırmadan okunuyor (sabit-kodlanmış yedek YOK, eksikse başlangıçta
  hata verir), üretim `appsettings.json`'ında sır yok, CORS `*` değil iki yerel origin'e kısıtlı.

Sonuç: sistematik taramada YENİ bir güvenlik açığı bulunmadı — her şüpheli nokta ya zaten
düzeltilmiş ya da kaynakla bilinçli birebir olduğu doğrulandı.

**Düzeltme (yukarıdaki #11'in kendisinde — docs/TESLIM-RAPORU.md'yi docs/API-PARITE-MATRISI.md ile
uzlaştırırken bulundu):** "üçü de birebir korunuyor, düzeltme gerekmedi" ifadesi `PermissionPageController.
Save` için YANLIŞTI. Kaynakla birebir olmak DOĞRU tespitti ama `API-PARITE-MATRISI.md` bu uç için ayrıca
"en azından `role_management`(create) yetkisi eklenecek" diye bir plan içeriyordu — bu uç, yeni bir yetki
sayfası oluşturduğunda TÜM mevcut kullanıcılara o sayfada dört hakkı da otomatik veren bir yan etki
taşıyor; "kaynakla aynı" olmak bu durumda yeterli gerekçe değildi. Plan uygulandı:
`[RequiresPermission(Create,"role_management")]` eklendi, canlı Docker'da hem yetkili (admin, 200) hem
yetkisiz (taze sıfır-yetkili kullanıcı, 403) uçtan uca doğrulandı. `PermissionEnforcementTests.cs`'e 2
yeni test eklendi (mutasyon testiyle doğrulandı) — toplam 95/95 test geçiyor.

**Kritik yön değişikliği #12 (bu güncellemede — backend iş kuralı/doğrulama tamlığı denetimi):**
Kullanıcı isteğiyle ("orijinal kodtan geride olan yerler var mı") her modülün olsold FormRequest
kurallarıyla HEDEF'in servis/controller katmanı satır satır karşılaştırıldı. En ciddi bulgular sırasıyla
düzeltildi:
- **Teklif — `load_number` atandıktan sonra düzenleme/silme kilidi yoktu:** olsold, bir Teklif Yük'e
  dönüştükten sonra `load_number` alanının doldurulduğunu ve BİR DAHA değiştirilemeyeceğini varsayıyordu
  ama HEDEF'te bu kilit hiç portlanmamıştı — dönüştürülmüş bir Teklif hâlâ serbestçe güncellenebiliyor/
  silinebiliyordu (paket/mali kalem satırları dahil). `LoadWriteService.UpdateAsync/DeleteContentsAsync/
  DeleteFinancialItemsAsync` ve `LoadService.UpdateTimeOutAsync`'e kilit eklendi (tüm hedef satırlar
  SİLMEDEN ÖNCE toplu kontrol edilir — olsold'un "sil, sonra hata al, yarısı commit'lenmiş kalsın"
  hatasını tekrarlamadan). 3 yeni test (`LoadTests.cs`), biri mutasyon testiyle doğrulandı.
- **Araç — `plate_number` benzersizliği hiç uygulanmıyordu, 9 alan da zorunlu değildi:** olsold'un
  `CarSave`/`CarUpdate` FormRequest'leri `plate_number` (zorunlu+benzersiz) yanında `car_type/
  romork_type/vehicle_owner/vehicle_status/customer_id/km/width/length/height/capacity`'yi de zorunlu
  kılıyordu; HEDEF'te SADECE `plate_number` boş-kontrolü vardı, geri kalan 9 alan tamamen isteğe bağlı
  kabul ediliyordu ve iki araç aynı plakayla kaydedilebiliyordu. `CarService`'e `CarSaveResult` deseni
  (Account/Load'daki desenle birebir) eklendi, `CarController.Validate()` 10 alanı da kaynağın Türkçe
  mesajlarıyla kontrol ediyor. **Not:** olsold'un `cars` tablo migration'ında `plate_number` için DB
  seviyesinde unique KISITI YOK — benzersizlik sadece FormRequest katmanında; HEDEF bunu birebir taşıdı
  (DB index eklenmedi, yalnızca servis katmanı kontrolü — gereksiz migration/veri sıfırlama döngüsünden
  kaçınıldı). Frontend (`VehiclesPage.tsx`) de aynı anda düzeltildi: 9 alanın hiçbirinde `required`
  işareti ya da hata gösterimi yoktu — backend artık reddettiği için bu, kullanıcıya SEBEPSİZ başarısız
  bir "Kaydet" gibi görünürdü. 4 yeni test (`CarTests.cs`), biri mutasyon testiyle doğrulandı.
- **Kullanıcılar — `phone` hiç zorunlu/benzersiz değildi, `phone_country_id` oluşturmada zorunlu
  değildi:** olsold `UserSave`: `phone: required|unique`, `phone_country_id: required`; `UserUpdate`:
  `phone: required|unique` (kendisi hariç) ama `phone_country_id` hiç doğrulanmıyor — HEDEF hiçbirini
  uygulamıyordu. `UserService.PhoneExistsAsync` eklendi (normalize edilmiş — yalnızca rakam — değerle
  karşılaştırıyor; olsold'un HAM değerle karşılaştırıp normalize edilmiş değeri kaydetmesinden doğan
  gerçek-yinelenen-kaçırma hatasını TEKRARLAMIYOR, bilinçli bir iyileştirme). `UserController.
  ValidateAsync`'e `requirePhoneCountryId` parametresi eklenerek asimetri (yalnızca oluşturmada zorunlu)
  birebir taşındı. Aynı sebeple frontend (`UsersPage.tsx`) de düzeltildi: Ülke Kodu/Telefon alanlarında
  ne `required` ne hata gösterimi vardı. Bu değişiklik, `phone`/`phone_country_id` göndermeyen 2 paylaşılan
  test yardımcısını (`TestUserHelper.CreateUserAsync`, 6 farklı test dosyasından çağrılıyor) ve 2 doğrudan
  Araç-oluşturma çağrısını (`ExpeditionLoadMappingTests.cs`, `PermissionEnforcementTests.cs`) kırdı — hepsi
  gerçek (seed edilmiş) lookup id'leriyle dolduracak şekilde güncellendi (`TestCarHelper.cs` yeni eklendi).
  3 yeni test (`UserFormTests.cs`), biri mutasyon testiyle doğrulandı.
- Her iki düzeltme de önce yerel `dotnet test`'te (in-process `WebApplicationFactory`) yeşil çıktı, SONRA
  canlı Docker'da (`docker compose build api && docker compose up -d api`) yeniden test edildi — ilk canlı
  denemede Kullanıcılar formu telefon olmadan BAŞARIYLA kaydetti, çünkü Docker imajı hâlâ ESKİ derlemeyi
  çalıştırıyordu (`dotnet test` kendi in-process host'unu kullandığı için bunu yakalayamaz). İmaj yeniden
  derlenip yeniden başlatıldıktan sonra (Postgres named volume'e dokunulmadan, veri kaybı yok) hem Araç hem
  Kullanıcılar formunda tüm 10/2 zorunlu alan hatası tarayıcıda uçtan uca doğrulandı.
- **Ayrıca bulunan, henüz düzeltilmemiş kalan bulgular** (görev listesinde takip ediliyor): Teklif'in
  `load_content.*` satır bazlı zorunlu alanları + `status_type_id==5` koşullu mali kalem bloğu; Sefer'in
  tarih-sırası kontrolü + `expedition_status_id==8` koşullu zorunlu alanları; Müşteri'nin `country_id`/
  `discount` zorunluluğu + güncellemede `name` zorunluluğu; Kullanıcılar'ın `password_confirmation` eşleşme
  kontrolü (hem backend hem frontend'de alan hiç yok — bilinçli olarak ayrı bir işe bırakıldı, çünkü tek
  başına backend eklemek mevcut formu kırar). Bunların hiçbiri veri kaybına yol açan bir güvenlik açığı
  değil — sadece kaynağın reddettiği bazı geçersiz girdilerin HEDEF'te sessizce kabul edilmesi.

**Kritik yön değişikliği #13 (bu güncellemede — Kritik yön değişikliği #12'nin devamı, Müşteri):**
olsold `FrontAccountController\RequestSave`/`RequestUpdate`: `name`, `country_id`, `discount` üçü de
her iki uçta da zorunlu (aynı kurallar). HEDEF'te `Save` sadece `name`'i, `Update` ise sadece `id`'yi
kontrol ediyordu — `country_id`/`discount` hiç doğrulanmıyordu ve **Update'te `name` bile zorunlu
değildi** (boş isimle güncelleme geçiyordu). `AccountFormRequest.Discount`'u `int?`'e çevirmek gerekti
(önceden non-nullable `int` idi — "hiç gönderilmedi" ile "0 gönderildi" ayırt edilemiyordu; olsold'da
`discount=0` `required` kuralını geçer, yalnızca alan tamamen eksikse reddedilir). `AccountController`'a
Car/User'daki desenle aynı `Validate()` eklendi, her iki uca da bağlandı. Frontend (`CustomersPage.tsx`):
Ülke ve İndirim Tutarı alanlarında `required`/hata gösterimi yoktu, eklendi (Hesap Adı zaten vardı).
Bu değişiklik, hesap oluşturan 6 test dosyasındaki (`CarTests`, `AccountFormTests`,
`AccountVisibilityTests`, `InvoiceTests`, `LoadTests`, `LoadTransferTests`) TÜM minimal-hesap-formu
çağrılarını (9 çağrı noktası) kırdı — hepsi yeni `TestAccountHelper.cs` ile güncellendi. Canlı Docker'da
(imaj yeniden derlenip) hem `country_id` reddi hem `discount=0`'ın geçerli sayılması uçtan uca
doğrulandı. 3 yeni test (`AccountFormTests.cs`), biri mutasyon testiyle doğrulandı.

**Kritik yön değişikliği #14 (bu güncellemede — Kritik yön değişikliği #12'nin devamı, Teklif):**
En büyük ve en riskli doğrulama boşluğu: olsold `LoadSave`/`LoadUpdate`, `load_content.*` (paket satırı)
için 9 alanı (`product_type_id/case_type_id/quantity/width/height/length/gross_weight/lademeter/
stackable`) satır satır zorunlu kılıyordu — HEDEF'te yalnızca dizinin BOŞ OLMADIĞI kontrol ediliyordu,
satır içi alanların hiçbiri doğrulanmıyordu. Ayrıca `status_type_id == 5` ("Olumlu") olduğunda devreye
giren KOŞULLU blok (güzergah/taraf/römork/çalışma-şekli/talimat + tüm `load_financial_item.*` satır
alanları) HİÇ portlanmamıştı. `LoadController.Validate()` genişletildi: `load_content` satırları artık
indeksli anahtarlarla (`load_content.{i}.field`, kaynağın Laravel wildcard-kural JSON şekliyle birebir)
doğrulanıyor; `status_type_id == 5` iken 8 ek alan + `load_financial_item` dizisi + onun 8 satır alanı da
aynı şekilde zorunlu. **Kaynağın tuhaf ama doğrulanmış bir davranışı birebir taşındı:** herhangi bir mali
kalemde `net_price == 0` ise, açıklama kuralı Laravel'in joker karakter (`*`) semantiği yüzünden YALNIZCA
o satıra değil, dizideki TÜM satırlara uygulanıyor — bu proje kodda `TurkishDecimal.Parse(...) == 0`
kontrolüyle ve canlı testle doğrulandı. `LoadFormRequest.WayOfWorking` `int?`'e çevrildi (Discount/plaka
ile aynı gerekçe — "0 (Spot)" geçerli bir seçim, "hiç gönderilmedi"den ayırt edilemiyordu). Frontend
(`QuotesPage.tsx`): 5 tekil koşullu alana (`Yük/Taşıma Tipi`, `Talimat`, `Römork Tipi`, `Çıkış/Varış
Ülkesi`, `Gönderici`/`Alıcı`) VE 17 satır-içi alana (9 `load_content` + 8 `load_financial_item`, indeksli
hata anahtarlarıyla `errors[\`load_content.${i}.alan\`]` deseninde) `required`/hata gösterimi eklendi —
`way_of_working` zaten önceki bir oturumda eklenmişti, aynı (koşulsuz-göster) yaklaşım izlendi. Bu
değişiklik `LoadTests.cs`'teki paylaşılan `RequiredFieldsForm` yardımcısını VE bir round-trip testini
kırdı (satır içi alanlar eksikti) — düzeltildi. Canlı Docker'da (imaj yeniden derlenip) hem durum≠5'te
koşullu alanların İSTENMEDİĞİ hem durum=5'te İSTENDİĞİ uçtan uca doğrulandı. 3 yeni test (`LoadTests.cs`),
ikisi mutasyon testiyle doğrulandı — 111/111 test geçiyor.

**Kritik yön değişikliği #15 (bu güncellemede — Kritik yön değişikliği #12'nin devamı, Sefer):**
olsold `expeditionSave`/`expeditionUpdate`: `romork_id/expedition_type/work_type/department_id` her
iki uçta da zorunlu; `expedition_status_id` yalnızca Update'te zorunlu; `expedition_status_id == 8`
ise `car_exit_date/release_date/return_date/loading_date` + `start_city_id/load_city_id/end_city_id`
de zorunlu olur; `return_date`/`loading_date` durumdan BAĞIMSIZ olarak `release_date`'ten küçük
olamaz. HEDEF'te `ExpeditionController.Save`/`Update` HİÇBİR alan doğrulaması yapmıyordu — bu, bu
oturumda bulunan en büyük "sıfır doğrulama" boşluğuydu. `Validate()` eklendi, kaynağın mesajlarıyla
(bazı yerlerde kaynağın KENDİ tutarsız yazımları — "Deparman" yazım hatası, çift boşluklu "Romork"
mesajı gibi — normalize edilerek) birebir. Frontend (`TripsPage.tsx`): "İş Tipi"/"Departman"/"Sefer
Tipi" alanlarında `required` işareti VARDI ama `error` bağlama HİÇ yoktu (sessiz başarısızlık riski),
düzeltildi; `car_exit_date` alanı formda TAMAMEN YOKTU, eklendi; `start_city_id`/`load_city_id`/
`end_city_id` şehir seçicileri formda TAMAMEN YOKTU (backend zaten kabul ediyordu — klasik
"backend hazır, frontend eksik" deseni), üçü de `/api/v1/city` lookup'ıyla eklendi. **Test altyapısı
notu:** hem `expedition_types` hem `cities` hem `expedition_statuses` tabloları — olsold'da da hiçbir
seed/migration INSERT'i olmadığından — hem kaynakta hem hedefte gerçek dağıtımda boş başlar (yalnızca
admin ekranından doldurulur); testler bu satırları doğrudan DbContext ile (durum=8 için ham SQL,
"generated by default as identity" sütununa açık ID vererek) kendi kurdu. Ayrıca `POST /api/v1/
expedition`'ın gerçek Siber'e yazması nedeniyle (test ortamında bilinçli olarak yapılandırılmamış)
Update testleri, `ExpeditionLoadMappingTests.cs`'nin zaten kullandığı doğrudan DbContext-seed desenini
izledi. Yeni `ExpeditionTests.cs` — 5 test, ikisi mutasyon testiyle doğrulandı — 116/116 test geçiyor.

**Kritik yön değişikliği #16 (bu güncellemede — Yük, Paketler/Finans alan tamlığı):** İki ayrı
küçük ama gerçek "backend hazır, frontend eksik" bulgusu. (1) Paketler sekmesi: `width/length/
height/stackable` yazma tarafı (`LoadTransferUpdateService.PackageInput`) VE okuma DTO'su
(`LoadTransferPackageDto`) zaten destekliyordu, form bu 4 alanı hiç RENDER etmiyordu — 3 satırlık
`<FormField>` eklemesiydi. (2) Finans sekmesi: `LoadTransferInvoiceItem.Status` ("pending/
invoice_received/invoice_issued") okuma DTO'sunda ZATEN dönüyordu ama backend'in yazma tarafı
(`UpsertInvoiceItemsAsync`) her satırı SABİT `"pending"` yazıyordu — olsold `$item['status'] ??
'pending'` gönderileni kabul ediyor. `InvoiceItemInput.Status` eklendi, `UpsertInvoiceItemsAsync`
kaynakla birebir hâle getirildi. Frontend: `Select` alanı eklendi, kaynağın `financial_item_status_
type` filtre kuralı (buysell=Alış ise "Faturası Kesildi" hariç, aksi hâlde "Faturası Geldi" hariç)
birebir taşındı. **Test altyapısı notu:** `LoadTransferTests.cs`'nin paylaşılan `SeedLoadTransferAsync`
yardımcısı `load_number_work_type`'ı hiç set etmiyordu — finans kalemleri bu alan üzerinden (metin
eşleşmesiyle) ilişkilendirildiğinden, aynı anda çalışan iki test aynı "boş" gruba düşüp birbirinin
kaydını okuyordu; bu da düzeltildi (benzersiz değer). 2 yeni test, biri mutasyon testiyle
doğrulandı — 118/118 test geçiyor.

**Kritik yön değişikliği #17 (bu güncellemede — Giriş sayfası, saf frontend):** olsold
`GuestLogin.vue` ile satır satır karşılaştırıldı, 5 gerçek fark bulundu: (1) boş alan client-side
koruması yoktu (`Lütfen tüm alanları doldurun.` — kaynakta API çağrısından ÖNCE kontrol ediliyor);
(2) şifre göster/gizle düğmesi yoktu (kaynak PrimeVue `Password toggleMask` kullanıyor); (3) etiket
kaymaları: başlık "Hoş geldiniz"→"Giriş Yap", alt başlık "Hesabınıza giriş yapın"→"Giriş
bilgilerinizi eksiksiz doldurunuz.", alan etiketi "Parola"→"Şifre"; (4) **bilinçli bir güvenlik
davranışı birebir değildi:** kaynak, doğrulama/401/ağ hatası fark etmeksizin HER giriş
başarısızlığında TEK genel mesaj gösteriyor ("Giriş bilgileri hatalı.") — hangi alanın (e-posta mı
şifre mi) yanlış olduğunu bilinçli olarak açığa çıkarmıyor (kullanıcı numaralandırma saldırılarına
karşı standart bir önlem); HEDEF alan bazlı hatalar + hata tipine göre farklı mesajlar gösteriyordu,
bu daha "yardımcı" ama kaynağın güvenlik duruşunu zayıflatıyordu — birebir uyumlu hâle getirildi;
(5) oturum çerezi süresi 8 gün yerine 7 gündü (`Cookies.set(..., { expires: 7 })`). Tümü
`LoginPage.tsx`/`api.ts`'te düzeltildi, canlı Docker'da (boş form, hatalı bilgiler, doğru bilgiler,
göster/gizle düğmesi) uçtan uca doğrulandı. Backend'e dokunulmadığından mevcut 118 test etkilenmedi.

## 2. Tamamlanan iş (gerçekten çalışır, doğrulanmış)

- **Backend:** 3 katman (`OLS.API`→`OLS.Business`→`OLS.DataAccess`), 59 tablo, EF Core/Npgsql +
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
- **118 otomatik test, hepsi geçiyor** (89 entegrasyon + 29 birim) — gerçek Postgres'e karşı, gerçek HTTP
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
dotnet test                                     → 118/118 geçti (29 birim + 89 entegrasyon), ~1.3 dk
docker compose up -d --build                    → 4 servis (postgres/siber-mock/api/frontend) sağlıklı
curl -X POST .../api/v1/login (admin)           → 200, gerçek JWT
GET /api/v1/account (Docker API, canlı)         → 200, gerçek cari listesi
SELECT COUNT(*) (dev veritabanı, temizlik sonrası) → 1 kullanıcı, 1 cari, 1 araç — hepsi gerçek
```

Tüm komutların tam çıktıları ve context'i: [docs/TEST-RAPORU.md](TEST-RAPORU.md).

## 6. Test durumu (özet)

118/118 otomatik test geçiyor (29 OLS.Business.Tests + 89 OLS.API.IntegrationTests). Kapsanan: auth
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

**Bu güncellemede eklenen testler** (§1 Kritik yön değişikliği #8-#11): `AccountFormTests.cs`
(Müşteri'nin çoklu Hesap Türü koruma/değiştirme senaryosu), `CarTests.cs` (Araç'ın Siber id
eşleşmesi), `UserFormTests.cs` (avatar/PDKS/ülke kodu round-trip + Tümünü Seç'in yalnızca hedeflenen
sütunu güncellediği), `UserGoalTests.cs` (Hedefler CRUD + tarih-aralığı çakışma kuralı, 6 test),
`ContactFormTests.cs` (Destek Talebi CRUD + arama filtresi + kimliksiz erişim reddi, 5 test) —
hepsi en az bir mutasyon testiyle (bulguyu geçici olarak geri alıp testin gerçekten kırıldığını,
düzeltmeyi geri koyunca tekrar geçtiğini kanıtlayarak) doğrulandı.

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
  ekleme/kaldırma. Kritik yön değişikliği #14'te ayrıca: `load_content`/`load_financial_item` satır
  bazlı zorunlu alanları + `status_type_id==5` koşullu bloğu (güzergah/taraf/römork/mali kalem) backend'e
  VE forma eklendi; #18'de `load_number` sonrası düzenleme/silme kilidi eklendi. Otomatik test:
  `LoadTests.cs` (tam alan round-trip + Türkçe ondalık ayrıştırma + satır/koşullu doğrulama, 9 test).
  Bu güncellemede AYRICA düzeltildi (bkz. §1 Kritik yön değişikliği #4): Mali Kalemler'deki Alış/Satış
  değerlerinin TERS olması, "Kalem" alanının yanlış tabloyu (`item_type` yerine `financial_item`)
  kullanması, Acente/Navlun Ödeyen Firma/Mali Kalem Cari seçicilerinin `account_type_id`'ye göre hiç
  filtrelenmemesi. DÜRÜST NOT — sekme YAPISI kaynaktan farklı: olsold'un gerçek `OfferFormDrawer.vue`'sunda
  Taraflar/Güzergah ayrı sekme değil, Genel Bilgiler içinde; buradaki alan kapsamı birebir ama gruplama
  farklı. **[Kritik yön değişikliği #6'da eklendi]** "E-Posta Ayarları" sekmesi artık var: backend
  `LoadDetailDto`'ya `email_to`/`email_cc` (okuma, `LoadEmails` tablosundan) eklendi — yazma tarafı
  (`EmailTo`/`EmailCc`) zaten destekliyordu, yalnızca frontend'den hiç gönderilmiyordu ve okuma tarafı
  hiç yoktu. Frontend'de yeni `EmailChipInput` bileşeni (yaz+Ekle+silinebilir chip listesi) kaynağın
  `AutoComplete multiple` serbest-metin-çoğul davranışına işlevsel eşdeğer. Canlı Docker'da ekle→
  kaydet→yeniden aç ve sil→kaydet→DB'de 0 satır ile tam döngü doğrulandı. Hâlâ eklenmeyen tek şey:
  "İlgili E-Posta" sekmesi (yalnızca teklif AI'dan/mail'den oluştuysa görünür, `saveAi` bu kapsamda
  zaten YOK — bkz. §4 AI satırı) — bilinçli olarak dışarıda, işlevsel bir karşılığı olmadığından.
- **Yük** (`LoadsPage.tsx`) — TAMAMLANDI: 7 sekmeli (Genel Bilgiler/Paketler/Finans/Görevliler/
  Hareketler/Faturalar/Dosya Arşivi — son 5'i bu güncellemede eklendi, önceden yalnızca ilk 2 vardı ve
  form salt-okunurdu) düzenleme formu; Teklif→Yük dönüşüm tetikleyicisi (Teklifler ekranında `siber_id`
  dolu satırlarda görünen kamyon ikonu → `POST /api/v1/load_transfer`) eklendi. Liste sütunları düzeltildi.
  olsold'un gerçek `LoadFormDrawer.vue`'sunun 8 sekmesiyle birebir — yalnızca koşullu "İlgili E-Posta"
  eksik (yalnızca AI/mail'den oluşan tekliflerde görünür, `saveAi` zaten kapsam dışı — bkz. §4).
  Kritik yön değişikliği #16'da ayrıca: Paketler'e En/Boy/Yükseklik/İstiflenebilir eklendi; Finans'a
  Durum alanı (backend yazma tarafı dahil) eklendi. Otomatik test: `LoadTransferTests.cs` (5 test).
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
- **Sefer** (`TripsPage.tsx`) — TAMAMLANDI: satıra tıklayınca açılan ayrı bir detay/düzenleme Drawer'ı
  eklendi (önceden yalnızca "Yeni Sefer" oluşturma vardı, mevcut kaydı açmanın hiçbir yolu yoktu),
  3 sekmeli (Genel Bilgiler/Bağlı Yükler/Hareketler — kaynakta `ExpeditionFormDrawer.vue`'nin
  Bilgiler/Sefer İçeriği/Hareketler sekmeleriyle aynı sıra). Bağlı Yükler sekmesi TAM ÇALIŞIYOR:
  sefere eklenebilecek (henüz bağlanmamış) yükleri arayan bir seçici, ekleme/çıkarma, toplam
  adet/ağırlık/hacim özeti — hem PostgreSQL hem GERÇEK Siber-mock senkronuyla canlı doğrulandı (bu,
  Teklif→Yük dönüşümünün AKSİNE, Siber kimlik eşlemesine bağımlı değil). "Sefer Tipi" alanı artık
  zorunlu işaretlendi (backend'in gerçekte zorunlu tuttuğu ama frontend'in yıldızlamadığı bir alan
  olduğu bu oturumda bulundu). Kritik yön değişikliği #15'te ayrıca: backend'de HİÇ olmayan
  `Save`/`Update` alan doğrulaması + `expedition_status_id==8` koşullu bloğu eklendi; formda eksik
  `car_exit_date` alanı ve 3 şehir seçicisi (`start_city_id`/`load_city_id`/`end_city_id`) eklendi.
  Otomatik test: `ExpeditionLoadMappingTests.cs` (bağlama+silme + BR-006/007 romork tipi eşleşmeme
  senaryosu) + `ExpeditionTests.cs` (5 test, doğrulama sözleşmesi).
  - **Hareketler**: **[Kritik yön değişikliği #7'de eklendi]** Sekme daha önce TAMAMEN yoktu — bu
    satırda önceden "bilinçli kapsam dışı: aynı `expedition_statuses` boş-tablo kısıtı" olarak
    belgeleniyordu; bu gerekçe STALE çıktı (Kritik yön değişikliği #3 bu kısıtı zaten çözmüştü, not
    hiç güncellenmemişti — ayrıntı: §1). Backend (`ExpeditionController.Movements/SaveMovement/
    DeleteMovement`, tam CRUD) zaten tamdı, Yük'ün Hareketler sekmesiyle birebir aynı desende
    frontend'e bağlandı: liste, "Yeni Hareket Ekle" (Durum+Konum zorunlu), silme, ve
    `deletedMovements.length > 0` koşullu "Silinen Hareketler" görünümü. Canlı Docker'da uçtan uca
    doğrulandı (oluştur→sil→silinenlerde görün tam döngüsü, gerçek SEF-2 kaydı üzerinde).
  - **Genel Bilgiler kaydı**: bu satırda önceden "ortam kısıtı yüzünden ÇALIŞTIRILAMIYOR" olarak
    belgeleniyordu — bu da AYNI STALE kökten çıktı (Kritik yön değişikliği #3). Bu oturumda
    `PUT /api/v1/expedition` canlı Docker'a karşı (dokunmadan Kaydet'e basarak) çalıştırıldı → 200 OK,
    ve DB'de `romork_id/work_type/department_id/status_id/expedition_type_id` alanlarının round-trip
    sonrası dolu kaldığı doğrulandı (silent-wipe yok). Hâlâ eksik: bu akış için `LoadTests.cs`
    tarzında ayrı bir otomatik regresyon testi yazılmadı, yalnızca canlı doğrulandı.
- **Fatura** (`InvoicesPage.tsx`) — TAMAMLANDI: satıra tıklayınca açılan ayrı bir detay/düzenleme
  Drawer'ı eklendi, 3 sekmeli (kaynakla birebir isimlendirilmiş "Bilgiler"/"Kalemler"/"Ek Bilgiler" —
  **[Kritik yön değişikliği #8'de düzeltildi]** önceden "Genel Bilgiler"/"Dipnotlar" idi). Kalemler
  sekmesi bir "Fatura'nın kendi satırları" DEĞİL — kaynağın gerçek veri modelini yansıtıyor: mevcut
  `load_transfer_invoice_item` kayıtlarını arayıp faturaya EŞLEYEN bir seçici (`load_transfer_invoice_maps`),
  backend'in "her güncellemede eşlemeleri baştan kur" davranışına uygun olarak yerelde biriktirilip
  Kaydet'te toplu gönderiliyor. **[Kritik yön değişikliği #8'de düzeltildi]** bu seçici önceden HİÇ
  filtrelenmiyordu (kaynakta `status=pending` + `buysell` + cari filtreli) — zaten başka bir faturaya
  bağlı veya yönü uyuşmayan kalemler de seçilebiliyordu (çift faturalandırma riski); filtreler + zaten-
  eklenmiş kalemler için "Eklendi" rozeti eklendi. Ek Bilgiler sekmesi bağımsız, anında kaydedilen CRUD
  (kendi REST uçları var). Müşteri alanı da `AccountPicker`'a yükseltildi. **[Kritik yön değişikliği
  #8'de eklendi]** kaynağın 3 sekmeli liste yapısı (Gelen/Giden/**Onay Bekleyen**) portta 2 sekmeydi —
  Onay Bekleyen filtresi eklendi (`invoice_statuses.name == "Onay Bekliyor"`tan çözülerek, kaynaktaki
  hardcoded `7` yerine). Canlıda uçtan uca doğrulandı: gerçek bir kalem eklenip kaydedildi (DB'de
  `load_transfer_invoice_maps` satırı VE kalemin durumu `invoice_issued`'a geçtiği doğrulandı —
  kaynağın alış/satış kuralı birebir), sonra kaldırılıp tekrar boşaltıldığı doğrulandı; dipnot eklenip
  silindi. Otomatik test: `InvoiceTests.cs` (4 test). HÂLÂ EKSİK — bilinçli kapsam dışı: PDF önizleme,
  Uyumsoft draft/send/cancel/approve UI'si (backend'de zaten hiç portlanmadı — bkz. `InvoiceController.cs`
  üstündeki yorum).
- **Müşteri** (`CustomersPage.tsx`) — TAMAMLANDI (Kritik yön değişikliği #8, #13): çoklu Hesap Türü seçimi
  (tekilden çoklu chip-toggle'a düzeltildi — EN CİDDİ bulgu, gerçek veri kaybı), Görevli + Faturalar
  sekmeleri eklendi, avatar yükleme eklendi, `tax_office` serbest metinden gerçek lookup'a çevrildi.
  Kritik yön değişikliği #13'te ayrıca: `country_id`/`discount` zorunlu (ikisi de), `name` güncellemede
  de zorunlu — backend'e VE forma eklendi. Otomatik test: `AccountFormTests.cs` (6 test).
- **Araç** (`VehiclesPage.tsx`) — TAMAMLANDI (Kritik yön değişikliği #8, #12): "Kiralanan Firma" alanı
  eklendi, `AccountOption`'a `siber_id` eklenerek cari-Siber id eşlemesi doğru çözülüyor. Kritik yön
  değişikliği #12'de ayrıca: `plate_number` benzersizliği + 9 eksik zorunlu alan (`car_type/romork_type/
  vehicle_owner/vehicle_status/customer_id/km/width/length/height/capacity`) backend'e VE forma (`required`
  + hata gösterimi) eklendi. Otomatik test: `CarTests.cs` (5 test).
- **Kullanıcılar** (`UsersPage.tsx`) — TAMAMLANDI (Kritik yön değişikliği #9, #12): avatar/PDKS/ülke kodu
  alanları + "Tümünü Seç" toplu yetki güncellemesi eklendi; ayrıca kaynakta var olup portta hiç
  bulunmayan "Hedefler" sekmesi (aylık satış hedefi CRUD'u, `api/v1/user_goal`) — kullanıcı onayıyla —
  eklendi (bkz. §1, kapsam çelişkisinin çözümü). Kritik yön değişikliği #12'de ayrıca: `phone` zorunlu+
  benzersiz, `phone_country_id` yalnızca oluşturmada zorunlu — backend'e VE forma eklendi. Otomatik test:
  `UserFormTests.cs` (5 test) + `UserGoalTests.cs` (6 test).
- **Destek Talebi** (`SupportPage.tsx`) — TAMAMLANDI (Kritik yön değişikliği #10): liste kolonları ve
  "Durum" rozeti metni kaynakla birebir eşleştirildi (önceden kaynakta hiç geçmeyen "Çözüldü"/"Açık"
  kullanıyordu), detay panelindeki eksik gönderim-tarihi alanı eklendi, kaynakta görsel-ama-işlevsiz
  olan arama kutusu gerçek bir filtreye bağlandı. Otomatik test: `ContactFormTests.cs` (5 test).

**Bu oturumda bulunan bir hata daha:** Fatura oluşturma formunda "Vade Tarihi" zorunlu
İŞARETLENMEMİŞTİ ama backend boş bırakılırsa HER ZAMAN reddediyor (`InvoiceController.Validate` →
`invoice_execution_date`). Canlıda denendi, doğrulandı, düzeltildi (hem oluşturma hem düzenleme
formunda `*` eklendi) — `InvoiceTests.CreateInvoice_WithoutExecutionDate_ReturnsValidationError` ile
regresyon testi de eklendi.

"Birebir" alan parite şartı Teklif için tam karşılanıyor; Yük için çekirdek+ilişkili-kayıt
alanlarında (Hareketler DAHİL, bkz. Kritik yön değişikliği #5) karşılanıyor (yalnızca PDF/Uyumsoft
kasıtlı olarak dışarıda); Fatura için çekirdek+ilişkili-kayıt alanlarında karşılanıyor (PDF/Uyumsoft
kasıtlı olarak dışarıda); Sefer için Bağlı Yükler VE Hareketler tam karşılanıyor (bkz. Kritik yön
değişikliği #7), Genel Bilgiler kaydı da artık çalışıyor (bkz. yukarıdaki not — önceki "ortam kısıtı"
iddiası staleydi); Müşteri, Araç, Kullanıcılar ve Destek Talebi için de tam karşılanıyor (bkz. Kritik
yön değişikliği #8-#10) — Kullanıcılar'ın "Hedefler" sekmesi dahil, kullanıcı onayıyla kapsama eklendi.
Sekiz modülün SEKİZİ de artık bu titizlikte (kaynağın gerçek `*FormDrawer.vue`'sü satır satır
karşılaştırılarak) denetlendi.

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
- **Sistematik güvenlik taraması (Kritik yön değişikliği #11):** API'deki tüm 21 controller'ın
  yetkilendirme kapsamı tek tek doğrulandı (8 modülün CRUD uçları + 23+ referans/tanım modülünün
  paylaşımlı `LookupControllerBase<T>`'i dahil). `[Authorize]` var ama `[RequiresPermission]` YOK
  görünen 3 uç tek tek olsold kaynağıyla karşılaştırıldı — üçü de kaynakta zaten yalnızca `auth:api`
  ile korunuyor, birebir korunduğu doğrulandı. Dosya yükleme (uzantı beyaz listesi + boyut sınırı +
  her zaman yeniden üretilen rastgele dosya adı + yol gezinmesi koruması), JWT anahtarı (yalnızca
  yapılandırmadan, sabit-kodlanmış yedek yok), rate limiting kapsamı (yalnızca gerçek anonim iki uç)
  ayrıca doğrulandı. Sonuç: yeni bir açık bulunmadı — bkz. §1 Kritik yön değişikliği #11.

## 10. Önerilen sonraki adımlar (öncelik sırasıyla)

1. Kalan 7 modül için 3-viewport görsel kontrolünü tamamlamak (yalnızca Müşteriler tam kontrol edildi).
2. `ValidateRequired`'ın kalan sekiz alan kontrolü ve BR-010/013 için ek entegrasyon testleri.
3. "AI'dan teklif" özelliğinin gerekip gerekmediğine karar vermek — gerekiyorsa backend adaptör
  üzerinden, tarayıcıya asla anahtar sızdırmadan.
4. Sefer Genel Bilgiler kaydı ve Hareketler sekmesi için özel otomatik regresyon testleri yazmak
   (Kritik yön değişikliği #7'de yalnızca canlı Docker'da doğrulandı, `ExpeditionLoadMappingTests.cs`
   gibi ayrı bir test dosyası henüz yok — bkz. §8).
