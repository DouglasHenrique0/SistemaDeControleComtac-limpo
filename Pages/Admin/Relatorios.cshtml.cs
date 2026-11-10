using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Pages.Admin;

[Authorize(Roles = ApplicationRoles.Admin)]
public class RelatoriosModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect($"/Relatorios/Historico{Request.QueryString}");
    }
}
