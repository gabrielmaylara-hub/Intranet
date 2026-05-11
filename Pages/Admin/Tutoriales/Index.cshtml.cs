using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin.Tutoriales;

public class IndexModel : PageModel
{
    private readonly ITutorialRepository _tutorialesRepo;
    private readonly IArchivoService     _archivos;
    private readonly ILogger<IndexModel> _log;

    public IndexModel(
        ITutorialRepository tutorialesRepo,
        IArchivoService archivos,
        ILogger<IndexModel> log)
    {
        _tutorialesRepo = tutorialesRepo;
        _archivos       = archivos;
        _log            = log;
    }

    public IEnumerable<Tutorial> Tutoriales { get; private set; } = [];

    [BindProperty] public int      Id          { get; set; }
    [BindProperty] public string   Titulo      { get; set; } = string.Empty;
    [BindProperty] public string?  Descripcion { get; set; }
    [BindProperty] public bool     Activo      { get; set; } = true;
    [BindProperty] public IFormFile? Video     { get; set; }
    [BindProperty] public IFormFile? Miniatura { get; set; }

    public string? Mensaje { get; private set; }
    public bool    EsError { get; private set; }

    public async Task OnGetAsync() =>
        Tutoriales = await _tutorialesRepo.ObtenerTodosAsync();

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Titulo))
        {
            EsError = true;
            Mensaje = "El título del tutorial es obligatorio.";
            await OnGetAsync();
            return Page();
        }

        string? videoAnterior = null;
        string? miniaturaAnterior = null;
        string? archivoPath = null;
        string? miniPath = null;
        string? videoNuevo = null;
        string? miniaturaNueva = null;

        if (Id > 0)
        {
            var existente = await _tutorialesRepo.ObtenerPorIdAsync(Id);
            videoAnterior = existente?.ArchivoPath;
            miniaturaAnterior = existente?.MiniaturaPath;
            archivoPath = videoAnterior;
            miniPath = miniaturaAnterior;
        }

        if (Video is not null && Video.Length > 0)
        {
            // Valida que sea un archivo de video
            var ext = Path.GetExtension(Video.FileName).ToLowerInvariant();
            if (ext != ".mp4")
            {
                EsError = true;
                Mensaje = "Solo se permiten archivos de video .mp4.";
                await OnGetAsync();
                return Page();
            }
        }

        if (Miniatura is not null && Miniatura.Length > 0)
        {
            var ext = Path.GetExtension(Miniatura.FileName).ToLowerInvariant();
            if (ext is not ".png" and not ".jpg" and not ".jpeg")
            {
                EsError = true;
                Mensaje = "Solo se permiten miniaturas PNG o JPG.";
                await OnGetAsync();
                return Page();
            }
        }

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
            await OnGetAsync();
            return Page();
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(videoNuevo, "video");
            LimpiarArchivoNuevo(miniaturaNueva, "miniatura");

            _log.LogError(ex, "Error al almacenar archivos del tutorial {TutorialId}.", Id);
            EsError = true;
            Mensaje = "No se pudieron almacenar los archivos del tutorial.";
            await OnGetAsync();
            return Page();
        }

        var tutorial = new Tutorial
        {
            Id           = Id,
            Titulo       = Titulo.Trim(),
            Descripcion  = Descripcion?.Trim(),
            ArchivoPath  = archivoPath,
            MiniaturaPath= miniPath,
            Activo       = Activo,
            Orden        = 0,
            FechaCreacion= DateTime.Now
        };

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
            await OnGetAsync();
            return Page();
        }

        EliminarArchivoAnteriorSiFueReemplazado(videoAnterior, videoNuevo, "video");
        EliminarArchivoAnteriorSiFueReemplazado(miniaturaAnterior, miniaturaNueva, "miniatura");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var tutorial = await _tutorialesRepo.ObtenerPorIdAsync(id);
        if (tutorial is null)
            return RedirectToPage();

        var filasEliminadas = await _tutorialesRepo.EliminarAsync(id);
        if (filasEliminadas > 0)
        {
            EliminarArchivoPosteriorABd(tutorial.ArchivoPath, "video");
            EliminarArchivoPosteriorABd(tutorial.MiniaturaPath, "miniatura");
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var tutorial = await _tutorialesRepo.ObtenerPorIdAsync(id);
        if (tutorial is not null)
            await _tutorialesRepo.CambiarEstadoAsync(id, !tutorial.Activo);
        return RedirectToPage();
    }

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
}
