# Padrão obrigatório de engenharia

Estas regras valem para qualquer pessoa, agente ou modelo que altere este repositório.

## Issues, branches e Pull Requests

1. Antes de alterar código, crie ou localize uma Issue que descreva o problema, o risco e os critérios de aceite.
2. Nunca desenvolva diretamente na `main`. Use uma branch curta no formato `tipo/<issue>-descricao`, por exemplo `fix/42-login`.
3. Toda correção, melhoria, manutenção ou funcionalidade deve chegar por Pull Request. Comece como draft enquanto houver trabalho pendente.
4. A descrição da PR deve mencionar a Issue. Use `Closes #42` apenas quando todos os critérios estiverem concluídos; use `Refs #42` para trabalho parcial.
5. Uma PR deve ter escopo revisável, explicar risco/rollback e listar as validações executadas. Não misture alterações sem relação.
6. Nunca faça deploy direto da máquina local ou da `main`. O deploy parte de commit revisado, passa pelos checks e usa ambiente protegido.

## Gates antes de merge

- `dotnet restore`, `dotnet format --verify-no-changes`, build Release e todos os testes devem passar.
- O pipeline Node deve passar em Biome, TypeScript, Knip, ArchContract (no escopo TS) e Playwright.
- Mudanças de regra de negócio precisam de teste unitário; banco/HTTP precisam de integração; fluxo crítico precisa de E2E.
- Alterações arquiteturais devem manter os testes NetArchTest. Mutation testing com Stryker.NET roda periodicamente e antes de releases relevantes.
- Gitleaks não pode encontrar segredos. Nunca reduza um gate apenas para fazer a PR ficar verde.

## Segurança e dados

- Segredos ficam em User Secrets no desenvolvimento e em cofre/variáveis protegidas por ambiente na produção. Nunca registre valores de senha, token, cookie, string de conexão ou parâmetros sensíveis.
- Rotas são autenticadas por padrão. Uma exceção anônima exige `[AllowAnonymous]` explícito e justificativa na PR. APIs devem responder 401/403, sem redirecionar HTML.
- Toda operação mutável de navegador mantém antiforgery. Cookies de autenticação permanecem `HttpOnly`, `Secure` e com `SameSite` adequado.
- Use EF Core/LINQ ou parâmetros explícitos. SQL concatenado com input é proibido.
- Todo input recebe validação server-side, limites de tamanho, normalização e testes de casos inválidos.
- Mudanças em dados sensíveis exigem classificação, criptografia, rotação de chaves e revisão de logs.
- RLS do SQL Server só pode ser ativada com um atributo confiável de posse/escopo e `SESSION_CONTEXT` configurado e limpo por conexão; teste vazamento entre conexões agrupadas.

## Observabilidade e debugging de produção

- Todo request/endpoint tem Request ID único, devolvido em `X-Request-ID` e correlacionado ao Trace ID.
- Todo erro é registrado no backend com exceção, stack trace completo e contexto sanitizado. A resposta ao usuário nunca expõe stack trace.
- Todo log é estruturado e emitido em JSON; texto livre, interpolação de valores e dados sensíveis são proibidos.
- Todo serviço/dependência possui health check detalhado, separando liveness de readiness sem vazar infraestrutura.
- Todo acesso ao banco registra duração, resultado, provedor e fingerprint segura; não registre SQL/valores sensíveis.
- Todo cache deve passar por abstração instrumentada com hit, miss, erro e duração. Um cache sem métricas não pode ser introduzido.
- Serviços expõem métricas de latência, throughput, erros, memória, GC e CPU/processo por OpenTelemetry.
- Fluxos críticos (login, entrada/saída, financeiro, banco e geração de relatório) precisam de métricas e alertas configuráveis para anomalias.
- O pipeline de produção precisa observar health/SLO após deploy e executar rollback automático quando a janela de verificação falhar.

## Stack e decisões

- OpenTelemetry é a camada neutra. Datadog e New Relic recebem OTLP conforme o ambiente; Sentry é opcional para exceções/traces. Evite agentes duplicados emitindo a mesma telemetria.
- O núcleo é ASP.NET Core Razor Pages + Bootstrap. Cult UI é React + TypeScript + Tailwind/shadcn e não deve introduzir uma segunda stack. Ideias visuais podem ser adaptadas em HTML/CSS nativo.
- ArchContract valida apenas TypeScript; NetArchTest protege a arquitetura C#.

## Documentação e conclusão

- Atualize README, SECURITY, runbooks e changelog quando o comportamento operacional mudar.
- Registre comandos executados e resultados na PR.
- Não marque Issue como concluída nem use `Closes` enquanto houver dependência externa, configuração de produção ou critério pendente.
