using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class IndexModel : PageModel
{
    private readonly IAccesoRapidoRepository  _accesosRepo;
    private readonly IAvisoRepository         _avisosRepo;
    private readonly ITutorialRepository      _tutorialesRepo;

    public IndexModel(
        IAccesoRapidoRepository  accesosRepo,
        IAvisoRepository         avisosRepo,
        ITutorialRepository      tutorialesRepo)
    {
        _accesosRepo    = accesosRepo;
        _avisosRepo     = avisosRepo;
        _tutorialesRepo = tutorialesRepo;
    }

    public IEnumerable<AccesoRapido> AccesosRapidos { get; private set; } = [];
    public IEnumerable<Aviso>        Avisos          { get; private set; } = [];
    public IEnumerable<Tutorial>     Tutoriales      { get; private set; } = [];

    public async Task OnGetAsync()
    {
        AccesosRapidos = await _accesosRepo.ObtenerTodosAsync(soloActivos: true);

        // Solo los 5 avisos más recientes activos
        var todosAvisos = await _avisosRepo.ObtenerTodosAsync(soloActivos: true);
        Avisos = todosAvisos.Take(5);

        // Solo los 4 tutoriales más recientes activos
        var todosTutoriales = await _tutorialesRepo.ObtenerTodosAsync(soloActivos: true);
        Tutoriales = todosTutoriales.Take(4);
    }
}
