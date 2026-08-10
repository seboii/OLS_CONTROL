# Veri Modeli

Kaynak: `src/OLS.DataAccess/Entities/*.cs` (58 varlık) + `20260810122004_ScopedBaseline` migrasyonu.
Aşağıdaki tablo listesi, çalışan veritabanına karşı `\dt` ile doğrulandı (58 gerçek tablo +
`__EFMigrationsHistory`) — kaynak kod okumasından tahmin edilmedi.

## Kapsam kararı

olsold'un tüm şemasından (91 varlık, orijinal envanterde) yalnızca **8 seçili modülün** çalışması için
gerekli 58 tablo taşındı. Dashboard/hedef-ciro, PDKS, mesajlaşma, Excel yönetimi, gümrük (transit
beyanname/ordino/yetki mektubu) ve CMS'e ait tablolar bilinçli olarak dışarıda bırakıldı — bu
entity'lere işaret eden navigation property'ler kaynak entity'lerden temizlendi (derleme zamanında
görünür kalmaları yanlış bir "bu da kapsamda" izlenimi verirdi).

## Tablo listesi (58)

```
account_contact_people      currencies                   load_transfer_delivery_methods
account_type_mappings       departments                  load_transfer_invoice_items
account_types                destinations                 load_transfer_invoice_maps
accounts                     districts                    load_transfer_movements
car_owners                   expedition_load_mappings     load_transfer_packages
car_status_types             expedition_movements         load_transfer_types
car_types                    expedition_statuses          load_transfers
cars                         expedition_types              loading_types
case_types                   expeditions                  loads
cities                        financial_items              movement_types
countries                    instructions                 payment_types
invoice_footers               invoice_statuses             product_types
invoice_types                 invoices                     revoked_tokens
item_types                    load_charge_people           romork_types
load_contents                 load_emails                  status_types
load_files                    load_financial_items         tax_offices
load_movements                 load_status_types            transport_types
user_account_mappings         user_permission_pages        user_permissions
users                          website_contact_forms        work_types
```

## Modül → çekirdek tablo eşlemesi

| Modül | Ana tablo | Doğrudan bağlı alt tablolar |
|---|---|---|
| Müşteri | `accounts` | `account_contact_people`, `account_type_mappings`, `user_account_mappings` |
| Teklif | `loads` | `load_contents`, `load_financial_items`, `load_charge_people`, `load_emails`, `load_files` |
| Yük | `load_transfers` | `load_transfer_movements`, `load_transfer_packages`, `load_transfer_invoice_items`, `load_transfer_invoice_maps` |
| Sefer | `expeditions` | `expedition_movements`, `expedition_load_mappings` |
| Fatura | `invoices` | `invoice_footers` |
| Araç | `cars` | — (bağımsız; `car_types`/`car_owners`/`car_status_types` lookup) |
| Kullanıcılar | `users` | `user_permissions`, `user_account_mappings`, `revoked_tokens` |
| Destek Talebi | `website_contact_forms` | — |
| Yetki altyapısı | `user_permission_pages` × `user_permissions` | bkz. YETKI-MATRISI.md |

Coğrafya (`countries`/`cities`/`districts`) ve 20+ lookup tablosu (`work_types`, `status_types`,
`payment_types`, ...) yukarıdaki tüm modüllerin form alanlarında ortak kullanılır.

## Teklif → Yük ilişkisi (BR-002/003/004/005)

