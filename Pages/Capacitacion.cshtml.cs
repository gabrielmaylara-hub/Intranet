using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class CapacitacionModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;

    public CapacitacionModel(IArchivoSeccionRepository archivosRepo) =>
        _archivosRepo = archivosRepo;

    public IEnumerable<ArchivoSeccion> ArchivosInternos { get; private set; } = [];

    public async Task OnGetAsync() =>
        ArchivosInternos = await _archivosRepo.ObtenerPorSeccionAsync("capacitacion", soloActivos: true);
}
