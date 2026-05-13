using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Mail;

namespace Intranet.Pages.Admin.Configuracion;

public class IndexModel : PageModel
{
    private const string GrupoHeader = "header_principal";
    private const string GrupoFooterRecursos = "footer_recursos";
    private const string GrupoFooterSistemas = "footer_sistemas";

    private const int MaxNombreSitio = 120;
    private const int MaxTextoCorto = 150;
    private const int MaxTextoMedio = 300;
    private const int MaxTextoLargo = 500;
    private const int MaxEmail = 254;
    private const int MaxTelefono = 60;
    private const int MaxDireccion = 250;
    private const int MaxUrl = 500;

    private readonly IConfiguracionRepository _configRepo;
    private readonly IArchivoService _archivos;

    public IndexModel(IConfiguracionRepository configRepo, IArchivoService archivos)
    {
        _configRepo = configRepo;
        _archivos = archivos;
    }

    [BindProperty] public string NombreSitio { get; set; } = string.Empty;
    [BindProperty] public string HeaderSubtitulo { get; set; } = string.Empty;

    [BindProperty] public string HomeHeroEtiqueta { get; set; } = string.Empty;
    [BindProperty] public string HomeHeroTitulo { get; set; } = string.Empty;
    [BindProperty] public string HomeHeroDescripcion { get; set; } = string.Empty;
    [BindProperty] public string HomeBuscadorPlaceholder { get; set; } = string.Empty;
    [BindProperty] public string HomeAccesosEtiqueta { get; set; } = string.Empty;
    [BindProperty] public string HomeAccesosTitulo { get; set; } = string.Empty;
    [BindProperty] public string HomeAccesosDescripcion { get; set; } = string.Empty;
    [BindProperty] public string HomeAvisosEtiqueta { get; set; } = string.Empty;
    [BindProperty] public string HomeAvisosTitulo { get; set; } = string.Empty;
    [BindProperty] public string HomeAvisosVacio { get; set; } = string.Empty;
    [BindProperty] public string HomeTutorialesEtiqueta { get; set; } = string.Empty;
    [BindProperty] public string HomeTutorialesTitulo { get; set; } = string.Empty;
    [BindProperty] public string HomeTutorialesVerTodos { get; set; } = string.Empty;
    [BindProperty] public string HomeTutorialesVacio { get; set; } = string.Empty;

    [BindProperty] public string FooterTexto { get; set; } = string.Empty;
    [BindProperty] public string FooterSubtexto { get; set; } = string.Empty;
    [BindProperty] public string FooterRecursosTitulo { get; set; } = string.Empty;
    [BindProperty] public string FooterSistemasTitulo { get; set; } = string.Empty;
    [BindProperty] public string FooterContactoTitulo { get; set; } = string.Empty;
    [BindProperty] public string FooterEmail { get; set; } = string.Empty;
    [BindProperty] public string FooterTelefono { get; set; } = string.Empty;
    [BindProperty] public string FooterDireccion { get; set; } = string.Empty;
    [BindProperty] public string FooterCopyright { get; set; } = string.Empty;

    [BindProperty] public string ColorDorado { get; set; } = "#c9922a";
    [BindProperty] public string ColorDoradoBrillo { get; set; } = "#e8a820";
    [BindProperty] public IFormFile? Logo { get; set; }

    [BindProperty] public List<SitioEnlaceInput> HeaderLinks { get; set; } = [];
    [BindProperty] public List<SitioEnlaceInput> FooterRecursosLinks { get; set; } = [];
    [BindProperty] public List<SitioEnlaceInput> FooterSistemasLinks { get; set; } = [];

    [TempData] public string? Mensaje { get; set; }
    [TempData] public bool EsError { get; set; }
    public string LogoActual { get; private set; } = string.Empty;

    public async Task OnGetAsync() => await CargarFormularioAsync();

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var errorValidacion = ValidarYNormalizarConfiguracion();
        if (errorValidacion is not null)
        {
            EsError = true;
            Mensaje = errorValidacion;
            await CargarLogoActualAsync();
            return Page();
        }

