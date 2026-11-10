using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Services;

public sealed class RelatorioFinanceiroService
{
    private readonly AppDbContext _context;

    public RelatorioFinanceiroService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RelatorioFinanceiroMensal> ObterAsync(
        int ano,
        int mes,
        CancellationToken cancellationToken = default)
    {
        ValidarCompetencia(ano, mes);

        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1);

        var linhasReceita = await _context.Receitas
            .AsNoTracking()
            .Where(receita => receita.Data >= inicio && receita.Data < fim)
            .OrderBy(receita => receita.Data)
            .ThenBy(receita => receita.RegistroVeiculo.NumeroDoDia)
            .ThenBy(receita => receita.Tipo)
            .Select(receita => new FonteReceita
            {
                RegistroVeiculoId = receita.RegistroVeiculoId,
                NumeroDoDia = receita.RegistroVeiculo.NumeroDoDia,
                DataHoraEntrada = receita.RegistroVeiculo.DataHoraEntrada,
                DataHoraSaida = receita.RegistroVeiculo.DataHoraSaida,
                Placa = receita.RegistroVeiculo.Placa,
                Frota = receita.RegistroVeiculo.Frota,
                Empresa = receita.RegistroVeiculo.Empresa,
                Cliente = receita.RegistroVeiculo.Cliente,
                OSComtac = receita.RegistroVeiculo.OSComtac,
                FezConserto = receita.RegistroVeiculo.FezConserto,
                FezSelagem = receita.RegistroVeiculo.FezSelagem,
                FezEnsaio = receita.RegistroVeiculo.FezEnsaio,
                ServicosRealizados = receita.RegistroVeiculo.ServicosRealizados,
                Tecnico = receita.RegistroVeiculo.TecnicoResponsavel,
                Tipo = receita.Tipo,
                Valor = receita.Valor,
                Destinatario = receita.Destinatario,
                Documento = receita.Documento,
                Pagamento = receita.Pagamento,
                StatusFinanceiro = receita.StatusFinanceiro,
                DataRecebimento = receita.DataRecebimento,
                Nota = receita.Nota
            })
            .ToListAsync(cancellationToken);

        var receitas = linhasReceita
            .GroupBy(item => item.RegistroVeiculoId)
            .Select(grupo => CriarItemReceita(grupo.OrderBy(item => item.Tipo).ToList()))
            .OrderBy(item => item.Data)
            .ThenBy(item => item.NumeroDoDia)
            .ToList();

        var despesas = await _context.Despesas
            .AsNoTracking()
            .Where(despesa => despesa.Data >= inicio && despesa.Data < fim)
            .OrderBy(despesa => despesa.Data)
            .ThenBy(despesa => despesa.Id)
            .Select(despesa => new RelatorioDespesaItem
            {
                Data = despesa.Data,
                Descricao = despesa.Descricao,
                Categoria = despesa.Categoria,
                Pagamento = despesa.Pagamento,
                Valor = despesa.Valor,
                Observacao = despesa.Observacao
            })
            .ToListAsync(cancellationToken);

        return new RelatorioFinanceiroMensal(ano, mes, receitas, despesas);
    }

    public async Task<IReadOnlyList<int>> ObterAnosDisponiveisAsync(
        int anoAtual,
        CancellationToken cancellationToken = default)
    {
        var anosReceitas = await _context.Receitas
            .AsNoTracking()
            .Select(receita => receita.Data.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        var anosDespesas = await _context.Despesas
            .AsNoTracking()
            .Select(despesa => despesa.Data.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        return anosReceitas
            .Concat(anosDespesas)
            .Append(anoAtual)
            .Distinct()
            .OrderByDescending(ano => ano)
            .ToList();
    }

    public static void ValidarCompetencia(int ano, int mes)
    {
        if (ano is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), "O ano informado é inválido.");
        }

        if (mes is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mes), "O mês informado é inválido.");
        }
    }

    private static RelatorioFinanceiroItem CriarItemReceita(IReadOnlyList<FonteReceita> linhas)
    {
        var primeira = linhas[0];
        var pagamentos = linhas
            .Where(item => item.Pagamento.HasValue)
            .Select(item => item.Pagamento!.Value)
            .Distinct()
            .ToList();

        var recebido = linhas.All(item => item.StatusFinanceiro == StatusFinanceiro.Recebido);
        var datasRecebimento = linhas
            .Where(item => item.DataRecebimento.HasValue)
            .Select(item => item.DataRecebimento!.Value)
            .ToList();
        var notas = linhas
            .Select(item => item.Nota?.Trim())
            .Where(nota => !string.IsNullOrWhiteSpace(nota))
            .Select(nota => nota!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RelatorioFinanceiroItem
        {
            RegistroVeiculoId = primeira.RegistroVeiculoId,
            NumeroDoDia = primeira.NumeroDoDia,
            Data = DataOperacionalService.Obter(primeira.DataHoraEntrada, primeira.DataHoraSaida),
            Placa = primeira.Placa,
            Frota = primeira.Frota,
            Empresa = primeira.Empresa,
            Cliente = primeira.Cliente,
            OSComtac = primeira.OSComtac,
            Servico = ServicoDescricaoService.Obter(
                primeira.FezConserto,
                primeira.FezSelagem,
                primeira.FezEnsaio,
                primeira.ServicosRealizados),
            Tecnico = primeira.Tecnico,
            Destinatario = PrimeiroTexto(linhas.Select(item => item.Destinatario)),
            Documento = PrimeiroTexto(linhas.Select(item => item.Documento)),
            Pagamento = pagamentos.Count == 1 ? pagamentos[0] : null,
            StatusFinanceiro = recebido ? StatusFinanceiro.Recebido : StatusFinanceiro.Pendente,
            DataRecebimento = recebido && datasRecebimento.Count > 0 ? datasRecebimento.Max() : null,
            Nota = notas.Count == 0 ? null : string.Join(" | ", notas),
            Valor = linhas.Sum(item => item.Valor),
            ValorSelagem = linhas.Where(item => item.Tipo == TipoReceita.Selagem).Sum(item => item.Valor),
            ValorConserto = linhas.Where(item => item.Tipo == TipoReceita.Conserto).Sum(item => item.Valor)
        };
    }

    private static string? PrimeiroTexto(IEnumerable<string?> valores) => valores
        .Select(valor => valor?.Trim())
        .FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor));

    private sealed class FonteReceita
    {
        public int RegistroVeiculoId { get; init; }
        public int NumeroDoDia { get; init; }
        public DateTime DataHoraEntrada { get; init; }
        public DateTime? DataHoraSaida { get; init; }
        public string Placa { get; init; } = string.Empty;
        public string? Frota { get; init; }
        public string Empresa { get; init; } = string.Empty;
        public string Cliente { get; init; } = string.Empty;
        public string OSComtac { get; init; } = string.Empty;
        public bool FezConserto { get; init; }
        public bool FezSelagem { get; init; }
        public bool FezEnsaio { get; init; }
        public string? ServicosRealizados { get; init; }
        public string? Tecnico { get; init; }
        public TipoReceita Tipo { get; init; }
        public decimal Valor { get; init; }
        public string? Destinatario { get; init; }
        public string? Documento { get; init; }
        public FormaPagamento? Pagamento { get; init; }
        public StatusFinanceiro StatusFinanceiro { get; init; }
        public DateTime? DataRecebimento { get; init; }
        public string? Nota { get; init; }
    }
}

