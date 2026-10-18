import { defineConfig } from "@playwright/test";

const WEB_ORIGIN = "http://localhost:5173";
const API_HEALTH_URL = "http://localhost:5080/health";
const API_COMMAND = "dotnet run --project ../src/Rubik.Api --launch-profile http";
const WEB_COMMAND = "npm run dev";
const SERVER_START_TIMEOUT_MS = 120_000;

// `npm run e2e` starts the API (5080) and Vite (5173), waits until both answer, and stops them at the end.
// Servers already running on those ports are not reused: the flow must start from a fresh in-memory API.
export default defineConfig({
  testDir: "./e2e",
  reporter: "list",
  use: { baseURL: WEB_ORIGIN },
  projects: [{ name: "chromium", use: { browserName: "chromium" } }],
  webServer: [
    {
      command: API_COMMAND,
      url: API_HEALTH_URL,
      reuseExistingServer: false,
      timeout: SERVER_START_TIMEOUT_MS,
    },
    {
      command: WEB_COMMAND,
      url: WEB_ORIGIN,
      reuseExistingServer: false,
      timeout: SERVER_START_TIMEOUT_MS,
    },
  ],
});
