# Decisão sobre Cult UI

## Resultado

Cult UI não será instalado neste projeto. A documentação oficial informa que os componentes são feitos para React e TypeScript, usam Tailwind CSS v4 e se integram ao ecossistema shadcn. O Sistema de Controle Comtac usa ASP.NET Core Razor Pages, HTML server-side e Bootstrap.

Adicionar Cult UI exigiria manter duas stacks de frontend, um pipeline Node para a aplicação, hidratação React e dois sistemas de estilo. Isso aumenta dependências, superfície de ataque, tamanho do cliente e custo de manutenção sem resolver uma necessidade funcional atual.

## Uso aprovado

Podemos usar o Cult UI apenas como referência visual: boa hierarquia, componentes com fonte disponível, movimento discreto e estados vazios bem apresentados. A página 404 aplica esses princípios em HTML/CSS nativo, respeita `prefers-reduced-motion` e não carrega JavaScript adicional.

## Quando reavaliar

Reavaliar somente se houver uma Issue aprovada para migrar todo o frontend para React/Tailwind, com análise de acessibilidade, performance, segurança, testes e plano de retirada do Bootstrap.

Fontes consultadas:

- https://www.cult-ui.com/docs
- https://www.cult-ui.com/docs/installation
- https://github.com/nolly-studio/cult-ui