public sealed class RelatorioFinanceiroMensal
{
    public RelatorioFinanceiroMensal(
        int ano,
        int mes,
        IReadOnlyList<RelatorioFinanceiroItem> itens,
        IReadOnlyList<RelatorioDespesaItem>? despesas = null)
    {
        Ano = ano;
        Mes = mes;
        Itens = itens;
        Despesas = despesas ?? [];
    }

    public int Ano { get; }
    public int Mes { get; }
    public IReadOnlyList<RelatorioFinanceiroItem> Itens { get; }
    public IReadOnlyList<RelatorioDespesaItem> Despesas { get; }
    public decimal Total => Itens.Sum(item => item.Valor);
    public decimal TotalDespesas => Despesas.Sum(item => item.Valor);
    public decimal Resultado => Total - TotalDespesas;
    public decimal ContasAReceber => Itens
        .Where(item => item.StatusFinanceiro == StatusFinanceiro.Pendente)
        .Sum(item => item.Valor);
    public int QuantidadeVeiculos => Itens.Count;
    public decimal TicketMedio => QuantidadeVeiculos == 0 ? 0m : Total / QuantidadeVeiculos;
    public decimal TotalSelagem => Itens.Sum(item => item.ValorSelagem);
    public decimal TotalConserto => Itens.Sum(item => item.ValorConserto);
}

public sealed class RelatorioFinanceiroItem
{
    public int RegistroVeiculoId { get; init; }
    public int NumeroDoDia { get; init; }
    public DateTime Data { get; init; }
    public string Placa { get; init; } = string.Empty;
    public string? Frota { get; init; }
    public string Empresa { get; init; } = string.Empty;
    public string Cliente { get; init; } = string.Empty;
    public string OSComtac { get; init; } = string.Empty;
    public string Servico { get; init; } = string.Empty;
    public string? Tecnico { get; init; }
    public string? Destinatario { get; init; }
    public string? Documento { get; init; }
    public FormaPagamento? Pagamento { get; init; }
    public StatusFinanceiro StatusFinanceiro { get; init; } = StatusFinanceiro.Pendente;
    public DateTime? DataRecebimento { get; init; }
    public string? Nota { get; init; }
    public decimal Valor { get; init; }
    public decimal ValorSelagem { get; init; }
    public decimal ValorConserto { get; init; }

    public TipoReceita Tipo => ValorSelagem > 0m && ValorConserto == 0m
        ? TipoReceita.Selagem
        : ValorConserto > 0m && ValorSelagem == 0m
            ? TipoReceita.Conserto
            : TipoReceita.Outro;

    public string TipoDescricao => ValorSelagem > 0m && ValorConserto > 0m
        ? "Conserto + selagem / ensaio"
        : Tipo.ObterDescricao();

    public string Descricao => Servico;
}

public sealed class RelatorioDespesaItem
{
    public DateTime Data { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public CategoriaDespesa Categoria { get; init; }
    public FormaPagamento? Pagamento { get; init; }
    public decimal Valor { get; init; }
    public string? Observacao { get; init; }
}
