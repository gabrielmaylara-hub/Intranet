using Intranet.Models;
using Intranet.Pages.Admin;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Intranet.Pages.Admin.Archivos;

public class IndexModel : AdminPageModel
{
    protected override bool RequiereAdminGeneral => true;

    private const int MaxNombre = 180;
    private const int MaxDescripcion = 300;
    private const int MaxUrl = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xls", ".xlsx", ".png", ".doc", ".docx", ".ppt", ".pptx", ".jpg", ".jpeg"
    };

    public static readonly Dictionary<string, string> Secciones = new()
    {
        ["formatos"] = "Formatos Contraloría",
        ["manuales"] = "Manuales Justicia NET",
        ["dgie"] = "Solicitudes DGIE",
        ["identidad"] = "Identidad Gráfica",
        ["capacitacion"] = "Oferta Académica"
    };

    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IArchivoService _archivosSvc;
    private readonly IConfiguracionRepository _configRepo;
    private readonly ILogger<IndexModel> _log;

    public IndexModel(
        IArchivoSeccionRepository archivosRepo,
        IArchivoService archivosSvc,
        IConfiguracionRepository configRepo,
        ILogger<IndexModel> log)
    {
        _archivosRepo = archivosRepo;
        _archivosSvc = archivosSvc;
        _configRepo = configRepo;
        _log = log;
    }

    public string SeccionActual { get; private set; } = "formatos";
    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];
    public IEnumerable<OfertaAcademicaLiga> LigasOfertaAcademica { get; private set; } = [];
    public bool EsCapacitacion => SeccionActual == "capacitacion";
    public bool EditandoArchivo => EditarId.HasValue;
    public bool EditandoLiga => EditarLigaId.HasValue;
    public int? EditarId { get; private set; }
    public int? EditarLigaId { get; private set; }
    public string? ArchivoActualPath { get; private set; }

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = string.Empty;
    [BindProperty] public string? Descripcion { get; set; }
    [BindProperty] public bool Activo { get; set; } = true;
    [BindProperty] public IFormFile? Archivo { get; set; }

    [BindProperty] public int LigaId { get; set; }
    [BindProperty] public string LigaTitulo { get; set; } = string.Empty;
    [BindProperty] public string? LigaDescripcion { get; set; }
    [BindProperty] public string LigaUrl { get; set; } = string.Empty;
    [BindProperty] public bool LigaActiva { get; set; } = true;

    [TempData] public string? Mensaje { get; set; }
    [TempData] public bool EsError { get; set; }

    public async Task OnGetAsync(string? seccion = null, int? editarId = null, int? editarLigaId = null)
    {
        await CargarSeccionAsync(seccion);

        if (editarId.HasValue)
            await PrepararEdicionArchivoAsync(editarId.Value);

        if (EsCapacitacion && editarLigaId.HasValue)
            await PrepararEdicionLigaAsync(editarLigaId.Value);
    }

    public async Task<IActionResult> OnPostGuardarAsync(string seccion)
    {
        await CargarSeccionAsync(seccion);

        Nombre = NormalizarTexto(Nombre);
        Descripcion = NormalizarTexto(Descripcion);

        var error = ValidarDocumento();
        if (error is not null)
            return await PageConErrorAsync(error);

        return Id > 0
            ? await ActualizarArchivoAsync()
            : await CrearArchivoAsync();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id, string seccion)
    {
        await CargarSeccionAsync(seccion);
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is null || archivo.Seccion != SeccionActual)
            return RedirectToPage(new { seccion = SeccionActual });

        var filasEliminadas = await _archivosRepo.EliminarAsync(id);
        if (filasEliminadas > 0)
            EliminarArchivoPosteriorABd(archivo.ArchivoPath, "archivo");

        return RedirectToPage(new { seccion = SeccionActual });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string seccion)
    {
        await CargarSeccionAsync(seccion);
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is not null && archivo.Seccion == SeccionActual)
            await _archivosRepo.CambiarEstadoAsync(id, !archivo.Activo);

        return RedirectToPage(new { seccion = SeccionActual });
    }

    public async Task<IActionResult> OnPostGuardarLigaAsync(string seccion)
    {
        await CargarSeccionAsync(seccion);
        if (!EsCapacitacion)
            return RedirectToPage(new { seccion = SeccionActual });

        LigaTitulo = NormalizarTexto(LigaTitulo);
        LigaDescripcion = NormalizarTexto(LigaDescripcion);
        LigaUrl = NormalizarTexto(LigaUrl);

        var error = ValidarLiga();
        if (error is not null)
            return await PageConErrorAsync(error);

        var ligas = await ObtenerLigasOfertaAcademicaAsync();
        if (LigaId > 0)
        {
            var liga = ligas.FirstOrDefault(l => l.Id == LigaId);
            if (liga is null)
                return await PageConErrorAsync("La liga seleccionada ya no existe.");

            liga.Titulo = LigaTitulo;
            liga.Descripcion = LigaDescripcion;
            liga.Url = LigaUrl;
            liga.Activa = LigaActiva;
        }
        else
        {
            ligas.Add(new OfertaAcademicaLiga
            {
                Id = ligas.Count == 0 ? 1 : ligas.Max(l => l.Id) + 1,
                Titulo = LigaTitulo,
                Descripcion = LigaDescripcion,
                Url = LigaUrl,
                Orden = ligas.Count == 0 ? 1 : ligas.Max(l => l.Orden) + 1,
                Activa = LigaActiva
            });
        }

        await GuardarLigasOfertaAcademicaAsync(ligas);
        Mensaje = LigaId > 0 ? "Liga actualizada correctamente." : "Liga agregada correctamente.";
        EsError = false;
        return RedirectToPage(new { seccion = SeccionActual });
    }

    public async Task<IActionResult> OnPostToggleLigaAsync(int id, string seccion)
    {
        await CargarSeccionAsync(seccion);
        if (!EsCapacitacion)
            return RedirectToPage(new { seccion = SeccionActual });

        var ligas = await ObtenerLigasOfertaAcademicaAsync();
        var liga = ligas.FirstOrDefault(l => l.Id == id);
        if (liga is not null)
        {
            liga.Activa = !liga.Activa;
            await GuardarLigasOfertaAcademicaAsync(ligas);
        }

        return RedirectToPage(new { seccion = SeccionActual });
    }

    public async Task<IActionResult> OnPostEliminarLigaAsync(int id, string seccion)
    {
        await CargarSeccionAsync(seccion);
        if (!EsCapacitacion)
            return RedirectToPage(new { seccion = SeccionActual });

        var ligas = await ObtenerLigasOfertaAcademicaAsync();
        ligas.RemoveAll(l => l.Id == id);
        await GuardarLigasOfertaAcademicaAsync(ligas);

        return RedirectToPage(new { seccion = SeccionActual });
    }

    private async Task<IActionResult> CrearArchivoAsync()
    {
        if (Archivo is null || Archivo.Length == 0)
            return await PageConErrorAsync("Selecciona un archivo para subir.");

        var rutaRelativa = await GuardarArchivoNuevoAsync(Archivo);
        if (rutaRelativa is null)
            return Page();

        var registro = new ArchivoSeccion
        {
            Seccion = SeccionActual,
            Nombre = Nombre,
            Descripcion = Descripcion,
            ArchivoPath = rutaRelativa,
            Activo = Activo,
            Orden = await ObtenerSiguienteOrdenAsync()
        };

        try
        {
            var idCreado = await _archivosRepo.InsertarAsync(registro);
            if (idCreado <= 0)
                throw new InvalidOperationException("No se pudo registrar el archivo en la base de datos.");
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(rutaRelativa, "archivo");
            _log.LogError(ex, "Error al registrar archivo para la seccion {Seccion}.", SeccionActual);
            return await PageConErrorAsync("No se pudo guardar el registro del archivo. El archivo nuevo fue descartado.");
        }

        Mensaje = "Archivo subido correctamente.";
        EsError = false;
        return RedirectToPage(new { seccion = SeccionActual });
    }

    private async Task<IActionResult> ActualizarArchivoAsync()
    {
        var existente = await _archivosRepo.ObtenerPorIdAsync(Id);
        if (existente is null || existente.Seccion != SeccionActual)
            return await PageConErrorAsync("El archivo seleccionado ya no existe.");

        string? rutaNueva = null;
        if (Archivo is not null && Archivo.Length > 0)
        {
            rutaNueva = await GuardarArchivoNuevoAsync(Archivo);
            if (rutaNueva is null)
                return Page();
        }

        var rutaAnterior = existente.ArchivoPath;
        existente.Nombre = Nombre;
        existente.Descripcion = Descripcion;
        existente.Activo = Activo;
        existente.ArchivoPath = rutaNueva ?? existente.ArchivoPath;

        try
        {
            var filas = await _archivosRepo.ActualizarAsync(existente);
            if (filas != 1)
                throw new InvalidOperationException("No se pudo actualizar el archivo en la base de datos.");
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(rutaNueva))
                LimpiarArchivoNuevo(rutaNueva, "archivo");

            _log.LogError(ex, "Error al actualizar archivo {Id} de la seccion {Seccion}.", Id, SeccionActual);
            return await PageConErrorAsync("No se pudo actualizar el registro del archivo.");
        }

        if (!string.IsNullOrWhiteSpace(rutaNueva) &&
            !string.Equals(rutaAnterior, rutaNueva, StringComparison.OrdinalIgnoreCase))
        {
            EliminarArchivoPosteriorABd(rutaAnterior, "archivo anterior");
        }

        Mensaje = "Archivo actualizado correctamente.";
        EsError = false;
        return RedirectToPage(new { seccion = SeccionActual });
    }

    private async Task<string?> GuardarArchivoNuevoAsync(IFormFile archivo)
    {
        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(ext))
        {
            await PageConErrorAsync("Tipo de archivo no permitido. Usa PDF, Excel, PNG, Word, PowerPoint, JPG o JPEG.");
            return null;
        }

        try
        {
            return await _archivosSvc.GuardarAsync(archivo, $"archivos/{SeccionActual}");
        }
        catch (InvalidOperationException ex)
        {
            await PageConErrorAsync(ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al almacenar archivo para la seccion {Seccion}.", SeccionActual);
            await PageConErrorAsync("No se pudo almacenar el archivo.");
            return null;
        }
    }

    private async Task CargarSeccionAsync(string? seccion)
    {
        SeccionActual = Secciones.ContainsKey(seccion ?? "") ? seccion! : "formatos";
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync(SeccionActual);
        LigasOfertaAcademica = EsCapacitacion
            ? await ObtenerLigasOfertaAcademicaAsync(soloActivas: false)
            : [];
    }

    private async Task PrepararEdicionArchivoAsync(int id)
    {
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is null || archivo.Seccion != SeccionActual)
        {
            EsError = true;
            Mensaje = "No se encontro el archivo solicitado.";
            return;
        }

        EditarId = archivo.Id;
        Id = archivo.Id;
        Nombre = archivo.Nombre;
        Descripcion = archivo.Descripcion;
        Activo = archivo.Activo;
        ArchivoActualPath = archivo.ArchivoPath;
    }

    private async Task PrepararEdicionLigaAsync(int id)
    {
        var liga = (await ObtenerLigasOfertaAcademicaAsync()).FirstOrDefault(l => l.Id == id);
        if (liga is null)
        {
            EsError = true;
            Mensaje = "No se encontro la liga solicitada.";
            return;
        }

        EditarLigaId = liga.Id;
        LigaId = liga.Id;
        LigaTitulo = liga.Titulo;
        LigaDescripcion = liga.Descripcion;
        LigaUrl = liga.Url;
        LigaActiva = liga.Activa;
    }

    private async Task<int> ObtenerSiguienteOrdenAsync()
    {
        var existentes = await _archivosRepo.ObtenerPorSeccionAsync(SeccionActual);
        return existentes.Any() ? existentes.Max(a => a.Orden) + 1 : 1;
    }

    private async Task<List<OfertaAcademicaLiga>> ObtenerLigasOfertaAcademicaAsync(bool soloActivas = false)
    {
        var config = await _configRepo.ObtenerTodosAsync();
        var json = config.GetValueOrDefault(OfertaAcademicaLiga.ClaveConfiguracion);

        try
        {
            var ligas = string.IsNullOrWhiteSpace(json)
                ? new List<OfertaAcademicaLiga>()
                : JsonSerializer.Deserialize<List<OfertaAcademicaLiga>>(json, JsonOptions) ?? [];

            ligas = await AsegurarLigaInicialAsync(config, ligas);

            return ligas
                .Where(l => !soloActivas || l.Activa)
                .OrderBy(l => l.Orden)
                .ThenBy(l => l.Titulo)
                .ToList();
        }
        catch (JsonException ex)
        {
            _log.LogWarning(ex, "No se pudo leer la configuracion JSON de ligas de Oferta Academica.");
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

        await GuardarLigasOfertaAcademicaAsync(ligas, marcarSemilla: true);
        return ligas;
    }

    private async Task GuardarLigasOfertaAcademicaAsync(
        IEnumerable<OfertaAcademicaLiga> ligas,
        bool marcarSemilla = true)
    {
        var ordenadas = ligas
            .OrderBy(l => l.Orden)
            .ThenBy(l => l.Titulo)
            .ToList();

        var valores = new Dictionary<string, string>
        {
            [OfertaAcademicaLiga.ClaveConfiguracion] = JsonSerializer.Serialize(ordenadas, JsonOptions)
        };

        if (marcarSemilla)
            valores[OfertaAcademicaLiga.ClaveSemillaAplicada] = "1";

        await _configRepo.GuardarMultiplesAsync(valores);
    }

    private string? ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            return "El nombre del archivo es obligatorio.";

        if (Nombre.Length > MaxNombre)
            return $"El nombre no debe exceder {MaxNombre} caracteres.";

        if (Descripcion?.Length > MaxDescripcion)
            return $"La descripcion no debe exceder {MaxDescripcion} caracteres.";

        if (!EsNombreVisibleSeguro(Nombre))
            return "El nombre no debe contener rutas, separadores ni caracteres de control.";

        if (Archivo is not null && Archivo.Length > 0)
        {
            var ext = Path.GetExtension(Archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(ext))
                return "Tipo de archivo no permitido. Usa PDF, Excel, PNG, Word, PowerPoint, JPG o JPEG.";
        }

        return null;
    }

    private string? ValidarLiga()
    {
        if (string.IsNullOrWhiteSpace(LigaTitulo))
            return "El titulo de la liga es obligatorio.";

        if (LigaTitulo.Length > MaxNombre)
            return $"El titulo no debe exceder {MaxNombre} caracteres.";

        if (LigaDescripcion?.Length > MaxDescripcion)
            return $"La descripcion no debe exceder {MaxDescripcion} caracteres.";

        if (string.IsNullOrWhiteSpace(LigaUrl))
            return "La URL de la liga es obligatoria.";

        if (LigaUrl.Length > MaxUrl)
            return $"La URL no debe exceder {MaxUrl} caracteres.";

        if (!UrlPermitida(LigaUrl))
            return "La URL debe ser interna o iniciar con http/https. No se permiten esquemas peligrosos.";

        return null;
    }

    private static bool UrlPermitida(string url)
    {
        if (url.StartsWith("/", StringComparison.Ordinal) && !url.StartsWith("//", StringComparison.Ordinal))
            return !url.Contains('\\') && !url.Contains("..", StringComparison.Ordinal);

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool EsNombreVisibleSeguro(string nombre) =>
        !nombre.Any(char.IsControl) &&
        !nombre.Contains('/', StringComparison.Ordinal) &&
        !nombre.Contains('\\', StringComparison.Ordinal) &&
        !nombre.Contains("..", StringComparison.Ordinal);

    private async Task<IActionResult> PageConErrorAsync(string mensaje)
    {
        EsError = true;
        Mensaje = mensaje;
        if (Id > 0)
        {
            EditarId = Id;
            var existente = await _archivosRepo.ObtenerPorIdAsync(Id);
            if (existente is not null)
                ArchivoActualPath = existente.ArchivoPath;
        }

        if (LigaId > 0)
            EditarLigaId = LigaId;

        Archivos = await _archivosRepo.ObtenerPorSeccionAsync(SeccionActual);
        LigasOfertaAcademica = EsCapacitacion
            ? await ObtenerLigasOfertaAcademicaAsync(soloActivas: false)
            : [];
        return Page();
    }

    private static string NormalizarTexto(string? valor) => valor?.Trim() ?? string.Empty;

    private void LimpiarArchivoNuevo(string? rutaRelativa, string tipo)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        try
        {
            _archivosSvc.Eliminar(rutaRelativa);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No se pudo limpiar el archivo nuevo de {Tipo} despues de un fallo de guardado.",
                tipo);
        }
    }

    private void EliminarArchivoPosteriorABd(string? rutaRelativa, string tipo)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        try
        {
            _archivosSvc.Eliminar(rutaRelativa);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "La BD se actualizo, pero no se pudo eliminar el archivo fisico de {Tipo}.",
                tipo);
        }
    }
}
