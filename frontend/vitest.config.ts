import path from 'path'
import { defineConfig } from 'vitest/config'

/**
 * Test yapılandırması vite.config.ts'ten AYRI tutuldu.
 *
 * `vitest/config` üzerinden tanımlansaydı üretim derlemesi (`vite build`,
 * web imajının içinde koşuyor) vitest'i çözmek zorunda kalırdı. Testler
 * derleme yolunu hiç ilgilendirmiyor.
 *
 * jsdom: test edilen modüller localStorage, sessionStorage, document.cookie
 * ve fetch kullanıyor.
 */
export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.ts', 'src/**/*.test.tsx'],
    restoreMocks: true,
  },
})
