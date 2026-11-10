using NetArchTest.Rules;

namespace SistemaDeControleComtac.Tests;

public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Program).Assembly;

    [Fact]
    public void Models_NaoDependemDePaginasOuInfraestrutura()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("SistemaDeControleComtac.Models")
            .ShouldNot()
            .HaveDependencyOnAny(
                "SistemaDeControleComtac.Pages",
                "SistemaDeControleComtac.Data",
                "SistemaDeControleComtac.Health",
                "SistemaDeControleComtac.Middleware")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Servicos_NaoDependemDaCamadaDeApresentacao()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("SistemaDeControleComtac.Services")
            .ShouldNot()
            .HaveDependencyOn("SistemaDeControleComtac.Pages")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(TestResult result)
    {
        return result.FailingTypeNames is null
            ? "Violação arquitetural sem tipo identificado."
            : $"Tipos fora do contrato: {string.Join(", ", result.FailingTypeNames)}";
    }
}
