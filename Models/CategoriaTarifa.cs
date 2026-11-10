using System.ComponentModel.DataAnnotations;

namespace SistemaDeControleComtac.Models;

public enum CategoriaTarifa
{
    [Display(Name = "Escolar")]
    Escolar = 1,

    [Display(Name = "Caminhão, van ou ônibus")]
    Pesado = 2
}

public static class CategoriaTarifaExtensions
{
    public static string ObterDescricao(this CategoriaTarifa categoria) => categoria switch
    {
        CategoriaTarifa.Escolar => "Escolar",
        CategoriaTarifa.Pesado => "Caminhão, van ou ônibus",
        _ => "Categoria não informada"
    };
}
