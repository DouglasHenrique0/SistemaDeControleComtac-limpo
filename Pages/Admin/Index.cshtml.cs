using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Pages.Admin;

[Authorize(Roles = ApplicationRoles.Admin)]
public class IndexModel : PageModel
{
    public bool IsAdmin { get; private set; }
    public bool IsColaborador { get; private set; }

    public void OnGet()
    {
        IsAdmin = User.IsInRole(ApplicationRoles.Admin);
        IsColaborador = User.IsInRole(ApplicationRoles.Colaborador);
    }

}
