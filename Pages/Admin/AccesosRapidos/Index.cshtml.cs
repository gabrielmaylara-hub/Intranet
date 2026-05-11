using System.Text.Json;
using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin.AccesosRapidos;

public class IndexModel : PageModel
{
    private readonly IAccesoRapidoRepository _accesosRepo;
    private readonly IArchivoService         _archivos;
    private readonly ILogger<IndexModel>     _log;

    public IndexModel(
        IAccesoRapidoRepository accesosRepo,
        IArchivoService archivos,
        ILogger<IndexModel> log)
    {
        _accesosRepo = accesosRepo;
        _archivos    = archivos;
        _log         = log;
    }

    public IEnumerable<AccesoRapido> Accesos { get; private set; } = [];

    [BindProperty] public int     Id               { get; set; }
    [BindProperty] public string  Nombre           { get; set; } = string.Empty;
    [BindProperty] public string  UrlAcceso        { get; set; } = string.Empty;
    [BindProperty] public bool    AbreNuevaVentana { get; set; } = true;
    [BindProperty] public bool    Activo           { get; set; } = true;
    [BindProperty] public IFormFile? Icono         { get; set; }
    [BindProperty] public IFormFile? Banner        { get; set; }

    public string? Mensaje { get; private set; }
    public bool    EsError { get; private set; }

