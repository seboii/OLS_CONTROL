# OLS — Nakliye / Lojistik / Gümrükleme ERP

Uluslararası nakliye, lojistik ve gümrükleme operasyonlarını yöneten **OLS**'nin ASP.NET Core 9 +
PostgreSQL + React ile yazılmış sürümü. Uygulama, kurumun mevcut **Siber ERP**'si (MSSQL) ile çift
yönlü çalışır: cari, teklif, yük, sefer, araç, kullanıcı ve evrak verisi Siber'den okunur; uygulamada
açılan kayıtlar Siber'e geri yazılır.

## Modüller

| Modül | Yol | Yetki slug'ı |
|---|---|---|
| Panel | `/panel` | — |
| Müşteriler (cari) | `/musteriler` | `account_management` |
| Teklifler | `/teklifler` | `load_management` |
| Yükler | `/yukler` | `load_management` |
| Seferler | `/seferler` | `expedition_management` |
| Faturalar | `/faturalar` | `invoice_management` |
| Finans | `/finans` | `finance_management` |
| Muhasebe | `/muhasebe` | `accounting_management` |
| Araçlar | `/araclar` | `car_management` |
| Kullanıcılar | `/kullanicilar` | `user_management`, `role_management` |
| Destek talepleri | `/destek-talepleri` | `support_request_management` |
| Raporlama | `/raporlama` | `report_management` |
| Denetim kaydı | `/denetim` | `audit_log_management` |

## Hızlı başlangıç

Gereksinim: Docker Desktop.

```bash
cp .env.example .env
docker compose up -d --build
```

| Servis | Adres | Not |
|---|---|---|
| Arayüz | http://localhost:8105 | nginx + React üretim derlemesi |
| API | http://localhost:8106 | `/health` ile canlılık kontrolü |
| PostgreSQL | localhost:5443 | uygulama veritabanı |
| MSSQL (yerel taklit) | localhost:1444 | `SIBER_CONNECTION_OVERRIDE` boşken kullanılır |

API konteyneri ilk açılışta migrasyonları uygular ve temel verileri (yetki sayfaları, roller, durum
kodları, tanım tabloları) yükler.

## Ağa açma

Uygulamayı telefondan veya ağdaki diğer bilgisayarlardan kullanmak için `.env` içinde makinenin LAN
adresini tanımlayın:

```
LAN_ORIGIN=http://192.168.1.50:8105
FRONTEND_BIND_HOST=0.0.0.0
API_BIND_HOST=0.0.0.0
```

`LAN_ORIGIN` API'nin CORS izin listesine işlenir. Arayüz kendi API isteklerini nginx üzerinden aynı
köken içinde (`/api` → API konteyneri) yönlendirdiği için istemcide ayrı bir adres tanımlamak gerekmez;
tarayıcıdan yalnızca arayüz portu açılır.

Veritabanı portları bilinçli olarak `127.0.0.1`'e bağlıdır (`DB_BIND_HOST`, `SIBER_BIND_HOST`) —
PostgreSQL ve MSSQL ağa açılmaz.

## Siber entegrasyonu

`SIBER_CONNECTION_OVERRIDE` dolu olduğunda uygulama **canlı Siber veritabanına** bağlanır; boş
bırakılırsa `docker-compose` içindeki yerel MSSQL taklidi kullanılır. Taklit yalnızca akış doğrulaması
içindir, veri sadakati hedeflemez.

**Okuma yönü.** Arka plan senkronu cari, kullanıcı, teklif, yük, sefer, araç ve mali kalem tablolarını
düzenli aralıkla tarar; yalnızca değişen kayıtları günceller. Siber tarafında engellenen kullanıcılar
uygulamada pasife alınır.

**Yazma yönü.** Uygulamadan açılan teklif, yük ve sefer kayıtları Siber'e yazılır. Kayıt numaraları
Siber'in kendi sayaç düzenine göre, `sp_getapplock` ile kilitlenmiş tek bir işlem içinde üretilir —
eşzamanlı iki kullanıcı aynı numarayı alamaz. Sefer numarası (yıl, araç sahibi) kapsamında ilerler.