`loads` (Teklif) ve `load_transfers` (Yük) AYRI tablolardır — Yük, Teklif'in bir kopyası/dönüşümü
değil, kendi kayıt kümesidir. `load_transfers.connected_load_number` alanı hangi teklife karşılık
geldiğini metinsel olarak taşır (olsold'un orijinal tasarımı — foreign key DEĞİL). Bir Teklif'in
Yük'e dönüşmesi iş kuralı seviyesinde (belirli `status_type_id` değerlerinde) uygulanır, veritabanı
seviyesinde zorlanmaz.

## DATA-002 — durum kodları (`status_types`) asla ham sayıya göre karşılaştırılmaz

**Risk:** `status_types` tablosu ortama göre farklı sırada/id'lerle seed edilebilir. Kaynak kodun
gerçek çalışma zamanı davranışı (literal karşılaştırmalardan çıkarıldı, `StatusTypeSeeder.php`'nin
kullanılmayan/yanıltıcı isimlerinden DEĞİL) şu şekildeydi:

| Gerçek id (olsold) | Kod | Anlam |
|---|---|---|
| 1 | `REJECTED` | Olumsuz |
| 2 | `ORDER` | Sipariş |
| 3 | `CORRECTION` | Düzeltme Talebi |
| 4 | `OFFER` | Teklif |
| 5 | `APPROVED` | Olumlu |

HEDEF'te bu eşleme `StatusTypeCodes` sabitleriyle (`DbSeeder.cs`) ve `status_types.number` (string kod)
kolonuyla sabitlendi — iş mantığı `status_type_id == 4` gibi ham sayı karşılaştırması YAPMAZ, önce
`number = 'OFFER'` olan satırın id'sini sorgulayıp öyle karşılaştırır (ya da doğrudan kodu karşılaştırır).
Doğrulama:

```
docker exec ols-scoped-postgres psql -U postgres -d ols_scoped -c "SELECT id, number, name FROM status_types ORDER BY id;"
```
```
 id |   number   |      name
----+------------+-----------------
  1 | REJECTED   | Olumsuz
  2 | ORDER      | Sipariş
  3 | CORRECTION | Düzeltme Talebi
  4 | OFFER      | Teklif
  5 | APPROVED   | Olumlu
```

## Para/ölçü alanları

Tüm parasal alanlar (`invoices.payable_amount`, `load_transfers.weight_fee`, fatura kalemleri vb.)
`decimal` — `float`/`double` KULLANILMADI (görev tanımının açık şartı). `cars` tablosundaki fiziksel
ölçü alanları (`width`,`length`,`height`,`capacity`,`km`) `double precision` — bunlar para değil, kaynak
şemadaki orijinal tipleri korundu.

FormData'dan gelen sayısal metin alanları (Türkçe virgül/nokta belirsizliği) `TurkishDecimal.Parse`
ile ayrıştırılır — bkz. `OLS.Business.Tests/TurkishDecimalTests.cs` (14 test, TEST-RAPORU.md §2.2).

## Soft delete

`users.deleted_at` — tek soft-delete alanı bu 8 modülde aktif kullanılıyor (`AuthService.LoginAsync`,
`GetAuthenticatedUserAsync` sorguları `DeletedAt == null` filtresiyle çalışır). Diğer tablolarda
soft-delete kolonu şemada varsa da (olsold mirası) bu modüllerin iş akışında aktif kullanılmıyor —
silme işlemleri gerçek `DELETE`.

## Zaman damgaları — `IClock` soyutlaması

`OLS.Business.Common.IClock`: eski (legacy) tablolardaki `created_at`/`updated_at` kolonları
Europe/Istanbul yerel saatiyle yazılır (olsold/Laravel `DateTime.Now` davranışının birebir korunması —
raporlarda/ekranlarda saat kayması olmaması için), yeni eklenen altyapı tabloları (`revoked_tokens.expires_at`
gibi) UTC kullanır. Servisler doğrudan `DateTime.Now`/`DateTime.UtcNow` ÇAĞIRMAZ, `IClock.Now` üzerinden
enjekte edilir — bu da testlerde zamanın sabitlenebilmesini sağlar (bu oturumda ayrı bir clock testi
yazılmadı, ama soyutlama testedilebilir durumda).

## Kimlik türleri

- Çoğu tablo `bigint` (`long`) PK — olsold'un auto-increment `id` kolonlarının doğrudan karşılığı.
- Coğrafya tabloları (`countries`/`cities`/`districts`) ve bazı ilişki tabloları `uuid` PK kullanır
  (olsold'da da `uuid` idi — GUID'ler C# tarafında `Guid` olarak eşlendi).
- `status_types.number`, `account_types` vb. lookup'larda ekstra bir STRING kod kolonu varsa (DATA-002
  deseni), iş mantığı bu koda göre çalışır, ham `id`'ye göre değil.