        var valores = new Dictionary<string, string>
        {
            ["nombre_sitio"] = NombreSitio,
            ["header_subtitulo"] = HeaderSubtitulo,
            ["home_hero_etiqueta"] = HomeHeroEtiqueta,
            ["home_hero_titulo"] = HomeHeroTitulo,
            ["home_hero_descripcion"] = HomeHeroDescripcion,
            ["home_buscador_placeholder"] = HomeBuscadorPlaceholder,
            ["home_accesos_etiqueta"] = HomeAccesosEtiqueta,
            ["home_accesos_titulo"] = HomeAccesosTitulo,
            ["home_accesos_descripcion"] = HomeAccesosDescripcion,
            ["home_avisos_etiqueta"] = HomeAvisosEtiqueta,
            ["home_avisos_titulo"] = HomeAvisosTitulo,
            ["home_avisos_vacio"] = HomeAvisosVacio,
            ["home_tutoriales_etiqueta"] = HomeTutorialesEtiqueta,
            ["home_tutoriales_titulo"] = HomeTutorialesTitulo,
            ["home_tutoriales_ver_todos"] = HomeTutorialesVerTodos,
            ["home_tutoriales_vacio"] = HomeTutorialesVacio,
            ["footer_texto"] = FooterTexto,
            ["footer_subtexto"] = FooterSubtexto,
            ["footer_recursos_titulo"] = FooterRecursosTitulo,
            ["footer_sistemas_titulo"] = FooterSistemasTitulo,
            ["footer_contacto_titulo"] = FooterContactoTitulo,
            ["footer_contacto_email"] = FooterEmail,
            ["footer_contacto_tel"] = FooterTelefono,
            ["footer_direccion"] = FooterDireccion,
            ["footer_copyright"] = FooterCopyright,
            ["color_dorado"] = NormalizarHex(ColorDorado, "#c9922a"),
            ["color_dorado_bri"] = NormalizarHex(ColorDoradoBrillo, "#e8a820")
        };

        await _configRepo.GuardarMultiplesAsync(valores);
        await _configRepo.GuardarEnlacesAsync(GrupoHeader, AModelo(HeaderLinks, GrupoHeader));
        await _configRepo.GuardarEnlacesAsync(GrupoFooterRecursos, AModelo(FooterRecursosLinks, GrupoFooterRecursos));
        await _configRepo.GuardarEnlacesAsync(GrupoFooterSistemas, AModelo(FooterSistemasLinks, GrupoFooterSistemas));

        Mensaje = "Configuración guardada correctamente.";
        EsError = false;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSubirLogoAsync()
    {
        if (Logo is null || Logo.Length == 0)
        {
            EsError = true;
            Mensaje = "Selecciona un archivo de imagen.";
            await CargarFormularioAsync();
            return Page();
        }

        var extensionesPermitidas = new[] { ".png", ".svg", ".jpg", ".jpeg" };
        var ext = Path.GetExtension(Logo.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(ext))
        {
            EsError = true;
            Mensaje = "Solo se permiten imágenes PNG, SVG o JPG.";
            await CargarFormularioAsync();
            return Page();
        }

        var configActual = await _configRepo.ObtenerTodosAsync();
        var logoAnterior = configActual.GetValueOrDefault("logo_path", "");
        string rutaRelativa;

        try
        {
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
            await CargarFormularioAsync();
            return Page();
        }

        await _configRepo.GuardarAsync("logo_path", rutaRelativa);

        Mensaje = "Logo actualizado correctamente.";
        await CargarFormularioAsync();
        return Page();
    }

    private async Task CargarFormularioAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        NombreSitio = config.GetValueOrDefault("nombre_sitio", "Intranet FGET");
        HeaderSubtitulo = config.GetValueOrDefault("header_subtitulo", "Fiscalía General del Estado de Tabasco");

