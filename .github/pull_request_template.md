## Issue

Refs #

> Troque `Refs` por `Closes` somente quando todos os critérios da Issue estiverem concluídos.

## O que mudou

-

## Segurança e observabilidade

- [ ] Não inclui segredos, dados pessoais nem parâmetros sensíveis em código/logs.
- [ ] Request IDs, logs JSON, métricas e health checks foram mantidos ou ampliados.
- [ ] Autenticação, autorização, antiforgery, cookies e validação foram revisados quando aplicável.

## Validação

- [ ] `dotnet format --verify-no-changes`
- [ ] `dotnet build --configuration Release --no-restore`
- [ ] `dotnet test --configuration Release --no-build`
- [ ] `pnpm quality`
- [ ] `pnpm e2e`

## Deploy e rollback

Risco:

Monitoramento após deploy:

Rollback:
