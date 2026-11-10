import path from "node:path";
import { defineConfig, devices } from "@playwright/test";

const baseURL = "http://127.0.0.1:5216";
const databasePath = path.resolve(
  process.env.E2E_DATABASE_PATH ?? path.join(".artifacts", "comtac-e2e.db"),
);

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI
    ? [["line"], ["html", { open: "never", outputFolder: "playwright-report" }]]
    : "list",
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    command:
      "dotnet run --no-build --configuration Release --no-launch-profile --urls http://127.0.0.1:5216",
    url: `${baseURL}/health/live`,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: "E2E",
      DatabaseProvider: "Sqlite",
      ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`,
      BootstrapAdmin__Email: process.env.E2E_ADMIN_EMAIL ?? "e2e-admin@example.invalid",
      BootstrapAdmin__Password: process.env.E2E_ADMIN_PASSWORD ?? "",
    },
  },
});
