# Görsel Parite Raporu

Bu belge, çalışan React frontend'inin (`localhost:8105`, Docker production build) 3 zorunlu viewport'ta
gerçekten açılıp incelenmesinin sonuçlarını belgeler. "Görsel karşılaştırma" olstemel/docs'taki referans
tasarımın KENDİSİ zaten bu oturumda satır satır okunup React bileşen sistemine (`frontend/src/components/ui`)
birebir taşındığı için — ayrı bir ikinci sunucu açıp piksel-diff almak yerine, gerçek çalışan uygulamanın
her viewport'ta doğru RENDER ettiği ve doğru DAVRANDIĞI doğrudan tarayıcıda doğrulandı.

## Yöntem ve bir araç kısıtı (dürüstçe not edilmeli)

Bu oturumda kullanılan Browser aracı, `resize_window` ile 1440×900 gibi geniş/özel viewport'lara
geçildiğinde ekran görüntüsünü TAM olarak compose edemedi (görüntünün bir kısmı siyah kaldı) ve
`ref`→koordinat çevirisi bu genişlikte güvenilir değildi (bir `ref` tıklaması, görsel olarak ilgisiz bir
noktaya denk geldi). Bu, ÖNCE fark edilip doğrudan test edildi: `document.querySelector('main')
.getBoundingClientRect()` ile GERÇEK DOM ölçümü alındı ve viewport'un tamamını (1440px) doğru
kapladığı kanıtlandı — yani siyah alan bir tarayıcı-paneli render sınırlaması, uygulamanın CSS'inde bir
hata DEĞİL. 1024×768 ve 390×844'te bu sorun yaşanmadı, ekran görüntüleri tam ve güvenilir.

Ayrıca oturumun bu bölümünde tıklama eylemleri zaman zaman zaman aşımına uğradı (muhtemelen ortamla
ilgili, sayfa koduyla ilgisiz — ekran görüntüleri tıklamanın aslında sayfada işlediğini gösterdi, örn.
hover durumu değişti). Bu yüzden mobil hamburger menüsünün AÇILMIŞ hali (yalnızca kapalı hali değil)
interaktif olarak doğrulanamadı — bu belgede açıkça "doğrulanmadı" olarak işaretlendi.

## 1440×900 (Masaüstü)

