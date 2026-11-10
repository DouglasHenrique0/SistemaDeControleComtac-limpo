using System.Globalization;
using ClosedXML.Excel;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Services;

public sealed class RelatorioExcelService
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] Cabecalhos =
    [
        "DESTINATÁRIO",
        "DOCUMENTO",
        "PAGAMENTO",
        "PLACA",
        "FROTA",
        "SERVIÇO EXECUTADO",
        "RECEBIMENTO",
        "VALOR",
        "NOTA",
        "DATA",
        "TÉCNICO",
        "ITAÚ",
        "CEF",
        "DP.ITAÚ",
        "DP.CEF"
    ];

    private static readonly string[] CoresDasLinhas =
    [
        "#F8F2F5",
        "#F0F7F8",
        "#F2F7F1",
        "#FAFAFA"
    ];

    private const string CorCabecalho = "#E8EAF0";
    private const string CorBorda = "#9CA1AE";
    private const string CorTotal = "#FFF2B2";
    private const string CorBanco = "#DDF3F7";
    private const string CorDeposito = "#F6E7EF";
    private const string FormatoMoeda = "R$ #,##0.00;[Red](R$ #,##0.00);-";
    private const int LinhaCabecalho = 1;
    private const int PrimeiraLinhaDados = 2;

    public byte[] Criar(RelatorioFinanceiroMensal relatorio, DateTime geradoEm)
    {
        ArgumentNullException.ThrowIfNull(relatorio);
        _ = geradoEm;

        using var workbook = new XLWorkbook();
        workbook.CalculateMode = XLCalculateMode.Auto;

        var worksheet = workbook.Worksheets.Add(ObterNomeDaAba(relatorio.Ano, relatorio.Mes));
        PrepararPlanilha(worksheet);
        EscreverCabecalho(worksheet);

        var linha = PrimeiraLinhaDados;
        foreach (var item in relatorio.Itens)
        {
            EscreverItem(worksheet, linha, item);
            AplicarEstiloDaLinha(worksheet, linha, linha - PrimeiraLinhaDados);
            linha++;
        }

        var ultimaLinhaDados = linha - 1;
        var linhaTotal = linha;
        EscreverTotal(worksheet, linhaTotal, ultimaLinhaDados);
        FinalizarPlanilha(worksheet, ultimaLinhaDados, linhaTotal);

        workbook.RecalculateAllFormulas();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string ObterNomeDaAba(int ano, int mes)
    {
        var competencia = new DateTime(ano, mes, 1);
        return competencia.ToString("MMMM.yyyy", CulturaPtBr).ToUpper(CulturaPtBr);
    }

    private static void PrepararPlanilha(IXLWorksheet worksheet)
    {
        worksheet.ShowGridLines = true;
        worksheet.Style.Font.FontName = "Trebuchet MS";
        worksheet.Style.Font.FontSize = 9;
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        worksheet.Column(1).Width = 24;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 16;
        worksheet.Columns(4, 5).Width = 13;
        worksheet.Column(6).Width = 38;
        worksheet.Column(7).Width = 15;
        worksheet.Column(8).Width = 16;
        worksheet.Column(9).Width = 24;
        worksheet.Column(10).Width = 14;
        worksheet.Column(11).Width = 20;
        worksheet.Columns(12, 15).Width = 11;

        worksheet.Columns(1, 3).Style.Alignment.WrapText = true;
        worksheet.Column(6).Style.Alignment.WrapText = true;
        worksheet.Column(9).Style.Alignment.WrapText = true;
        worksheet.Column(11).Style.Alignment.WrapText = true;
    }

    private static void EscreverCabecalho(IXLWorksheet worksheet)
    {
        for (var indice = 0; indice < Cabecalhos.Length; indice++)
        {
            worksheet.Cell(LinhaCabecalho, indice + 1).SetValue(Cabecalhos[indice]);
        }

        var cabecalho = worksheet.Range(LinhaCabecalho, 1, LinhaCabecalho, Cabecalhos.Length);
        cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml(CorCabecalho);
        cabecalho.Style.Font.Bold = true;
        cabecalho.Style.Font.FontColor = XLColor.FromHtml("#252936");
        cabecalho.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cabecalho.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cabecalho.Style.Alignment.WrapText = true;
        AplicarBordas(cabecalho);

        worksheet.Cell(LinhaCabecalho, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.Cell(LinhaCabecalho, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.Row(LinhaCabecalho).Height = 28;
    }

    private static void EscreverItem(IXLWorksheet worksheet, int linha, RelatorioFinanceiroItem item)
    {
        var destinatario = string.IsNullOrWhiteSpace(item.Destinatario)
            ? item.Cliente
            : item.Destinatario;

        DefinirTexto(worksheet.Cell(linha, 1), destinatario);
        DefinirTexto(worksheet.Cell(linha, 2), item.Documento);
        DefinirTexto(worksheet.Cell(linha, 3), item.Pagamento?.ObterDescricao());
        DefinirTexto(worksheet.Cell(linha, 4), item.Placa);
        DefinirTexto(worksheet.Cell(linha, 5), item.Frota);
        DefinirTexto(worksheet.Cell(linha, 6), item.Servico);

        if (item.DataRecebimento.HasValue)
        {
            worksheet.Cell(linha, 7).Value = item.DataRecebimento.Value;
        }

        worksheet.Cell(linha, 8).Value = item.Valor;
        DefinirTexto(worksheet.Cell(linha, 9), item.Nota);
        worksheet.Cell(linha, 10).Value = item.Data;
        DefinirTexto(worksheet.Cell(linha, 11), item.Tecnico);
        // As colunas bancárias permanecem vazias: não existe uma regra segura para preenchê-las automaticamente.

        worksheet.Cell(linha, 7).Style.DateFormat.Format = "dd/mm/yyyy";
        worksheet.Cell(linha, 8).Style.NumberFormat.Format = FormatoMoeda;
        worksheet.Cell(linha, 10).Style.DateFormat.Format = "dd/mm/yyyy";
        worksheet.Row(linha).Height = 26;
    }

    private static void AplicarEstiloDaLinha(IXLWorksheet worksheet, int linha, int indice)
    {
        var intervalo = worksheet.Range(linha, 1, linha, Cabecalhos.Length);
        intervalo.Style.Fill.BackgroundColor = XLColor.FromHtml(CoresDasLinhas[indice % CoresDasLinhas.Length]);
        AplicarBordas(intervalo);

        worksheet.Range(linha, 12, linha, 13).Style.Fill.BackgroundColor = XLColor.FromHtml(CorBanco);
        worksheet.Range(linha, 14, linha, 15).Style.Fill.BackgroundColor = XLColor.FromHtml(CorDeposito);
    }

    private static void EscreverTotal(IXLWorksheet worksheet, int linhaTotal, int ultimaLinhaDados)
    {
        worksheet.Cell(linhaTotal, 1).SetValue("TOTAL");
        worksheet.Cell(linhaTotal, 1).Style.Font.Bold = true;

        if (ultimaLinhaDados >= PrimeiraLinhaDados)
        {
            worksheet.Cell(linhaTotal, 8).FormulaA1 = $"SUM(H{PrimeiraLinhaDados}:H{ultimaLinhaDados})";
        }
        else
        {
            worksheet.Cell(linhaTotal, 8).Value = 0m;
        }

        worksheet.Cell(linhaTotal, 8).Style.Font.Bold = true;
        worksheet.Cell(linhaTotal, 8).Style.NumberFormat.Format = FormatoMoeda;

        var total = worksheet.Range(linhaTotal, 1, linhaTotal, Cabecalhos.Length);
        total.Style.Fill.BackgroundColor = XLColor.FromHtml(CorTotal);
        total.Style.Border.TopBorder = XLBorderStyleValues.Medium;
        total.Style.Border.TopBorderColor = XLColor.FromHtml(CorBorda);
        total.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        total.Style.Border.BottomBorderColor = XLColor.FromHtml(CorBorda);
        total.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        total.Style.Border.LeftBorderColor = XLColor.FromHtml(CorBorda);
        total.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        total.Style.Border.RightBorderColor = XLColor.FromHtml(CorBorda);
        worksheet.Row(linhaTotal).Height = 24;
    }

    private static void FinalizarPlanilha(IXLWorksheet worksheet, int ultimaLinhaDados, int linhaTotal)
    {
        if (ultimaLinhaDados >= PrimeiraLinhaDados)
        {
            worksheet.Range(LinhaCabecalho, 1, ultimaLinhaDados, Cabecalhos.Length).SetAutoFilter();
        }

        worksheet.SheetView.FreezeRows(LinhaCabecalho);
        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.PageSetup.PrintAreas.Add(LinhaCabecalho, 1, linhaTotal, Cabecalhos.Length);
        worksheet.PageSetup.SetRowsToRepeatAtTop(LinhaCabecalho, LinhaCabecalho);
    }

    private static void AplicarBordas(IXLRange intervalo)
    {
        intervalo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        intervalo.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        intervalo.Style.Border.OutsideBorderColor = XLColor.FromHtml(CorBorda);
        intervalo.Style.Border.InsideBorderColor = XLColor.FromHtml(CorBorda);
    }

    private static void DefinirTexto(IXLCell celula, string? valor)
    {
        celula.SetValue(valor ?? string.Empty);
    }
}
