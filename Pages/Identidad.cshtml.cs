using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class IdentidadModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;

    public IdentidadModel(IArchivoSeccionRepository archivosRepo) =>
        _archivosRepo = archivosRepo;

    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];

    public async Task OnGetAsync() =>
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync("identidad", soloActivos: true);
}