        HomeHeroEtiqueta = config.GetValueOrDefault("home_hero_etiqueta", "Portal institucional");
        HomeHeroTitulo = config.GetValueOrDefault("home_hero_titulo", "INTRANET FGET");
        HomeHeroDescripcion = config.GetValueOrDefault(
            "home_hero_descripcion",
            "Punto de acceso para sistemas, formatos, manuales, solicitudes y recursos de trabajo de la Fiscalía General del Estado de Tabasco.");
        HomeBuscadorPlaceholder = config.GetValueOrDefault("home_buscador_placeholder", "Busca formatos, correo, manuales, capacitación...");
        HomeAccesosEtiqueta = config.GetValueOrDefault("home_accesos_etiqueta", "Directorio de servicios");
        HomeAccesosTitulo = config.GetValueOrDefault("home_accesos_titulo", "ACCESOS RÁPIDOS");
        HomeAccesosDescripcion = config.GetValueOrDefault(
            "home_accesos_descripcion",
            "Accesos concentrados para consulta del personal. Cada elemento dirige al sistema, sección o recurso correspondiente.");
        HomeAvisosEtiqueta = config.GetValueOrDefault("home_avisos_etiqueta", "Comunicación interna");
        HomeAvisosTitulo = config.GetValueOrDefault("home_avisos_titulo", "AVISOS Y COMUNICADOS");
        HomeAvisosVacio = config.GetValueOrDefault("home_avisos_vacio", "No hay avisos publicados en este momento.");
        HomeTutorialesEtiqueta = config.GetValueOrDefault("home_tutoriales_etiqueta", "Material de apoyo");
        HomeTutorialesTitulo = config.GetValueOrDefault("home_tutoriales_titulo", "TUTORIALES Y VIDEOS");
        HomeTutorialesVerTodos = config.GetValueOrDefault("home_tutoriales_ver_todos", "Ver todos →");
        HomeTutorialesVacio = config.GetValueOrDefault("home_tutoriales_vacio", "No hay tutoriales publicados en este momento.");

        FooterTexto = config.GetValueOrDefault("footer_texto", "Fiscalía General del Estado de Tabasco");
        FooterSubtexto = config.GetValueOrDefault("footer_subtexto", "Dirección General de Informática y Estadística");
        FooterRecursosTitulo = config.GetValueOrDefault("footer_recursos_titulo", "RECURSOS");
        FooterSistemasTitulo = config.GetValueOrDefault("footer_sistemas_titulo", "SISTEMAS");
        FooterContactoTitulo = config.GetValueOrDefault("footer_contacto_titulo", "CONTACTO");
        FooterEmail = config.GetValueOrDefault("footer_contacto_email", "");
        FooterTelefono = config.GetValueOrDefault("footer_contacto_tel", "");
        FooterDireccion = config.GetValueOrDefault("footer_direccion", "Villahermosa, Tabasco, México");
        FooterCopyright = config.GetValueOrDefault(
            "footer_copyright",
            "© 2026 Fiscalía General del Estado de Tabasco. Todos los derechos reservados.");

        ColorDorado = config.GetValueOrDefault("color_dorado", "#c9922a");
        ColorDoradoBrillo = config.GetValueOrDefault("color_dorado_bri", "#e8a820");
        LogoActual = config.GetValueOrDefault("logo_path", "");

