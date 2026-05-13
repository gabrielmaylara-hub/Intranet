using Intranet.Models;
using Intranet.Pages.Admin;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Intranet.Pages.Admin.Tutoriales;

public class IndexModel : AdminPageModel
{
    private const int MaxTitulo = 200;
    private const int MaxDescripcion = 1000;

    private readonly ITutorialRepository _tutorialesRepo;
    private readonly IAreaPublicacionRepository _areasRepo;
    private readonly IArchivoService _archivos;
    private readonly ILogger<IndexModel> _log;

    public IndexModel(
        ITutorialRepository tutorialesRepo,
        IAreaPublicacionRepository areasRepo,
        IArchivoService archivos,
        ILogger<IndexModel> log)
    {
        _tutorialesRepo = tutorialesRepo;
        _areasRepo = areasRepo;
        _archivos = archivos;
        _log = log;
    }

    public IEnumerable<Tutorial> Tutoriales { get; private set; } = [];
    public IEnumerable<AreaPublicacion> Areas { get; private set; } = [];
    public string? AreaActualNombre { get; private set; }
    public bool PuedeGestionarTutoriales { get; private set; }
    public bool EsAdminGeneralActual => EsAdminGeneral();

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Titulo { get; set; } = string.Empty;
    [BindProperty] public string? Descripcion { get; set; }
    [BindProperty] public bool Activo { get; set; } = true;
    [BindProperty] public int? AreaPublicacionId { get; set; }
    [BindProperty] public IFormFile? Video { get; set; }
    [BindProperty] public IFormFile? Miniatura { get; set; }

    [BindProperty(SupportsGet = true)] public int? AreaFiltro { get; set; }

    public string? Mensaje { get; private set; }
    public bool EsError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarPaginaAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var contexto = await ObtenerContextoAsync();
        if (!contexto.PuedeGestionar)
        {
            EsError = true;
            Mensaje = "Tu usuario no tiene un área de publicación activa asignada.";
            await CargarPaginaAsync();
            return Page();
        }

        Tutorial? existente = null;
        string? videoAnterior = null;
        string? miniaturaAnterior = null;
        string? archivoPath = null;
        string? miniPath = null;

        if (Id > 0)
        {
            existente = await _tutorialesRepo.ObtenerPorIdAsync(Id);
            if (existente is null)
            {
                EsError = true;
                Mensaje = "No se encontró el tutorial seleccionado.";
                await CargarPaginaAsync();
                return Page();
            }

            if (!PuedeOperarTutorial(contexto, existente))
                return StatusCode(StatusCodes.Status403Forbidden);

            videoAnterior = existente.ArchivoPath;
            miniaturaAnterior = existente.MiniaturaPath;
            archivoPath = videoAnterior;
            miniPath = miniaturaAnterior;
        }

        var error = await ValidarFormularioAsync(contexto);
        if (error is not null)
        {
            EsError = true;
            Mensaje = error;
            await CargarPaginaAsync();
            return Page();
        }

        string? videoNuevo = null;
        string? miniaturaNueva = null;

        try
        {
            if (Video is not null && Video.Length > 0)
            {
                videoNuevo = await _archivos.GuardarAsync(Video, "tutoriales");
                archivoPath = videoNuevo;
            }

            if (Miniatura is not null && Miniatura.Length > 0)
            {
                miniaturaNueva = await _archivos.GuardarAsync(Miniatura, "tutoriales");
                miniPath = miniaturaNueva;
            }
        }
        catch (InvalidOperationException ex)
        {
            LimpiarArchivoNuevo(videoNuevo, "video");
            LimpiarArchivoNuevo(miniaturaNueva, "miniatura");

            EsError = true;
            Mensaje = ex.Message;
            await CargarPaginaAsync();
            return Page();
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(videoNuevo, "video");
            LimpiarArchivoNuevo(miniaturaNueva, "miniatura");

            _log.LogError(ex, "Error al almacenar archivos del tutorial {TutorialId}.", Id);
            EsError = true;
            Mensaje = "No se pudieron almacenar los archivos del tutorial.";
            await CargarPaginaAsync();
            return Page();
        }

        var areaFinal = contexto.EsAdminGeneral ? NormalizarAreaAdmin(AreaPublicacionId) : contexto.AreaId;
        var usuarioId = ObtenerUsuarioId();

