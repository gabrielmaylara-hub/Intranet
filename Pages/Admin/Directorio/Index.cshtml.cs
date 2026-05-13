using System.Text;
using System.Text.Json;
using Intranet.Models;
using Intranet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace Intranet.Pages.Admin.Directorio;

public class IndexModel : PageModel
{
    private const int MaxFilasImportacion = 1000;
    private const long MaxArchivoCsvBytes = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDirectorioRepository _directorioRepo;

    public IndexModel(IDirectorioRepository directorioRepo) =>
        _directorioRepo = directorioRepo;

    public IEnumerable<DirectorioEntrada> Entradas { get; private set; } = [];
    public IEnumerable<DirectorioArea> Areas { get; private set; } = [];
    public ImportacionDirectorioPreview? Importacion { get; private set; }

    [BindProperty] public int    Id        { get; set; }
    [BindProperty] public string Area      { get; set; } = string.Empty;
    [BindProperty] public string Nombre    { get; set; } = string.Empty;
    [BindProperty] public string Extension { get; set; } = string.Empty;
    [BindProperty] public int    Orden     { get; set; }
    [BindProperty] public bool   Activo    { get; set; } = true;

    [BindProperty] public int     AreaId        { get; set; }
    [BindProperty] public string  AreaNombre    { get; set; } = string.Empty;
    [BindProperty] public string? AreaTitular   { get; set; }
    [BindProperty] public string? AreaUbicacion { get; set; }
    [BindProperty] public string? AreaCorreo    { get; set; }
    [BindProperty] public int     AreaOrden     { get; set; }
    [BindProperty] public bool    AreaActivo    { get; set; } = true;

    [BindProperty] public IFormFile? ArchivoCsv { get; set; }
    [BindProperty] public string? ImportacionJson { get; set; }

    public string? Mensaje { get; private set; }
    public bool    EsError { get; private set; }
    public bool    AnclarCargaCsv { get; private set; }

    public async Task OnGetAsync() => await CargarListasAsync();

    private async Task CargarListasAsync()
    {
        Entradas = await _directorioRepo.ObtenerTodosAsync();
        Areas = await _directorioRepo.ObtenerAreasAsync();
    }

    public IActionResult OnGetPlantilla()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Area,Titular,Ubicacion,Correo,Nombre,Extension,Orden,Activo");

        var contenido = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();

