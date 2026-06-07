import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg'],
      manifest: {
        name: 'Demirler Porselen',
        short_name: 'Demirler',
        description: 'Demirler Porselen — stok ve cari yönetim',
        theme_color: '#14233b',
        background_color: '#14233b',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: 'pwa-192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512.png', sizes: '512x512', type: 'image/png' },
        ],
      },
      workbox: {
        navigateFallback: '/index.html',
        globPatterns: ['**/*.{js,css,html,svg,png,woff2}'],
      },
    }),
  ],
  server: {
    port: 5173,
    host: true, // LAN'a aç: telefon/diğer cihazlar da http://<PC-IP>:5173 ile erişebilir
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
      '/uploads': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
});
