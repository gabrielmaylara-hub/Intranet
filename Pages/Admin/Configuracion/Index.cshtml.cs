using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Intranet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Mail;

namespace Intranet.Pages.Admin.Configuracion;

public class IndexModel : PageModel
{
    // Estos grupos deben coincidir con sitio_enlaces.grupo y con el render del
    // header/footer publico. No usar texto visible como clave persistente.
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

    [BindProperty] public string PaginaFormatosTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaFormatosDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaManualesTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaManualesDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaDgieTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaDgieDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaIdentidadTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaIdentidadDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaCapacitacionTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaCapacitacionDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaCapacitacionInternosTitulo { get; set; } = string.Empty;
    [BindProperty] public bool PaginaCapacitacionExternoActivo { get; set; } = true;
    [BindProperty] public string PaginaCapacitacionExternoTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaCapacitacionExternoDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaCapacitacionExternoBotonTexto { get; set; } = string.Empty;
    [BindProperty] public string PaginaCapacitacionExternoBotonUrl { get; set; } = string.Empty;
    [BindProperty] public string PaginaTutorialesTitulo { get; set; } = string.Empty;
    [BindProperty] public string PaginaTutorialesDescripcion { get; set; } = string.Empty;
    [BindProperty] public string PaginaTutorialesVacio { get; set; } = string.Empty;
    [BindProperty] public string PaginaTutorialesVideoNoSoportado { get; set; } = string.Empty;
    [BindProperty] public string PaginaTutorialesVideoProximamente { get; set; } = string.Empty;

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
    [BindProperty] public string TabActiva { get; set; } = "identidad";

    [BindProperty] public List<SitioEnlaceInput> HeaderLinks { get; set; } = [];
    [BindProperty] public List<SitioEnlaceInput> FooterRecursosLinks { get; set; } = [];
    [BindProperty] public List<SitioEnlaceInput> FooterSistemasLinks { get; set; } = [];

    [TempData] public string? Mensaje { get; set; }
    [TempData] public bool EsError { get; set; }
    public string LogoActual { get; private set; } = string.Empty;

    public async Task OnGetAsync() => await CargarFormularioAsync();

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        // Un solo handler guarda todas las pestanas para preservar el modelo
        // completo. La UI lo presenta por bloques, pero la validacion es central.
        var errorValidacion = ValidarYNormalizarConfiguracion();
        if (errorValidacion is not null)
        {
            EsError = true;
            Mensaje = errorValidacion;
            await CargarLogoActualAsync();
            return Page();
        }

