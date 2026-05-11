using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin.Avisos;

public class IndexModel : PageModel
{
    private readonly IAvisoRepository _avisosRepo;

    public IndexModel(IAvisoRepository avisosRepo) => _avisosRepo = avisosRepo;

    public IEnumerable<Aviso> Avisos { get; private set; } = [];

    [BindProperty] public int     Id                { get; set; }
    [BindProperty] public string  Titulo            { get; set; } = string.Empty;
    [BindProperty] public string? Contenido         { get; set; }
    [BindProperty] public string  FechaPublicacion  { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    [BindProperty] public bool    Activo            { get; set; } = true;

    public string? Mensaje { get; private set; }
    public bool    EsError { get; private set; }

    public async Task OnGetAsync() =>
        Avisos = await _avisosRepo.ObtenerTodosAsync();

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Titulo))
        {
            EsError = true;
            Mensaje = "El título del aviso es obligatorio.";
            await OnGetAsync();
            return Page();
        }

        if (!DateTime.TryParse(FechaPublicacion, out var fecha))
            fecha = DateTime.Today;

        var aviso = new Aviso
        {
            Id               = Id,
            Titulo           = Titulo.Trim(),
            Contenido        = Contenido?.Trim(),
            FechaPublicacion = fecha,
            Activo           = Activo,
            Orden            = 0
        };

        if (Id > 0)
            await _avisosRepo.ActualizarAsync(aviso);
        else
            await _avisosRepo.InsertarAsync(aviso);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        await _avisosRepo.EliminarAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var aviso = await _avisosRepo.ObtenerPorIdAsync(id);
        if (aviso is not null)
            await _avisosRepo.CambiarEstadoAsync(id, !aviso.Activo);
        return RedirectToPage();
    }
}
