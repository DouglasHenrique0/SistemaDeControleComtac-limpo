using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SistemaDeControleComtac.Models;

public class UsuarioIdentity : IdentityUser
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    [StringLength(100, ErrorMessage = "Cargo deve ter no máximo 100 caracteres.")]
    public string Cargo { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Informe um email válido.")]
    public override string? Email { get; set; }
}
