using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Tests;

public sealed class PageIntegrationTests(ComtacWebApplicationFactory factory)
    : IClassFixture<ComtacWebApplicationFactory>
{
    [Theory]
    [InlineData("/Conta/Login")]
    [InlineData("/Conta/AcessoNegado")]
    [InlineData("/Privacy")]
    [InlineData("/Error")]
    public async Task PaginasPublicas_AbremSemErro(string caminho)
    {
        using var client = factory.CriarCliente();

        var resposta = await client.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("text/html", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/css/site.css", "text/css")]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css", "text/css")]
    [InlineData("/js/site.js", "text/javascript")]
    [InlineData("/lib/bootstrap/dist/js/bootstrap.bundle.min.js", "text/javascript")]
    public async Task ArquivosEstaticos_SaoPublicosENaoRedirecionamParaLogin(
        string caminho,
        string mediaType)
    {
        using var client = factory.CriarCliente();

        var resposta = await client.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Null(resposta.Headers.Location);
        Assert.Equal(mediaType, resposta.Content.Headers.ContentType?.MediaType);
        Assert.True(resposta.Content.Headers.ContentLength > 0);
    }

    [Fact]
    public async Task ArquivoEstaticoCompactado_EntregaCorpoGzip()
    {
        using var client = factory.CriarCliente();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/css/site.css");
        request.Headers.AcceptEncoding.ParseAdd("gzip");

        using var resposta = await client.SendAsync(request);
        var conteudo = await resposta.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Null(resposta.Headers.Location);
        Assert.Equal("text/css", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("gzip", resposta.Content.Headers.ContentEncoding);
        Assert.NotEmpty(conteudo);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Veiculos/RegistrarEntrada")]
    [InlineData("/Veiculos/RelatorioDoDia")]
    [InlineData("/Relatorios/Historico")]
    [InlineData("/Admin")]
    [InlineData("/Financeiro/Receita")]
    [InlineData("/Financeiro/RelatoriosExcel")]
    [InlineData("/Conta/Cadastrar")]
    [InlineData("/Conta/AlterarSenha")]
    public async Task Administrador_AbreTodasAsPaginasProtegidas(string caminho)
    {
        using var client = factory.CriarCliente(ApplicationRoles.Admin);

        var resposta = await client.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Administrador_AbreDetalhesDeRegistroExistente()
    {
        using var client = factory.CriarCliente(ApplicationRoles.Admin);

        var resposta = await client.GetAsync($"/Veiculos/Detalhes?id={factory.RegistroId}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("ABC1D23", await resposta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Administrador_AbreFormularioDeSaidaDeRegistroExistente()
    {
        using var client = factory.CriarCliente(ApplicationRoles.Admin);

        var resposta = await client.GetAsync($"/Veiculos/RegistrarSaida/{factory.RegistroId}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var conteudo = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("Registrar saída", conteudo, StringComparison.Ordinal);
        Assert.Contains("ABC1D23", conteudo, StringComparison.Ordinal);
        Assert.Contains("id=\"consertoFields\"", conteudo, StringComparison.Ordinal);
        Assert.Contains("hidden", conteudo, StringComparison.Ordinal);
        Assert.Contains("R$ 301,47", conteudo, StringComparison.Ordinal);
        Assert.Contains("R$ 211,38", conteudo, StringComparison.Ordinal);
        Assert.Contains("R$ 484,08", conteudo, StringComparison.Ordinal);
        Assert.Contains("R$ 393,99", conteudo, StringComparison.Ordinal);
        Assert.Contains("registrar-saida", conteudo, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Veiculos/RegistrarEntrada")]
    [InlineData("/Veiculos/RelatorioDoDia")]
    [InlineData("/Relatorios/Historico")]
    [InlineData("/Conta/AlterarSenha")]
    public async Task Colaborador_AbreSomenteAreasOperacionais(string caminho)
    {
        using var client = factory.CriarCliente(ApplicationRoles.Colaborador);

        var resposta = await client.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Financeiro/Receita")]
    [InlineData("/Financeiro/RelatoriosExcel")]
    [InlineData("/Conta/Cadastrar")]
    public async Task Colaborador_NaoAcessaAreasAdministrativas(string caminho)
    {
        using var client = factory.CriarCliente(ApplicationRoles.Colaborador);

        var resposta = await client.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Veiculos/RegistrarEntrada")]
    [InlineData("/Relatorios/Historico")]
    [InlineData("/Financeiro/Receita")]
    public async Task Visitante_NaoAcessaPaginasProtegidas(string caminho)
    {
        using var client = factory.CriarCliente();

        var resposta = await client.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/Conta/Login")]
    [InlineData("/Conta/Logout")]
    [InlineData("/Conta/Cadastrar")]
    [InlineData("/Veiculos/RegistrarEntrada")]
    [InlineData("/Veiculos/RegistrarSaida")]
    [InlineData("/Financeiro/RelatoriosExcel?handler=Gerar")]
    public async Task FormulariosPost_SemTokenAntifalsificacao_SaoBloqueados(string caminho)
    {
        using var client = factory.CriarCliente(ApplicationRoles.Admin);

        var resposta = await client.PostAsync(caminho, new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task RespostasIncluemCabecalhosBasicosDeSeguranca()
    {
        using var client = factory.CriarCliente();

        var resposta = await client.GetAsync("/Privacy");

        Assert.Equal("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", resposta.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("same-origin", resposta.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'self'", resposta.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("camera=(), microphone=(), geolocation=()", resposta.Headers.GetValues("Permissions-Policy").Single());
    }

    [Fact]
    public async Task TodaRespostaIncluiRequestIdUnico()
    {
        using var client = factory.CriarCliente();

        var primeira = await client.GetAsync("/Privacy");
        var segunda = await client.GetAsync("/Privacy");
        var primeiroId = primeira.Headers.GetValues("X-Request-ID").Single();
        var segundoId = segunda.Headers.GetValues("X-Request-ID").Single();

        Assert.Matches("^[a-f0-9]{32}$", primeiroId);
        Assert.Matches("^[a-f0-9]{32}$", segundoId);
        Assert.NotEqual(primeiroId, segundoId);
    }

    [Fact]
    public async Task HealthChecks_ExpõemLivenessEReadinessSemAutenticacao()
    {
        using var client = factory.CriarCliente();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");
        var readyContent = await ready.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Contains("\"database\"", readyContent, StringComparison.Ordinal);
        Assert.Contains("\"report-storage\"", readyContent, StringComparison.Ordinal);
        Assert.DoesNotContain("exception", readyContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiExigeAutenticacaoESemRedirecionamentoHtml()
    {
        using var client = factory.CriarCliente();

        var response = await client.GetAsync("/api/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public void CookieDeAutenticacao_TemProtecoesObrigatorias()
    {
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal("__Host-Comtac.Auth", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
    }

    [Fact]
    public async Task Login_AplicaRateLimitPorOrigem()
    {
        using var client = factory.CriarCliente();
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 11; attempt++)
        {
            var response = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent([]));
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }

    [Fact]
    public async Task EnderecosInvalidosRetornamRespostaControlada()
    {
        using var client = factory.CriarCliente(ApplicationRoles.Admin);

        var detalheInexistente = await client.GetAsync("/Veiculos/Detalhes?id=999999");
        var arquivoInexistente = await client.GetAsync("/Financeiro/RelatoriosExcel?handler=Baixar&id=999999");
        var paginaInexistente = await client.GetAsync("/pagina-que-nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, detalheInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, arquivoInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, paginaInexistente.StatusCode);
        Assert.Contains(
            "Esta página não foi encontrada",
            await paginaInexistente.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }
}

public sealed class ComtacWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public ComtacWebApplicationFactory()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"comtac-integration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        using var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        var registro = RegistroBuilder.Criar(DateTime.Today, "ABC1D23");
        context.RegistrosVeiculos.Add(registro);
        context.SaveChanges();
        RegistroId = registro.Id;
    }

    public int RegistroId { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DatabaseProvider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CriarCliente(string? role = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(role))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        }

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (disposing && File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers[RoleHeader].ToString();
        if (string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "Usuário de teste"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
