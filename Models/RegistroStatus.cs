namespace SistemaDeControleComtac.Models;

public static class RegistroStatus
{
    public const string NoPatio = "No pátio";
    public const string Liberado = "Liberado";

    public static bool EstaNoPatio(string? status) =>
        string.Equals(status?.TrimEnd('.'), NoPatio, StringComparison.OrdinalIgnoreCase);
}
