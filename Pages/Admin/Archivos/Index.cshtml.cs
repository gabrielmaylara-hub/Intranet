using Intranet.Models;
using Intranet.Pages.Admin;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Intranet.Pages.Admin.Archivos;

public class IndexModel : AdminPageModel
{
    protected override bool RequiereAdminGeneral => true;

    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IArchivoService           _archivosSvc;
    private readonly ILogger<IndexModel>       _log;

    // Secciones disponibles con sus etiquetas de presentación
    public static readonly Dictionary<string, string> Secciones = new()
    {
        ["formatos"]     = "Formatos Contraloría",
        ["manuales"]     = "Manuales Justicia NET",
        ["dgie"]         = "Solicitudes DGIE",
        ["identidad"]    = "Identidad Gráfica",
        ["capacitacion"] = "Oferta Académica"
    };

    public IndexModel(
        IArchivoSeccionRepository archivosRepo,
        IArchivoService archivosSvc,
        ILogger<IndexModel> log)
    {
        _archivosRepo = archivosRepo;
        _archivosSvc  = archivosSvc;
        _log          = log;
    }

    public string SeccionActual { get; private set; } = "formatos";
    public IEnumerable<ArchivoSeccion> Archivos { get; private set; } = [];

    [BindProperty] public string   Nombre      { get; set; } = string.Empty;
    [BindProperty] public string?  Descripcion { get; set; }
    [BindProperty] public IFormFile? Archivo   { get; set; }

    public string? Mensaje { get; private set; }
    public bool    EsError { get; private set; }

    public async Task OnGetAsync(string? seccion = null)
    {
        SeccionActual = Secciones.ContainsKey(seccion ?? "") ? seccion! : "formatos";
        Archivos = await _archivosRepo.ObtenerPorSeccionAsync(SeccionActual);
    }

    public async Task<IActionResult> OnPostSubirAsync(string seccion)
    {
        SeccionActual = Secciones.ContainsKey(seccion) ? seccion : "formatos";

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            EsError = true;
            Mensaje = "El nombre del archivo es obligatorio.";
            await OnGetAsync(SeccionActual);
            return Page();
        }

        if (Archivo is null || Archivo.Length == 0)
        {
            EsError = true;
            Mensaje = "Selecciona un archivo PDF para subir.";
            await OnGetAsync(SeccionActual);
            return Page();
        }

        var ext = Path.GetExtension(Archivo.FileName).ToLowerInvariant();
        if (ext != ".pdf")
        {
            EsError = true;
            Mensaje = "Solo se permiten archivos PDF.";
            await OnGetAsync(SeccionActual);
            return Page();
        }

        var subcarpeta   = $"archivos/{SeccionActual}";
        string? rutaRelativa = null;

        try
        {
            rutaRelativa = await _archivosSvc.GuardarAsync(Archivo, subcarpeta);
        }
        catch (InvalidOperationException ex)
        {
            EsError = true;
            Mensaje = ex.Message;
            await OnGetAsync(SeccionActual);
            return Page();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error al almacenar archivo PDF para la sección {Seccion}.", SeccionActual);
            EsError = true;
            Mensaje = "No se pudo almacenar el archivo PDF.";
            await OnGetAsync(SeccionActual);
            return Page();
        }

        var registro = new ArchivoSeccion
        {
            Seccion     = SeccionActual,
            Nombre      = Nombre.Trim(),
            Descripcion = Descripcion?.Trim(),
            ArchivoPath = rutaRelativa,
            Activo      = true,
            Orden       = 0
        };

        try
        {
            var idCreado = await _archivosRepo.InsertarAsync(registro);
            if (idCreado <= 0)
                throw new InvalidOperationException("No se pudo registrar el archivo en la base de datos.");
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(rutaRelativa, "PDF");

            _log.LogError(ex, "Error al registrar archivo PDF para la sección {Seccion}.", SeccionActual);
            EsError = true;
            Mensaje = "No se pudo guardar el registro del archivo. El PDF nuevo fue descartado.";
            await OnGetAsync(SeccionActual);
            return Page();
        }

        Mensaje = "Archivo subido correctamente.";
        return RedirectToPage(new { seccion = SeccionActual });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id, string seccion)
    {
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is null)
            return RedirectToPage(new { seccion });

        var filasEliminadas = await _archivosRepo.EliminarAsync(id);
        if (filasEliminadas > 0)
            EliminarArchivoPosteriorABd(archivo.ArchivoPath, "PDF");

        return RedirectToPage(new { seccion });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string seccion)
    {
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is not null)
            await _archivosRepo.CambiarEstadoAsync(id, !archivo.Activo);
        return RedirectToPage(new { seccion });
    }

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
                "No se pudo limpiar el archivo nuevo de {Tipo} después de un fallo de guardado.",
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
                "La BD se actualizó, pero no se pudo eliminar el archivo físico de {Tipo}.",
                tipo);
        }
    }
}
