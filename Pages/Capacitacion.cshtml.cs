using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Intranet.Pages;

public class CapacitacionModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
    public IEnumerable<OfertaAcademicaLiga> LigasOfertaAcademica { get; private set; } = [];
    public string Titulo { get; private set; } = "Oferta Académica";
    public string Descripcion { get; private set; } =
        "Cursos de capacitación y formación profesional disponibles para el personal de la Fiscalía General del Estado de Tabasco.";
    public string InternosTitulo { get; private set; } = "Cursos y Materiales Internos";

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        Titulo = config.GetValueOrDefault("pagina_capacitacion_titulo", Titulo);
        Descripcion = config.GetValueOrDefault("pagina_capacitacion_descripcion", Descripcion);
        InternosTitulo = config.GetValueOrDefault("pagina_capacitacion_internos_titulo", InternosTitulo);
        ArchivosInternos = await _archivosRepo.ObtenerPorSeccionAsync("capacitacion", soloActivos: true);
        LigasOfertaAcademica = await ObtenerLigasAsync(config);
    }

    private async Task<IEnumerable<OfertaAcademicaLiga>> ObtenerLigasAsync(
        IReadOnlyDictionary<string, string> config)
    {
        try
        {
            var json = config.GetValueOrDefault(OfertaAcademicaLiga.ClaveConfiguracion);
            var ligas = string.IsNullOrWhiteSpace(json)
                ? new List<OfertaAcademicaLiga>()
                : JsonSerializer.Deserialize<List<OfertaAcademicaLiga>>(json, JsonOptions) ?? [];

            ligas = await AsegurarLigaInicialAsync(config, ligas);

            return ligas
                .Where(l => l.Activa && UrlPermitida(l.Url))
                .OrderBy(l => l.Orden)
                .ThenBy(l => l.Titulo)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<List<OfertaAcademicaLiga>> AsegurarLigaInicialAsync(
        IReadOnlyDictionary<string, string> config,
        List<OfertaAcademicaLiga> ligas)
    {
        if (config.GetValueOrDefault(OfertaAcademicaLiga.ClaveSemillaAplicada) == "1")
            return ligas;

        if (!ligas.Any(OfertaAcademicaLiga.EsSigaacej))
        {
            ligas.Add(OfertaAcademicaLiga.CrearSigaacej(
                config,
                ligas.Count == 0 ? 1 : ligas.Max(l => l.Id) + 1,
                ligas.Count == 0 ? 1 : ligas.Max(l => l.Orden) + 1));
        }

        await _configRepo.GuardarMultiplesAsync(new Dictionary<string, string>
        {
            [OfertaAcademicaLiga.ClaveConfiguracion] = JsonSerializer.Serialize(ligas, JsonOptions),
            [OfertaAcademicaLiga.ClaveSemillaAplicada] = "1"
        });

        return ligas;
    }

    private static bool UrlPermitida(string url)
    {
        if (url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal))
            return !url.Contains('\\') && !url.Contains("..", StringComparison.Ordinal);

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
