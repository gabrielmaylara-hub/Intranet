using System.Globalization;
using System.Text;
using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class DirectorioModel : PageModel
{
    private readonly IDirectorioRepository _directorioRepo;

    public DirectorioModel(IDirectorioRepository directorioRepo) =>
        _directorioRepo = directorioRepo;

    public IReadOnlyList<DirectorioGrupo> Grupos { get; private set; } = [];

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true, Name = "area")]
    public string? Area { get; set; }

    [BindProperty(SupportsGet = true, Name = "extension")]
    public string? Extension { get; set; }

    public bool TieneBusqueda =>
        !string.IsNullOrWhiteSpace(Q) ||
        !string.IsNullOrWhiteSpace(Area) ||
        !string.IsNullOrWhiteSpace(Extension);

    public async Task OnGetAsync()
    {
        Q = Q?.Trim();
        Area = Area?.Trim();
        Extension = Extension?.Trim();

        var entradas = await _directorioRepo.ObtenerTodosAsync(soloActivos: true);
        var areas = (await _directorioRepo.ObtenerAreasAsync())
            .ToDictionary(a => a.Nombre, StringComparer.OrdinalIgnoreCase);

        Grupos = entradas
            .Where(e => CumpleFiltros(e, areas))
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

    private bool CumpleFiltros(
        DirectorioEntrada entrada,
        Dictionary<string, DirectorioArea> areas)
    {
        var area = areas.TryGetValue(entrada.Area, out var existente)
            ? existente
            : new DirectorioArea { Nombre = entrada.Area, Activo = true };

        if (!area.Activo)
            return false;

        if (!CoincideBusqueda(area.Nombre, Area) && !string.IsNullOrWhiteSpace(Area))
            return false;

        if (!CoincideBusqueda(entrada.Extension, Extension) && !string.IsNullOrWhiteSpace(Extension))
            return false;

        if (string.IsNullOrWhiteSpace(Q))
            return true;

        return CoincideBusqueda(area.Nombre, Q) ||
               CoincideBusqueda(entrada.Nombre, Q) ||
               CoincideBusqueda(entrada.Extension, Q) ||
               CoincideBusqueda(area.Titular, Q) ||
               CoincideBusqueda(area.Ubicacion, Q) ||
               CoincideBusqueda(area.Correo, Q);
    }

    private static bool CoincideBusqueda(string? valor, string? filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
            return true;

        if (string.IsNullOrWhiteSpace(valor))
            return false;

        return Normalizar(valor).Contains(Normalizar(filtro), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalizar(string valor)
    {
        var descompuesto = valor.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);

        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed record DirectorioGrupo(
    DirectorioArea Area,
    IReadOnlyList<DirectorioEntrada> Entradas);
