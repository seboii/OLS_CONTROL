# infra — dağıtım yapılandırması

Uygulamanın çalıştırılmasıyla ilgili her şey burada: imaj tanımları, nginx
yapılandırması, sahte Siber şeması ve ortam override'ları.

```
infra/
  compose/
    dev.yml            geliştirme: veritabanı ve API portlarını yerele açar
    cloudflared.yml    yayın: Cloudflare tüneli, gelen port açmadan
  docker/
    api/Dockerfile     ASP.NET Core 9 derleme + çalıştırma
    web/Dockerfile     React derleme + nginx ile servis
    web/nginx.conf     TEK giriş noktası: statik + /api + /storage
    siber-mock/*.sql   yerel sahte Siber şeması ve örnek verisi
```

Kök dizindeki `docker-compose.yml` bu dosyaları kullanır.

## Ağ mimarisi

Dışarıya **tek port** açılır: `web` (nginx). Diğer servisler port
yayınlamaz; birbirlerine yalnızca `ols-scoped` Docker ağı üzerinden, servis
adlarıyla erişirler.

```
tarayıcı ──▶ web (nginx, 8105) ──┬──▶ api:8080 ──┬──▶ postgres:5432
                                 │               └──▶ siber-mock:1433
                                 └── statik arayüz (aynı konteyner)
```

Bunun üç sonucu var:

- **Saldırı yüzeyi tek porta iner.** Veritabanı parolası sızsa bile porta
  erişim yok.
- **CORS devreye girmez.** Tarayıcı arayüzü de API'yi de aynı kökende görür.
- **Tünel tek upstream'e bağlanır.** cloudflared yalnızca `web:80` bilir.

`web` ve arayüz aynı konteynerde birleştirildi. Ayrı bir "frontend" konteyneri
artı ayrı bir "edge proxy" kurmak iki nginx katmanı ve gereksiz bir ağ
atlaması demekti.

## Çalıştırma

**Varsayılan (yalnızca 8105 açık):**

```bash
docker compose up -d --build
```

**Geliştirme (veritabanı ve API portları da açık):**

```bash
docker compose -f docker-compose.yml -f infra/compose/dev.yml up -d --build
```

Entegrasyon testleri `localhost:5443`'teki Postgres'e ihtiyaç duyar, bu yüzden
`dotnet test` çalıştırmadan önce dev override'ı kullanın. Her seferinde
yazmamak için `.env` dosyanıza ekleyin:

```
COMPOSE_FILE=docker-compose.yml:infra/compose/dev.yml
```

**Cloudflare ile yayın:**

```bash
docker compose -f docker-compose.yml -f infra/compose/cloudflared.yml up -d --build
```

Ayrıntı ve gerekli `.env` girdileri için `infra/compose/cloudflared.yml`
başındaki açıklamaya bakın.

## Portlar

| Servis | Ana yığın | dev.yml ile | Notu |
|---|---|---|---|
| web (nginx) | **8105** | 8105 | Tek giriş noktası |
| api | — | 127.0.0.1:8106 | Doğrudan curl/Swagger |
| postgres | — | 127.0.0.1:5443 | Entegrasyon testleri |
| siber-mock | — | 127.0.0.1:1444 | sqlcmd ile inceleme |

Dev portları bilerek `127.0.0.1`'e bağlı: geliştiricinin kendi makinesi
içindir, ağa açılmaları gerekmez.

## Neden `ports` ana dosyada yok

Compose'da `ports` listesi override ile **kaldırılamaz**, yalnızca eklenebilir.
Bu yüzden güvenli olan (portsuz) hâl ana dosyada durur ve portlar dev
override'ında eklenir. Ters kurgu — ana dosyada portlar, üretimde kaldıran bir
override — teknik olarak mümkün değil ve sunucuda yanlışlıkla açık port bırakma
riski taşırdı.

## Ortam değişkenleri

Ağ ile ilgili olanlar (tamamı için `.env.example`):

| Değişken | Varsayılan | Açıklama |
|---|---|---|
| `WEB_PORT` | 8105 | Tek yayınlanan port |
| `WEB_BIND_HOST` | 0.0.0.0 | Tünel kullanınca `127.0.0.1` yapın |
| `PUBLIC_ORIGIN` | — | Yayın adresi; API'nin CORS listesine girer |
| `CLOUDFLARE_TUNNEL_TOKEN` | — | Zero Trust panelinden alınır |
| `SIBER_CONNECTION_OVERRIDE` | — | Doluysa CANLI Siber'e bağlanır |
