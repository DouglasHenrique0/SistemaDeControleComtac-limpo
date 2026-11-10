using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Tests;

public class RelatorioExcelServiceTests
{
    private static readonly string[] CabecalhosEsperados =
    [
        "DESTINATÁRIO", "DOCUMENTO", "PAGAMENTO", "PLACA", "FROTA", "SERVIÇO EXECUTADO",
        "RECEBIMENTO", "VALOR", "NOTA", "DATA", "TÉCNICO", "ITAÚ", "CEF", "DP.ITAÚ", "DP.CEF"
    ];

    [Fact]
    public void Criar_GeraUmaUnicaAbaMensalComGradeSimples()
    {
        var bytes = CriarArquivoCompleto();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = Assert.Single(workbook.Worksheets);

        Assert.Equal("AGOSTO.2026", worksheet.Name);
        Assert.Equal(CabecalhosEsperados, worksheet.Row(1).Cells(1, 15).Select(celula => celula.GetString()).ToArray());
        Assert.Equal("Cliente Pendente", worksheet.Cell(2, 1).GetString());
        Assert.Equal("ABC1D23", worksheet.Cell(2, 4).GetString());
        Assert.Equal("F-10", worksheet.Cell(2, 5).GetString());
        Assert.Equal("Conserto + Selagem com Lacração", worksheet.Cell(2, 6).GetString());
        Assert.Equal(401.47m, worksheet.Cell(2, 8).GetValue<decimal>());
        Assert.Equal("Ana", worksheet.Cell(2, 11).GetString());
        Assert.True(worksheet.Cell(2, 12).IsEmpty());
        Assert.True(worksheet.Cell(2, 15).IsEmpty());
    }

    [Fact]
    public void Criar_PreservaTiposFormatosFiltroCongelamentoETotal()
    {
        var bytes = CriarArquivoCompleto();

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet("AGOSTO.2026");

        Assert.Equal(XLDataType.DateTime, worksheet.Cell(2, 7).DataType);
        Assert.Equal(XLDataType.Number, worksheet.Cell(2, 8).DataType);
        Assert.Equal(XLDataType.DateTime, worksheet.Cell(2, 10).DataType);
        Assert.Equal("dd/mm/yyyy", worksheet.Cell(2, 7).Style.DateFormat.Format);
        Assert.Contains("R$", worksheet.Cell(2, 8).Style.NumberFormat.Format);
        Assert.Equal("dd/mm/yyyy", worksheet.Cell(2, 10).Style.DateFormat.Format);
        Assert.True(worksheet.AutoFilter.IsEnabled);
        Assert.Equal(1, worksheet.SheetView.SplitRow);
        Assert.Equal("SUM(H2:H3)", worksheet.Cell(4, 8).FormulaA1);
        workbook.RecalculateAllFormulas();
        Assert.Equal(612.85, worksheet.Cell(4, 8).GetDouble(), 2);
        Assert.Equal("TOTAL", worksheet.Cell(4, 1).GetString());
    }

