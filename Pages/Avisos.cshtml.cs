using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class AvisosModel : PageModel
{
    private readonly IAvisoRepository _avisosRepo;
    private readonly IConfiguracionRepository _configRepo;

    public AvisosModel(
        IAvisoRepository avisosRepo,
        IConfiguracionRepository configRepo)
    {
        _avisosRepo = avisosRepo;
        _configRepo = configRepo;
    }

    public IEnumerable<Aviso> Avisos { get; private set; } = [];
    public string Etiqueta { get; private set; } = "Comunicación interna";
    public string Titulo { get; private set; } = "Avisos y Comunicados";
    public string Descripcion { get; private set; } =
        "Consulta los avisos y comunicados publicados por las áreas de la institución.";
    public string MensajeVacio { get; private set; } = "No hay avisos publicados por el momento.";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Etiqueta = config.GetValueOrDefault("home_avisos_etiqueta", Etiqueta);
        Titulo = config.GetValueOrDefault("home_avisos_titulo", Titulo);

        var avisos = await _avisosRepo.ObtenerTodosAsync(soloActivos: true);
        Avisos = avisos
            .OrderByDescending(a => a.FechaPublicacion)
            .ToList();
    }
}
