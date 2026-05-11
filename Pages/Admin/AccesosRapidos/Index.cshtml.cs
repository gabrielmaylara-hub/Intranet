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

    public IndexModel(IAccesoRapidoRepository accesosRepo, IArchivoService archivos)
    {
        _accesosRepo = accesosRepo;
        _archivos    = archivos;
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

        string? iconoPath = null;
        string? bannerPath = null;
        var ordenActual = 999;

        if (Id > 0)
        {
            // Edición: conserva ícono y orden si no se reemplazan desde el formulario.
            var existente = await _accesosRepo.ObtenerPorIdAsync(Id);
            iconoPath = existente?.IconoPath;
            bannerPath = existente?.BannerPath;
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

            try
            {
                var iconoAnterior = iconoPath;
                iconoPath = await _archivos.GuardarAsync(Icono, "iconos");

                if (!string.IsNullOrWhiteSpace(iconoAnterior))
                    _archivos.Eliminar(iconoAnterior);
            }
            catch (InvalidOperationException ex)
            {
                EsError = true;
                Mensaje = ex.Message;
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

            try
            {
                var bannerAnterior = bannerPath;
                bannerPath = await _archivos.GuardarAsync(Banner, "banners/accesos");

                if (!string.IsNullOrWhiteSpace(bannerAnterior))
                    _archivos.Eliminar(bannerAnterior);
            }
            catch (InvalidOperationException ex)
            {
                EsError = true;
                Mensaje = ex.Message;
                await OnGetAsync();
                return Page();
            }
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

        if (Id > 0)
            await _accesosRepo.ActualizarAsync(acceso);
        else
            await _accesosRepo.InsertarAsync(acceso);

        Mensaje = Id > 0 ? "Acceso actualizado." : "Acceso creado.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var acceso = await _accesosRepo.ObtenerPorIdAsync(id);
        if (acceso is not null)
        {
            _archivos.Eliminar(acceso.IconoPath);
            _archivos.Eliminar(acceso.BannerPath);
        }

        await _accesosRepo.EliminarAsync(id);
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

    public record ReordenItem(int Id, int Orden);
}
