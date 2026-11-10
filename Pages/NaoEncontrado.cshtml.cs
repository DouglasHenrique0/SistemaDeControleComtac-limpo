using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SistemaDeControleComtac.Pages;

[AllowAnonymous]
public sealed class NaoEncontradoModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
    }
}
