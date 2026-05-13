using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class ManualesModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IConfiguracionRepository _configRepo;

    public ManualesModel(
        IArchivoSeccionRepository archivosRepo,
        IConfiguracionRepository configRepo)
    {
        _archivosRepo = archivosRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];
    public string Titulo { get; private set; } = "Manuales de Capacitación Justicia.NET";
    public string Descripcion { get; private set; } =
        "Manuales de usuario y capacitación del sistema Justicia.NET. Consulta o descarga el manual que necesitas.";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_manuales_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_manuales_descripcion", Descripcion);
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync("manuales", soloActivos: true);
    }
}
