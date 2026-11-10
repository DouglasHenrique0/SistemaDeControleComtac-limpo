using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Observability;

namespace SistemaDeControleComtac.Pages.Conta;

[AllowAnonymous]
[EnableRateLimiting("login")]
public class LoginModel : PageModel
{
    private readonly SignInManager<UsuarioIdentity> _signInManager;
    private readonly UserManager<UsuarioIdentity> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<UsuarioIdentity> signInManager,
        UserManager<UsuarioIdentity> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            RecordAttempt("invalid_input");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        {
            RecordAttempt("invalid_input");
            ModelState.AddModelError(string.Empty, "Email e senha são obrigatórios.");
            return Page();
        }

        var usuario = await _userManager.FindByEmailAsync(Email.Trim());

        if (usuario is null || !usuario.Ativo)
        {
            RecordAttempt("invalid_credentials");
            ModelState.AddModelError(string.Empty, "Não foi possível entrar com essas credenciais.");
            return Page();
        }

        var resultadoLogin = await _signInManager.PasswordSignInAsync(
            usuario,
            Senha,
            isPersistent: false,
            lockoutOnFailure: true);

        if (resultadoLogin.Succeeded)
        {
            RecordAttempt("success");
            return RedirectToPage("/Index");
        }

        if (resultadoLogin.IsLockedOut)
        {
            RecordAttempt("locked_out");
            ModelState.AddModelError(string.Empty, "Conta temporariamente bloqueada após várias tentativas. Tente novamente em 15 minutos.");
            return Page();
        }

        RecordAttempt("invalid_credentials");
        ModelState.AddModelError(string.Empty, "Não foi possível entrar com essas credenciais.");
        return Page();
    }

    private void RecordAttempt(string outcome)
    {
        Telemetry.LoginAttempts.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
        _logger.LogInformation(
            "Login attempt completed. {Outcome} {RequestId}",
            outcome,
            HttpContext.TraceIdentifier);
    }
}
