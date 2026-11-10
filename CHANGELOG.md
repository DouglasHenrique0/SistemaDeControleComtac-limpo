# Histórico de versões

## 1.0.0 — 23/08/2026

Primeira versão preparada para avaliação.

### Operação

- campo Frota incluído na entrada;
- formulário de saída simplificado, mantendo a propriedade histórica `ServicosRealizados` no banco;
- fluxo Entrada → Saída → Liberado revisado;
- campos condicionais para conserto e selagem/ensaio;
- valor de conserto limitado a números positivos;
- tarifas de selagem/ensaio calculadas automaticamente.

### Histórico e financeiro

- histórico organizado por mês e liberado para colaboradores autenticados;
- filtros por período e placa;
- financeiro mantido exclusivo para administradores;
- receitas de conserto e selagem registradas na liberação do veículo;
- relatório mensal em tela e exportação limpa para Excel.

### Qualidade e segurança

- versão exibida no sistema;
- consultas de leitura otimizadas;
- proteção contra edição excessiva de campos na saída;
- migrações não destrutivas e banco anterior preservado em backup;
- suíte automatizada com 32 testes;
- artefatos locais de depuração removidos do repositório.
