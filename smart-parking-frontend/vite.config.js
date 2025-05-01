import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5126',
        changeOrigin: true,
        secure: false
      },
      '/parkingHub': {
        target: 'http://localhost:5126',
        changeOrigin: true,
        secure: false,
        ws: true
      },
      '/DebugFrames': {
        target: 'http://localhost:5126',
        changeOrigin: true,
        secure: false
      }
    }
  }
});
