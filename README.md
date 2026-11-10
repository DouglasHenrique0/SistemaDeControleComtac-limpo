# Sistema de Controle Comtac

Aplicação interna para registrar a entrada, o atendimento e a saída de veículos. A versão atual é a **1.0.0**.

## O que a versão 1.0 entrega

- entrada de veículos com placa, frota, empresa, cliente, OS e dados do TCO;
- saída protegida contra alteração indevida de campos e liberação duplicada;
- registro de conserto com descrição e valor;
- cálculo automático da tarifa de selagem/ensaio, com ou sem GRU;
- histórico acessível a administradores e colaboradores, organizado por mês;
- financeiro mensal exclusivo para administradores;
- exportação do relatório mensal em Excel;
- autenticação por Identity, perfis e bloqueio após tentativas inválidas;
- autenticação padrão das APIs, rate limit de login e cookies protegidos;
- RLS no SQL Server para as tabelas financeiras;
- logs JSON, Request ID, OpenTelemetry e health checks;
- testes unitários, de integração, arquitetura e E2E.

## Abrir no VS Code

Abra a pasta deste arquivo ou a solução `SistemaDeControleComtac.sln`. Depois execute:

```powershell
dotnet restore
dotnet build
dotnet run
```

O terminal mostrará o endereço local do sistema. Para executar todos os testes:

```powershell
dotnet test SistemaDeControleComtac.sln
```

Os gates Node/TypeScript usam `pnpm`:

```powershell
pnpm install --frozen-lockfile
pnpm quality
pnpm e2e
```

## Banco e atualização

O sistema real usa SQL Server. No desenvolvimento local, a instância configurada é
`localhost\SQLEXPRESS` e o banco é `SistemaDeControleComtac`. A conexão usa a conta do
Windows e fica em User Secrets, fora do Git:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=SistemaDeControleComtac;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

No Docker (servidor), a senha do `sa` fica no arquivo `.env`, ao lado do
`docker-compose.yml`. Ele não vai para o Git nem para a imagem:

```bash
cp .env.example .env   # depois edite e preencha SA_PASSWORD
docker compose up -d
```

O `appsettings.json` não guarda nenhuma connection string: a aplicação lê
`ConnectionStrings:DefaultConnection` de User Secrets (desenvolvimento) ou da variável
de ambiente `ConnectionStrings__DefaultConnection` (produção).

Depois de fazer backup, aplique novas migrações com:

```powershell
dotnet ef database update
```

SQLite permanece somente nos testes automatizados em memória. As migrações SQL Server
ficam em `SqlServerMigrations`; as migrações SQLite antigas são mantidas como histórico,
mas não são executadas pelo sistema atual.

As instruções para consultar tabelas, editar dados e criar backups pelo VS Code estão em
`docs/ADMINISTRACAO-SQL-SERVER.md`.

## Operação em produção

- `GET /health/live`: liveness do processo;
- `GET /health/ready`: banco e armazenamento de relatórios;
- `X-Request-ID`: identificador único presente em toda resposta;
- logs: JSON estruturado no console;
- traces e métricas: OpenTelemetry, com exportação OTLP opcional;
- erros: integração opcional com Sentry.

Os runbooks estão em `docs/OBSERVABILIDADE.md`, `docs/RLS-E-CRIPTOGRAFIA.md` e
`docs/DEPLOY-E-ROLLBACK.md`. O padrão obrigatório para Issues, PRs, segurança, testes e
monitoramento está em `AGENTS.md`.

## Interface

O projeto mantém Razor Pages + Bootstrap. A análise de Cult UI e a decisão de não adicionar
uma segunda stack React/Tailwind estão em `docs/CULT-UI-DECISAO.md`. A identidade do novo
favicon está registrada em `docs/IDENTIDADE-VISUAL.md`.

## Segurança

Não coloque senhas, `.env`, arquivos `.db`, backups `.bak` ou chaves no repositório. As
orientações para criar o primeiro administrador e preparar produção estão em `SECURITY.md`.
