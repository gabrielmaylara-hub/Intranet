using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class FormatosModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;

    public FormatosModel(IArchivoSeccionRepository archivosRepo) =>
        _archivosRepo = archivosRepo;

    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];

    public async Task OnGetAsync() =>
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync("formatos", soloActivos: true);
}