**Evrak arşivi.** Siber'in `sbr_arsiv` tablosu ve FTP arşivi hem okunur hem yazılır. Bir yüke, sefere
veya teklife Siber üzerinden eklenmiş evraklar uygulamada listelenir ve indirilebilir; uygulamadan
yüklenen dosyalar da aynı arşive gönderilir. Klasör düzeyi kodu kayıt türüne göre belirlenir (yük iş
türüne göre `0401`–`0404`, sefer `0405`, teklif `04113`).

**Kim açtı, kim dokundu.** Siber teklif, yük ve sefer kayıtlarında kullanıcı izini tutuyor;
uygulama bunu her kaydın detayında gösterir. "Kaydı açan" üç modülde de eksiksiz doludur;
"son işlem yapan" teklifte %81, yükte %85, seferde %30 oranında dolu olduğu için o satır
veri yoksa hiç gösterilmez. Kullanıcı Siber'de kodla tutulduğundan (91 koddan 88'i yerel
bir kullanıcıya karşılık geliyor), eşleşme yoksa kodun kendisi gösterilir.

**İşlem geçmişi.** Teklif, yük, sefer, fatura ve cari detaylarında ayrı bir "İşlem Geçmişi"
sekmesi kaydın tüm değişikliklerini listeler: her işlem için kim, ne zaman ve hangi alanın
hangi değerden hangi değere geçtiği. Kaynak Siber'in kendi değişiklik günlüğüdür — bu altı
tablo için 253 binden fazla işlem kaydı taşınmıştır. Değerler alan adlarıyla eşleştirilemediğinde
(çok satırlı bir metin alanı hizalamayı bozduğunda) yanlış bir eşleşme göstermek yerine
yalnızca değişen alan adları listelenir.

**Program dışı silmeler.** Doğrudan Siber ekranından silinen bir kayıt, uygulamada canlı
görünmeye devam ediyordu. Artık her tam senkronda Siber'den gelen kimlik kümesi yerelle
karşılaştırılır ve eksik kayıtlar "Siber'de silinmiş" olarak işaretlenir. Kayıt yerelden
SİLİNMEZ — bağlı finans kayıtları, evrak arşivi ve denetim izi korunur, kayıt Siber'de
yeniden görünürse işaret kalkar.

Listeler bu kayıtları varsayılan olarak gizler; yük, teklif ve sefer ekranlarındaki
**"Siberde silinenler"** düğmesi yalnızca silinenleri listeler ve her kayıt kırmızı bir
rozetle işaretlenir. Silme ayrıca Denetim Kaydı ekranına düşer.

Siber kendi günlüğünde silme işlemlerini de tuttuğu için, kaydı **kimin ne zaman sildiği**
gösterilebiliyor. Bu bilgi her kayıtta bulunmaz (Siber ekranından geçmemiş kayıtların
günlük satırı yoktur); bulunamadığında yalnızca durumun fark edildiği an gösterilir. İki
zaman damgası ayrı tutulur: gerçek silme anı ile fark edilme anı aynı şey değildir.

Silme kontrolünün bir güvenlik eşiği vardır: Siber'den gelen kayıt sayısı yereldekinin
yarısından azsa hiçbir kayıt işaretlenmez. Bu olmadan yarım dönen tek bir çekim tüm tabloyu
silinmiş sayardı.

**Kimlik eşleşmesi.** Siber `uniqueidentifier` değerlerini büyük harfle döndürür, .NET küçük harfle
üretir. PostgreSQL karşılaştırması harfe duyarlı olduğu için Siber tarafındaki kimlikler sorgularda
küçük harfe indirgenir.

## Yetkilendirme

Yetki modeli **sayfa slug'ı × CRUD bayrağı** temellidir: her kullanıcının her sayfa için ayrı
okuma/ekleme/güncelleme/silme hakkı vardır. Listelenmeyen bir slug varsayılan olarak reddedilir.

Bunun üzerine, Siber departmanlarından türetilmiş **8 rol** bir şablon katmanı olarak durur: Yönetim,
Satış & Pazarlama, İhracat Operasyon, İthalat Operasyon, Transit Operasyon, Muhasebe & Finans,
İdari İşler ve Standart Kullanıcı. Rol atandığında şablon kullanıcının yetki satırlarına yazılır;
şablonda yer almayan sayfalar sıfırlanır, böylece önceki rolden kalan hak sessizce sürmez. Atama
sonrası yetkiler kullanıcı ekranından tek tek düzenlenebilir.

