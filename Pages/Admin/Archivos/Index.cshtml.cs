using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin.Archivos;

public class IndexModel : PageModel
{
    private readonly IArchivoSeccionRepository _archivosRepo;
    private readonly IArchivoService           _archivosSvc;

    // Secciones disponibles con sus etiquetas de presentación
    public static readonly Dictionary<string, string> Secciones = new()
    {
        ["formatos"]     = "Formatos Contraloría",
        ["manuales"]     = "Manuales Justicia NET",
        ["dgie"]         = "Solicitudes DGIE",
        ["identidad"]    = "Identidad Gráfica",
        ["capacitacion"] = "Oferta Académica"
    };

    public IndexModel(IArchivoSeccionRepository archivosRepo, IArchivoService archivosSvc)
    {
        _archivosRepo = archivosRepo;
        _archivosSvc  = archivosSvc;
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
        string rutaRelativa;

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

        var registro = new ArchivoSeccion
        {
            Seccion     = SeccionActual,
            Nombre      = Nombre.Trim(),
            Descripcion = Descripcion?.Trim(),
            ArchivoPath = rutaRelativa,
            Activo      = true,
            Orden       = 0
        };

        await _archivosRepo.InsertarAsync(registro);
        Mensaje = "Archivo subido correctamente.";
        return RedirectToPage(new { seccion = SeccionActual });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id, string seccion)
    {
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is not null)
            _archivosSvc.Eliminar(archivo.ArchivoPath);

        await _archivosRepo.EliminarAsync(id);
        return RedirectToPage(new { seccion });
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string seccion)
    {
        var archivo = await _archivosRepo.ObtenerPorIdAsync(id);
        if (archivo is not null)
            await _archivosRepo.CambiarEstadoAsync(id, !archivo.Activo);
        return RedirectToPage(new { seccion });
    }
}
