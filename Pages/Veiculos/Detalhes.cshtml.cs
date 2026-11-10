using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Pages.Veiculos;

[Authorize]
public class DetalhesModel(AppDbContext context) : PageModel
{
    private readonly AppDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public RegistroVeiculo Veiculo { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Id <= 0)
        {
            return RedirectToPage("/Veiculos/RelatorioDoDia");
        }

        var veiculo = await _context.RegistrosVeiculos
            .AsNoTracking()
            .FirstOrDefaultAsync(registro => registro.Id == Id, cancellationToken);

        if (veiculo is null)
        {
            return NotFound();
        }

        Veiculo = veiculo;
        return Page();
    }
}