`super_admin` slug'ı ayrı bir nesne-seviyesi kural taşır: okuma hakkı olan kullanıcı tüm carileri
görür, olmayan yalnızca kendisine atanmış carileri görür.

**Şirket kapsamı.** Kurum iki şirket üzerinden çalışır. Avrora ekibindeki kullanıcılar yalnızca Avrora
yük, sefer ve tekliflerini görür; diğer kullanıcılar Avrora kayıtlarını görmez. Kapsam kullanıcının
şirket alanından, o boşsa e-posta alan adından belirlenir; yönetici yetkisi olanlar her iki tarafı da
görür.

## Finans ve muhasebe

Modül Siber'in muhasebe tablolarını (`sfy_*`) yerele aynalar ve iki ayrı ekrana böler.

**Finans** (`finance_management`) — operasyonun günlük kullandığı taraf:

- **Cari bakiye ve ekstre.** Bakiye hiçbir yerde SAKLANMAZ, her sorguda fiş satırlarından
  hesaplanır. Cari bağı Siber'in `sfy_fisdetay.kartoteksid` alanından kurulur;
  `sbr_firma.muhasebekod` canlıda 7.429 firmanın hiçbirinde dolu olmadığı için hesap kodundan
  eşleme yapılamaz. Ekstrede tarih aralığı verilirse açılış bakiyesi aralıktan önceki tüm
  hareketlerden hesaplanır, böylece kapanış cari bakiyesiyle birebir tutar.
- **Fatura.** Gelir ve gider faturaları Siber'de tek tabloda tutulur, ayrım `gc` alanıyla yapılır.
  Yük bağı fatura BAŞLIĞINDAKİ `modulid`/`modulkod` çiftinden kurulur — satırdaki `yukid` sütunu
  Siber'de hiç doldurulmamıştır.
- **Tahsilat/ödeme.** Kayıt çift taraflıdır; bir taraf cari, diğeri kasa/banka hesabı olabilir.
  Çek/senet alanları taşınmadı: Siber'de 29.007 kaydın hiçbirinde dolu değil.

