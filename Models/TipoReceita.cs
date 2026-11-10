using System.ComponentModel.DataAnnotations;

namespace SistemaDeControleComtac.Models;

public enum TipoReceita
{
    [Display(Name = "Outro")]
    Outro = 0,

    [Display(Name = "Selagem / ensaio")]
    Selagem = 1,

    [Display(Name = "Conserto")]
    Conserto = 2
}

public static class TipoReceitaExtensions
{
    public static string ObterDescricao(this TipoReceita tipo) => tipo switch
    {
        TipoReceita.Selagem => "Selagem / ensaio",
        TipoReceita.Conserto => "Conserto",
        _ => "Outro"
    };
}