        // Las claves son el contrato con configuracion_sitio y las vistas
        // publicas. Cambiarlas exige migracion/seed y ajuste del render.
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
            ["pagina_formatos_titulo"] = PaginaFormatosTitulo,
            ["pagina_formatos_descripcion"] = PaginaFormatosDescripcion,
            ["pagina_manuales_titulo"] = PaginaManualesTitulo,
            ["pagina_manuales_descripcion"] = PaginaManualesDescripcion,
            ["pagina_dgie_titulo"] = PaginaDgieTitulo,
            ["pagina_dgie_descripcion"] = PaginaDgieDescripcion,
            ["pagina_identidad_titulo"] = PaginaIdentidadTitulo,
            ["pagina_identidad_descripcion"] = PaginaIdentidadDescripcion,
            ["pagina_capacitacion_titulo"] = PaginaCapacitacionTitulo,
            ["pagina_capacitacion_descripcion"] = PaginaCapacitacionDescripcion,
            ["pagina_capacitacion_internos_titulo"] = PaginaCapacitacionInternosTitulo,
            ["pagina_capacitacion_externo_activo"] = PaginaCapacitacionExternoActivo ? "1" : "0",
            ["pagina_capacitacion_externo_titulo"] = PaginaCapacitacionExternoTitulo,
            ["pagina_capacitacion_externo_descripcion"] = PaginaCapacitacionExternoDescripcion,
            ["pagina_capacitacion_externo_boton_texto"] = PaginaCapacitacionExternoBotonTexto,
            ["pagina_capacitacion_externo_boton_url"] = PaginaCapacitacionExternoBotonUrl,
            ["pagina_tutoriales_titulo"] = PaginaTutorialesTitulo,
            ["pagina_tutoriales_descripcion"] = PaginaTutorialesDescripcion,
            ["pagina_tutoriales_vacio"] = PaginaTutorialesVacio,
            ["pagina_tutoriales_video_no_soportado"] = PaginaTutorialesVideoNoSoportado,
            ["pagina_tutoriales_video_proximamente"] = PaginaTutorialesVideoProximamente,
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
        // POST-Redirect-GET: evita reenvio al actualizar y conserva la pestana.
        return RedirectToPage(null, null, null, FragmentoTabActiva());
    }

    public async Task<IActionResult> OnPostSubirLogoAsync()
    {
        if (Logo is null || Logo.Length == 0)
        {
            EsError = true;
            Mensaje = "Selecciona un archivo de imagen.";
            TabActiva = "identidad";
            await CargarFormularioAsync();
            return Page();
        }

        var extensionesPermitidas = new[] { ".png", ".svg", ".jpg", ".jpeg" };
        var ext = Path.GetExtension(Logo.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(ext))
        {
            EsError = true;
            Mensaje = "Solo se permiten imágenes PNG, SVG o JPG.";
            TabActiva = "identidad";
            await CargarFormularioAsync();
            return Page();
        }

        var configActual = await _configRepo.ObtenerTodosAsync();
        var logoAnterior = configActual.GetValueOrDefault("logo_path", "");
        string rutaRelativa;

        try
        {
            // Reusa ArchivoService para validar extension, firma, tamano y ruta.
            // No guardar logos directamente en wwwroot ni confiar en FileName.
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
            TabActiva = "identidad";
            await CargarFormularioAsync();
            return Page();
        }

        await _configRepo.GuardarAsync("logo_path", rutaRelativa);

        Mensaje = "Logo actualizado correctamente.";
        EsError = false;
        TabActiva = "identidad";
        return RedirectToPage(null, null, null, "config-identidad");
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

        PaginaFormatosTitulo = config.GetValueOrDefault("pagina_formatos_titulo", "Formatos de Contraloría");
        PaginaFormatosDescripcion = config.GetValueOrDefault(
            "pagina_formatos_descripcion",
            "Formatos oficiales de la Contraloría Interna de la Fiscalía General del Estado de Tabasco. Descarga el formato que necesitas en formato PDF.");
        PaginaManualesTitulo = config.GetValueOrDefault("pagina_manuales_titulo", "Manuales de Capacitación Justicia.NET");
        PaginaManualesDescripcion = config.GetValueOrDefault(
            "pagina_manuales_descripcion",
            "Manuales de usuario y capacitación del sistema Justicia.NET. Consulta o descarga el manual que necesitas.");
        PaginaDgieTitulo = config.GetValueOrDefault("pagina_dgie_titulo", "Solicitud de Anuencia Técnica DGIE");
        PaginaDgieDescripcion = config.GetValueOrDefault(
            "pagina_dgie_descripcion",
            "Formatos y documentos para tramitar una solicitud de anuencia técnica ante la Dirección General de Informática y Estadística.");
        PaginaIdentidadTitulo = config.GetValueOrDefault("pagina_identidad_titulo", "Kit de Identidad Gráfica FGET");
        PaginaIdentidadDescripcion = config.GetValueOrDefault(
            "pagina_identidad_descripcion",
            "Recursos gráficos oficiales de la Fiscalía General del Estado de Tabasco: logotipos, paleta de colores, tipografías y lineamientos de uso.");
        PaginaCapacitacionTitulo = config.GetValueOrDefault("pagina_capacitacion_titulo", "Oferta Académica");
        PaginaCapacitacionDescripcion = config.GetValueOrDefault(
            "pagina_capacitacion_descripcion",
            "Cursos de capacitación y formación profesional disponibles para el personal de la Fiscalía General del Estado de Tabasco.");
        PaginaCapacitacionInternosTitulo = config.GetValueOrDefault("pagina_capacitacion_internos_titulo", "Cursos y Materiales Internos");
        PaginaCapacitacionExternoActivo = EsActivo(config.GetValueOrDefault("pagina_capacitacion_externo_activo", "1"));
        PaginaCapacitacionExternoTitulo = config.GetValueOrDefault("pagina_capacitacion_externo_titulo", "Sistema Integral de Gestión Académica");
        PaginaCapacitacionExternoDescripcion = config.GetValueOrDefault(
            "pagina_capacitacion_externo_descripcion",
            "Accede al sistema SIGAACEJ del Tribunal Superior de Justicia del Estado de Tabasco para consultar la oferta académica institucional compartida.");
        PaginaCapacitacionExternoBotonTexto = config.GetValueOrDefault("pagina_capacitacion_externo_boton_texto", "Acceder a SIGAACEJ ↗");
        PaginaCapacitacionExternoBotonUrl = config.GetValueOrDefault("pagina_capacitacion_externo_boton_url", "https://sigaacej.tsj-tabasco.gob.mx/");
        PaginaTutorialesTitulo = config.GetValueOrDefault("pagina_tutoriales_titulo", "Tutoriales Institucionales");
        PaginaTutorialesDescripcion = config.GetValueOrDefault(
            "pagina_tutoriales_descripcion",
            "Videos de capacitación y guías de uso de los sistemas institucionales de la Fiscalía General del Estado de Tabasco.");
        PaginaTutorialesVacio = config.GetValueOrDefault("pagina_tutoriales_vacio", "No hay tutoriales publicados en este momento.");
        PaginaTutorialesVideoNoSoportado = config.GetValueOrDefault("pagina_tutoriales_video_no_soportado", "Tu navegador no soporta reproducción de video.");
        PaginaTutorialesVideoProximamente = config.GetValueOrDefault("pagina_tutoriales_video_proximamente", "Video próximamente");

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
        // Validacion backend: el navegador ayuda, pero no es la frontera de
        // seguridad. Todo lo visible en publico pasa por este saneamiento.
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
        PaginaFormatosTitulo = NormalizarTexto(PaginaFormatosTitulo);
        PaginaFormatosDescripcion = NormalizarTexto(PaginaFormatosDescripcion);
        PaginaManualesTitulo = NormalizarTexto(PaginaManualesTitulo);
        PaginaManualesDescripcion = NormalizarTexto(PaginaManualesDescripcion);
        PaginaDgieTitulo = NormalizarTexto(PaginaDgieTitulo);
        PaginaDgieDescripcion = NormalizarTexto(PaginaDgieDescripcion);
        PaginaIdentidadTitulo = NormalizarTexto(PaginaIdentidadTitulo);
        PaginaIdentidadDescripcion = NormalizarTexto(PaginaIdentidadDescripcion);
        PaginaCapacitacionTitulo = NormalizarTexto(PaginaCapacitacionTitulo);
        PaginaCapacitacionDescripcion = NormalizarTexto(PaginaCapacitacionDescripcion);
        PaginaCapacitacionInternosTitulo = NormalizarTexto(PaginaCapacitacionInternosTitulo);
        PaginaCapacitacionExternoTitulo = NormalizarTexto(PaginaCapacitacionExternoTitulo);
        PaginaCapacitacionExternoDescripcion = NormalizarTexto(PaginaCapacitacionExternoDescripcion);
        PaginaCapacitacionExternoBotonTexto = NormalizarTexto(PaginaCapacitacionExternoBotonTexto);
        PaginaCapacitacionExternoBotonUrl = NormalizarTexto(PaginaCapacitacionExternoBotonUrl);
        PaginaTutorialesTitulo = NormalizarTexto(PaginaTutorialesTitulo);
        PaginaTutorialesDescripcion = NormalizarTexto(PaginaTutorialesDescripcion);
        PaginaTutorialesVacio = NormalizarTexto(PaginaTutorialesVacio);
        PaginaTutorialesVideoNoSoportado = NormalizarTexto(PaginaTutorialesVideoNoSoportado);
        PaginaTutorialesVideoProximamente = NormalizarTexto(PaginaTutorialesVideoProximamente);
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
            (PaginaFormatosTitulo, MaxTextoCorto, "Título de Formatos"),
            (PaginaFormatosDescripcion, MaxTextoLargo, "Descripción de Formatos"),
            (PaginaManualesTitulo, MaxTextoCorto, "Título de Manuales"),
            (PaginaManualesDescripcion, MaxTextoLargo, "Descripción de Manuales"),
            (PaginaDgieTitulo, MaxTextoCorto, "Título de DGIE"),
            (PaginaDgieDescripcion, MaxTextoLargo, "Descripción de DGIE"),
            (PaginaIdentidadTitulo, MaxTextoCorto, "Título de Identidad"),
            (PaginaIdentidadDescripcion, MaxTextoLargo, "Descripción de Identidad"),
            (PaginaCapacitacionTitulo, MaxTextoCorto, "Título de Capacitación"),
            (PaginaCapacitacionDescripcion, MaxTextoLargo, "Descripción de Capacitación"),
            (PaginaCapacitacionInternosTitulo, MaxTextoCorto, "Título de materiales internos"),
            (PaginaCapacitacionExternoTitulo, MaxTextoCorto, "Título del enlace académico"),
            (PaginaCapacitacionExternoDescripcion, MaxTextoLargo, "Descripción del enlace académico"),
            (PaginaCapacitacionExternoBotonTexto, MaxTextoCorto, "Texto del botón académico"),
            (PaginaTutorialesTitulo, MaxTextoCorto, "Título de página Tutoriales"),
            (PaginaTutorialesDescripcion, MaxTextoLargo, "Descripción de página Tutoriales"),
            (PaginaTutorialesVacio, MaxTextoMedio, "Mensaje vacío de página Tutoriales"),
            (PaginaTutorialesVideoNoSoportado, MaxTextoMedio, "Mensaje de video no soportado"),
            (PaginaTutorialesVideoProximamente, MaxTextoCorto, "Mensaje de video próximamente"),
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
        if (PaginaCapacitacionExternoBotonUrl.Length > MaxUrl)
            return $"La URL del botón académico no debe superar {MaxUrl} caracteres.";
        if (ContieneControl(PaginaCapacitacionExternoBotonUrl))
            return "La URL del botón académico contiene caracteres no permitidos.";
        if (PaginaCapacitacionExternoActivo && string.IsNullOrWhiteSpace(PaginaCapacitacionExternoBotonUrl))
            return "El enlace académico activo necesita URL.";
        if (!string.IsNullOrWhiteSpace(PaginaCapacitacionExternoBotonUrl) &&
            !UrlValida(PaginaCapacitacionExternoBotonUrl))
        {
            return "La URL del enlace académico debe ser una ruta interna /... o una URL http/https.";
        }

        return
            ValidarYNormalizarEnlaces(HeaderLinks, "menú superior") ??
            ValidarYNormalizarEnlaces(FooterRecursosLinks, "footer recursos") ??
            ValidarYNormalizarEnlaces(FooterSistemasLinks, "footer sistemas");
    }

    private static string? ValidarYNormalizarEnlaces(
        List<SitioEnlaceInput> enlaces,
        string grupoVisible)
    {
        // URL permitida: ruta interna /... o http/https. Evita javascript:,
        // rutas UNC y controles que puedan romper el render publico.
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

    private static bool EsActivo(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ||
        valor.Equals("1", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("true", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("activo", StringComparison.OrdinalIgnoreCase);

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

    private string FragmentoTabActiva() =>
        (TabActiva ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "home" => "config-home",
            "menu" => "config-menu",
            "paginas" => "config-paginas",
            "footer" => "config-footer",
            _ => "config-identidad"
        };

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
