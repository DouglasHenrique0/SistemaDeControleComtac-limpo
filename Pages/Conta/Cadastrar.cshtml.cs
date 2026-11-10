using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Pages.Conta;

[Authorize(Roles = ApplicationRoles.Admin)]
public class CadastrarModel(UserManager<UsuarioIdentity> userManager) : PageModel
{
    private readonly UserManager<UsuarioIdentity> _userManager = userManager;

    [BindProperty]
    public UsuarioIdentity NovoUsuario { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        var nome = NovoUsuario.Nome?.Trim();
        var email = NovoUsuario.Email?.Trim();

        if (string.IsNullOrWhiteSpace(nome))
        {
            ModelState.AddModelError(nameof(NovoUsuario.Nome), "Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(nameof(NovoUsuario.Email), "Email é obrigatório.");
        }
        else if (!new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(nameof(NovoUsuario.Email), "Informe um email válido.");
        }
        else if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(NovoUsuario.Email), "Email já está em uso.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = new UsuarioIdentity
        {
            Nome = nome ?? string.Empty,
            Email = email ?? string.Empty,
            UserName = email ?? string.Empty,
            Ativo = true
        };

        var resultadoCriacao = await _userManager.CreateAsync(usuario, Senha);

        if (!resultadoCriacao.Succeeded)
        {
            foreach (var erro in resultadoCriacao.Errors)
            {
                ModelState.AddModelError(string.Empty, erro.Description);
            }

            return Page();
        }

        var resultadoRole = await _userManager.AddToRoleAsync(usuario, ApplicationRoles.Colaborador);

        if (!resultadoRole.Succeeded)
        {
            await _userManager.DeleteAsync(usuario);

            foreach (var erro in resultadoRole.Errors)
            {
                ModelState.AddModelError(string.Empty, erro.Description);
            }

            return Page();
        }

        TempData[NotificacaoChaves.Sucesso] = "Colaborador cadastrado com sucesso.";
        return RedirectToPage("/Conta/Cadastrar");
    }
}
