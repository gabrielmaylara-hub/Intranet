using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class DirectorioModel : PageModel
{
    private readonly IDirectorioRepository _directorioRepo;

    public DirectorioModel(IDirectorioRepository directorioRepo) =>
        _directorioRepo = directorioRepo;

    public IReadOnlyList<DirectorioGrupo> Grupos { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var entradas = await _directorioRepo.ObtenerTodosAsync(soloActivos: true);
        var areas = (await _directorioRepo.ObtenerAreasAsync())
            .ToDictionary(a => a.Nombre, StringComparer.OrdinalIgnoreCase);

        Grupos = entradas
            .GroupBy(e => e.Area)
            .Select(g =>
            {
                var area = areas.TryGetValue(g.Key, out var existente)
                    ? existente
                    : new DirectorioArea { Nombre = g.Key, Activo = true };

                var extensiones = g
                    .OrderBy(e => e.Orden)
                    .ThenBy(e => e.Nombre)
                    .ToList();

                return new DirectorioGrupo(area, extensiones);
            })
            .Where(g => g.Area.Activo)
            .OrderBy(g => g.Area.Orden)
            .ThenBy(g => g.Area.Nombre)
            .ToList();
    }
}

public sealed record DirectorioGrupo(
    DirectorioArea Area,
    IReadOnlyList<DirectorioEntrada> Entradas);
