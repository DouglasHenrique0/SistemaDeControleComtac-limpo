using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Services;

public static class TarifaService
{
    public const decimal EscolarComGru = 301.47m;
    public const decimal EscolarSemGru = 211.38m;
    public const decimal PesadoComGru = 484.08m;
    public const decimal PesadoSemGru = 393.99m;

    public static decimal Calcular(CategoriaTarifa categoria, bool incluiGru) => (categoria, incluiGru) switch
    {
        (CategoriaTarifa.Escolar, true) => EscolarComGru,
        (CategoriaTarifa.Escolar, false) => EscolarSemGru,
        (CategoriaTarifa.Pesado, true) => PesadoComGru,
        (CategoriaTarifa.Pesado, false) => PesadoSemGru,
        _ => throw new ArgumentOutOfRangeException(nameof(categoria), categoria, "Categoria de tarifa inválida.")
    };
}
