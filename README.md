# OLS — Scoped .NET Port (Müşteri, Teklif, Yük, Sefer, Fatura, Araç, Kullanıcılar, Destek Talebi)

Uluslararası nakliye/lojistik/gümrükleme ERP'si **OLS**'nin, mevcut Laravel sisteminden (`olsold`)
seçilen **8 kullanıcı modülünün** ASP.NET Core 9 + PostgreSQL + React'e taşınmış, çalışır durumdaki
kapsamlı portu. Kapsam dışı bırakılan modüller (dashboard/ciro, PDKS, mesajlaşma, Excel yönetimi,
muhasebe planı, gümrük beyanname/ordino, CMS) bilinçli bir karar — ayrıntı için
[docs/SECILI-MODUL-PARITE-MATRISI.md](docs/SECILI-MODUL-PARITE-MATRISI.md).

Bu proje `olsold`/`olsnew`'i **okur ama değiştirmez** — bağımsız, kendi git geçmişine sahip bir teslim.

## Hızlı başlangıç (Docker — tek komut)

Gereksinim: Docker Desktop.

```bash
cp .env.example .env
docker compose up -d --build
```

Servisler ayağa kalktığında:

| Servis | URL/port | Not |
|---|---|---|
| Frontend | http://localhost:8105 | nginx + React production build |
| API | http://localhost:8106 | `/health` ile canlılık kontrolü |
| PostgreSQL | localhost:5443 | uygulama veritabanı |
| Sahte Siber (MSSQL) | localhost:1444 | **CANLI Siber ERP'ye asla bağlanmaz** — yalnızca akış doğrulaması için yerel taklit |

İlk açılışta API konteyneri migrasyonları otomatik uygular ve temel verileri (yetki sayfaları, durum
kodları, lookup'lar, geliştirme admin kullanıcısı) seed eder.

**Geliştirme admin girişi** (yalnızca `ASPNETCORE_ENVIRONMENT=Development`'ta, sabit varsayılan olarak
üretimde KULLANILMAZ — bkz. `Seed:AdminEmail`/`Seed:AdminPassword`):

```
E-posta:  admin@ols-scoped.local
Şifre:    ChangeMe!Dev1
```

## Yerel geliştirme (Docker olmadan, hızlı iterasyon için)

Backend'i lokal çalıştırıp yalnızca Postgres/Siber-mock'u Docker'da tutmak isterseniz:

```bash
docker compose up -d postgres siber-mock siber-init
dotnet run --project src/OLS.API   # appsettings.Development.json: Postgres localhost:5443
```

Frontend'i Vite dev sunucusuyla (hot reload):

```bash
cd frontend
npm install
npm run dev   # http://localhost:5173, /api ve /storage backend'e (5197) proxy'lenir
```

## Testler

```bash
dotnet test                                          # tüm çözüm (53 test)
dotnet test tests/OLS.Business.Tests                 # 29 birim testi, veritabanı gerekmez
dotnet test tests/OLS.API.IntegrationTests            # 24 entegrasyon testi, gerçek Postgres gerekir (localhost:5443)
```

Entegrasyon testleri `docker compose`'daki Postgres'e karşı, HER ÇALIŞTIRMADA rastgele adlı izole bir
veritabanı (`ols_scoped_inttest_*`) oluşturup silerek çalışır — geliştirme veritabanınızı (`ols_scoped`)
etkilemez. Ayrıntı ve bu mekanizmayı bulurken karşılaşılan gerçek sorunlar için
[docs/TEST-RAPORU.md](docs/TEST-RAPORU.md).

## Mimari

- **Backend:** ASP.NET Core 9, 3 katman (`OLS.API` → `OLS.Business` → `OLS.DataAccess`), EF Core/Npgsql
  (PostgreSQL — ana veri), Dapper (Siber/MSSQL — yalnızca legacy senkronizasyon uçları). JWT Bearer auth
  (jti tabanlı iptal listesi), sayfa-slug × CRUD bayrağı yetki modeli (rol tabanlı DEĞİL — bkz.
  [docs/YETKI-MATRISI.md](docs/YETKI-MATRISI.md)), snake_case JSON, `/api/v1` öneki.
- **Frontend:** React 19 + TypeScript + Vite + Tailwind v4, `docs/` altındaki hazır tasarımın birebir
  portu (koyu lacivert daraltılabilir sidebar, kompakt kurumsal bileşenler).
- **Veri modeli:** 58 tablo, kapsam dışı 33 tablo taşınmadı. Ayrıntı:
  [docs/VERI-MODELI.md](docs/VERI-MODELI.md).

## Belgeler

| Belge | İçerik |
|---|---|
| [docs/SECILI-MODUL-PARITE-MATRISI.md](docs/SECILI-MODUL-PARITE-MATRISI.md) | Modül bazlı ekran/uç parite tablosu |
| [docs/API-PARITE-MATRISI.md](docs/API-PARITE-MATRISI.md) | Her olsold uç noktası için tek tek durum (PORT/YENİ/KAPSAM-DIŞI/...) |
| [docs/YETKI-MATRISI.md](docs/YETKI-MATRISI.md) | Yetki modeli, sayfa-slug listesi, nesne-seviyesi kurallar (super_admin, kendi-rolünü-okuma) |
| [docs/VERI-MODELI.md](docs/VERI-MODELI.md) | Tablo listesi, modül→tablo eşlemesi, DATA-002 durum kodu sabitliği |
| [docs/TEST-RAPORU.md](docs/TEST-RAPORU.md) | Gerçekten çalıştırılan testler, sonuçları, geliştirme sırasında bulunan gerçek hatalar |
| [docs/TESLIM-RAPORU.md](docs/TESLIM-RAPORU.md) | Kapsam, tamamlanan/eksik iş, bilinen kısıtlar, teslim özeti |

## Bilinen kısıtlar

Bu port, 8 modülün TEMEL akışlarını (liste/oluştur/düzenle/sil, yetki zorlaması, giriş/çıkış) uçtan uca
çalışır ve test edilmiş durumda teslim eder. Teklif/Sefer/Fatura'nın zengin alan derinliği (çoklu sekme,
çoklu satır girişleri, dosya yükleme) ve 3-viewport görsel karşılaştırma henüz tamamlanmadı — güncel,
dürüst durum için [docs/TESLIM-RAPORU.md](docs/TESLIM-RAPORU.md)'ye bakın.
