using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class TutorialesModel : PageModel
{
    private readonly ITutorialRepository _tutorialesRepo;

    public TutorialesModel(ITutorialRepository tutorialesRepo) =>
        _tutorialesRepo = tutorialesRepo;

    public IEnumerable<Tutorial> Tutoriales { get; private set; } = [];

    public async Task OnGetAsync() =>
        Tutoriales = await _tutorialesRepo.ObtenerTodosAsync(soloActivos: true);
}
