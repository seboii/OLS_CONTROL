# Web katmanı: React arayüzünü derler ve nginx ile SERVİS EDER.
#
# Bu imaj yığındaki TEK giriş noktasıdır — statik dosyaları kendi servis eder,
# /api ve /storage isteklerini Docker ağı üzerinden API konteynerine geçirir.
# Ayrı bir "frontend" konteyneri + ayrı bir "edge proxy" kurmak iki nginx
# katmanı ve gereksiz bir ağ atlaması demekti; tek katmanda birleştirildi.
FROM node:22-alpine AS build
WORKDIR /app

# Önce bağımlılık dosyaları: paket listesi değişmedikçe npm katmanı önbellekten gelir.
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci --no-audit --no-fund || npm install --no-audit --no-fund

COPY frontend/ ./

# `npm run build` = tsc -b + vite build. Tip hatası derlemeyi durdurur;
# `tsc --noEmit` bu projede hiçbir şeyi denetlemiyor (kök tsconfig proje
# referansları kullanıyor), bu yüzden burada build script'i çağrılır.
RUN npm run build

FROM nginx:alpine AS runtime

COPY infra/docker/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost/index.html || exit 1
