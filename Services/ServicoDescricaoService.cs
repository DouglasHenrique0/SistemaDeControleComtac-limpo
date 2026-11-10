using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Services;

public static class ServicoDescricaoService
{
    public static string Obter(RegistroVeiculo registro)
    {
        ArgumentNullException.ThrowIfNull(registro);
        return Obter(
            registro.FezConserto,
            registro.FezSelagem,
            registro.FezEnsaio,
            registro.ServicosRealizados);
    }

    public static string Obter(bool fezConserto, bool fezSelagem, bool fezEnsaio, string? textoHistorico)
    {
        var servicos = new List<string>();

        if (fezConserto)
        {
            servicos.Add("Conserto");
        }

        if (fezSelagem)
        {
            servicos.Add("Selagem com Lacração");
        }
        else if (fezEnsaio)
        {
            servicos.Add("Ensaio");
        }

        if (servicos.Count > 0)
        {
            return string.Join(" + ", servicos);
        }

        return string.IsNullOrWhiteSpace(textoHistorico)
            ? "Não informado"
            : textoHistorico.Trim();
    }
}
