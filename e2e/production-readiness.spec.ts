import { expect, test } from "@playwright/test";

test("carrega os estilos da tela de login sem redirecionar os assets", async ({ page }) => {
  const assetsComFalha: string[] = [];
  page.on("response", (response) => {
    const caminho = new URL(response.url()).pathname;
    if ((caminho.endsWith(".css") || caminho.endsWith(".js")) && !response.ok()) {
      assetsComFalha.push(`${response.status()} ${caminho}`);
    }
  });

  const response = await page.goto("/Conta/Login");

  expect(response?.status()).toBe(200);
  expect(assetsComFalha).toEqual([]);
  await expect(page.locator(".simple-login-card")).toHaveCSS("border-radius", "14px");
  await expect(page.locator(".simple-login-card")).toHaveCSS("background-color", "rgb(37, 41, 52)");
});

test("exibe a página 404 própria sem alterar o status", async ({ page }) => {
  const response = await page.goto("/rota-que-nao-existe");

  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { name: "Esta página não foi encontrada" })).toBeVisible();
});

test("mantém health checks públicos e detalhados", async ({ request }) => {
  const live = await request.get("/health/live");
  const ready = await request.get("/health/ready");

  expect(live.ok()).toBeTruthy();
  expect(await live.json()).toMatchObject({ status: "Healthy" });
  expect(ready.ok()).toBeTruthy();
  expect(await ready.json()).toMatchObject({
    status: "Healthy",
    checks: expect.arrayContaining([
      expect.objectContaining({ name: "database", status: "Healthy" }),
      expect.objectContaining({ name: "report-storage", status: "Healthy" }),
    ]),
  });
});

test("protege a API sem redirecionar para HTML", async ({ request }) => {
  const response = await request.get("/api/status", { maxRedirects: 0 });

  expect(response.status()).toBe(401);
});

test("administrador executa login, entrada, saída e gestão de usuários", async ({
  page,
}, testInfo) => {
  const password = process.env.E2E_ADMIN_PASSWORD;
  test.skip(!password, "Defina E2E_ADMIN_PASSWORD para executar o fluxo autenticado.");
  const identificador = Date.now().toString();
  const placa = `TST0A${identificador.slice(-2)}`;
  const colaboradorEmail = `e2e-colaborador-${identificador}-${testInfo.retry}@example.invalid`;

  await page.goto("/Conta/Login");
  await page.getByLabel("E-mail").fill(process.env.E2E_ADMIN_EMAIL ?? "e2e-admin@example.invalid");
  await page.getByLabel("Senha").fill(password ?? "");
  await page.getByRole("button", { name: "Entrar" }).click();

  await expect(page).toHaveURL("/");
  await expect(page.getByRole("link", { name: "Administração" })).toBeVisible();

  await page.getByRole("link", { name: "Entrada", exact: true }).click();
  await page.getByLabel("Placa").fill(placa);
  await page.getByLabel("Empresa").fill("Empresa E2E");
  await page.getByLabel("OS Comtac").fill(`OS-E2E-${testInfo.retry}`);
  await page.getByLabel("Cliente").fill("Cliente E2E");
  await page.getByLabel("Modelo TCO").fill("TCO E2E");
  await page.getByLabel("Número de série").fill(`E2E0000${testInfo.retry}`);
  await page.getByRole("button", { name: "Registrar Entrada" }).click();
  await expect(page.getByText("Veículo registrado com sucesso.")).toBeVisible();

  await page.goto("/Veiculos/RelatorioDoDia");
  const registro = page.getByRole("row").filter({ hasText: placa });
  await expect(registro).toBeVisible();
  await registro.getByRole("link", { name: "Registrar Saída" }).click();

  const camposConserto = page.locator("#consertoFields");
  const camposSelagem = page.locator("#selagemFields");
  await expect(camposConserto).toBeHidden();
  await expect(camposSelagem).toBeHidden();

  await page.locator("#fezConserto").check();
  await expect(camposConserto).toBeVisible();
  await page.getByLabel("Observação do conserto").fill("Ajuste E2E");
  await page.getByLabel("Valor do conserto").fill("80.00");

  await page.locator("#fezSelagem").check();
  await expect(camposSelagem).toBeVisible();
  await expect(camposSelagem).toContainText("R$ 301,47");
  await expect(camposSelagem).toContainText("R$ 211,38");
  await expect(camposSelagem).toContainText("R$ 484,08");
  await expect(camposSelagem).toContainText("R$ 393,99");
  await page.getByLabel("Tipo de veículo").selectOption("1");
  await page.getByLabel("Sim, com GRU").check();
  await expect(page.locator("[data-tariff-preview]")).toContainText("R$ 301,47");

  await page.getByLabel("Confirmo que o veículo pode ser liberado.").check();
  await page.getByRole("button", { name: "Registrar saída" }).click();
  await expect(
    page.getByText("Saída registrada. O veículo foi liberado com sucesso."),
  ).toBeVisible();

  await page.goto("/Relatorios/Historico");
  await expect(page.getByRole("heading", { name: "Histórico de veículos" })).toBeVisible();
  await expect(page.getByText(placa, { exact: true })).toBeVisible();

  await page.goto("/Financeiro/Receita");
  await expect(page.getByRole("heading", { name: "Relatório financeiro" })).toBeVisible();
  const lancamento = page.getByRole("row").filter({ hasText: placa });
  await expect(lancamento).toBeVisible();
  await expect(lancamento).toContainText("R$ 381,47");

  const apiStatus = await page.evaluate(async () => {
    const response = await fetch("/api/status");
    return response.status;
  });
  expect(apiStatus).toBe(200);

  await page.goto("/Admin");
  await expect(page.getByRole("heading", { name: "Área exclusiva da Dona" })).toBeVisible();
  await expect(page.getByText("Administrador", { exact: true })).toBeVisible();

  await page.goto("/Conta/Cadastrar");
  await page.getByLabel("Nome").fill("Colaborador E2E");
  await page.getByLabel("Email").fill(colaboradorEmail);
  await page.getByLabel("Senha").fill("Colaborador-E2E-Aa!123");
  await page.getByRole("button", { name: "Cadastrar colaborador" }).click();
  await expect(page.getByText("Colaborador cadastrado com sucesso.")).toBeVisible();
});
