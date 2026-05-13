using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class CapacitacionModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IConfiguracionRepository _configRepo;

    public CapacitacionModel(
        IArchivoSeccionRepository archivosRepo,
        IConfiguracionRepository configRepo)
    {
        _archivosRepo = archivosRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<ArchivoSeccion> ArchivosInternos { get; private set; } = [];
    public string Titulo { get; private set; } = "Oferta Académica";
    public string Descripcion { get; private set; } =
        "Cursos de capacitación y formación profesional disponibles para el personal de la Fiscalía General del Estado de Tabasco.";
    public string InternosTitulo { get; private set; } = "Cursos y Materiales Internos";
    public bool ExternoActivo { get; private set; } = true;
    public string ExternoTitulo { get; private set; } = "Sistema Integral de Gestión Académica";
    public string ExternoDescripcion { get; private set; } =
        "Accede al sistema SIGAACEJ del Tribunal Superior de Justicia del Estado de Tabasco para consultar la oferta académica institucional compartida.";
    public string ExternoBotonTexto { get; private set; } = "Acceder a SIGAACEJ ↗";
    public string ExternoBotonUrl { get; private set; } = "https://sigaacej.tsj-tabasco.gob.mx/";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_capacitacion_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_capacitacion_descripcion", Descripcion);
        InternosTitulo = config.GetValueOrDefault("pagina_capacitacion_internos_titulo", InternosTitulo);
        ExternoActivo = EsActivo(config.GetValueOrDefault("pagina_capacitacion_externo_activo", "1"));
        ExternoTitulo = config.GetValueOrDefault("pagina_capacitacion_externo_titulo", ExternoTitulo);
        ExternoDescripcion = config.GetValueOrDefault("pagina_capacitacion_externo_descripcion", ExternoDescripcion);
        ExternoBotonTexto = config.GetValueOrDefault("pagina_capacitacion_externo_boton_texto", ExternoBotonTexto);
        ExternoBotonUrl = config.GetValueOrDefault("pagina_capacitacion_externo_boton_url", ExternoBotonUrl);
        ArchivosInternos = await _archivosRepo.ObtenerPorSeccionAsync("capacitacion", soloActivos: true);

        static bool EsActivo(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ||
            valor.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("activo", StringComparison.OrdinalIgnoreCase);
    }
}
