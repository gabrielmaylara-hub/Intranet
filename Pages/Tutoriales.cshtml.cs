using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class TutorialesModel : PageModel
{
    private readonly ITutorialRepository _tutorialesRepo;
    private readonly IConfiguracionRepository _configRepo;

    public TutorialesModel(
        ITutorialRepository tutorialesRepo,
        IConfiguracionRepository configRepo)
    {
        _tutorialesRepo = tutorialesRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<Tutorial> Tutoriales { get; private set; } = [];
    public string Titulo { get; private set; } = "Tutoriales Institucionales";
    public string Descripcion { get; private set; } =
        "Videos de capacitación y guías de uso de los sistemas institucionales de la Fiscalía General del Estado de Tabasco.";
    public string MensajeVacio { get; private set; } = "No hay tutoriales publicados en este momento.";
    public string VideoNoSoportado { get; private set; } = "Tu navegador no soporta reproducción de video.";
    public string VideoProximamente { get; private set; } = "Video próximamente";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_tutoriales_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_tutoriales_descripcion", Descripcion);
        MensajeVacio = config.GetValueOrDefault("pagina_tutoriales_vacio", MensajeVacio);
        VideoNoSoportado = config.GetValueOrDefault("pagina_tutoriales_video_no_soportado", VideoNoSoportado);
        VideoProximamente = config.GetValueOrDefault("pagina_tutoriales_video_proximamente", VideoProximamente);
        Tutoriales = await _tutorialesRepo.ObtenerTodosAsync(soloActivos: true);
    }
}
