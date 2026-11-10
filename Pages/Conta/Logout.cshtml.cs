using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Pages.Conta;

[Authorize]
public class LogoutModel : PageModel
{
    private readonly SignInManager<UsuarioIdentity> _signInManager;

    public LogoutModel(SignInManager<UsuarioIdentity> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
