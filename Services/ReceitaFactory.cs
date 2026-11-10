using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Services;

public static class ReceitaFactory
{
    public static IReadOnlyList<Receita> CriarParaSaida(
        RegistroVeiculo registro,
        bool fezSelagem,
        CategoriaTarifa? categoriaTarifa,
        bool? incluiGru,
        bool fezConserto,
        string? descricaoConserto,
        decimal? valorConserto,
        DateTime data)
    {
        ArgumentNullException.ThrowIfNull(registro);

        var receitas = new List<Receita>();

        if (fezSelagem)
        {
            if (!categoriaTarifa.HasValue || !incluiGru.HasValue)
            {
                throw new ArgumentException("Categoria e opção de GRU são obrigatórias para a selagem.");
            }

            var descricaoGru = incluiGru.Value ? "com GRU" : "sem GRU";
            receitas.Add(new Receita
            {
                RegistroVeiculoId = registro.Id,
                RegistroVeiculo = registro,
                Tipo = TipoReceita.Selagem,
                CategoriaTarifa = categoriaTarifa,
                IncluiGru = incluiGru,
                Valor = TarifaService.Calcular(categoriaTarifa.Value, incluiGru.Value),
                Descricao = $"{categoriaTarifa.Value.ObterDescricao()} — {descricaoGru}",
                Data = data
            });
        }

        if (fezConserto)
        {
            if (string.IsNullOrWhiteSpace(descricaoConserto) || !valorConserto.HasValue || valorConserto.Value <= 0)
            {
                throw new ArgumentException("Descrição e valor positivo são obrigatórios para o conserto.");
            }

            receitas.Add(new Receita
            {
                RegistroVeiculoId = registro.Id,
                RegistroVeiculo = registro,
                Tipo = TipoReceita.Conserto,
                Valor = decimal.Round(valorConserto.Value, 2, MidpointRounding.AwayFromZero),
                Descricao = descricaoConserto.Trim(),
                Data = data
            });
        }

        return receitas;
    }
}
