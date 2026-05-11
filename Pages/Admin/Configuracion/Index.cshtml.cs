using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Intranet.Pages.Admin.Configuracion;

public class IndexModel : PageModel
{
    private readonly IConfiguracionRepository _configRepo;
    private readonly IArchivoService          _archivos;

    public IndexModel(IConfiguracionRepository configRepo, IArchivoService archivos)
    {
        _configRepo = configRepo;
        _archivos   = archivos;
    }

    public IEnumerable<ConfiguracionSitio> Configuraciones { get; private set; } = [];

    [BindProperty] public string NombreSitio       { get; set; } = string.Empty;
    [BindProperty] public string FooterTexto        { get; set; } = string.Empty;
    [BindProperty] public string FooterSubtexto     { get; set; } = string.Empty;
    [BindProperty] public string FooterEmail        { get; set; } = string.Empty;
    [BindProperty] public string FooterTelefono     { get; set; } = string.Empty;
    [BindProperty] public string FooterDireccion    { get; set; } = string.Empty;
    [BindProperty] public string ColorDorado        { get; set; } = "#c9922a";
    [BindProperty] public string ColorDoradoBrillo  { get; set; } = "#e8a820";
    [BindProperty] public IFormFile? Logo           { get; set; }

    public string? Mensaje { get; private set; }
    public bool    EsError { get; private set; }
    public string  LogoActual { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        NombreSitio    = config.GetValueOrDefault("nombre_sitio",          "Intranet FGET");
        FooterTexto    = config.GetValueOrDefault("footer_texto",           "");
        FooterSubtexto = config.GetValueOrDefault("footer_subtexto",        "");
        FooterEmail    = config.GetValueOrDefault("footer_contacto_email",  "");
        FooterTelefono = config.GetValueOrDefault("footer_contacto_tel",    "");
        FooterDireccion= config.GetValueOrDefault("footer_direccion",       "");
        ColorDorado    = config.GetValueOrDefault("color_dorado",           "#c9922a");
        ColorDoradoBrillo = config.GetValueOrDefault("color_dorado_bri",    "#e8a820");
        LogoActual     = config.GetValueOrDefault("logo_path",              "");
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var valores = new Dictionary<string, string>
        {
            ["nombre_sitio"]          = NombreSitio    ?? "",
            ["footer_texto"]          = FooterTexto     ?? "",
            ["footer_subtexto"]       = FooterSubtexto  ?? "",
            ["footer_contacto_email"] = FooterEmail     ?? "",
            ["footer_contacto_tel"]   = FooterTelefono  ?? "",
            ["footer_direccion"]      = FooterDireccion ?? "",
            ["color_dorado"]          = NormalizarHex(ColorDorado, "#c9922a"),
            ["color_dorado_bri"]      = NormalizarHex(ColorDoradoBrillo, "#e8a820")
        };

        await _configRepo.GuardarMultiplesAsync(valores);
        Mensaje = "Configuración guardada correctamente.";
        await OnGetAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSubirLogoAsync()
    {
        if (Logo is null || Logo.Length == 0)
        {
            EsError = true;
            Mensaje = "Selecciona un archivo de imagen.";
            await OnGetAsync();
            return Page();
        }

        var extensionesPermitidas = new[] { ".png", ".svg", ".jpg", ".jpeg" };
        var ext = Path.GetExtension(Logo.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(ext))
        {
            EsError = true;
            Mensaje = "Solo se permiten imágenes PNG, SVG o JPG.";
            await OnGetAsync();
            return Page();
        }

        var configActual = await _configRepo.ObtenerTodosAsync();
        var logoAnterior = configActual.GetValueOrDefault("logo_path", "");
        string rutaRelativa;

        try
        {
            // Sobrescribe el logo anterior usando nombre fijo "logo".
            rutaRelativa = await _archivos.GuardarAsync(Logo, "config", "logo");

            if (!string.IsNullOrWhiteSpace(logoAnterior)
                && !string.Equals(logoAnterior, rutaRelativa, StringComparison.OrdinalIgnoreCase))
            {
                _archivos.Eliminar(logoAnterior);
            }
        }
        catch (InvalidOperationException ex)
        {
            EsError = true;
            Mensaje = ex.Message;
            await OnGetAsync();
            return Page();
        }

        await _configRepo.GuardarAsync("logo_path", rutaRelativa);

        Mensaje = "Logo actualizado correctamente.";
        await OnGetAsync();
        return Page();
    }

    private static string NormalizarHex(string? valor, string fallback)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return fallback;

        valor = valor.Trim();
        var esHexValido = valor.Length == 7
            && valor[0] == '#'
            && valor.Skip(1).All(Uri.IsHexDigit);

        return esHexValido ? valor : fallback;
    }
}
