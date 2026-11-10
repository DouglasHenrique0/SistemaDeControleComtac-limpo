using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Pages.Conta;

[Authorize]
public class AlterarSenhaModel(
    UserManager<UsuarioIdentity> userManager,
    SignInManager<UsuarioIdentity> signInManager) : PageModel
{
    private readonly UserManager<UsuarioIdentity> _userManager = userManager;
    private readonly SignInManager<UsuarioIdentity> _signInManager = signInManager;

    [BindProperty]
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NovaSenha), ErrorMessage = "A confirmação não corresponde à nova senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmacaoNovaSenha { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null || !usuario.Ativo)
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Conta/Login");
        }

        var resultado = await _userManager.ChangePasswordAsync(usuario, SenhaAtual, NovaSenha);
        if (!resultado.Succeeded)
        {
            foreach (var erro in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, erro.Description);
            }

            return Page();
        }

        await _signInManager.RefreshSignInAsync(usuario);
        TempData[NotificacaoChaves.Sucesso] = "Senha alterada com sucesso.";
        return RedirectToPage();
    }
}
