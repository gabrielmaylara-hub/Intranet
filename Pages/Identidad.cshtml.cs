using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class IdentidadModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IConfiguracionRepository _configRepo;

    public IdentidadModel(
        IArchivoSeccionRepository archivosRepo,
        IConfiguracionRepository configRepo)
    {
        _archivosRepo = archivosRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];
    public string Titulo { get; private set; } = "Kit de Identidad Gráfica FGET";
    public string Descripcion { get; private set; } =
        "Recursos gráficos oficiales de la Fiscalía General del Estado de Tabasco: logotipos, paleta de colores, tipografías y lineamientos de uso.";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_identidad_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_identidad_descripcion", Descripcion);
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync("identidad", soloActivos: true);
    }
}
