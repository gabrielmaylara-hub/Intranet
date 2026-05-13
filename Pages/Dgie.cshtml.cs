using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class DgieModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IConfiguracionRepository _configRepo;

    public DgieModel(
        IArchivoSeccionRepository archivosRepo,
        IConfiguracionRepository configRepo)
    {
        _archivosRepo = archivosRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];
    public string Titulo { get; private set; } = "Solicitud de Anuencia Técnica DGIE";
    public string Descripcion { get; private set; } =
        "Formatos y documentos para tramitar una solicitud de anuencia técnica ante la Dirección General de Informática y Estadística.";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_dgie_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_dgie_descripcion", Descripcion);
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync("dgie", soloActivos: true);
    }
}