    public async Task OnGetAsync() =>
        Accesos = await _accesosRepo.ObtenerTodosAsync();

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(UrlAcceso))
        {
            EsError = true;
            Mensaje = "El nombre y la URL son obligatorios.";
            await OnGetAsync();
            return Page();
        }

        var urlNormalizada = UrlAcceso.Trim();
        if (!EsUrlAccesoValida(urlNormalizada))
        {
            EsError = true;
            Mensaje = "La URL debe iniciar con / o ser una dirección http/https válida.";
            await OnGetAsync();
            return Page();
        }

        string? iconoAnterior = null;
        string? bannerAnterior = null;
        string? iconoPath = null;
        string? bannerPath = null;
        string? iconoNuevo = null;
        string? bannerNuevo = null;
        var ordenActual = 999;

        if (Id > 0)
        {
            // Edición: conserva ícono y orden si no se reemplazan desde el formulario.
            var existente = await _accesosRepo.ObtenerPorIdAsync(Id);
            iconoAnterior = existente?.IconoPath;
            bannerAnterior = existente?.BannerPath;
            iconoPath = iconoAnterior;
            bannerPath = bannerAnterior;
            ordenActual = existente?.Orden ?? ordenActual;
        }

        if (Icono is not null && Icono.Length > 0)
        {
            var ext = Path.GetExtension(Icono.FileName).ToLowerInvariant();
            if (ext is not ".png" and not ".svg" and not ".jpg" and not ".jpeg")
            {
                EsError = true;
                Mensaje = "Solo se permiten íconos PNG, SVG o JPG.";
                await OnGetAsync();
                return Page();
            }
        }

        if (Banner is not null && Banner.Length > 0)
        {
            var ext = Path.GetExtension(Banner.FileName).ToLowerInvariant();
            if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
            {
                EsError = true;
                Mensaje = "Solo se permiten banners PNG, JPG o WEBP.";
                await OnGetAsync();
                return Page();
            }
        }

        try
        {
            if (Icono is not null && Icono.Length > 0)
            {
                iconoNuevo = await _archivos.GuardarAsync(Icono, "iconos");
                iconoPath = iconoNuevo;
            }

            if (Banner is not null && Banner.Length > 0)
            {
                bannerNuevo = await _archivos.GuardarAsync(Banner, "banners/accesos");
                bannerPath = bannerNuevo;
            }
        }
        catch (InvalidOperationException ex)
        {
            LimpiarArchivoNuevo(iconoNuevo, "icono");
            LimpiarArchivoNuevo(bannerNuevo, "banner");

            EsError = true;
            Mensaje = ex.Message;
            await OnGetAsync();
            return Page();
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(iconoNuevo, "icono");
            LimpiarArchivoNuevo(bannerNuevo, "banner");

            _log.LogError(ex, "Error al almacenar archivos del acceso rapido {AccesoId}.", Id);
            EsError = true;
            Mensaje = "No se pudieron almacenar los archivos del acceso rápido.";
            await OnGetAsync();
            return Page();
        }

        var acceso = new AccesoRapido
        {
            Id               = Id,
            Nombre           = Nombre.Trim(),
            Url              = urlNormalizada,
            IconoPath        = iconoPath,
            BannerPath       = bannerPath,
            AbreNuevaVentana = AbreNuevaVentana,
            Activo           = Activo,
            Orden            = Id > 0 ? ordenActual : 999 // El orden final se ajusta con Sortable.
        };

        try
        {
            if (Id > 0)
            {
                var filasVerificadas = await _accesosRepo.ActualizarAsync(acceso);
                if (filasVerificadas != 1)
                    throw new InvalidOperationException("No se encontro el acceso rapido a actualizar.");
            }
            else
            {
                var idCreado = await _accesosRepo.InsertarAsync(acceso);
                if (idCreado <= 0)
                    throw new InvalidOperationException("No se pudo crear el acceso rapido.");
            }
        }
        catch (Exception ex)
        {
            LimpiarArchivoNuevo(iconoNuevo, "icono");
            LimpiarArchivoNuevo(bannerNuevo, "banner");

            _log.LogError(ex, "Error al guardar el acceso rapido {AccesoId}.", Id);
            EsError = true;
            Mensaje = "No se pudo guardar el acceso rápido. Los archivos nuevos fueron descartados.";
            await OnGetAsync();
            return Page();
        }

        EliminarArchivoAnteriorSiFueReemplazado(iconoAnterior, iconoNuevo, "icono");
        EliminarArchivoAnteriorSiFueReemplazado(bannerAnterior, bannerNuevo, "banner");

        Mensaje = Id > 0 ? "Acceso actualizado." : "Acceso creado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var acceso = await _accesosRepo.ObtenerPorIdAsync(id);
        if (acceso is null)
            return RedirectToPage();

        var filasEliminadas = await _accesosRepo.EliminarAsync(id);
        if (filasEliminadas > 0)
        {
            EliminarArchivoPosteriorABd(acceso.IconoPath, "icono");
            EliminarArchivoPosteriorABd(acceso.BannerPath, "banner");
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var acceso = await _accesosRepo.ObtenerPorIdAsync(id);
        if (acceso is not null)
            await _accesosRepo.CambiarEstadoAsync(id, !acceso.Activo);
        return RedirectToPage();
    }

    // Llamado por Sortable.js vía fetch con JSON en el cuerpo
    public async Task<IActionResult> OnPostReordenarAsync()
    {
        Request.EnableBuffering();
        using var lector = new StreamReader(Request.Body, leaveOpen: true);
        var json = await lector.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(json))
            return BadRequest(new
            {
                ok = false,
                message = "No se pudo actualizar el orden de los accesos rápidos."
            });

        List<ReordenItem>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ReordenItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                ok = false,
                message = "El formato del orden recibido no es válido."
            });
        }

        if (items is null || items.Count == 0 || items.Any(i => i.Id <= 0))
            return BadRequest(new
            {
                ok = false,
                message = "No se pudo actualizar el orden de los accesos rápidos."
            });

        var idsDuplicados = items
            .GroupBy(i => i.Id)
            .Any(g => g.Count() > 1);

        if (idsDuplicados)
            return BadRequest(new
            {
                ok = false,
                message = "No se pudo actualizar el orden de los accesos rápidos."
            });

        var ordenCalculado = items
            .Select((item, indice) => (item.Id, Orden: indice + 1))
            .ToList();

        var filasVerificadas = await _accesosRepo.ActualizarOrdenAsync(ordenCalculado);

        if (filasVerificadas != ordenCalculado.Count)
            return NotFound(new
            {
                ok = false,
                message = "No se pudo actualizar el orden de los accesos rápidos."
            });

        return new JsonResult(new
        {
            ok = true,
            message = "Orden actualizado correctamente."
        });
    }

    private static bool EsUrlAccesoValida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var valor = url.Trim();

        if (valor.Any(char.IsControl))
            return false;

        if (valor.StartsWith("//", StringComparison.Ordinal))
            return false;

        if (valor.StartsWith("/", StringComparison.Ordinal))
            return true;

        if (Uri.TryCreate(valor, UriKind.Absolute, out var uri))
            return (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps) &&
                   !string.IsNullOrWhiteSpace(uri.Host);

        return false;
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
                "No se pudo limpiar el archivo nuevo de {Tipo} despues de un fallo de guardado: {Ruta}.",
                tipo,
                rutaRelativa);
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
                "La BD se actualizo, pero no se pudo eliminar el archivo fisico de {Tipo}: {Ruta}.",
                tipo,
                rutaRelativa);
        }
    }

    public record ReordenItem(int Id, int Orden);
}
