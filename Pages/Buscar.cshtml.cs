using System.Globalization;
using System.Text;
using System.Text.Json;
using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages;

public class BuscarModel : PageModel
{
    private const int MaxConsulta = 100;
    private const int MinConsulta = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, SeccionBusqueda> SeccionesArchivos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["formatos"] = new("Formatos Contraloría", "/formatos"),
            ["manuales"] = new("Manuales Justicia.NET", "/manuales"),
            ["dgie"] = new("Solicitudes DGIE", "/dgie"),
            ["identidad"] = new("Identidad Gráfica", "/identidad"),
            ["capacitacion"] = new("Oferta Académica", "/capacitacion")
        };

    private static readonly SeccionPrincipal[] SeccionesPrincipales =
    [
        new("Formatos", "Formatos de Contraloría disponibles para descarga.", "/formatos"),
        new("Manuales", "Manuales Justicia.NET y materiales de consulta.", "/manuales"),
        new("DGIE", "Solicitudes y documentos de anuencia técnica.", "/dgie"),
        new("Identidad", "Recursos de identidad gráfica institucional.", "/identidad"),
        new("Capacitación", "Oferta académica, documentos y ligas institucionales.", "/capacitacion"),
        new("Directorio", "Directorio público de áreas, extensiones y contactos.", "/Directorio"),
        new("Avisos y Comunicados", "Comunicaciones internas publicadas por las áreas.", "/Avisos")
    ];

    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IAvisoRepository _avisosRepo;
    private readonly ITutorialRepository _tutorialesRepo;
    private readonly IConfiguracionRepository _configRepo;

    public BuscarModel(
        IArchivoSeccionRepository archivosRepo,
        IAvisoRepository avisosRepo,
        ITutorialRepository tutorialesRepo,
        IConfiguracionRepository configRepo)
    {
        _archivosRepo = archivosRepo;
        _avisosRepo = avisosRepo;
        _tutorialesRepo = tutorialesRepo;
        _configRepo = configRepo;
    }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Q { get; set; }

    public string Consulta { get; private set; } = string.Empty;
    public string? Mensaje { get; private set; }
    public IReadOnlyList<ResultadoBusqueda> Resultados { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Consulta = PrepararConsulta(Q);

        if (string.IsNullOrWhiteSpace(Consulta))
        {
            Mensaje = "Ingresa una palabra para buscar en la intranet.";
            return;
        }

        if (Consulta.Length < MinConsulta)
        {
            Mensaje = "Escribe al menos 2 caracteres para buscar.";
            return;
        }

        var termino = Normalizar(Consulta);
        var resultados = new List<ResultadoBusqueda>();

        AgregarSeccionesPrincipales(resultados, termino);
        await AgregarArchivosAsync(resultados, termino);
        await AgregarAvisosAsync(resultados, termino);
        await AgregarTutorialesAsync(resultados, termino);
        await AgregarLigasOfertaAcademicaAsync(resultados, termino);

        Resultados = resultados
            .OrderBy(r => r.Puntaje)
            .ThenByDescending(r => r.Fecha ?? DateTime.MinValue)
            .ThenBy(r => r.Titulo)
            .ToList();

        if (Resultados.Count == 0)
            Mensaje = "No se encontraron resultados para la búsqueda realizada.";
    }

    private void AgregarSeccionesPrincipales(List<ResultadoBusqueda> resultados, string termino)
    {
        foreach (var seccion in SeccionesPrincipales)
        {
            var puntaje = CalcularPuntaje(termino, seccion.Titulo, seccion.Descripcion, "Sección");
            if (puntaje is null)
                continue;

            resultados.Add(new ResultadoBusqueda
            {
                Tipo = "Sección",
                Titulo = seccion.Titulo,
                Descripcion = seccion.Descripcion,
                Contexto = "Acceso principal",
                Url = seccion.Url,
                TextoAccion = "Ir a sección",
                Puntaje = puntaje.Value
            });
        }
    }

    private async Task AgregarArchivosAsync(List<ResultadoBusqueda> resultados, string termino)
    {
        var archivos = await _archivosRepo.ObtenerTodosAsync();

        foreach (var archivo in archivos.Where(a => a.Activo && SeccionesArchivos.ContainsKey(a.Seccion)))
        {
            var seccion = SeccionesArchivos[archivo.Seccion];
            var puntaje = CalcularPuntaje(termino, archivo.Nombre, archivo.Descripcion, seccion.Titulo);
            if (puntaje is null)
                continue;

            resultados.Add(new ResultadoBusqueda
            {
                Tipo = "Archivo",
                Titulo = archivo.Nombre,
                Descripcion = Recortar(archivo.Descripcion),
                Contexto = seccion.Titulo,
                Url = $"/descargar/archivo/{archivo.Id}",
                TextoAccion = "Descargar",
                UrlSecundaria = seccion.Url,
                TextoAccionSecundaria = "Ver sección",
                Puntaje = puntaje.Value
            });
        }
    }

    private async Task AgregarAvisosAsync(List<ResultadoBusqueda> resultados, string termino)
    {
        var avisos = await _avisosRepo.ObtenerTodosAsync(soloActivos: true);

        foreach (var aviso in avisos)
        {
            var puntaje = CalcularPuntaje(
                termino,
                aviso.Titulo,
                aviso.Contenido,
                aviso.AreaPublicacionNombre,
                aviso.PdfNombreOriginal);

            if (puntaje is null)
                continue;

            resultados.Add(new ResultadoBusqueda
            {
                Tipo = "Aviso",
                Titulo = aviso.Titulo,
                Descripcion = Recortar(aviso.Contenido),
                Contexto = string.IsNullOrWhiteSpace(aviso.AreaPublicacionNombre)
                    ? "Avisos y Comunicados"
                    : aviso.AreaPublicacionNombre,
                Url = $"/Avisos#aviso-{aviso.Id}",
                TextoAccion = "Ver aviso",
                UrlSecundaria = aviso.TienePdf ? $"/descargar/aviso/{aviso.Id}" : null,
                TextoAccionSecundaria = aviso.TienePdf ? "Descargar comunicado" : null,
                Fecha = aviso.FechaPublicacion,
                Puntaje = puntaje.Value
            });
        }
    }

    private async Task AgregarTutorialesAsync(List<ResultadoBusqueda> resultados, string termino)
    {
        var tutoriales = await _tutorialesRepo.ObtenerTodosAsync(soloActivos: true);

        foreach (var tutorial in tutoriales)
        {
            var puntaje = CalcularPuntaje(termino, tutorial.Titulo, tutorial.Descripcion, "Tutoriales");
            if (puntaje is null)
                continue;

            resultados.Add(new ResultadoBusqueda
            {
                Tipo = "Tutorial",
                Titulo = tutorial.Titulo,
                Descripcion = Recortar(tutorial.Descripcion),
                Contexto = "Tutoriales y videos",
                Url = "/Tutoriales",
                TextoAccion = "Ver tutoriales",
                Fecha = tutorial.FechaCreacion,
                Puntaje = puntaje.Value
            });
        }
    }

    private async Task AgregarLigasOfertaAcademicaAsync(List<ResultadoBusqueda> resultados, string termino)
    {
        foreach (var liga in await ObtenerLigasOfertaAcademicaAsync())
        {
            var puntaje = CalcularPuntaje(termino, liga.Titulo, liga.Descripcion, "Oferta Académica", liga.Url);
            if (puntaje is null)
                continue;

            resultados.Add(new ResultadoBusqueda
            {
                Tipo = "Liga",
                Titulo = liga.Titulo,
                Descripcion = Recortar(liga.Descripcion),
                Contexto = "Oferta Académica",
                Url = liga.Url,
                TextoAccion = "Abrir enlace",
                EsExterno = EsUrlExterna(liga.Url),
                Puntaje = puntaje.Value
            });
        }
    }

    private async Task<IEnumerable<OfertaAcademicaLiga>> ObtenerLigasOfertaAcademicaAsync()
    {
        try
        {
            var config = await _configRepo.ObtenerTodosAsync();
            var json = config.GetValueOrDefault(OfertaAcademicaLiga.ClaveConfiguracion);

            if (string.IsNullOrWhiteSpace(json))
                return [];

            var ligas = JsonSerializer.Deserialize<List<OfertaAcademicaLiga>>(json, JsonOptions) ?? [];

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

    private static int? CalcularPuntaje(
        string termino,
        string? titulo,
        string? descripcion,
        string? contexto,
        string? adicional = null)
    {
        if (Coincide(titulo, termino))
            return 1;

        if (Coincide(descripcion, termino) || Coincide(adicional, termino))
            return 2;

        if (Coincide(contexto, termino))
            return 3;

        return null;
    }

    private static bool Coincide(string? valor, string termino) =>
        !string.IsNullOrWhiteSpace(valor) && Normalizar(valor).Contains(termino, StringComparison.Ordinal);

    private static string PrepararConsulta(string? valor)
    {
        var consulta = (valor ?? string.Empty).Trim();
        return consulta.Length <= MaxConsulta ? consulta : consulta[..MaxConsulta];
    }

    private static string Recortar(string? valor)
    {
        var texto = (valor ?? string.Empty).Trim();
        return texto.Length <= 220 ? texto : $"{texto[..217]}...";
    }

    private static string Normalizar(string valor)
    {
        var normalizado = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizado.Length);

        foreach (var caracter in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(caracter));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    private static bool UrlPermitida(string url)
    {
        if (url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal))
            return !url.Contains('\\') && !url.Contains("..", StringComparison.Ordinal);

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool EsUrlExterna(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private sealed record SeccionBusqueda(string Titulo, string Url);

    private sealed record SeccionPrincipal(string Titulo, string Descripcion, string Url);
}

public class ResultadoBusqueda
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Contexto { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string TextoAccion { get; set; } = "Abrir";
    public string? UrlSecundaria { get; set; }
    public string? TextoAccionSecundaria { get; set; }
    public bool EsExterno { get; set; }
    public DateTime? Fecha { get; set; }
    public int Puntaje { get; set; }
}
