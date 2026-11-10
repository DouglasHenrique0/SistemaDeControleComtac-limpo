using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SistemaDeControleComtac.Pages.Conta;

[AllowAnonymous]
public class AcessoNegadoModel : PageModel
{
    public void OnGet()
    {
    }
}