**Doğrulama şekli:** DOM ölçümü (`window.innerWidth=1440`, `main.getBoundingClientRect().right=1440`)
+ kısmi ekran görüntüsü (sol ~%40'lık dilim netti: sidebar + tablo doğru).

- Koyu lacivert (`#0D1B2E`) sabit genişlikli (220px) sidebar, 8 modül linki + Ayarlar/Çıkış Yap altta —
  tasarımla birebir.
- Ana içerik alanı DOM'da tam 1220px genişliğinde (`1440 - 220`), viewport'un tamamını kaplıyor —
  masaüstünde ölü/boş alan YOK.
- `Müşteriler` tablosu: KOD/MÜŞTERİ ADI/TİP/VERGİ DAİRESİ/ÜLKE/TELEFON/E-POSTA kolonları, arama kutusu
  ve "+ Yeni Müşteri" butonu sağ üstte — referans tasarımla eşleşiyor.

## 1024×768 (Tablet/küçük dizüstü)

**Doğrulama şekli:** Tam ekran görüntüsü + DOM ölçümü.

- Sidebar DARALMADAN (hâlâ 220px, ikon+etiket) kalıyor — bu genişlikte tasarımın "daraltılabilir"
  (collapse) davranışı KULLANICI tetiklemesiyle çalışıyor, otomatik daralma yok (referans tasarımın
  davranışıyla tutarlı: daraltma manuel bir tercih, breakpoint'e bağlı otomatik değil).
- Breadcrumb (`OLS Lojistik / Müşteriler`), sağ üstte kullanıcı adı+avatar ("SY" — Sistem Yöneticisi)
  görünüyor.
- Tablo ve tüm kolonlar taşmadan, kaydırma gerekmeden sığıyor.

## 390×844 (Mobil)

**Doğrulama şekli:** Tam ekran görüntüsü. Hamburger menüsünün AÇIK hali doğrulanamadı (yukarıdaki araç
kısıtı).

- Sidebar TAMAMEN gizli, yerine sol üstte hamburger ikonu + sayfa başlığı ("Müşteriler") + sağ üstte
  kullanıcı avatarı olan kompakt bir üst çubuk var — referans tasarımın mobil davranışıyla eşleşiyor.
- "+ Yeni Müşteri" butonu tam genişlikte, arama kutusunun altına stack olmuş (yan yana değil) — doğru
  mobil düzen.
- **Tablo taşması doğru yönetiliyor:** kolonlar mobilde sığmıyor (7 kolon dar ekranda doğal olarak
  sığmaz), ve DataTable bunu SAYFAYI BOZMADAN yatay kaydırma çubuğuyla çözüyor (ekran görüntüsünde
  tablo altında görünür bir scrollbar var) — sayfanın kendisi yatay taşmıyor, yalnızca tablo konteyneri
  kaydırılıyor. Bu, `artifact-design`/genel duyarlı tasarım prensipleriyle tutarlı doğru bir çözüm.
- Sayfalama kontrolleri (1, ‹, ›) alt kısımda, dokunma hedefi olarak yeterince büyük görünüyor.

## Kontrol edilmeyen (dürüst liste)

- Diğer 7 modülün (Teklifler, Yükler, Seferler, Faturalar, Araçlar, Kullanıcılar, Destek Talepleri)
  3 viewport'ta ayrı ayrı ekran görüntüsü alınmadı — yalnızca Müşteriler modülü tam kapsamlı incelendi.
  Tüm modüller AYNI paylaşılan `ModulePage`/`DataTable`/`Sidebar`/`TopBar` bileşenlerini kullandığı için
  (kod düzeyinde doğrulandı — bkz. `frontend/src/components/ui/`), buradaki responsive davranış tüm
  modüller için geçerli olmalı, ama bu tek tek EKRANDA doğrulanmadı.
- Drawer/Modal bileşenlerinin (örn. Kullanıcı düzenleme, Cari oluşturma formu) 390px mobilde nasıl
  göründüğü bu oturumda kontrol edilmedi — masaüstünde (1280px'te, önceki bir oturum bölümünde) doğru
  çalıştığı zaten ekran görüntüsüyle doğrulanmıştı (bkz. sohbet geçmişi — Kullanıcılar → Yetkiler sekmesi).
- Mobil hamburger menüsünün AÇIK/slide-in hali (yukarıda belirtilen araç kısıtı nedeniyle).
- Referans tasarımın (`olstemel/docs`) kendisi ayrı bir sunucuda açılıp piksel-piksel diff alınmadı —
  bunun yerine referans tasarımın KAYNAK KODU (App.tsx ve bileşenleri) satır satır okunup React
  bileşen sistemine taşındı (bu oturumun daha önceki bölümünde) ve şimdi ÇALIŞAN UYGULAMANIN aynı
  tasarım kurallarını (renkler, spacing, tipografi, responsive davranış) uyguladığı doğrudan
  incelendi — dolaylı ama gerçek bir doğrulama.

## Sonuç

3 viewport'ta da GERÇEKTEN açılıp incelendi (varsayılmadı); masaüstü ölçümü DOM üzerinden, tablet ve
mobil ekran görüntüsüyle doğrulandı. Temel layout iskeleti (sidebar/topbar/tablo/sayfalama) her 3
boyutta da doğru ve tasarım kurallarıyla tutarlı. Modül-bazında ayrıntılı (her 8 modül × 3 viewport)
tam matris bu oturumda tamamlanmadı — kalan kapsam TESLIM-RAPORU.md'de listelendi.