    [Fact]
    public void Criar_UsaDestinatarioInformadoSemPerderFallbackDoCliente()
    {
        var relatorio = new RelatorioFinanceiroMensal(2026, 8,
        [
            new RelatorioFinanceiroItem
            {
                Data = new DateTime(2026, 8, 23),
                Placa = "ABC1D23",
                Cliente = "Cliente cadastrado",
                Destinatario = "Destinatário da nota",
                Servico = "Selagem",
                Valor = 301.47m
            },
            new RelatorioFinanceiroItem
            {
                Data = new DateTime(2026, 8, 24),
                Placa = "DEF4G56",
                Cliente = "Cliente usado como destinatário",
                Servico = "Ensaio",
                Valor = 211.38m
            }
        ]);

        var bytes = new RelatorioExcelService().Criar(relatorio, DateTime.Now);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet("AGOSTO.2026");

        Assert.Equal("Destinatário da nota", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Cliente usado como destinatário", worksheet.Cell(3, 1).GetString());
    }

    [Fact]
    public void Criar_TextoQuePareceFormulaPermaneceComoTexto()
    {
        var relatorio = new RelatorioFinanceiroMensal(2026, 8,
        [
            new RelatorioFinanceiroItem
            {
                Data = new DateTime(2026, 8, 23),
                Placa = "ABC1D23",
                Cliente = "Cliente",
                Servico = "+COMANDO",
                Destinatario = "=1+1",
                Valor = 10m
            }
        ]);

        var bytes = new RelatorioExcelService().Criar(relatorio, DateTime.Now);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet("AGOSTO.2026");

        Assert.Equal(XLDataType.Text, worksheet.Cell(2, 1).DataType);
        Assert.Equal("=1+1", worksheet.Cell(2, 1).GetString());
        Assert.Equal(XLDataType.Text, worksheet.Cell(2, 6).DataType);
        Assert.Equal("+COMANDO", worksheet.Cell(2, 6).GetString());
    }

    [Fact]
    public void Criar_MesSemRegistrosContinuaAbrindoComCabecalhoETotalZero()
    {
        var bytes = new RelatorioExcelService().Criar(
            new RelatorioFinanceiroMensal(2026, 8, []),
            new DateTime(2026, 8, 31, 18, 0, 0));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = Assert.Single(workbook.Worksheets);

        Assert.Equal("AGOSTO.2026", worksheet.Name);
        Assert.Equal(CabecalhosEsperados, worksheet.Row(1).Cells(1, 15).Select(celula => celula.GetString()).ToArray());
        Assert.Equal("TOTAL", worksheet.Cell(2, 1).GetString());
        Assert.Equal(0m, worksheet.Cell(2, 8).GetValue<decimal>());
        Assert.False(worksheet.AutoFilter.IsEnabled);
    }

    [Fact]
    public void Criar_GeraArquivoOpenXmlValidoSemGraficos()
    {
        var bytes = CriarArquivoCompleto();
        using var stream = new MemoryStream(bytes);
        using var document = SpreadsheetDocument.Open(stream, false);

        Assert.All(document.WorkbookPart!.WorksheetParts, worksheetPart => Assert.Null(worksheetPart.DrawingsPart));
        var errosOpenXml = new OpenXmlValidator().Validate(document).ToList();
        Assert.True(
            errosOpenXml.Count == 0,
            string.Join(Environment.NewLine, errosOpenXml.Select(erro => $"{erro.Path?.XPath}: {erro.Description}")));
    }

    private static byte[] CriarArquivoCompleto()
    {
        var relatorio = new RelatorioFinanceiroMensal(
            2026,
            8,
            [
                new RelatorioFinanceiroItem
                {
                    RegistroVeiculoId = 1,
                    NumeroDoDia = 1,
                    Data = new DateTime(2026, 8, 23),
                    Placa = "ABC1D23",
                    Frota = "F-10",
                    Cliente = "Cliente Pendente",
                    Servico = "Conserto + Selagem com Lacração",
                    Pagamento = FormaPagamento.Pix,
                    StatusFinanceiro = StatusFinanceiro.Pendente,
                    DataRecebimento = new DateTime(2026, 8, 23),
                    Valor = 401.47m,
                    ValorConserto = 100m,
                    ValorSelagem = 301.47m,
                    Tecnico = "Ana"
                },
                new RelatorioFinanceiroItem
                {
                    RegistroVeiculoId = 2,
                    NumeroDoDia = 2,
                    Data = new DateTime(2026, 8, 24),
                    Placa = "DEF4G56",
                    Frota = "F-20",
                    Cliente = "Cliente Recebido",
                    Servico = "Ensaio",
                    Pagamento = FormaPagamento.Cartao,
                    StatusFinanceiro = StatusFinanceiro.Recebido,
                    DataRecebimento = new DateTime(2026, 8, 24),
                    Valor = 211.38m,
                    ValorSelagem = 211.38m
                }
            ],
            [
                new RelatorioDespesaItem
                {
                    Data = new DateTime(2026, 8, 10),
                    Descricao = "Energia",
                    Categoria = CategoriaDespesa.ContasBasicas,
                    Pagamento = FormaPagamento.Boleto,
                    Valor = 80m
                }
            ]);

        return new RelatorioExcelService().Criar(relatorio, new DateTime(2026, 8, 31, 18, 0, 0));
    }
}
