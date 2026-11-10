using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Tests;

public class RelatorioFinanceiroServiceTests
{
    [Fact]
    public async Task ObterAsync_FiltraNoBancoConsolidaVeiculoECalculaResumo()
    {
        await using var database = await TestDatabase.CreateAsync();
        var agosto = RegistroBuilder.Criar(new DateTime(2026, 8, 5), "AGO1A11");
        agosto.FezSelagem = true;
        agosto.FezConserto = true;
        agosto.DataHoraSaida = new DateTime(2026, 8, 5, 17, 0, 0);
        agosto.TecnicoResponsavel = "Ana";
        var julho = RegistroBuilder.Criar(new DateTime(2026, 7, 5), "JUL1A11");
        database.Context.AddRange(agosto, julho);
        await database.Context.SaveChangesAsync();
        database.Context.Receitas.AddRange(
            new Receita { RegistroVeiculoId = agosto.Id, Tipo = TipoReceita.Selagem, Valor = 301.47m, Descricao = "Escolar — com GRU", Data = new DateTime(2026, 8, 5), Pagamento = FormaPagamento.Pix, StatusFinanceiro = StatusFinanceiro.Recebido, DataRecebimento = new DateTime(2026, 8, 6) },
            new Receita { RegistroVeiculoId = agosto.Id, Tipo = TipoReceita.Conserto, Valor = 100m, Descricao = "Ajuste", Data = new DateTime(2026, 8, 5), Pagamento = FormaPagamento.Pix, StatusFinanceiro = StatusFinanceiro.Recebido, DataRecebimento = new DateTime(2026, 8, 6) },
            new Receita { RegistroVeiculoId = julho.Id, Tipo = TipoReceita.Selagem, Valor = 211.38m, Descricao = "Escolar — sem GRU", Data = new DateTime(2026, 7, 5) });
        database.Context.Despesas.Add(new Despesa
        {
            Data = new DateTime(2026, 8, 10),
            Descricao = "Energia",
            Categoria = CategoriaDespesa.ContasBasicas,
            Valor = 80m
        });
        await database.Context.SaveChangesAsync();

        var service = new RelatorioFinanceiroService(database.Context);
        var relatorio = await service.ObterAsync(2026, 8);

        var item = Assert.Single(relatorio.Itens);
        Assert.Equal(401.47m, relatorio.Total);
        Assert.Equal(80m, relatorio.TotalDespesas);
        Assert.Equal(321.47m, relatorio.Resultado);
        Assert.Equal(0m, relatorio.ContasAReceber);
        Assert.Equal(1, relatorio.QuantidadeVeiculos);
        Assert.Equal(401.47m, relatorio.TicketMedio);
        Assert.Equal(301.47m, relatorio.TotalSelagem);
        Assert.Equal(100m, relatorio.TotalConserto);
        Assert.Equal("Conserto + selagem / ensaio", item.TipoDescricao);
        Assert.Equal("AGO1A11", item.Placa);
        Assert.Equal("F-10", item.Frota);
        Assert.Equal("Conserto + Selagem com Lacração", item.Servico);
        Assert.Equal(new DateTime(2026, 8, 5, 17, 0, 0), item.Data);
        Assert.Equal(FormaPagamento.Pix, item.Pagamento);
        Assert.Equal(StatusFinanceiro.Recebido, item.StatusFinanceiro);
        Assert.Single(relatorio.Despesas);
    }

    [Fact]
    public async Task ObterAsync_UsaTextoHistoricoQuandoNaoHaFlags()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 5));
        registro.ServicosRealizados = "Serviço antigo preservado";
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();
        database.Context.Receitas.Add(new Receita
        {
            RegistroVeiculoId = registro.Id,
            Tipo = TipoReceita.Outro,
            Valor = 50m,
            Descricao = "Histórico",
            Data = new DateTime(2026, 8, 5)
        });
        await database.Context.SaveChangesAsync();

        var relatorio = await new RelatorioFinanceiroService(database.Context).ObterAsync(2026, 8);

        Assert.Equal("Serviço antigo preservado", Assert.Single(relatorio.Itens).Servico);
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task ObterAsync_RejeitaCompetenciaInvalida(int ano, int mes)
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new RelatorioFinanceiroService(database.Context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ObterAsync(ano, mes));
    }
}
