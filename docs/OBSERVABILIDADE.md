# Observabilidade em produção

O sistema usa OpenTelemetry como camada neutra para traces e métricas, logs estruturados
em JSON no `stdout` e Sentry opcional para erros. Datadog e New Relic devem receber os
sinais por OTLP; não instale simultaneamente agentes que dupliquem a mesma telemetria.

Referências oficiais:

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
- [exportadores OpenTelemetry](https://opentelemetry.io/docs/languages/dotnet/exporters/)
- [OTLP no Datadog](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/)
- [OTLP no New Relic](https://docs.newrelic.com/docs/opentelemetry/best-practices/opentelemetry-otlp/)
- [Sentry para ASP.NET Core](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/)

## Sinais implementados

- cada request recebe um identificador novo, devolvido em `X-Request-ID` e correlacionado
  com o Trace ID;
- erros não tratados e falhas de banco são registrados com exceção, stack trace e contexto
  sanitizado;
- todos os logs da aplicação saem como JSON, incluindo scopes de request;
- traces automáticos de ASP.NET Core e `HttpClient`;
- métricas de HTTP, runtime .NET, memória, GC e processo;
- duração e falha de comandos do banco, com fingerprint SHA-256 e sem SQL/parâmetros;
- tentativas de login por resultado e rejeições por rate limit;
- cache instrumentado com `hit`, `miss`, `error` e duração da criação;
- `/health/live` verifica o processo e `/health/ready` verifica banco e armazenamento de
  relatórios. As respostas são detalhadas sem expor credenciais ou exceções.

Todo cache novo deve usar `IApplicationCache`. Acesso direto a `IMemoryCache` fora dessa
implementação perde as métricas obrigatórias.

## Ativar um backend OTLP

Use um OpenTelemetry Collector em produção quando possível. Habilite o exportador e
forneça o endpoint/credenciais somente pelo ambiente ou cofre de segredos:

```text
OpenTelemetry__Otlp__Enabled=true
OTEL_EXPORTER_OTLP_ENDPOINT=https://endpoint-do-coletor:4317
OTEL_EXPORTER_OTLP_HEADERS=chave=censurada
OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=production
```

Para New Relic, use o endpoint regional e o header `api-key` descritos na documentação
oficial. Para Datadog, prefira Collector/Agent com ingestão OTLP. Nunca grave essas chaves
em `appsettings.json`, arquivos `.env` versionados ou logs.

Para habilitar o Sentry, configure `Sentry__Dsn` no cofre do ambiente. A aplicação envia
stack trace, mas mantém PII desabilitada. Ajuste `Sentry__TracesSampleRate` conforme volume
e custo.

## Alertas mínimos configuráveis

Os nomes exatos das consultas dependem do backend escolhido, mas estes alertas devem ser
criados antes de liberar produção:

| Fluxo | Condição inicial sugerida | Ação |
| --- | --- | --- |
| disponibilidade | `/health/ready` falha por 2 minutos | incidente e rollback durante deploy |
| HTTP | taxa 5xx acima de 2% por 5 minutos | incidente |
| latência | p95 acima de 1 segundo por 10 minutos | investigação |
| banco | falhas maiores que zero ou p95 acima de 500 ms | incidente |
| login | aumento de falhas/rate limit acima de 3 desvios do normal | segurança |
| memória/CPU | memória crescente por 15 minutos ou CPU acima de 85% | capacidade |
| relatório | erro de geração ou storage unhealthy | incidente |
| cache | queda anormal da taxa de hit | investigação |

Os números são ponto de partida. Depois de uma semana de telemetria real, estabeleça SLOs
e ajuste os limites para o padrão de uso da empresa.

## Diagnóstico

1. Comece pelo `X-Request-ID` informado pelo usuário.
2. Localize o log JSON com `RequestId` e o `TraceId` correspondente.
3. Confira status, duração, autenticação e spans de banco/HTTP.
4. Em falhas, consulte a exceção completa no backend ou no Sentry; nunca a exponha na tela.
5. Compare com health, taxa de erros e métricas de runtime no mesmo intervalo.
