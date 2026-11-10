using System.Globalization;

namespace SistemaDeControleComtac.Services;

public sealed class RelatorioArquivoStorageService
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly string _diretorioRaiz;

    public RelatorioArquivoStorageService(IWebHostEnvironment environment)
    {
        _diretorioRaiz = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, "App_Data", "RelatoriosExcel"));
    }

    public async Task<ArquivoRelatorioSalvo> SalvarAsync(
        int ano,
        int mes,
        byte[] conteudo,
        CancellationToken cancellationToken = default)
    {
        RelatorioFinanceiroService.ValidarCompetencia(ano, mes);
        ArgumentNullException.ThrowIfNull(conteudo);

        var diretorioMes = Path.Combine(
            _diretorioRaiz,
            ano.ToString("0000", CultureInfo.InvariantCulture),
            mes.ToString("00", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(diretorioMes);

        var competencia = new DateTime(ano, mes, 1);
        var nomeMes = CulturaPtBr.TextInfo.ToTitleCase(competencia.ToString("MMMM", CulturaPtBr));
        var nomeBase = $"Relatorio_Comtac_{nomeMes}_{ano}";

        for (var versao = 1; versao <= 9999; versao++)
        {
            var sufixo = versao == 1 ? string.Empty : $"_v{versao}";
            var nomeArquivo = $"{nomeBase}{sufixo}.xlsx";
            var caminhoCompleto = Path.Combine(diretorioMes, nomeArquivo);

            try
            {
                await using var stream = new FileStream(
                    caminhoCompleto,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await stream.WriteAsync(conteudo, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                var caminhoRelativo = Path.GetRelativePath(_diretorioRaiz, caminhoCompleto)
                    .Replace(Path.DirectorySeparatorChar, '/');

                return new ArquivoRelatorioSalvo(nomeArquivo, caminhoRelativo, versao);
            }
            catch (IOException) when (File.Exists(caminhoCompleto))
            {
                // Outra geração já ocupou este nome. A próxima tentativa usa v2, v3...
            }
        }

        throw new IOException("Não foi possível determinar uma versão livre para o relatório.");
    }

    public async Task<byte[]?> LerAsync(string caminhoRelativo, CancellationToken cancellationToken = default)
    {
        var caminhoCompleto = ObterCaminhoAbsolutoSeguro(caminhoRelativo);
        if (!File.Exists(caminhoCompleto))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(caminhoCompleto, cancellationToken);
    }

    public void ExcluirSeExistir(string caminhoRelativo)
    {
        var caminhoCompleto = ObterCaminhoAbsolutoSeguro(caminhoRelativo);
        if (File.Exists(caminhoCompleto))
        {
            File.Delete(caminhoCompleto);
        }
    }

    private string ObterCaminhoAbsolutoSeguro(string caminhoRelativo)
    {
        if (string.IsNullOrWhiteSpace(caminhoRelativo) ||
            Path.IsPathRooted(caminhoRelativo) ||
            caminhoRelativo.Contains(':', StringComparison.Ordinal) ||
            caminhoRelativo.Contains('\\', StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(caminhoRelativo), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O caminho do relatório é inválido.");
        }

        var caminhoNormalizado = caminhoRelativo.Replace('/', Path.DirectorySeparatorChar);
        var caminhoCompleto = Path.GetFullPath(Path.Combine(_diretorioRaiz, caminhoNormalizado));
        var raizComSeparador = _diretorioRaiz.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!caminhoCompleto.StartsWith(raizComSeparador, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("O caminho do relatório está fora do armazenamento permitido.");
        }

        return caminhoCompleto;
    }
}

public sealed record ArquivoRelatorioSalvo(string NomeArquivo, string CaminhoRelativo, int Versao);