        HeaderLinks = await CargarEnlacesAsync(GrupoHeader);
        FooterRecursosLinks = await CargarEnlacesAsync(GrupoFooterRecursos);
        FooterSistemasLinks = await CargarEnlacesAsync(GrupoFooterSistemas);
    }

    private async Task CargarLogoActualAsync()
    {
        var config = await _configRepo.ObtenerTodosAsync();
        LogoActual = config.GetValueOrDefault("logo_path", "");
    }

    private async Task<List<SitioEnlaceInput>> CargarEnlacesAsync(string grupo)
    {
        var enlaces = await _configRepo.ObtenerEnlacesAsync(grupo);
        return enlaces
            .Select(SitioEnlaceInput.FromModel)
            .ToList();
    }

    private string? ValidarYNormalizarConfiguracion()
    {
        NombreSitio = NormalizarTexto(NombreSitio);
        HeaderSubtitulo = NormalizarTexto(HeaderSubtitulo);
        HomeHeroEtiqueta = NormalizarTexto(HomeHeroEtiqueta);
        HomeHeroTitulo = NormalizarTexto(HomeHeroTitulo);
        HomeHeroDescripcion = NormalizarTexto(HomeHeroDescripcion);
        HomeBuscadorPlaceholder = NormalizarTexto(HomeBuscadorPlaceholder);
        HomeAccesosEtiqueta = NormalizarTexto(HomeAccesosEtiqueta);
        HomeAccesosTitulo = NormalizarTexto(HomeAccesosTitulo);
        HomeAccesosDescripcion = NormalizarTexto(HomeAccesosDescripcion);
        HomeAvisosEtiqueta = NormalizarTexto(HomeAvisosEtiqueta);
        HomeAvisosTitulo = NormalizarTexto(HomeAvisosTitulo);
        HomeAvisosVacio = NormalizarTexto(HomeAvisosVacio);
        HomeTutorialesEtiqueta = NormalizarTexto(HomeTutorialesEtiqueta);
        HomeTutorialesTitulo = NormalizarTexto(HomeTutorialesTitulo);
        HomeTutorialesVerTodos = NormalizarTexto(HomeTutorialesVerTodos);
        HomeTutorialesVacio = NormalizarTexto(HomeTutorialesVacio);
        FooterTexto = NormalizarTexto(FooterTexto);
        FooterSubtexto = NormalizarTexto(FooterSubtexto);
        FooterRecursosTitulo = NormalizarTexto(FooterRecursosTitulo);
        FooterSistemasTitulo = NormalizarTexto(FooterSistemasTitulo);
        FooterContactoTitulo = NormalizarTexto(FooterContactoTitulo);
        FooterEmail = NormalizarTexto(FooterEmail);
        FooterTelefono = NormalizarTexto(FooterTelefono);
        FooterDireccion = NormalizarTexto(FooterDireccion);
        FooterCopyright = NormalizarTexto(FooterCopyright);

        var textos = new[]
        {
            (NombreSitio, MaxNombreSitio, "Nombre del sitio"),
            (HeaderSubtitulo, MaxTextoCorto, "Subtítulo del header"),
            (HomeHeroEtiqueta, MaxTextoCorto, "Etiqueta del hero"),
            (HomeHeroTitulo, MaxTextoCorto, "Título del hero"),
            (HomeHeroDescripcion, MaxTextoLargo, "Descripción del hero"),
            (HomeBuscadorPlaceholder, MaxTextoMedio, "Placeholder del buscador"),
            (HomeAccesosEtiqueta, MaxTextoCorto, "Etiqueta de accesos"),
            (HomeAccesosTitulo, MaxTextoCorto, "Título de accesos"),
            (HomeAccesosDescripcion, MaxTextoLargo, "Descripción de accesos"),
            (HomeAvisosEtiqueta, MaxTextoCorto, "Etiqueta de avisos"),
            (HomeAvisosTitulo, MaxTextoCorto, "Título de avisos"),
            (HomeAvisosVacio, MaxTextoMedio, "Mensaje vacío de avisos"),
            (HomeTutorialesEtiqueta, MaxTextoCorto, "Etiqueta de tutoriales"),
            (HomeTutorialesTitulo, MaxTextoCorto, "Título de tutoriales"),
            (HomeTutorialesVerTodos, MaxTextoCorto, "Texto de ver todos"),
            (HomeTutorialesVacio, MaxTextoMedio, "Mensaje vacío de tutoriales"),
            (FooterTexto, MaxTextoCorto, "Texto principal del footer"),
            (FooterSubtexto, MaxTextoCorto, "Subtexto del footer"),
            (FooterRecursosTitulo, MaxTextoCorto, "Título de recursos"),
            (FooterSistemasTitulo, MaxTextoCorto, "Título de sistemas"),
            (FooterContactoTitulo, MaxTextoCorto, "Título de contacto"),
            (FooterTelefono, MaxTelefono, "Teléfono de contacto"),
            (FooterDireccion, MaxDireccion, "Dirección"),
            (FooterCopyright, MaxTextoMedio, "Copyright")
        };

        foreach (var (valor, maximo, nombre) in textos)
        {
            if (valor.Length > maximo)
                return $"{nombre} no debe superar {maximo} caracteres.";
            if (!TextoSeguro(valor))
                return $"{nombre} contiene caracteres no permitidos.";
        }

        if (FooterEmail.Length > MaxEmail)
            return $"El correo de contacto no debe superar {MaxEmail} caracteres.";
        if (!EmailValido(FooterEmail))
            return "El correo de contacto no tiene un formato válido.";

        return
            ValidarYNormalizarEnlaces(HeaderLinks, "menú superior") ??
            ValidarYNormalizarEnlaces(FooterRecursosLinks, "footer recursos") ??
            ValidarYNormalizarEnlaces(FooterSistemasLinks, "footer sistemas");
    }

    private static string? ValidarYNormalizarEnlaces(
        List<SitioEnlaceInput> enlaces,
        string grupoVisible)
    {
        for (var i = 0; i < enlaces.Count; i++)
        {
            var enlace = enlaces[i];
            enlace.Texto = NormalizarTexto(enlace.Texto);
            enlace.Url = NormalizarTexto(enlace.Url);
            enlace.Orden = enlace.Orden > 0 ? enlace.Orden : i + 1;

            if (string.IsNullOrWhiteSpace(enlace.Texto))
                return $"El enlace {i + 1} de {grupoVisible} necesita texto.";
            if (string.IsNullOrWhiteSpace(enlace.Url))
                return $"El enlace {enlace.Texto} necesita URL.";
            if (enlace.Texto.Length > MaxTextoCorto)
                return $"El enlace {enlace.Texto} no debe superar {MaxTextoCorto} caracteres.";
            if (enlace.Url.Length > MaxUrl)
                return $"La URL de {enlace.Texto} no debe superar {MaxUrl} caracteres.";
            if (!TextoSeguro(enlace.Texto) || ContieneControl(enlace.Url))
                return $"El enlace {enlace.Texto} contiene caracteres no permitidos.";
            if (!UrlValida(enlace.Url))
                return $"La URL de {enlace.Texto} debe ser una ruta interna /... o una URL http/https.";
        }

        return null;
    }

    private static IEnumerable<SitioEnlace> AModelo(
        IEnumerable<SitioEnlaceInput> enlaces,
        string grupo) =>
        enlaces.Select(e => new SitioEnlace
        {
            Id = e.Id,
            Grupo = grupo,
            Texto = e.Texto,
            Url = e.Url,
            Orden = e.Orden,
            Activo = e.Activo
        });

    private static string NormalizarTexto(string? valor) => valor?.Trim() ?? string.Empty;

    private static bool EmailValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true;

        try
        {
            var direccion = new MailAddress(email);
            return string.Equals(direccion.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TextoSeguro(string valor) =>
        !ContieneControl(valor) &&
        !valor.Contains('<') &&
        !valor.Contains('>');

    private static bool ContieneControl(string valor) =>
        valor.Any(c => char.IsControl(c) && c is not '\t');

    private static bool UrlValida(string url)
    {
        if (url.StartsWith("/", StringComparison.Ordinal) &&
            !url.StartsWith("//", StringComparison.Ordinal) &&
            !url.Contains('\\'))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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

public class SitioEnlaceInput
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public static SitioEnlaceInput FromModel(SitioEnlace enlace) => new()
    {
        Id = enlace.Id,
        Texto = enlace.Texto,
        Url = enlace.Url,
        Orden = enlace.Orden,
        Activo = enlace.Activo
    };
}
