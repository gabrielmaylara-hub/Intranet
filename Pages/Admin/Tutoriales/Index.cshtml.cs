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

    public IndexModel(ITutorialRepository tutorialesRepo, IArchivoService archivos)
    {
        _tutorialesRepo = tutorialesRepo;
        _archivos       = archivos;
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

        string? archivoPath  = null;
        string? miniPath     = null;

        if (Id > 0)
        {
            var existente = await _tutorialesRepo.ObtenerPorIdAsync(Id);
            archivoPath  = existente?.ArchivoPath;
            miniPath     = existente?.MiniaturaPath;
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

            try
            {
                var videoAnterior = archivoPath;
                archivoPath = await _archivos.GuardarAsync(Video, "tutoriales");

                if (!string.IsNullOrWhiteSpace(videoAnterior))
                    _archivos.Eliminar(videoAnterior);
            }
            catch (InvalidOperationException ex)
            {
                EsError = true;
                Mensaje = ex.Message;
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

            try
            {
                var miniaturaAnterior = miniPath;
                miniPath = await _archivos.GuardarAsync(Miniatura, "tutoriales");

                if (!string.IsNullOrWhiteSpace(miniaturaAnterior))
                    _archivos.Eliminar(miniaturaAnterior);
            }
            catch (InvalidOperationException ex)
            {
                EsError = true;
                Mensaje = ex.Message;
                await OnGetAsync();
                return Page();
            }
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

        if (Id > 0)
            await _tutorialesRepo.ActualizarAsync(tutorial);
        else
            await _tutorialesRepo.InsertarAsync(tutorial);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var tutorial = await _tutorialesRepo.ObtenerPorIdAsync(id);
        if (tutorial is not null)
        {
            _archivos.Eliminar(tutorial.ArchivoPath);
            _archivos.Eliminar(tutorial.MiniaturaPath);
        }

        await _tutorialesRepo.EliminarAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var tutorial = await _tutorialesRepo.ObtenerPorIdAsync(id);
        if (tutorial is not null)
            await _tutorialesRepo.CambiarEstadoAsync(id, !tutorial.Activo);
        return RedirectToPage();
    }
}
