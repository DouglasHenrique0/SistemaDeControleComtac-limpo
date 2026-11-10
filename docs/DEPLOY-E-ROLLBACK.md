# Deploy monitorado e rollback

O deploy de produção só pode partir de uma Pull Request aprovada e de um commit imutável.
Hoje o repositório possui os gates e a especificação abaixo, mas o deploy e o rollback
automáticos ainda não estão ativos porque o provedor de hospedagem não foi escolhido.

## Gates obrigatórios

1. Gitleaks sem segredos.
2. `dotnet format`, build Release e testes unitários/de integração verdes.
3. Biome, TypeScript, Knip e ArchContract verdes.
4. Playwright verde e cobertura publicada no Codecov.
5. Stryker.NET dentro do limite definido para release relevante.
6. backup verificado antes de migration destrutiva.
7. aprovação do ambiente protegido de produção.

## Sequência do deploy

1. gerar um artefato versionado uma única vez;
2. aplicar migrations com lock e estratégia de compatibilidade retroativa;
3. publicar em slot/canário sem remover a versão anterior;
4. aguardar `/health/live` e `/health/ready` ficarem saudáveis;
5. executar smoke tests autenticados e não autenticados;
6. encaminhar uma fração do tráfego e observar por pelo menos cinco minutos;
7. promover somente se health, taxa 5xx, p95, banco, CPU e memória estiverem dentro do
   limite configurado;
8. registrar versão, commit, horário, resultado e operador como evento no backend de
   observabilidade.

## Rollback automático

Durante a janela de verificação, qualquer uma destas condições deve acionar rollback para
o artefato anterior: readiness unhealthy por dois minutos, smoke test crítico falhando,
taxa 5xx acima de 2% ou regressão de p95 maior que 50%. O rollback interrompe a promoção,
restaura o slot/versão anterior e abre incidente com link para deploy, traces e logs.

Migration destrutiva não deve depender de rollback de schema. Use o padrão expand/contract:
primeiro adicione de forma compatível, migre dados e só remova a estrutura antiga em uma
release posterior.

## Pendências para ativação

- escolher o alvo (Azure App Service, container/Kubernetes, IIS ou outro);
- criar ambientes e regras de proteção no GitHub;
- definir registry/artefato e identidade do deploy;
- conectar Datadog, New Relic ou outro backend OTLP e criar os alertas;
- codificar no workflow o mecanismo específico de canário/slot e rollback;
- executar e documentar um teste real de rollback antes do go-live.