        var tutorial = existente ?? new Tutorial { FechaCreacion = DateTime.Now };
        tutorial.Id = Id;
        tutorial.Titulo = Titulo.Trim();
        tutorial.Descripcion = Descripcion?.Trim();
        tutorial.ArchivoPath = archivoPath;
        tutorial.MiniaturaPath = miniPath;
        tutorial.Activo = Activo;
        tutorial.Orden = existente?.Orden ?? 0;
        tutorial.AreaPublicacionId = areaFinal;

        if (Id > 0)
            tutorial.ActualizadoPorUsuarioId = usuarioId;
        else
            tutorial.CreadoPorUsuarioId = usuarioId;

        try
        {
            if (Id > 0)
            {
                var filasVerificadas = await _tutorialesRepo.ActualizarAsync(tutorial);
                if (filasVerificadas != 1)
                    throw new InvalidOperationException("No se encontró el tutorial a actualizar.");
            }
            else
            {
                var idCreado = await _tutorialesRepo.InsertarAsync(tutorial);
                if (idCreado <= 0)
                    throw new InvalidOperationException("No se pudo crear el tutorial.");
            }
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(videoNuevo, "video");
            LimpiarArchivoNuevo(miniaturaNueva, "miniatura");

            _log.LogError(ex, "Error al guardar el tutorial {TutorialId}.", Id);
            EsError = true;
            Mensaje = "No se pudo guardar el tutorial. Los archivos nuevos fueron descartados.";
            await CargarPaginaAsync();
            return Page();
        }

        EliminarArchivoAnteriorSiFueReemplazado(videoAnterior, videoNuevo, "video");
        EliminarArchivoAnteriorSiFueReemplazado(miniaturaAnterior, miniaturaNueva, "miniatura");

        return RedirectToPage(new { AreaFiltro });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var contexto = await ObtenerContextoAsync();
        if (!contexto.PuedeGestionar)
            return StatusCode(StatusCodes.Status403Forbidden);

        var tutorial = await _tutorialesRepo.ObtenerPorIdAsync(id);
        if (tutorial is null)
            return RedirectToPage(new { AreaFiltro });

        if (!PuedeOperarTutorial(contexto, tutorial))
            return StatusCode(StatusCodes.Status403Forbidden);

        var filasEliminadas = await _tutorialesRepo.EliminarAsync(id);
        if (filasEliminadas > 0)
        {
            EliminarArchivoPosteriorABd(tutorial.ArchivoPath, "video");
            EliminarArchivoPosteriorABd(tutorial.MiniaturaPath, "miniatura");
        }

        return RedirectToPage(new { AreaFiltro });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var contexto = await ObtenerContextoAsync();
        if (!contexto.PuedeGestionar)
            return StatusCode(StatusCodes.Status403Forbidden);

        var tutorial = await _tutorialesRepo.ObtenerPorIdAsync(id);
        if (tutorial is null)
            return RedirectToPage(new { AreaFiltro });

        if (!PuedeOperarTutorial(contexto, tutorial))
            return StatusCode(StatusCodes.Status403Forbidden);

