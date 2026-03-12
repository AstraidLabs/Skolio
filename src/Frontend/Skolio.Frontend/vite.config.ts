import { defineConfig } from 'vite';

const webHostUrl = process.env.SKOLIO_WEBHOST_URL ?? 'http://localhost:8080';

function manualChunks(id: string) {
  if (id.includes('node_modules/react-dom') || id.includes('node_modules/react/')) {
    return 'vendor-react';
  }

  if (id.includes('node_modules/qrcode')) {
    return 'vendor-qrcode';
  }

  if (id.includes('/src/organization/')) {
    return 'feature-organization';
  }

  if (id.includes('/src/administration/')) {
    return 'feature-administration';
  }

  if (id.includes('/src/identity/')) {
    return 'feature-identity';
  }

  if (id.includes('/src/academics/')) {
    return 'feature-academics';
  }

  if (id.includes('/src/communication/')) {
    return 'feature-communication';
  }

  if (id.includes('/src/shared/')) {
    return 'shared';
  }

  return undefined;
}

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
        manualChunks,
        entryFileNames: 'assets/index.js',
        chunkFileNames: 'assets/chunk-[name].js',
        assetFileNames: 'assets/[name][extname]'
      }
    }
  }
});

