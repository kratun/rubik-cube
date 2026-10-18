import react from "@vitejs/plugin-react";
import { configDefaults, defineConfig } from "vitest/config";

const DEV_SERVER_PORT = 5173;
const API_ORIGIN = "http://localhost:5080";
const API_PATH_PREFIX = "/api";
// Tests read the TTC fixtures of the backend tests (tests/Fixtures) so both sides share one source.
const SHARED_FIXTURES_DIR = "../tests/Fixtures";
const E2E_DIR = "e2e";

// Same-origin topology (AD-8): the app calls relative /api paths and Vite forwards them to the API.
// Vite serves index.html for other paths such as /cubes/<id>, so a shared link opens the app.
export default defineConfig({
  plugins: [react()],
  server: {
    port: DEV_SERVER_PORT,
    strictPort: true,
    proxy: { [API_PATH_PREFIX]: API_ORIGIN },
    fs: { allow: [".", SHARED_FIXTURES_DIR] },
  },
  test: {
    environment: "jsdom",
    globals: true,
    exclude: [...configDefaults.exclude, E2E_DIR],
    setupFiles: ["./src/setupTests.ts"],
  },
});