        await _tutorialesRepo.CambiarEstadoAsync(id, !tutorial.Activo, ObtenerUsuarioId());
        return RedirectToPage(new { AreaFiltro });
    }

    private async Task<string?> ValidarFormularioAsync(ContextoTutoriales contexto)
    {
        Titulo = Titulo.Trim();
        Descripcion = Descripcion?.Trim();

        if (string.IsNullOrWhiteSpace(Titulo))
            return "El título del tutorial es obligatorio.";
        if (Titulo.Length > MaxTitulo)
            return $"El título no debe superar {MaxTitulo} caracteres.";
        if (ContieneControl(Titulo))
            return "El título contiene caracteres no permitidos.";
        if (!string.IsNullOrWhiteSpace(Descripcion) && Descripcion.Length > MaxDescripcion)
            return $"La descripción no debe superar {MaxDescripcion} caracteres.";
        if (ContieneControl(Descripcion))
            return "La descripción contiene caracteres no permitidos.";

        if (contexto.EsAdminGeneral)
        {
            var area = NormalizarAreaAdmin(AreaPublicacionId);
            if (area.HasValue)
            {
                var areaExistente = await _areasRepo.ObtenerPorIdAsync(area.Value);
                if (areaExistente is null || !areaExistente.Activa)
                    return "Selecciona un área de publicación activa.";
            }
        }
        else if (contexto.AreaId is null)
        {
            return "Tu usuario no tiene un área de publicación activa asignada.";
        }

        var errorVideo = ValidarArchivo(Video, [".mp4"], "Solo se permiten archivos de video .mp4.");
        if (errorVideo is not null)
            return errorVideo;

        var errorMiniatura = ValidarArchivo(
            Miniatura,
            [".png", ".jpg", ".jpeg"],
            "Solo se permiten miniaturas PNG o JPG.");

        return errorMiniatura;
    }

    private async Task CargarPaginaAsync()
    {
        Areas = await _areasRepo.ObtenerActivasAsync();
        var contexto = await ObtenerContextoAsync();

        PuedeGestionarTutoriales = contexto.PuedeGestionar;
        AreaActualNombre = contexto.AreaNombre;

        if (contexto.EsAdminGeneral)
        {
            Tutoriales = AreaFiltro is > 0
                ? await _tutorialesRepo.ObtenerPorAreaPublicacionAsync(AreaFiltro.Value)
                : await _tutorialesRepo.ObtenerTodosAsync();
            return;
        }

        Tutoriales = contexto.AreaId.HasValue
            ? await _tutorialesRepo.ObtenerPorAreaPublicacionAsync(contexto.AreaId.Value)
            : [];
    }

    private async Task<ContextoTutoriales> ObtenerContextoAsync()
    {
        if (EsAdminGeneral())
            return new ContextoTutoriales(true, null, null, true);

        if (!EsUsuarioArea())
            return new ContextoTutoriales(false, null, null, false);

        var areaId = ObtenerAreaPublicacionId();
        if (areaId is null or <= 0)
            return new ContextoTutoriales(false, null, null, false);

        var area = await _areasRepo.ObtenerPorIdAsync(areaId.Value);
        return area is { Activa: true }
            ? new ContextoTutoriales(false, area.Id, area.Nombre, true)
            : new ContextoTutoriales(false, null, null, false);
    }

    private static bool PuedeOperarTutorial(ContextoTutoriales contexto, Tutorial tutorial) =>
        contexto.EsAdminGeneral ||
        contexto.AreaId.HasValue &&
        tutorial.AreaPublicacionId == contexto.AreaId.Value;

    private static int? NormalizarAreaAdmin(int? areaId) =>
        areaId is > 0 ? areaId : null;

    private static string? ValidarArchivo(
        IFormFile? archivo,
        IReadOnlyCollection<string> extensionesPermitidas,
        string mensaje)
    {
        if (archivo is null || archivo.Length == 0)
            return null;

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        return extensionesPermitidas.Contains(ext) ? null : mensaje;
    }

    private static bool ContieneControl(string? valor) =>
        !string.IsNullOrEmpty(valor) &&
        valor.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t');

    private void LimpiarArchivoNuevo(string? rutaRelativa, string tipo)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        try
        {
            _archivos.Eliminar(rutaRelativa);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No se pudo limpiar el archivo nuevo de {Tipo} después de un fallo de guardado.",
                tipo);
        }
    }

    private void EliminarArchivoAnteriorSiFueReemplazado(
        string? rutaAnterior,
        string? rutaNueva,
        string tipo)
    {
        if (string.IsNullOrWhiteSpace(rutaAnterior) ||
            string.IsNullOrWhiteSpace(rutaNueva) ||
            string.Equals(rutaAnterior, rutaNueva, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        EliminarArchivoPosteriorABd(rutaAnterior, tipo);
    }

    private void EliminarArchivoPosteriorABd(string? rutaRelativa, string tipo)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        try
        {
            _archivos.Eliminar(rutaRelativa);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "La BD se actualizó, pero no se pudo eliminar el archivo físico de {Tipo}.",
                tipo);
        }
    }

    private sealed record ContextoTutoriales(
        bool EsAdminGeneral,
        int? AreaId,
        string? AreaNombre,
        bool PuedeGestionar);
}
