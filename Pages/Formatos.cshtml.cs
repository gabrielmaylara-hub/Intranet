using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class FormatosModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IConfiguracionRepository _configRepo;

    public FormatosModel(
        IArchivoSeccionRepository archivosRepo,
        IConfiguracionRepository configRepo)
    {
        _archivosRepo = archivosRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];
    public string Titulo { get; private set; } = "Formatos de Contraloría";
    public string Descripcion { get; private set; } =
        "Formatos oficiales de la Contraloría Interna de la Fiscalía General del Estado de Tabasco. Descarga el formato que necesitas en formato PDF.";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_formatos_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_formatos_descripcion", Descripcion);
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync("formatos", soloActivos: true);
    }
}