**Muhasebe** (`accounting_management`) — defter ekranları: mizan, yevmiye fişleri ve hesap planı.
Fiş satırı hesap planına METİN eşleşmesiyle bağlanır (Siber'de yabancı anahtar yoktur); planda
karşılığı olmayan kod adsız görünür ama tutarı mizandan düşmez.

**Fatura açma.** Uygulamadan gelir ve gider faturası açılabilir. Kayıt ÖNCE Siber'e yazılır;
oradaki yazma başarısız olursa yerelde de kayıt oluşmaz. Gelir faturasının numarası
(seri, yıl) sayacından `sp_getapplock` ile kilitlenmiş tek işlem içinde üretilir; gider
faturasında numara tedarikçinin belgesinden alınır. Tutarlar sunucuda, satırlardan hesaplanır.

**Yaşlandırma raporu yoktur ve bu bilinçlidir.** Klasik yaşlandırma hangi faturanın ödendiğini
bilmeyi gerektirir; Siber `kapalifatura` alanını 38.425 faturanın 36.713'ünde boş bırakıyor.
Bunun yerine iki ayrı ve doğrulanabilir bilgi verilir: carinin net bakiyesi ve vadesi geçmiş
faturaların listesi.

## Denetim kaydı

Kayıt ekleme, güncelleme ve silme işlemleri kullanıcı, zaman ve değişen alan bazında saklanır. Parola
ve belirteç alanları kayda yazılmaz. `/denetim` ekranı yalnızca Yönetim rolüne açıktır ve yük numarası,
sefer veya kullanıcı üzerinden filtrelenebilir.

## Yerel geliştirme

Yalnızca veritabanlarını Docker'da tutup arka ucu yerelde çalıştırmak için:

```bash
docker compose up -d postgres siber-mock siber-init
```

```bash
dotnet run --project src/OLS.API
```

Arayüzü Vite geliştirme sunucusuyla (hot reload):

```bash
cd frontend && npm install && npm run dev
```

`http://localhost:5173` üzerinden açılır; `/api` ve `/storage` istekleri arka uca yönlendirilir.

Tip kontrolü için `npm run build` (veya `npx tsc -b`) kullanın — kök `tsconfig.json` proje
referanslarıyla çalıştığı için `tsc --noEmit` hiçbir dosyayı denetlemez.

## Testler

```bash
dotnet test
```

155 test: `tests/OLS.Business.Tests` altında 37 birim testi (veritabanı gerekmez),
`tests/OLS.API.IntegrationTests` altında 118 entegrasyon testi (Postgres gerekir).

Entegrasyon testleri her çalıştırmada rastgele adlı izole bir veritabanı (`ols_scoped_inttest_*`)
oluşturup siler; geliştirme veritabanını etkilemez.

## Mimari

- **Arka uç:** ASP.NET Core 9, üç katman (`OLS.API` → `OLS.Business` → `OLS.DataAccess`).
  EF Core/Npgsql ana veri için, Dapper Siber/MSSQL uçları için. JWT Bearer kimlik doğrulama
  (`jti` tabanlı iptal listesi), snake_case JSON, `/api/v1` öneki.
- **Ön yüz:** React 19 + TypeScript + Vite + Tailwind v4. Daraltılabilir yan menü, kompakt kurumsal
  bileşenler, modül başına tek sayfa.
- **Veri modeli:** 70 tablo. Durum kodları (1 Olumsuz, 2 Sipariş, 3 Düzeltme Talebi, 4 Teklif,
  5 Olumlu) Siber ile birebir sabittir; bu değerler değiştirilemez.

## Yapılandırma

| Değişken | Varsayılan | Açıklama |
|---|---|---|
| `FRONTEND_PORT` / `API_PORT` | 8105 / 8106 | Yayın portları |
| `FORWARD_DB_PORT` / `FORWARD_SIBER_PORT` | 5443 / 1444 | Veritabanı portları |
| `FRONTEND_BIND_HOST` / `API_BIND_HOST` | 0.0.0.0 | Ağa açılan servisler |
| `DB_BIND_HOST` / `SIBER_BIND_HOST` | 127.0.0.1 | Veritabanları yalnızca yerel |
| `LAN_ORIGIN` | boş | LAN erişimi için tam adres |
| `SIBER_CONNECTION_OVERRIDE` | boş | Doluysa canlı Siber'e bağlanır |
| `SEED_DEFAULT_USER_PASSWORD` | `Admin123` | Yeni hesapların başlangıç parolası |
| `SEED_RESET_ALL_PASSWORDS` | `false` | `true` ise tüm parolalar varsayılana çekilir |
| `JWT_KEY` | — | En az 32 bayt; üretimde mutlaka değiştirilmeli |

`.env` sürüm kontrolüne dahil değildir. `JWT_KEY`, `DB_PASSWORD` ve `SIBER_SA_PASSWORD` üretimde
`.env.example`'daki değerlerle bırakılmamalıdır.

## Bilinen kısıtlar

- **Siber'in yevmiye defteri kendi içinde dengede değil.** OLS şirketinde borç ve alacak toplamı
  arasında yaklaşık 308 milyon TL fark var (AVRORA tarafı tam dengede, 0,00). Fark ağırlıklı olarak
  2022-2023 kayıtlarında ve dönem kapanış kayıtlarının eksikliğinden kaynaklanıyor. Bu uygulamanın
  aktarımından değil, kaynak verinin kendisinden gelir; mizan ekranı Siber'in gösterdiği rakamı
  gösterir.
- **Tutarlarda kuruş düzeyinde sapma olabilir.** Siber parayı `float(53)` (ikili kayan nokta) olarak
  saklıyor. Uygulama satır bazında 2 haneye yuvarlar; toplam sapma 5,05 milyar TL'lik defterde
  0,13 TL ölçüsünde kalıyor.
- Bir yük yalnızca tek bir sefere bağlanır; buna karşılık Siber'in geçmiş verisinde birden fazla sefere
  bağlı yük kayıtları mevcuttur ve bunlar liste olarak gösterilir.
- Siber'in kendi verisindeki boş alanlar (departmansız sefer kayıtları gibi) uygulamada da boş görünür;
  bunlar uygulama hatası değildir.
- Evrak arşivi FTP üzerinden çalıştığı için arşiv sunucusuna erişilemediğinde dosya uygulamanın kendi
  deposuna kaydedilir, Siber arşivine gönderilemez ve bu durum günlüğe yazılır.
