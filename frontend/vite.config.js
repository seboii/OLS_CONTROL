import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwindcss from "@tailwindcss/vite";

// olsold'daki vite.config.js'in bagimsiz karsiligi.
// laravel-vite-plugin kaldirildi (artik Blade yok, giris noktasi index.html).
export default defineConfig({
  resolve: {
    alias: {
      "@": "/src",
      vue: "vue/dist/vue.esm-bundler.js",
    },
  },
  plugins: [
    vue({
      template: {
        transformAssetUrls: {
          base: null,
          includeAbsolute: false,
        },
      },
    }),
    tailwindcss(),
  ],
  server: {
    host: true,
    port: 5173,
    // Frontend kodu API'yi gorece yolla cagiriyor (axios.get("/api/v1/...")).
    // Gelistirmede istekleri .NET backend'e yonlendiriyoruz; uretimde ayni isi
    // nginx yapiyor (docker/frontend/nginx.conf).
    proxy: {
      "/api": {
        target: process.env.VITE_API_TARGET || "http://localhost:5197",
        changeOrigin: true,
      },
      "/storage": {
        target: process.env.VITE_API_TARGET || "http://localhost:5197",
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
});
