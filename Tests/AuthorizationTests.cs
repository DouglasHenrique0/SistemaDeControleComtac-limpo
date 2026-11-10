using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Pages;
using SistemaDeControleComtac.Pages.Financeiro;
using SistemaDeControleComtac.Pages.Relatorios;

namespace SistemaDeControleComtac.Tests;

public class AuthorizationTests
{
    [Fact]
    public void Historico_ExigeLoginSemRestringirAoAdmin()
    {
        var authorize = typeof(HistoricoModel).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrWhiteSpace(authorize.Roles));
    }

    [Fact]
    public void Financeiro_ContinuaRestritoAoAdmin()
    {
        var authorize = typeof(ReceitaModel).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(ApplicationRoles.Admin, authorize.Roles);
    }

    [Fact]
    public void RelatoriosExcel_ExigeExplicitamenteRoleAdmin()
    {
        var authorize = typeof(RelatoriosExcelModel).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(ApplicationRoles.Admin, authorize.Roles);
        Assert.DoesNotContain(ApplicationRoles.Colaborador, authorize.Roles ?? string.Empty);
    }

    [Fact]
    public void Privacidade_PodeSerConsultadaSemLogin()
    {
        Assert.NotNull(typeof(PrivacyModel).GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
