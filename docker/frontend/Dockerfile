# Vue 3 + Vite + Tailwind -> nginx ile servis
FROM node:22-alpine AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci --no-audit --no-fund || npm install --no-audit --no-fund

COPY frontend/ ./

# Vite, VITE_* değişkenlerini DERLEME ANINDA paketin içine gömer; çalışma
# zamanında okunamaz. Kökteki .env build bağlamına girmiyor (COPY yalnızca
# frontend/ alıyor), bu yüzden anahtar build arg ile aktarılıyor. Vite VITE_
# önekli process.env değişkenlerini .env dosyalarına göre öncelikli okur.
ARG VITE_OPENAI_API_KEY=""
ENV VITE_OPENAI_API_KEY=$VITE_OPENAI_API_KEY

RUN npm run build

FROM nginx:alpine AS runtime
COPY docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
