import { defineConfig } from 'vite';

const webHostUrl = process.env.SKOLIO_WEBHOST_URL ?? 'http://localhost:62820';

export default defineConfig({
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/bootstrap-config': {
        target: webHostUrl,
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    rollupOptions: {
      output: {
        entryFileNames: 'assets/index.js',
        chunkFileNames: 'assets/chunk-[name].js',
        assetFileNames: 'assets/[name][extname]'
      }
    }
  }
});