        return File(
            contenido,
            "text/csv; charset=utf-8",
            "plantilla_directorio.csv");
    }

    public async Task<IActionResult> OnPostPrevisualizarImportacionAsync()
    {
        if (ArchivoCsv is null || ArchivoCsv.Length == 0)
        {
            await MostrarErrorImportacionAsync("Selecciona un archivo CSV para importar.");
            return Page();
        }

        if (ArchivoCsv.Length > MaxArchivoCsvBytes)
        {
            await MostrarErrorImportacionAsync("El archivo CSV supera el tamano permitido de 2 MB.");
            return Page();
        }

        if (!Path.GetExtension(ArchivoCsv.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            await MostrarErrorImportacionAsync("El archivo debe tener extension .csv.");
            return Page();
        }

        var filas = await LeerCsvAsync(ArchivoCsv);
        Importacion = await PrepararImportacionAsync(filas);
        ImportacionJson = JsonSerializer.Serialize(filas, JsonOptions);
        AnclarCargaCsv = true;
        await CargarListasAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmarImportacionAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportacionJson))
        {
            await MostrarErrorImportacionAsync("No hay una importacion pendiente para confirmar.");
            return Page();
        }

        List<DirectorioCsvFila>? filas;
        try
        {
            filas = JsonSerializer.Deserialize<List<DirectorioCsvFila>>(ImportacionJson, JsonOptions);
        }
        catch (JsonException)
        {
            await MostrarErrorImportacionAsync("La previsualizacion de importacion no es valida.");
            return Page();
        }

        if (filas is null || filas.Count == 0)
        {
            await MostrarErrorImportacionAsync("No hay filas validas para importar.");
            return Page();
        }

        Importacion = await PrepararImportacionAsync(filas);
        ImportacionJson = JsonSerializer.Serialize(filas, JsonOptions);

        if (Importacion.Filas.Any(f => !f.PuedeAplicarse && f.Estado != EstadoImportacion.SinCambios))
        {
            EsError = true;
            Mensaje = "Corrige las filas con error o conflicto antes de confirmar.";
            AnclarCargaCsv = true;
            await CargarListasAsync();
            return Page();
        }

        var aplicadas = 0;
        var existentes = (await _directorioRepo.ObtenerTodosAsync())
            .ToList();

        try
        {
            foreach (var fila in Importacion.Filas)
            {
                await _directorioRepo.ActualizarAreaDesdeImportacionAsync(
                    fila.Area,
                    fila.Titular,
                    fila.Ubicacion,
                    fila.Correo);

                if (fila.Estado == EstadoImportacion.ActualizarDatosArea)
                {
                    aplicadas++;
                    continue;
                }

                if (fila.Estado == EstadoImportacion.Nuevo)
                {
                    await _directorioRepo.InsertarAsync(fila.ToEntrada());
                    aplicadas++;
                    continue;
                }

                if (fila.Estado is EstadoImportacion.ActualizarExtension
                    or EstadoImportacion.ActualizarOrdenEstado)
                {
                    var existente = existentes.FirstOrDefault(e =>
                        Coincide(e.Area, fila.Area) &&
                        Coincide(e.Nombre, fila.Nombre));

                    if (existente is null)
                        continue;

                    existente.Extension = fila.Extension;
                    existente.Orden = fila.Orden;
                    existente.Activo = fila.Activo;
                    await _directorioRepo.ActualizarAsync(existente);
                    aplicadas++;
                }
            }
        }
        catch (MySqlException ex) when (EsDuplicadoDirectorio(ex))
        {
            EsError = true;
            Mensaje = "No se pudo aplicar la importacion porque generaria un duplicado en el Directorio.";
            AnclarCargaCsv = true;
            await CargarListasAsync();
            return Page();
        }

        Mensaje = $"Importacion aplicada. Cambios guardados: {aplicadas}.";
        Importacion = null;
        ImportacionJson = null;
        return RedirectToPage(null, null, null, "carga-csv");
    }

    public IActionResult OnPostCancelarImportacion()
    {
        Importacion = null;
        ImportacionJson = null;
        return RedirectToPage(null, null, null, "carga-csv");
    }

    public async Task<IActionResult> OnPostReordenarExtensionesAsync(
        [FromBody] ReordenarExtensionesRequest? solicitud)
    {
        if (solicitud is null ||
            solicitud.AreaId <= 0 ||
            solicitud.Ids.Count == 0)
        {
            return BadRequest(new { mensaje = "La solicitud de reordenamiento no es valida." });
        }

        try
        {
            await _directorioRepo.ReordenarExtensionesAsync(
                solicitud.AreaId,
                solicitud.Ids);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (MySqlException ex) when (EsDuplicadoOrdenDirectorio(ex))
        {
            return BadRequest(new { mensaje = "Ya existe una extensión con ese orden interno dentro del área seleccionada." });
        }

        return new JsonResult(new { ok = true });
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Area) ||
            string.IsNullOrWhiteSpace(Nombre) ||
            string.IsNullOrWhiteSpace(Extension))
        {
            EsError = true;
            Mensaje = "Area, nombre y extension son obligatorios.";
            await OnGetAsync();
            return Page();
        }

        var areaSeleccionada = await _directorioRepo.ObtenerAreaPorNombreAsync(Area.Trim());
        if (areaSeleccionada is null)
        {
            EsError = true;
            Mensaje = "Selecciona un area existente antes de guardar la extension.";
            await CargarListasAsync();
            return Page();
        }

        var nombreNormalizado = Nombre.Trim();
        var extensionNormalizada = Extension.Trim();
        var entradas = (await _directorioRepo.ObtenerTodosAsync()).ToList();

        if (entradas.Any(e =>
            e.Id != Id &&
            Coincide(e.Area, areaSeleccionada.Nombre) &&
            Coincide(e.Nombre, nombreNormalizado)))
        {
            EsError = true;
            Mensaje = "Ya existe una extension para esa unidad en el area seleccionada.";
            await CargarListasAsync();
            return Page();
        }

        if (entradas.Any(e =>
            e.Id != Id &&
            Coincide(e.Area, areaSeleccionada.Nombre) &&
            Coincide(e.Extension, extensionNormalizada)))
        {
            EsError = true;
            Mensaje = "Ya existe ese numero de extension en el area seleccionada.";
            await CargarListasAsync();
            return Page();
        }

        if (entradas.Any(e =>
            e.Id != Id &&
            Coincide(e.Area, areaSeleccionada.Nombre) &&
            e.Orden == Orden))
        {
            EsError = true;
            Mensaje = "Ya existe una extensión con ese orden interno dentro del área seleccionada.";
            await CargarListasAsync();
            return Page();
        }

        var entrada = new DirectorioEntrada
        {
            Id = Id,
            Area = areaSeleccionada.Nombre,
            Nombre = nombreNormalizado,
            Extension = extensionNormalizada,
            Orden = Orden,
            Activo = Activo
        };

        try
        {
            if (Id > 0)
                await _directorioRepo.ActualizarAsync(entrada);
            else
                await _directorioRepo.InsertarAsync(entrada);
        }
        catch (MySqlException ex) when (EsDuplicadoDirectorio(ex))
        {
            EsError = true;
            Mensaje = EsDuplicadoOrdenDirectorio(ex)
                ? "Ya existe una extensión con ese orden interno dentro del área seleccionada."
                : "Ya existe una unidad o extension con esos datos en el area seleccionada.";
            await CargarListasAsync();
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGuardarAreaAsync()
    {
        if (AreaId == 0)
        {
            if (string.IsNullOrWhiteSpace(AreaNombre))
            {
                EsError = true;
                Mensaje = "El nombre del area es obligatorio.";
                await CargarListasAsync();
                return Page();
            }

            if (!CorreoValido(AreaCorreo))
            {
                EsError = true;
                Mensaje = "El correo del area no tiene un formato valido.";
                await CargarListasAsync();
                return Page();
            }

            var nombreNormalizado = AreaNombre.Trim();
            var existente = await _directorioRepo.ObtenerAreaPorNombreAsync(nombreNormalizado);
            if (existente is not null)
            {
                EsError = true;
                Mensaje = "El area ya existe. Usa Editar para actualizar sus datos.";
                await CargarListasAsync();
                return Page();
            }

            var areas = (await _directorioRepo.ObtenerAreasAsync()).ToList();
            var siguienteOrden = areas.Any()
                ? areas.Max(a => a.Orden) + 1
                : 1;
            var ordenArea = AreaOrden > 0 ? AreaOrden : siguienteOrden;

            if (areas.Any(a => a.Orden == ordenArea))
            {
                EsError = true;
                Mensaje = "Ya existe un area con ese orden. Usa un numero disponible.";
                await CargarListasAsync();
                return Page();
            }

            await _directorioRepo.InsertarAreaAsync(new DirectorioArea
            {
                Nombre = nombreNormalizado,
                Titular = AreaTitular?.Trim(),
                Ubicacion = AreaUbicacion?.Trim(),
                Correo = AreaCorreo?.Trim(),
                Orden = ordenArea,
                Activo = AreaActivo
            });

            return RedirectToPage();
        }

        var area = await _directorioRepo.ObtenerAreaPorIdAsync(AreaId);
        if (area is null)
        {
            EsError = true;
            Mensaje = "No se encontro el area seleccionada.";
            await CargarListasAsync();
            return Page();
        }

        if (!CorreoValido(AreaCorreo))
        {
            EsError = true;
            Mensaje = "El correo del area no tiene un formato valido.";
            await CargarListasAsync();
            return Page();
        }

        area.Titular = AreaTitular?.Trim();
        area.Ubicacion = AreaUbicacion?.Trim();
        area.Correo = AreaCorreo?.Trim();
        area.Orden = AreaOrden > 0 ? AreaOrden : area.Orden;
        area.Activo = AreaActivo;

        var areasExistentes = (await _directorioRepo.ObtenerAreasAsync()).ToList();
        if (areasExistentes.Any(a => a.Id != area.Id && a.Orden == area.Orden))
        {
            EsError = true;
            Mensaje = "Ya existe un area con ese orden. Usa un numero disponible.";
            await CargarListasAsync();
            return Page();
        }

        await _directorioRepo.ActualizarAreaAsync(area);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        await _directorioRepo.EliminarAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var entrada = await _directorioRepo.ObtenerPorIdAsync(id);
        if (entrada is not null)
            await _directorioRepo.CambiarEstadoAsync(id, !entrada.Activo);

        return RedirectToPage();
    }

    private async Task MostrarErrorImportacionAsync(string mensaje)
    {
        EsError = true;
        Mensaje = mensaje;
        AnclarCargaCsv = true;
        await CargarListasAsync();
    }

    private static async Task<List<DirectorioCsvFila>> LeerCsvAsync(IFormFile archivo)
    {
        await using var stream = archivo.OpenReadStream();
        using var lector = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var encabezado = await lector.ReadLineAsync();
        if (encabezado is null)
            return [];

        var columnas = ParsearLineaCsv(encabezado)
            .Select(NormalizarEncabezado)
            .ToList();

        var mapa = columnas
            .Select((nombre, indice) => new { nombre, indice })
            .ToDictionary(x => x.nombre, x => x.indice, StringComparer.OrdinalIgnoreCase);

        var filas = new List<DirectorioCsvFila>();
        var lineaNumero = 1;

        while (filas.Count < MaxFilasImportacion)
        {
            lineaNumero++;
            var linea = await lector.ReadLineAsync();
            if (linea is null)
                break;

            if (string.IsNullOrWhiteSpace(linea))
                continue;

            var valores = ParsearLineaCsv(linea);
            string Valor(string columna) =>
                mapa.TryGetValue(columna, out var i) && i < valores.Count
                    ? valores[i].Trim()
                    : string.Empty;

            filas.Add(new DirectorioCsvFila
            {
                Linea = lineaNumero,
                Area = Valor("area"),
                Titular = Valor("titular"),
                Ubicacion = Valor("ubicacion"),
                Correo = Valor("correo"),
                Nombre = Valor("nombre"),
                Extension = Valor("extension"),
                OrdenTexto = Valor("orden"),
                ActivoTexto = Valor("activo")
            });
        }

        return filas;
    }

    private async Task<ImportacionDirectorioPreview> PrepararImportacionAsync(
        List<DirectorioCsvFila> filas)
    {
        var existentes = (await _directorioRepo.ObtenerTodosAsync()).ToList();
        var areas = (await _directorioRepo.ObtenerAreasAsync()).ToList();
        var clavesExactas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var clavesAreaNombre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var clavesAreaExtension = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var clavesAreaOrden = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var resultado = new List<DirectorioImportacionFila>();

        foreach (var fila in filas)
        {
            var vista = DirectorioImportacionFila.FromCsv(fila);
            ValidarFila(vista);

            if (vista.Errores.Count == 0)
                ValidarDuplicadosArchivo(
                    vista,
                    clavesExactas,
                    clavesAreaNombre,
                    clavesAreaExtension,
                    clavesAreaOrden);

            if (vista.Errores.Count == 0)
                ClasificarFila(vista, existentes, areas);

            resultado.Add(vista);
        }

        return new ImportacionDirectorioPreview(resultado);
    }

    private static void ValidarDuplicadosArchivo(
        DirectorioImportacionFila fila,
        Dictionary<string, int> clavesExactas,
        Dictionary<string, int> clavesAreaNombre,
        Dictionary<string, int> clavesAreaExtension,
        Dictionary<string, int> clavesAreaOrden)
    {
        var claveExacta = Clave(fila.Area, fila.Nombre, fila.Extension);
        if (clavesExactas.TryGetValue(claveExacta, out var lineaExacta))
        {
            fila.Errores.Add($"Duplicado exacto dentro del archivo con la fila {lineaExacta}.");
            return;
        }

        var claveNombre = Clave(fila.Area, fila.Nombre);
        if (clavesAreaNombre.TryGetValue(claveNombre, out var lineaNombre))
            fila.Errores.Add($"El area y nombre ya aparecen en la fila {lineaNombre}.");

        if (!string.IsNullOrWhiteSpace(fila.Extension))
        {
            var claveExtension = Clave(fila.Area, fila.Extension);
            if (clavesAreaExtension.TryGetValue(claveExtension, out var lineaExtension))
                fila.Errores.Add($"El area y extension ya aparecen en la fila {lineaExtension}.");
        }

        var claveOrden = Clave(fila.Area, fila.Orden.ToString());
        if (clavesAreaOrden.TryGetValue(claveOrden, out var lineaOrden))
            fila.Errores.Add($"El area y orden interno ya aparecen en la fila {lineaOrden}.");

        clavesExactas[claveExacta] = fila.Linea;
        clavesAreaNombre[claveNombre] = fila.Linea;
        if (!string.IsNullOrWhiteSpace(fila.Extension))
            clavesAreaExtension[Clave(fila.Area, fila.Extension)] = fila.Linea;
        clavesAreaOrden[claveOrden] = fila.Linea;
    }

    private static void ValidarFila(DirectorioImportacionFila fila)
    {
        if (string.IsNullOrWhiteSpace(fila.Area))
            fila.Errores.Add("Area obligatoria.");
        if (string.IsNullOrWhiteSpace(fila.Nombre))
            fila.Errores.Add("Nombre obligatorio.");
        if (string.IsNullOrWhiteSpace(fila.Extension))
            fila.Errores.Add("Extension obligatoria.");

        ValidarLongitud(fila.Area, 180, "Area", fila);
        ValidarLongitud(fila.Nombre, 180, "Nombre", fila);
        ValidarLongitud(fila.Extension, 30, "Extension", fila);
        ValidarLongitud(fila.Titular, 180, "Titular", fila);
        ValidarLongitud(fila.Ubicacion, 250, "Ubicacion", fila);
        ValidarLongitud(fila.Correo, 180, "Correo", fila);

        if (!CorreoValido(fila.Correo))
            fila.Errores.Add("Correo invalido.");

        if (!string.IsNullOrWhiteSpace(fila.OrdenTexto) &&
            !int.TryParse(fila.OrdenTexto, out _))
        {
            fila.Errores.Add("Orden invalido.");
        }

        if (!string.IsNullOrWhiteSpace(fila.ActivoTexto) &&
            !TryParseActivo(fila.ActivoTexto, out _))
        {
            fila.Errores.Add("Activo debe ser 1, 0, true, false, si o no.");
        }

        fila.Orden = int.TryParse(fila.OrdenTexto, out var orden)
            ? orden
            : fila.Linea;
        fila.Activo = string.IsNullOrWhiteSpace(fila.ActivoTexto) ||
            TryParseActivo(fila.ActivoTexto, out var activo) && activo;
    }

    private static void ValidarLongitud(
        string? valor,
        int maximo,
        string campo,
        DirectorioImportacionFila fila)
    {
        if (!string.IsNullOrWhiteSpace(valor) && valor.Length > maximo)
            fila.Errores.Add($"{campo} supera {maximo} caracteres.");
    }

    private static void ClasificarFila(
        DirectorioImportacionFila fila,
        List<DirectorioEntrada> existentes,
        List<DirectorioArea> areas)
    {
        var exacto = existentes.FirstOrDefault(e =>
            Coincide(e.Area, fila.Area) &&
            Coincide(e.Nombre, fila.Nombre) &&
            Coincide(e.Extension, fila.Extension));

        var mismaAreaNombre = existentes.FirstOrDefault(e =>
            Coincide(e.Area, fila.Area) &&
            Coincide(e.Nombre, fila.Nombre));

        var mismaAreaExtension = existentes.FirstOrDefault(e =>
            Coincide(e.Area, fila.Area) &&
            Coincide(e.Extension, fila.Extension));

        var mismaAreaOrden = existentes.FirstOrDefault(e =>
            Coincide(e.Area, fila.Area) &&
            e.Orden == fila.Orden);

        var area = areas.FirstOrDefault(a => Coincide(a.Nombre, fila.Area));
        if (area is null)
        {
            fila.Estado = EstadoImportacion.Conflicto;
            fila.ObservacionArea = "El area debe existir previamente en Datos por area.";
            return;
        }

        fila.ObservacionArea = MetadataCambia(area, fila)
            ? "Actualizara datos del area si vienen informados."
            : string.Empty;

        if (exacto is not null)
        {
            if (exacto.Orden != fila.Orden || exacto.Activo != fila.Activo)
            {
                fila.Estado = EstadoImportacion.ActualizarOrdenEstado;
                fila.Observacion = "Coincide, pero cambiara orden o estado.";
                return;
            }

            fila.Estado = MetadataCambia(area, fila)
                ? EstadoImportacion.ActualizarDatosArea
                : EstadoImportacion.SinCambios;
            fila.Observacion = fila.Estado == EstadoImportacion.SinCambios
                ? "Ya existe exactamente igual."
                : "Solo se actualizaran datos del area.";
            return;
        }

        var idRegistroBase = exacto?.Id ?? mismaAreaNombre?.Id ?? 0;
        if (mismaAreaOrden is not null && mismaAreaOrden.Id != idRegistroBase)
        {
            fila.Estado = EstadoImportacion.Conflicto;
            fila.Observacion = $"El orden interno ya existe para {mismaAreaOrden.Nombre}. Revisar antes de importar.";
            return;
        }

        if (mismaAreaExtension is not null &&
            (mismaAreaNombre is null || mismaAreaExtension.Id != mismaAreaNombre.Id))
        {
            fila.Estado = EstadoImportacion.Conflicto;
            fila.Observacion = $"La extension ya existe para {mismaAreaExtension.Nombre}. Revisar antes de importar.";
            return;
        }

        if (mismaAreaNombre is not null)
        {
            fila.Estado = EstadoImportacion.ActualizarExtension;
            fila.Observacion = $"Cambiara extension {mismaAreaNombre.Extension} por {fila.Extension}.";
            return;
        }

        fila.Estado = EstadoImportacion.Nuevo;
        fila.Observacion = "Se agregara como extension nueva.";
    }

    private static bool MetadataCambia(DirectorioArea? area, DirectorioImportacionFila fila)
    {
        if (area is null)
            return !string.IsNullOrWhiteSpace(fila.Titular) ||
                   !string.IsNullOrWhiteSpace(fila.Ubicacion) ||
                   !string.IsNullOrWhiteSpace(fila.Correo);

        return CampoInformadoDistinto(fila.Titular, area.Titular) ||
               CampoInformadoDistinto(fila.Ubicacion, area.Ubicacion) ||
               CampoInformadoDistinto(fila.Correo, area.Correo);
    }

    private static bool CampoInformadoDistinto(string? importado, string? existente) =>
        !string.IsNullOrWhiteSpace(importado) &&
        !string.Equals(importado.Trim(), existente?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool CorreoValido(string? correo) =>
        string.IsNullOrWhiteSpace(correo) ||
        correo.Contains('@', StringComparison.Ordinal) &&
        correo.IndexOf('@') > 0 &&
        correo.IndexOf('@') < correo.Length - 1;

    private static bool Coincide(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool EsDuplicadoDirectorio(MySqlException ex) =>
        ex.Number == 1062 &&
        (ex.Message.Contains("uk_directorio_area_nombre", StringComparison.OrdinalIgnoreCase) ||
         ex.Message.Contains("uk_directorio_area_extension", StringComparison.OrdinalIgnoreCase) ||
         ex.Message.Contains("uk_directorio_area_orden", StringComparison.OrdinalIgnoreCase));

    private static bool EsDuplicadoOrdenDirectorio(MySqlException ex) =>
        ex.Number == 1062 &&
        ex.Message.Contains("uk_directorio_area_orden", StringComparison.OrdinalIgnoreCase);

    private static string Clave(string area, string nombre, string extension) =>
        $"{area.Trim()}|{nombre.Trim()}|{extension.Trim()}";

    private static string Clave(string area, string valor) =>
        $"{area.Trim()}|{valor.Trim()}";

    private static string NormalizarEncabezado(string encabezado)
    {
        var valor = encabezado.Trim().Trim('\uFEFF').ToLowerInvariant();
        return valor
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u");
    }

    private static bool TryParseActivo(string valor, out bool activo)
    {
        switch (valor.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "si":
            case "sí":
            case "activo":
                activo = true;
                return true;
            case "0":
            case "false":
            case "no":
            case "inactivo":
                activo = false;
                return true;
            default:
                activo = false;
                return false;
        }
    }

    private static List<string> ParsearLineaCsv(string linea)
    {
        var resultado = new List<string>();
        var valor = new StringBuilder();
        var entreComillas = false;

        for (var i = 0; i < linea.Length; i++)
        {
            var c = linea[i];
            if (c == '"')
            {
                if (entreComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                {
                    valor.Append('"');
                    i++;
                }
                else
                {
                    entreComillas = !entreComillas;
                }

                continue;
            }

            if (c == ',' && !entreComillas)
            {
                resultado.Add(valor.ToString());
                valor.Clear();
                continue;
            }

            valor.Append(c);
        }

        resultado.Add(valor.ToString());
        return resultado;
    }
}

public class DirectorioCsvFila
{
    public int    Linea       { get; set; }
    public string Area        { get; set; } = string.Empty;
    public string Titular     { get; set; } = string.Empty;
    public string Ubicacion   { get; set; } = string.Empty;
    public string Correo      { get; set; } = string.Empty;
    public string Nombre      { get; set; } = string.Empty;
    public string Extension   { get; set; } = string.Empty;
    public string OrdenTexto  { get; set; } = string.Empty;
    public string ActivoTexto { get; set; } = string.Empty;
}

public sealed class ReordenarExtensionesRequest
{
    public int AreaId { get; set; }
    public List<int> Ids { get; set; } = [];
}

public sealed class DirectorioImportacionFila : DirectorioCsvFila
{
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public EstadoImportacion Estado { get; set; } = EstadoImportacion.Error;
    public string Observacion { get; set; } = string.Empty;
    public string ObservacionArea { get; set; } = string.Empty;
    public List<string> Errores { get; } = [];

    public bool PuedeAplicarse =>
        Errores.Count == 0 &&
        (Estado is EstadoImportacion.Nuevo
            or EstadoImportacion.ActualizarExtension
            or EstadoImportacion.ActualizarOrdenEstado
            or EstadoImportacion.ActualizarDatosArea);

    public string EstadoTexto =>
        Errores.Count > 0
            ? "Error"
            : Estado switch
            {
                EstadoImportacion.Nuevo => "Nuevo",
                EstadoImportacion.ActualizarExtension => "Actualizar extension",
                EstadoImportacion.ActualizarOrdenEstado => "Actualizar orden/estado",
                EstadoImportacion.ActualizarDatosArea => "Actualizar area",
                EstadoImportacion.SinCambios => "Sin cambios",
                EstadoImportacion.Conflicto => "Conflicto",
                _ => "Error"
            };

    public string Detalle =>
        Errores.Count > 0
            ? string.Join(" ", Errores)
            : string.Join(" ", new[] { Observacion, ObservacionArea }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

    public static DirectorioImportacionFila FromCsv(DirectorioCsvFila fila) => new()
    {
        Linea = fila.Linea,
        Area = fila.Area.Trim(),
        Titular = fila.Titular.Trim(),
        Ubicacion = fila.Ubicacion.Trim(),
        Correo = fila.Correo.Trim(),
        Nombre = fila.Nombre.Trim(),
        Extension = fila.Extension.Trim(),
        OrdenTexto = fila.OrdenTexto.Trim(),
        ActivoTexto = fila.ActivoTexto.Trim()
    };

    public DirectorioEntrada ToEntrada() => new()
    {
        Area = Area,
        Nombre = Nombre,
        Extension = Extension,
        Orden = Orden,
        Activo = Activo
    };
}

public sealed class ImportacionDirectorioPreview
{
    public ImportacionDirectorioPreview(IReadOnlyList<DirectorioImportacionFila> filas) =>
        Filas = filas;

    public IReadOnlyList<DirectorioImportacionFila> Filas { get; }
    public int Total => Filas.Count;
    public int Nuevos => Filas.Count(f => f.Estado == EstadoImportacion.Nuevo);
    public int Actualizaciones => Filas.Count(f =>
        f.Estado is EstadoImportacion.ActualizarExtension
            or EstadoImportacion.ActualizarOrdenEstado
            or EstadoImportacion.ActualizarDatosArea);
    public int SinCambios => Filas.Count(f => f.Estado == EstadoImportacion.SinCambios);
    public int Conflictos => Filas.Count(f => f.Estado == EstadoImportacion.Conflicto);
    public int Errores => Filas.Count(f => f.Errores.Count > 0);
    public int Aplicables => Filas.Count(f => f.PuedeAplicarse);
    public bool PuedeConfirmar => Aplicables > 0 && Errores == 0 && Conflictos == 0;
}

public enum EstadoImportacion
{
    Error,
    Nuevo,
    ActualizarExtension,
    ActualizarOrdenEstado,
    ActualizarDatosArea,
    SinCambios,
    Conflicto
}
