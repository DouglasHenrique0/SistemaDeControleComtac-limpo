using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Tests;

public sealed class RelatorioArquivoStorageServiceTests : IDisposable
{
    private readonly string _diretorioTeste = Path.Combine(
        Path.GetTempPath(),
        $"comtac-storage-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SalvarAsync_RegeneracaoCriaV2SemSobrescreverOriginal()
    {
        Directory.CreateDirectory(_diretorioTeste);
        var service = CriarService();

        var primeiro = await service.SalvarAsync(2026, 8, [1, 2, 3]);
        var segundo = await service.SalvarAsync(2026, 8, [4, 5, 6]);

        Assert.Equal("Relatorio_Comtac_Agosto_2026.xlsx", primeiro.NomeArquivo);
        Assert.Equal("Relatorio_Comtac_Agosto_2026_v2.xlsx", segundo.NomeArquivo);
        Assert.Equal(1, primeiro.Versao);
        Assert.Equal(2, segundo.Versao);
        Assert.Equal([1, 2, 3], await service.LerAsync(primeiro.CaminhoRelativo));
        Assert.Equal([4, 5, 6], await service.LerAsync(segundo.CaminhoRelativo));
    }

    [Fact]
    public async Task LerAsync_ArquivoInexistenteRetornaNulo()
    {
        Directory.CreateDirectory(_diretorioTeste);
        var resultado = await CriarService().LerAsync("2026/08/inexistente.xlsx");

        Assert.Null(resultado);
    }

    [Theory]
    [InlineData("../segredo.xlsx")]
    [InlineData("C:/segredo.xlsx")]
    [InlineData("..\\segredo.xlsx")]
    [InlineData("\\\\servidor\\compartilhamento\\segredo.xlsx")]
    [InlineData("2026/08/arquivo.txt")]
    public async Task LerAsync_RejeitaCaminhoForaDaPastaPrivada(string caminho)
    {
        Directory.CreateDirectory(_diretorioTeste);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CriarService().LerAsync(caminho));
    }

    public void Dispose()
    {
        var raizCompleta = Path.GetFullPath(_diretorioTeste);
        var raizTemporaria = Path.GetFullPath(Path.GetTempPath());
        if (Directory.Exists(raizCompleta) && raizCompleta.StartsWith(raizTemporaria, StringComparison.OrdinalIgnoreCase))
        {
            Directory.Delete(raizCompleta, recursive: true);
        }
    }

    private RelatorioArquivoStorageService CriarService()
        => new(new TestWebHostEnvironment { ContentRootPath = _diretorioTeste });

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SistemaDeControleComtac.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
